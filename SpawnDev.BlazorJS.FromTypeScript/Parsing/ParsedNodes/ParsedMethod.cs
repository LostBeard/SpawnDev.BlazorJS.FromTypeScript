using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedMethod : Parsed
    {
        public ParsedInterfaceOrClass Parent { get; set; }
        public string JSModuleNamespace => Parent?.JSModuleNamespace ?? "";
        public string JSNameSpaceName => IsConstructor ? string.Join(".", $"{JSModuleNamespace}.{Parent.Name}".Split('.', StringSplitOptions.RemoveEmptyEntries)) : string.Join(".", $"{JSModuleNamespace}.{Parent.Name}.{Name}".Split('.', StringSplitOptions.RemoveEmptyEntries));
        public bool IsConstructor { get; set; }
        public bool IsStatic { get; set; }
        public string Name { get; set; } = "";
        public ParsedType ReturnType { get; set; }
        public List<ParsedMethodParameter> Parameters { get; set; } = new List<ParsedMethodParameter>();
        public ParsedMethod()
        {

        }
        public override string ToString()
        {
            return ToString(0);
        }
        public string ToString(string indentation)
        {
            return ToString(indentation?.Length ?? 0);
        }
        string GetMethodParamsAsCSharp(ParsedMethod parsedMethod)
        {
            var sb = new List<string>();
            foreach (var p in parsedMethod.Parameters)
            {
                sb.Add($"{(p.IsParamsArray ? "params " : "")}{p.Type.Name} {p.Name}");
            }
            return string.Join(", ", sb);
        }
        string GetMethodParamNames(ParsedMethod parsedMethod, string prepend = "")
        {
            var paramNames = parsedMethod.Parameters.Select(p => p.Name).ToList();
            var ret = string.Join(", ", paramNames);
            return !string.IsNullOrEmpty(ret) ? $"{prepend}{ret}" : ret;

        }
        public string ToString(int indentation)
        {
            var ret = "";
            if (IsConstructor)
            {
                // constructor
                ret = $@"
/// <summary>
/// {SourceText?.Trim().Replace("\n", "\n/// ")}
/// </summary>
public {CSMethodName}({GetMethodParamsAsCSharp(this)}) => base(JS.New(""{JSNameSpaceName}""{GetMethodParamNames(this, ", ")}));
";
            }
            #region Static
            else if (IsStatic && ReturnsTaskVoid)
            {
                // static async method no return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public static Task {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JS.CallVoidAsync(""{JSNameSpaceName}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else if (IsStatic && ReturnsTask)
            {
                // static async method with return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public static Task<{ReturnType!.GetTypeArgumentsDelimited(Parent.Name.TitleCaseInvariant())}> {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JS.CallAsync<{ReturnType!.GetTypeArgumentsDelimited(Parent.Name.TitleCaseInvariant())}>(""{JSNameSpaceName}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else if (IsStatic && ReturnsVoid)
            {
                // static non-async method no return value
                ret = $@"
/// <summary>
/// {SourceText?.Trim().Replace("\n", "\n/// ")}
/// </summary>
public static void {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    JS.CallVoid(""{JSNameSpaceName}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else if (IsStatic)
            {
                // static non-async method with return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public static {CSReturnType} {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JS.Call<{CSReturnType}>(""{JSNameSpaceName}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            #endregion
            #region Instance
            else if (ReturnsTaskVoid)
            {
                // static async method no return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public Task {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JSRef!.CallVoidAsync(""{Name}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else if (ReturnsTask)
            {
                // instance async method with return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public Task<{ReturnType!.GetTypeArgumentsDelimited(Parent.Name.TitleCaseInvariant())}> {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JSRef!.CallAsync<{ReturnType!.GetTypeArgumentsDelimited(Parent.Name.TitleCaseInvariant())}>(""{Name}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else if (ReturnsVoid)
            {
                // instance non-async method no return value
                ret = $@"
/// <summary>
/// {SourceText?.Trim().Replace("\n", "\n/// ")}
/// </summary>
public void {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    JSRef!.CallVoid(""{Name}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            else
            {
                // instance non-async method with return value
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n/// ")}
/// </summary>
public {CSReturnType} {CSMethodName}({GetMethodParamsAsCSharp(this)})
{{
    return JSRef!.Call<{CSReturnType}>(""{Name}""{GetMethodParamNames(this, ", ")});
}}
".Trim();
            }
            #endregion
            if (indentation > 0)
            {
                var padding = new string(' ', indentation);
                ret = Regex.Replace(ret, "^", padding, RegexOptions.Multiline);
            }
            return ret;
        }
        //string JSModuleNamespaced(string i)
        //{
        //    return string.IsNullOrEmpty(JSModuleNamespace) ? i : $"{JSModuleNamespace}.{i}";
        //}
        bool ReturnsTask => IsAsync;
        bool IsAsync => ReturnType?.IsPromise ?? false;
        string StaticKeyword => IsStatic ? "static " : "";
        string JSorJSRefDot => IsStatic ? "JS." : "JSRef!.";
        bool ReturnsVoid => CSReturnType == "void";
        bool ReturnsTaskVoid => CSReturnType == "Task";
        string CSMethodName
        {
            get
            {
                if (IsConstructor) return Parent.Name;
                return Name.TitleCaseInvariant();
            }
        }
        bool HasReturnType => !IsConstructor && !string.IsNullOrEmpty(ReturnType?.Name) && ReturnType.Name != "void";
        string CSReturnType
        {
            get
            {
                if (IsConstructor) return "";
                var ret = ReturnType?.Name.Trim() ?? "";
                if (ret.StartsWith("Promise<"))
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                else if (ret == "Promise")
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                if (ret == "this") ret = Parent.Name.TitleCaseInvariant();
                return ret;
            }
        }
    }
}
