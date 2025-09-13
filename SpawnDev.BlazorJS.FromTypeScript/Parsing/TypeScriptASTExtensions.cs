using SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes;
using Sdcb.TypeScript.TsParser;
using Sdcb.TypeScript.TsTypes;
using System.Text;
using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing
{

    // https://ts-ast-viewer.com/
    public static class TypeScriptASTExtensions
    {
        public static string GetView(this Node _this, string pad = "")
        {
            if (_this.Children.Any())
            {
                var padNext = string.IsNullOrEmpty(pad) ? " " : $"{pad}{pad}";
                return $@"{pad}{_this.Kind} {_this.Children.Count} : ""{_this.IdentifierStr}""
{string.Join($"", _this.Children.Select(o => o.GetView(padNext)))}
".TrimEnd() + "\n";
            }
            else
            {
                return $@"{pad}{_this.Kind} : ""{_this.IdentifierStr}"" Text: ""{_this.GetTextSafe()}""
".TrimEnd() + "\n";
            }
        }
        public static string GetTextSafe(this Node _this)
        {
            try
            {
                return _this.GetText();
            }
            catch { }
            return "";
        }
        public static string TitleCaseInvariant(this string t)
        {
            if (t == null) return null;
            return t.Length <= 1 ? t.ToUpperInvariant() : string.Join(" ", t.Split(' ').Select(o => $"{o.Substring(0, 1).ToUpper()}{o.Substring(1)}"));
        }
        public static IEnumerable<T> OfKind<T>(this Node _this)
        {
            foreach (Node child in _this.Children)
            {
                foreach (Node descendant in child.GetDescendants())
                {
                    if (descendant is T asT)
                    {
                        yield return asT;
                    }
                }
            }
        }
        public static bool HasKind(this Node _this, SyntaxKind modifier) => _this.Children.OfKind(modifier).Any();
        public static List<Node> GetModifiers(this Node _this) => _this.Children.Where(o => Utilities.IsModifierKind(o.Kind)).ToList();
        public static List<Node> GetNonModifiers(this Node _this) => _this.Children.Where(o => !Utilities.IsModifierKind(o.Kind)).ToList();
        public static ParsedInterfaceOrClass ParseInterfaceOrClass(this Node _this, ParsedModule parent, string JSModuleNamespace)
        {
            var ret = new ParsedInterfaceOrClass();
            ret.JSModuleNamespace = JSModuleNamespace;
            ret.Parent = parent;
            ret.SourceText = _this.GetTextSafe();
            if (_this is InterfaceDeclaration interfaceDeclaration)
            {
                ret.IsClass = false;
            }
            else if (_this is ClassDeclaration classDeclaration)
            {
                ret.IsClass = true;
            }
            else
            {
                throw new NotImplementedException();
            }
            var typeParameters = _this.Children.Where(o => o.Kind == SyntaxKind.TypeParameter).Select(o => o.IdentifierStr).ToList();
            var inherits = _this.OfKind(SyntaxKind.HeritageClause).Select(o => o as HeritageClause).ToList();
            ret.IsAbstract = _this.OfKind(SyntaxKind.AbstractKeyword).Any();
            var inheritsNames = new List<string>();
            if (inherits.Count > 0)
            {
                var child = inherits.First();
                if (child.Children.Count == 1)
                {
                    var child0 = child.Children.First();
                    if (child0.Kind == SyntaxKind.ExpressionWithTypeArguments)
                    {
                        inheritsNames.Add(child0.IdentifierStr);
                    }
                    else
                    {
                        var nmtt = true;
                    }
                }
                else if (child.Children.Count == 2)
                {
                    var childa = child.Children.First();
                    var isPartial = childa.GetTextSafe() == "Partial";
                    var child0 = child.Children.Last();
                    if (child0.Kind == SyntaxKind.ExpressionWithTypeArguments)
                    {
                        inheritsNames.Add(child0.IdentifierStr);
                    }
                    else
                    {
                        var nmtt = true;
                    }
                }
                else
                {
                    var childa = child.Children.First();
                    var isPartial = childa.GetTextSafe() == "Partial";
                    var child0 = child.Children.Last();
                    if (child0.Kind == SyntaxKind.ExpressionWithTypeArguments)
                    {
                        inheritsNames.Add(child0.IdentifierStr);
                    }
                    else
                    {
                        var nmtt = true;
                    }
                }
                var nmt = true;
            }
            ret.Name = _this.IdentifierStr;
            ret.Extends = inheritsNames;
            ret.TypeParameters = typeParameters;
            foreach (var child in _this.Children)
            {
                var str = child.GetTextSafe();
                var modifiers = child.Children.Where(o => Utilities.IsModifierKind(o.Kind)).ToList();
                var others = child.Children.Except(modifiers).ToList();
                if (child is MethodDeclaration || child is ConstructorDeclaration)
                {
                    var m = ParseMethod(ret, child);
                    if (m == null)
                    {
                        continue;
                    }
                    ret.Methods.Add(m);
                }
                else
                if (child is PropertyDeclaration || child is PropertySignature)
                {
                    var m = new ParsedProperty();
                    m.Parent = ret;
                    m.SourceText = str;
                    if (string.IsNullOrEmpty(child.IdentifierStr))
                    {
                        continue;
                    }
                    m.Name = child.IdentifierStr;
                    if (m.Name.StartsWith("readonly") && m.Name.Length > "readonly".Length)
                    {
                        m.SourceText = m.SourceText.Replace("readonly", "readonly ");
                        m.Name = m.Name.substring("readonly".Length);
                        m.ReadOnly = true;
                    }
                    else
                    {
                        m.ReadOnly = child.Children.OfKind(SyntaxKind.ReadonlyKeyword).Any();
                    }
                    m.IsStatic = child.Children.OfKind(SyntaxKind.StaticKeyword).Any();
                    m.IsOverride = child.Children.OfKind(SyntaxKind.OverrideKeyword).Any();
                    if (m.IsOverride)
                    {
                        var overrideee = true;
                    }
                    m.IsNullable = child.Children.OfKind(SyntaxKind.QuestionToken).Any();
                    var typeNode = child.Children.Last();
                    var typeStr = GetTypeString(typeNode);
                    m.Type = new ParsedType
                    {
                        Name = typeStr,
                        IsNullable = false
                    };
                    if (others.Count() != 2)
                    {
                        var gg = true;
                    }
                    ret.Properties.Add(m);
                }
                else if (child is SetAccessorDeclaration setAccessorDeclaration)
                {
                    var m = ret.Properties.FirstOrDefault(o => o.Name == setAccessorDeclaration.IdentifierStr);
                    if (m == null)
                    {
                        m = new ParsedProperty();
                        m.Parent = ret;
                        m.SourceText = str;
                        m.Name = setAccessorDeclaration.IdentifierStr;
                        var tNode = child.Children.Last();
                        var returnType = GetTypeString(tNode);
                        m.Type = new ParsedType
                        {
                            SourceText = tNode.GetTextSafe(),
                            Name = returnType,
                        };
                        ret.Properties.Add(m);
                    }
                    m.HasSet = true;
                }
                else if (child is GetAccessorDeclaration getAccessorDeclaration)
                {
                    var m = ret.Properties.FirstOrDefault(o => o.Name == getAccessorDeclaration.IdentifierStr);
                    if (m == null)
                    {
                        var tNode = child.Children.Last();
                        m = new ParsedProperty();
                        m.Parent = ret;
                        m.SourceText = str;
                        m.Name = getAccessorDeclaration.IdentifierStr;
                        var returnType = GetTypeString(tNode);
                        m.Type = new ParsedType
                        {
                            SourceText = tNode.GetTextSafe(),
                            Name = returnType,
                        };
                        ret.Properties.Add(m);
                    }
                    m.HasGet = true;
                }
                else if (child.Kind == SyntaxKind.ExportKeyword)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.Identifier)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.HeritageClause)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.TypeParameter)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.AbstractKeyword)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.DefaultKeyword)
                {
                    var nmt = true;
                }
                else if (child.Kind == SyntaxKind.DeclareKeyword)
                {
                    var nmt = true;
                }
                else
                {
                    var nmt = true;
                }
            }
            return ret;
        }
        static ParsedMethod? ParseMethod(ParsedInterfaceOrClass parent, Node child)
        {
            if (child.Kind != SyntaxKind.Constructor
                && child.Kind != SyntaxKind.MethodDeclaration
                && child.Kind != SyntaxKind.MethodSignature
                && child.Kind != SyntaxKind.FunctionType
                && child.Kind != SyntaxKind.FunctionDeclaration
                && child.Kind != SyntaxKind.FunctionExpression)
            {
                return null;
            }
            var isConstructor = child.Kind == SyntaxKind.Constructor;
            var m = new ParsedMethod();
            m.Parent = parent;
            m.IsConstructor = isConstructor;
            m.SourceText = child.GetTextSafe();
            m.Name = child.IdentifierStr ?? "";
            if (!isConstructor)
            {
                var tNode = child.Children.Last();
                var returnType = GetTypeString(tNode);// child.Children.Last().GetTextSafe();
                if (returnType.Contains("Promise"))
                {
                    var nmt = true;
                }
                m.ReturnType = new ParsedType
                {
                    SourceText = tNode.GetTextSafe(),
                    Name = returnType,
                };
            }
            var typeNodes = child.OfKind<ParameterDeclaration>();
            foreach (var typeNode in typeNodes)
            {
                var paramStr = typeNode.GetTextSafe();
                var pOthers = typeNode.GetNonModifiers();
                var pModifiers = typeNode.GetModifiers();
                var leftCnt = pOthers.Count();
                if (leftCnt == 2)
                {
                    var tNode = pOthers.Last();
                    m.Parameters.Add(new ParsedMethodParameter
                    {
                        Name = typeNode.IdentifierStr,
                        Optional = typeNode.HasKind(SyntaxKind.QuestionToken),
                        IsParamsArray = typeNode.HasKind(SyntaxKind.DotDotDotToken),
                        Type = new ParsedType
                        {
                            SourceText = tNode.GetTextSafe(),
                            Name = GetTypeString(tNode)
                        }
                    });
                }
                else
                {
                    var nmt5 = true;
                }
            }
            return m;
        }
        public static Dictionary<string, string[]> JSToCSharpTypeConversions = new Dictionary<string, string[]> {
            {"bool", ["boolean", "true", "false"] },
            {"float", ["number"] },
            {"object", ["null", "object"] },
        };
        public static string GetTypeString(this Node node)
        {
            var ret = "";
            switch (node.Kind)
            {
                case SyntaxKind.UnionType:
                    var typeStrings = node.Children.Select(GetTypeString).Distinct().ToList();
                    typeStrings = typeStrings.Where(o => !new[] { "undefined", "null", "void" }.Contains(o)).ToList();
                    if (typeStrings.Count == 0)
                    {
                        ret = "object";
                    }
                    else if (typeStrings.Count == 1)
                    {
                        ret = typeStrings.First();
                    }
                    else
                    {
                        ret = $"Union<{string.Join(", ", typeStrings.Select(o => o.Trim('"')))}>";
                    }
                    break;
                case SyntaxKind.LastTypeNode:
                    ret = GetTypeString(node.Children[0]);
                    break;
                case SyntaxKind.NumberKeyword:
                case SyntaxKind.Identifier:
                case SyntaxKind.StringKeyword:
                    ret = node.GetTextSafe();
                    break;
                case SyntaxKind.StringLiteral:
                    ret = "string";
                    break;
                case SyntaxKind.NumericLiteral:
                    ret = "number";
                    break;
                case SyntaxKind.TypeLiteral:
                    ret = "object";
                    break;
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.BooleanKeyword:
                    ret = "bool";
                    break;
                case SyntaxKind.TypeReference:
                    var typeReferenceChildren = node.Children.Where(o => o.Kind != SyntaxKind.Identifier).ToList();
                    var subTypes = typeReferenceChildren.Select(o => GetTypeString(o)).Distinct().ToList();
                    subTypes = subTypes.Where(o => !new[] { "undefined", "null", "void" }.Contains(o)).ToList();
                    var pType = node.IdentifierStr;
                    if (subTypes.Any())
                    {
                        ret = $"{pType}<{string.Join(", ", subTypes)}>";
                    }
                    else
                    {
                        ret = pType;
                    }
                    break;
                case SyntaxKind.FunctionType:
                    ret = GetTypeString(node.Children[0]);
                    break;
                case SyntaxKind.Parameter:
                    ret = GetTypeString(node.GetNonModifiers().Last());
                    break;
                default:
                    ret = TryGetText(node);
                    break;
            }
            {
                var ms = Regex.Match(ret, @"^(.+)\[\]");
                if (ms.Success)
                {
                    var typeName = ms.Groups[1].Value;
                    var conversion = JSToCSharpTypeConversions.FirstOrDefault(o => o.Value.Contains(typeName)).Key;
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        ret = Regex.Replace(ret, $@"\b{typeName}\b", conversion);
                    }
                }
            }
            {
                var conversion = JSToCSharpTypeConversions.FirstOrDefault(o => o.Value.Contains(ret)).Key;
                if (!string.IsNullOrEmpty(conversion))
                {
                    ret = conversion;
                }
            }
            return ret;
        }
        static string TryGetText(Node node)
        {
            try
            {
                return node.GetTextSafe();
            }
            catch { }
            return "";
        }
        //public static ParsedInterfaceOrClass ToCSharpInterface(this InterfaceDeclaration _this)
        //{
        //    var ret = new ParsedInterfaceOrClass();
        //    ret.IsClass = false;
        //    var typeParameters = _this.Children.Where(o => o.Kind == SyntaxKind.TypeParameter).Select(o => o.IdentifierStr).ToList();
        //    var inherits = _this.OfKind(SyntaxKind.HeritageClause).Select(o => o as HeritageClause).ToList();
        //    var inheritsNames = new List<string>();
        //    if (inherits.Count > 0)
        //    {
        //        var child = inherits.First();
        //        if (child.Children.Count == 1)
        //        {
        //            var child0 = child.Children.First();
        //            if (child0.Kind == SyntaxKind.ExpressionWithTypeArguments)
        //            {
        //                inheritsNames.Add(child0.IdentifierStr);
        //            }
        //            else
        //            {
        //                var nmtt = true;
        //            }
        //        }
        //        else
        //        {
        //            var nmtt = true;
        //        }
        //        var nmt = true;
        //    }
        //    ret.Name = _this.IdentifierStr;
        //    ret.Extends = inheritsNames;
        //    ret.TypeParameters = typeParameters;
        //    foreach (var child in _this.Children)
        //    {
        //        if (child is MethodSignature methodSignature)
        //        {
        //            var m = new ParsedMethod();

        //            var nmt = true;
        //        }
        //        else
        //        {
        //            var nmt = true;
        //        }
        //    }
        //    return ret;
        //}
        public static string ToCSharpTypeName(this Node _this)
        {
            var ret = "";
            if (_this.Kind == SyntaxKind.VoidKeyword) ret = "void";
            else if (_this.Kind == SyntaxKind.StringKeyword) ret = "string";
            else if (_this.Kind == SyntaxKind.SymbolKeyword) ret = "object";
            else if (_this.Kind == SyntaxKind.Unknown) ret = "object";
            else if (_this.Kind == SyntaxKind.ObjectKeyword) ret = "object";
            else if (_this.Kind == SyntaxKind.AnyKeyword) ret = "object";
            else if (_this.Kind == SyntaxKind.BooleanKeyword) ret = "bool";
            else if (_this.Kind == SyntaxKind.UnionType) ret = "object";
            else if (_this.Kind == SyntaxKind.NumberKeyword) ret = "double";
            else if (_this.Kind == SyntaxKind.TypeReference)
            {
                ret = _this.IdentifierStr;
                //if (typeReferences != null && typeReferences.Count > 0)
                //{
                //    ret = _this.IdentifierStr; // typeReferences.First();
                //    //typeReferences.RemoveAt(0);
                //}
                //else
                //{
                //    ret = "object";
                //}
            }
            else if (_this is FunctionTypeNode funcNode)
            {
                ret = funcNode.ToCSharpInterface();
                var ret1 = "object";
            }
            else if (_this.Kind == SyntaxKind.ArrayType)
            {
                if (_this.Children.Count != 1)
                {
                    throw new Exception();
                }
                ret = _this.Children[0].ToCSharpTypeName() + "[]";
            }
            else
            {
                ret = "object";
            }
            // typeparameters
            //var typeParameters = _this.Children.Where(o => o.Kind != SyntaxKind.TypeParameter).Select(o => o.IdentifierStr).ToList();
            //if (typeParameters.Any())
            //{
            //    ret += $"<{string.Join(", ", typeParameters)}>";
            //}
            var typeParameters1 = _this.Children.Where(o => o.Kind != SyntaxKind.Identifier).Select(o => o.ToCSharpTypeName()).ToList();
            if (_this.Kind == SyntaxKind.TypeReference && typeParameters1.Any())
            {
                ret += $"<{string.Join(", ", typeParameters1)}>";
            }
            switch (ret)
            {
                case "unknown":
                    ret = "object";
                    break;
                case "ArrayLike":
                    var aaret = "object";
                    break;
            }
            return ret;
        }
        static string CSharpVarName(string identifierNode)
        {
            switch (identifierNode)
            {
                case "this": return "Root";
                case "string": return "str";
                default: return identifierNode;
            }
        }

        public static string ToCSharpParamter(this ParameterDeclaration _this, bool isLast)
        {
            var identifierNode = CSharpVarName(_this.IdentifierStr);
            if (string.IsNullOrEmpty(identifierNode))
            {
                var nmt = true;
            }
            var typeNode = _this.Children[1];
            if (_this.Children.Count != 2 && _this.Children.Count != 3)
            {
                var nmt = true;
            }
            var nullableToken = _this.OfKind(SyntaxKind.QuestionToken).Any() ? "?" : "";
            var dotdotdotToken = "";
            var dotdotdotArrayStr = "";
            if (isLast && _this.OfKind(SyntaxKind.DotDotDotToken).Any())
            {
                dotdotdotToken = "params ";
                dotdotdotArrayStr = "[]";
            }
            return $"{dotdotdotToken}{typeNode.ToCSharpTypeName()}{dotdotdotArrayStr}{nullableToken} {identifierNode}";
        }
        public static string ToCSharpInterface(this FunctionTypeNode _this)
        {
            var sb = new StringBuilder();
            //sb.Append("object");
            var returnTypeNode = _this.Children.Last();
            if (returnTypeNode.Kind == SyntaxKind.Parameter)
            {
                throw new Exception();
            }
            var typeParameters = _this.Children.Where(o => o.Kind == SyntaxKind.TypeParameter).Select(o => o.IdentifierStr).ToList();
            var paramNodes = _this.Children.Where(o => o is ParameterDeclaration pd && pd.IdentifierStr != "this").Select(o => o as ParameterDeclaration).ToList();
            var returnTypeName = returnTypeNode.ToCSharpTypeName();
            if (returnTypeNode.Kind == SyntaxKind.TypeReference && typeParameters.Count > 0)
            {
                returnTypeName = typeParameters.First();
            }
            //var csharpMethodName = _this.IdentifierStr.Substring(0, 1).ToUpperInvariant() + _this.IdentifierStr.Substring(1);
            if (returnTypeName == "void")
            {
                sb.Append($"Action");
                if (typeParameters.Any())
                {
                    sb.Append("<");
                    foreach (var paramNode in paramNodes)
                    {
                        var isLast = paramNode == paramNodes.Last();
                        sb.Append(paramNode.ToCSharpTypeName());
                        if (!isLast) sb.Append(", ");
                    }
                    sb.Append(">");
                }
            }
            else
            {
                sb.Append($"Func");
                sb.Append("<");
                if (typeParameters.Any())
                {
                    foreach (var paramNode in paramNodes)
                    {
                        sb.Append(paramNode.ToCSharpTypeName());
                        sb.Append(", ");
                    }
                }
                sb.Append($"{returnTypeName}>");
            }
            return sb.ToString();
        }
        public static string ToCSharpInterface(this MethodSignature _this)
        {
            var sb = new StringBuilder();
            var returnTypeNode = _this.Children.Last();
            if (returnTypeNode.Kind == SyntaxKind.Parameter)
            {
                throw new Exception();
            }
            var typeParameters = _this.Children.Where(o => o.Kind == SyntaxKind.TypeParameter).Select(o => o.IdentifierStr).ToList();
            var paramNodes = _this.Children.Where(o => o is ParameterDeclaration pd && pd.IdentifierStr != "this").Select(o => o as ParameterDeclaration).ToList();
            var returnTypeName = returnTypeNode.ToCSharpTypeName();
            if (returnTypeNode.Kind == SyntaxKind.TypeReference && typeParameters.Count > 0)
            {
                returnTypeName = typeParameters.First();
            }
            if (string.IsNullOrEmpty(_this.IdentifierStr))
            {
                return "";
            }
            var csharpMethodName = _this.IdentifierStr.Substring(0, 1).ToUpperInvariant() + _this.IdentifierStr.Substring(1);
            sb.Append($"    {returnTypeName} {csharpMethodName}");
            if (typeParameters.Any())
            {
                if (typeParameters.Count > 1)
                {
                    var nmt1 = true;
                }
                sb.Append($"<{string.Join(", ", typeParameters)}>");
            }
            sb.Append($"(");
            foreach (var paramNode in paramNodes)
            {
                var isLast = paramNode == paramNodes.Last();
                var pstr = paramNode.ToCSharpParamter(isLast);
                sb.Append(pstr);
                if (!isLast) sb.Append(", ");
            }
            sb.AppendLine(");");
            return sb.ToString();
        }
    }
}
