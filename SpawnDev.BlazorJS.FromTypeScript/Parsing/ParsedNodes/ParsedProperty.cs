using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedProperty : Parsed
    {
        public ParsedInterfaceOrClass Parent { get; set; }
        public string JSModuleNamespace => Parent?.JSModuleNamespace ?? "";
        public string JSNameSpaceName => string.Join(".", $"{JSModuleNamespace}.{Parent.Name}.{Name}".Split('.', StringSplitOptions.RemoveEmptyEntries));
        public bool IsNullable { get; set; }
        public bool IsStatic { get; set; }
        public bool IsOverride { get; set; }
        public string Name { get; set; } = "";
        public string CSPropertyName => Name.TitleCaseInvariant();
        public bool HasGet { get; set; }
        public bool HasSet { get; set; }
        public bool ReadOnly { get; set; }
        public bool ShouldHaveSetter => HasSet || (!HasGet && !ReadOnly);
        public ParsedType Type { get; set; }
        public ParsedProperty() { }
        public override string ToString()
        {
            return ToString(0);
        }
        public string ToString(string indentation)
        {
            return ToString(indentation?.Length ?? 0);
        }
        bool ReturnsVoid => CSReturnType == "void";
        bool ReturnsTaskVoid => CSReturnType == "Task";
        bool ReturnsTask => IsAsync;
        bool IsAsync => Type?.IsPromise ?? false;
        public string ToString(int indentation)
        {
            var ret = "";
            //
            #region Static
            if (IsStatic && ReturnsTaskVoid)
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public static {CSReturnType} {Name.TitleCaseInvariant()} {{ get => JS.GetAsyncVoid(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JS.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            else if (IsStatic && ReturnsTask)
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public static {CSReturnType} {Name.TitleCaseInvariant()} {{ get => JS.GetAsync<{Type!.GetTypeArgumentsDelimited(Parent.CSClassName)}>(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JS.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            else if (IsStatic)
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public static {CSReturnType} {Name.TitleCaseInvariant()} {{ get => JS.Get<{CSReturnType}>(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JS.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            #endregion
            #region Instance
            else if (ReturnsTaskVoid)
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public {CSReturnType} {CSPropertyName} {{ get => JSRef!.GetAsyncVoid(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JSRef!.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            else if (ReturnsTask)
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public {CSReturnType} {CSPropertyName} {{ get => JSRef!.GetAsync<{Type!.GetTypeArgumentsDelimited(Parent.CSClassName)}>(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JSRef!.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            else
            {
                ret = $@"
/// <summary>
/// {SourceText?.Replace("\n", "\n        /// ")}
/// </summary>
public {CSReturnType} {CSPropertyName} {{ get => JSRef!.Get<{CSReturnType}>(""{JSNameSpaceName}"");{(!ShouldHaveSetter ? "" : $@" set => JSRef!.Set(""{JSNameSpaceName}"", value);")} }}
".Trim();
            }
            #endregion
            //
            if (indentation > 0)
            {
                var padding = new string(' ', indentation);
                ret = Regex.Replace(ret, "^", padding, RegexOptions.Multiline);
            }
            return ret;
        }
        public string CSReturnType
        {
            get
            {
                var ret = Type?.Name.Trim() ?? "";
                if (ret.StartsWith("Promise<"))
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                else if (ret == "Promise")
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                if (ret == "this") ret = Parent.CSClassName;
                return ret;
            }
        }
    }
}
