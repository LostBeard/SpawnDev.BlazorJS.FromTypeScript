using Sdcb.TypeScript;
using Sdcb.TypeScript.TsTypes;
using SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes;
using SpawnDev.BlazorJS.FromTypeScript.Services;
using System.Text.RegularExpressions;
using Type = System.Type;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing
{
    public class BlazorJSProject
    {
        List<Type> UnhandledTypes = new List<System.Type>();
        public string SourcePath { get; private set; }
        //public string OutPath { get; private set; }
        public Dictionary<string, ParsedModule> Modules { get; } = new Dictionary<string, ParsedModule>();
        public List<ParsedConstant> Constants => Modules.SelectMany(o => o.Value.Constants).ToList();
        public List<ParsedInterfaceOrClass> Interfaces => Modules.SelectMany(o => o.Value.Interfaces).ToList();
        public List<ParsedEnum> Enums => Modules.SelectMany(o => o.Value.Enums).ToList();
        public List<ParsedTypeAlias> TypeAliases => Modules.SelectMany(o => o.Value.TypeAliases).ToList();
        public string ProjectName { get; private set; }
        public string JSModuleNamespace { get; private set; } = "";
        public bool NameSpaceFromPath { get; private set; }
        IAsyncFileSystem FS;
        public event Action OnProgress = default!;
        public long TypeScriptDeclarationFileCount { get; private set; }
        public BlazorJSProject(IAsyncFileSystem fs, string srcPath, string projectName, string jsModuleNamespace, bool nameSpaceFromPath = false)
        {
            FS = fs;
            ProjectName = projectName;
            SourcePath = srcPath;
            //OutPath = outPath;
            NameSpaceFromPath = nameSpaceFromPath;
            JSModuleNamespace = jsModuleNamespace;
        }
        string JSModuleNamespaced(string i)
        {
            return string.IsNullOrEmpty(JSModuleNamespace) ? i : $"{JSModuleNamespace}.{i}";
        }
        public async Task ProcessDir()
        {
            Modules.Clear();
            var files = await FS.GetInfos(SourcePath, true);
            var tsdFiles = files.Where(o => o.Name.EndsWith(".d.ts")).ToList();
            TypeScriptDeclarationFileCount = tsdFiles.Count;
            foreach (var file in tsdFiles)
            {
                await ParseTypeScriptDefinationsFile(file.FullPath);
                OnProgress?.Invoke();
            }
            //var files = FS.EnumerateInfos(SourcePath, true);
            //await foreach (var file in files)
            //{
            //    if (file.Name.EndsWith(".d.ts"))
            //    {
            //        await ParseTypeScriptDefinationsFile(file.FullPath);
            //        OnProgress?.Invoke();
            //    }
            //}
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
        public bool IgnoreUnderscoreMembers = true;
        public async Task WriteProject(string OutPath, Action<int, int> progressCallback)
        {
            
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            if (!await FS.DirectoryExists(OutPath)) Directory.CreateDirectory(OutPath);
            var total = Modules.Count;
            var done = 0;
            foreach (var m in Modules.Values)
            {
                foreach (var c in m.Interfaces)
                {
                    var fileNameP = IOPath.Combine(OutPath, m.SubPath, $"{c.Name}.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = $@"
using System.Text;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    public class {c.CSClassName}{(c.Extends.Any() ? $" : {c.Extends.First()}" : " : JSObject")}
    {{
        #region Constructors
        /// <inheritdoc/>
        public {c.CSClassName}(IJSInProcessObjectReference _ref) : base(_ref) {{ }}
{string.Join("\r\n", c.Methods.Where(o => o.IsConstructor).OrderByDescending(o => o.IsConstructor).ThenBy(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion

        #region Properties
{string.Join("\r\n", c.Properties.Where(o => !IgnoreUnderscoreMembers || !o.Name.StartsWith("_")).OrderBy(o => o.Name).Select(m =>  m.ToString("        ")))}
        #endregion

        #region Methods
{string.Join("\r\n", c.Methods.Where(o => !o.IsConstructor && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderByDescending(o => o.IsConstructor).ThenBy(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion
    }}
}}
";
                    await FS.Write(fileName, code);
                }
                foreach (var c in m.Enums)
                {
                    var fileNameP = IOPath.Combine(OutPath, m.SubPath, $"{c.Name}.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    //var fileName = IOPath.GetFullPath(IOPath.Combine(m.DestDir, $"{c.Name}.cs"));
                    var code = $@"
using System.Text;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    /// <summary>
    /// {c.SourceText?.Replace("\n", "\n    /// ")}
    /// </summary>
    public enum {c.CSEnumName}
    {{
        {string.Join("\n        ", c.Values.Select(o =>  $"{o.Name} = {o.Value},"))}
    }}
}}
";
                    await FS.Write(fileName, code);
                }
                if (m.TypeAliases.Any())
                {
                    // create a file for all type aliases

                }
                if (m.Constants.Any())
                {
                    // create a file for all type constants

                }
                done++;
                progressCallback?.Invoke(total, done);
            }
        }
        bool UseTitleCaseNaming = true;
        // https://ts-ast-viewer.com/
        async Task ParseTypeScriptDefinationsFile(string file)
        {
            file = IOPath.GetFullPath(file);
            var ext = IOPath.GetExtension(file);
            var fname = ext == ".js" ? IOPath.GetFileName(file).Replace(".js", ".cs") : IOPath.GetFileName(file).Replace(".d.ts", ".cs");
            if (!await FS.FileExists(file))
            {
                if (ext == ".js")
                {
                    file = file.Substring(0, file.LastIndexOf(".")) + ".d.ts";
                    if (!await FS.FileExists(file))
                    {
                        var nmtt = false;
                    }
                }
            }
            var subFilePath = Path.GetRelativePath(SourcePath, file);
            if (Modules.ContainsKey(subFilePath)) return;
            var fileDir = IOPath.GetDirectoryName(file);
            var subPath = Path.GetRelativePath(SourcePath, fileDir);
            var delimiter = subPath.Contains("/") ? "/" : @"\";
            if (UseTitleCaseNaming)
            {
                var subPathap = subPath.Split(new[] { '/', '\\' }).Select(o => o.TitleCaseInvariant());
                subPath = string.Join(delimiter, subPathap);
            }
            string? txt = null;
            try
            {
                Console.WriteLine($"Reading: {file}");
                txt = await FS.ReadText(file);
            }
            catch (Exception ex)
            {
                var nmtt = true;
                return;
            }
            if (txt == null)
            {
                var nmtttt = true;
            }
            var typescriptString = PreProcess(txt);
            var module = new ParsedModule
            {
                //Name = subFilePath,
                SubPath = subPath,
                //DestDir = destDir,
                //DestFile = outFile,
                SourceFile = file,
                //ProjectPath = subFilePath,
                ProjectNamespace = ProjectName,
                ModuleNamespaceSub = !NameSpaceFromPath ? "" : subPath.Replace("/", ".").Replace("\\", "."),
            };
            Modules.Add(subFilePath, module);
            await ParseTypeScriptDefinitions(module, typescriptString);
            OnProgress?.Invoke();
            //await FS.Write(outFile, ret);
        }
        Regex replaceReadonly = new Regex(@"\breadonly ");
        string PreProcess(string text)
        {
            // source: readonly Plane[]
            text = replaceReadonly.Replace(text, "readonly");
            return text;
        }
        List<string> Processing = new List<string>();
        async Task ParseTypeScriptDefinitions(ParsedModule sourceFile, string text)
        {
            var x = new TypeScriptAST(text, sourceFile.SourceFile);
            await ParseNodeChildren(sourceFile, x.RootNode.Children);
        }
        async Task ParseNode(ParsedModule sourceFile, Node child)
        {
            var fileDir = IOPath.GetDirectoryName(sourceFile.SourceFile);
            var str = child.GetText();
            if (child is ModuleDeclaration moduleDeclaration)
            {
                var nmt = true;
                var moduleName = moduleDeclaration.IdentifierStr;
                var moduleBlock = moduleDeclaration.Children.OfType<ModuleBlock>().FirstOrDefault();
                if (moduleBlock != null)
                {
                    await ParseNodeChildren(sourceFile, moduleBlock.Children);
                }
            }
            else if (child is VariableStatement variableStatement)
            {
                var nmt = true;
                foreach (var variableDeclartion in variableStatement.DeclarationList.Declarations)
                {
                    var name = variableDeclartion.IdentifierStr;
                    if (string.IsNullOrEmpty(name))
                    {
                        var nmt22 = true;
                        continue;
                    }
                    var typeNode = variableDeclartion.GetNonModifiers().Last();
                    var m = new ParsedConstant();
                    m.SourceText = variableDeclartion.GetText();
                    m.Name = variableDeclartion.IdentifierStr;
                    m.Type = new ParsedType
                    {
                        Name = typeNode.GetTypeString(),
                        SourceText = typeNode.GetText(),
                    };
                    sourceFile.Constants.Add(m);
                }
            }
            else if (child is FunctionDeclaration functionDeclaration)
            {
                var nmt = true;
            }
            else if (child is TypeAliasDeclaration typeAliasDeclaration)
            {
                var aliasName = typeAliasDeclaration.IdentifierStr;
                var unionType = typeAliasDeclaration.OfKind<UnionTypeNode>().ToList();
                var m = new ParsedTypeAlias();
                m.SourceText = child.GetText();
                m.Name = typeAliasDeclaration.IdentifierStr;
                sourceFile.TypeAliases.Add(m);
                var nmt = true;
            }
            else if (child is InterfaceDeclaration interfaceDeclaration)
            {
                var nmt = true;
                var interfaceStr = interfaceDeclaration.ParseInterfaceOrClass(JSModuleNamespace);
                sourceFile.Interfaces.Add(interfaceStr);
                //sb.AppendLine(interfaceStr);
                var nmt2 = true;
            }
            else if (child is ClassDeclaration classDeclaration)
            {
                var nmt = true;
                var interfaceStr = classDeclaration.ParseInterfaceOrClass(JSModuleNamespace);
                sourceFile.Interfaces.Add(interfaceStr);
                //sb.AppendLine(interfaceStr);
                var nmt2 = true;
            }
            else if (child is EnumDeclaration enumDeclaration)
            {
                var m = new ParsedEnum();
                m.SourceText = child.GetText();
                m.Name = enumDeclaration.IdentifierStr;
                foreach (var c in enumDeclaration.OfKind<EnumMember>())
                {
                    var enuMentry = new ParsedEnumEntry();
                    enuMentry.Name = c.IdentifierStr;
                    enuMentry.Value = c.OfKind<NumericLiteral>().FirstOrDefault()?.GetText();
                    m.Values.Add(enuMentry);
                }
                sourceFile.Enums.Add(m);
                var nmt = true;
            }
            else if (child is EndOfFileToken endOfFileToken)
            {
                var nmt = true;
            }
            else if (child is ExportDeclaration exportDeclaration)
            {
                var nmt = true;
            }
            else if (child is ExpressionStatement expressionStatement)
            {
                var nmt = true;
            }
            else if (child is Block block)
            {
                var nmt = true;
            }
            else if (child is ImportDeclaration importDeclaration)
            {
                if (importDeclaration.Children.FirstOrDefault()?.Kind == SyntaxKind.StringLiteral)
                {
                    var importName = importDeclaration.Children.First().GetText();
                    var nmt = true;
                }
                else  if(importDeclaration.Children.Count == 2)
                {
                    try
                    {
                        var importClause = importDeclaration.Children[0].IdentifierStr;
                        var strLit = (importDeclaration.Children[1] as StringLiteral)!.Text;
                        var scriptPath = IOPath.GetFullPath(IOPath.Combine(fileDir, strLit));
                        var filePath = IOPath.GetFullPath(scriptPath);
                        await ParseTypeScriptDefinationsFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        var nmt = true;
                    }
                }
            }
            else if (child is ExportAssignment exportAssignment)
            {
                var nmt = true;
            }
            else if (child is EmptyStatement emptyStatement)
            {
                var nmt = true;
            }
            else if (child is LabeledStatement labeledStatement)
            {
                var nmt = true;
            }
            else
            {
                var type = child.GetType();
                if (!UnhandledTypes.Contains(type))
                {
                    UnhandledTypes.Add(type);
                }
                var nmt = true;
            }
        }
        async Task ParseNodeChildren(ParsedModule sourceFile, List<Node> children)
        {
            foreach (var child in children)
            {
                await ParseNode(sourceFile, child);
            }
        }
        //InterfaceInfo ParseInterfaceDeclaration(InterfaceDeclaration x)
        //{
        //    InterfaceInfo? ret = new InterfaceInfo();
        //    var Name = GetInterfaceName(x);
        //    if (string.IsNullOrEmpty(Name) || Name.Contains(" ") || Name.Contains("|"))
        //    {
        //        return ret;
        //    }
        //    ret.Name = Name;
        //    ret.CSClassName = Name;
        //    ret.CSInterfaceName = $"I{ret.CSClassName}";
        //    var Heritage = x.OfKind(SyntaxKind.HeritageClause).FirstOrDefault() as HeritageClause;
        //    if (Heritage != null)
        //    {
        //        var extendsTypes = Heritage.Types.Select(o => o.IdentifierStr).ToList();
        //        var extendsCSTypes = extendsTypes.Select(o => GetTypeMapping(o)).Where(o => !string.IsNullOrEmpty(o)).ToList();
        //        ret.Extends = extendsTypes;
        //        ret.CSExtends = extendsCSTypes;
        //    }

        //    var Methods = x.OfKind(SyntaxKind.Constructor).Concat(x.OfKind(SyntaxKind.MethodDeclaration))
        //        .Select(x => new
        //        {
        //            Name = x is ConstructorDeclaration ctor ? ".ctor" : x.IdentifierStr,
        //            IsPublic = x.First.Kind != SyntaxKind.PrivateKeyword,
        //            Args = ((ISignatureDeclaration)x).Parameters.Select(x => new
        //            {
        //                Name = x.Children.OfKind(SyntaxKind.Identifier).FirstOrDefault().GetText(),
        //                Type = x.Children.OfKind(SyntaxKind.TypeReference).FirstOrDefault()?.GetText(),
        //            }),
        //        }).ToArray();

        //    foreach (var ctor in x.OfKind(SyntaxKind.ConstructSignature))
        //    {
        //        var nmt2 = true;
        //    }

        //    foreach (var prop in x.OfKind(SyntaxKind.PropertySignature))
        //    {
        //        var propInfo = ReadInterfaceProperty(prop);
        //        if (string.IsNullOrEmpty(propInfo.Name))
        //        {
        //            continue;
        //        }
        //        if (string.IsNullOrEmpty(propInfo.CSTypeNameFirst))
        //        {
        //            continue;
        //        }
        //        ret.Properties.Add(propInfo);
        //    }

        //    foreach (var prop in Methods)
        //    {
        //        var g = "";
        //    }

        //    ////var json = JsonSerializer.Serialize(tmp);
        //    var outFile = IOPath.Combine(OutPath, $"{ret.CSClassName}.cs");
        //    if (await FS.FileExists(outFile))
        //    {
        //        var exists = true;
        //    }
        //    var template = ret.ToCSClass();
        //    await FS.Write(outFile, template);
        //    return ret;
        //}
        Dictionary<string, string[]> _typeMappings = new Dictionary<string, string[]> {
            { "bool", new []{ "boolean" } },
            { "double", new []{ "number" } },
            { "string", new []{ "String" } },
            { "object", new []{ "any", "object" } },
            { "", new []{ "<void>" } },
        };
        string GetTypeMapping(string tsType)
        {
            var ret = tsType;
            if (string.IsNullOrEmpty(tsType) || tsType.Contains(" ") || tsType.Contains("|"))
            {
                return "object";
            }
            else if (tsType == "null")
            {
                return "object";
            }
            var mappings = _typeMappings.Where(o => o.Value.Contains(tsType, StringComparer.OrdinalIgnoreCase)).ToList();
            if (mappings.Count > 0)
            {
                ret = mappings.First().Key;
            }
            else
            {
                _typeMappings.Add(tsType, new[] { tsType });
                Console.WriteLine($"tsType added: {tsType}");
            }
            return ret;
        }
        string GetInterfaceName(Node x)
        {
            var name = x.IdentifierStr;
            if (string.IsNullOrEmpty(name))
            {
                name = x.GetText();
            }
            if (string.IsNullOrEmpty(name) && x.First is LiteralExpression literal)
            {
                name = x.First.GetText();
            }
            if (string.IsNullOrEmpty(name))
            {
                var bbb = false;
            }
            return name;
        }
        // https://www.typescriptlang.org/docs/handbook/interfaces.html
        //InterfacePropertyInfo ReadInterfaceProperty(Node x)
        //{
        //    var ret = new InterfacePropertyInfo();
        //    var children = x.Children.ToList();
        //    var isReadOnly = children.First().Kind == SyntaxKind.ReadonlyKeyword;
        //    if (isReadOnly) children = children.Skip(1).ToList();
        //    var chIdent = children.First();
        //    var propertyName = chIdent.Kind == SyntaxKind.ObjectLiteralExpression ? chIdent.First.GetText() : chIdent.GetText();
        //    children = children.Skip(1).ToList();
        //    var propertyIsOptional = children.First().Kind == SyntaxKind.QuestionToken;
        //    if (propertyIsOptional) children = children.Skip(1).ToList();
        //    var typesFound = ReadTypes(children);
        //    ret.Name = propertyName;
        //    ret.CSName = propertyName.Substring(0, 1).ToUpperInvariant() + propertyName.Substring(1);
        //    ret.IsReadOnly = isReadOnly;
        //    ret.TypeNames = typesFound;
        //    ret.CSTypeNames = typesFound.Where(o => !string.IsNullOrEmpty(o) && o != "null").Select(o => GetTypeMapping(o)).Where(o => !string.IsNullOrEmpty(o)).ToList();
        //    return ret;
        //}
        List<string> ReadTypes(IEnumerable<Node> children)
        {
            var typesFound = new List<string>();
            foreach (var ch in children)
            {
                var typeName = "";
                if (ch.Kind == SyntaxKind.ArrayType)
                {
                    var arrayTypes = ReadTypes(ch.Children);
                    // TODO - verify proper handling of any variations on this
                    var isNullable = arrayTypes.Contains("null");
                    if (isNullable) arrayTypes = arrayTypes.Where(o => o != "null").ToList();
                    if (arrayTypes.Count == 1)
                    {
                        var isNullableString = isNullable ? "?" : "";
                        typeName = $"{arrayTypes.First()}{isNullableString}[]";
                    }
                    else
                    {
                        // TODO
                        continue;
                    }
                }
                else if (ch is TypeReferenceNode typeReferenceNode)
                {
                    typeName = typeReferenceNode.IdentifierStr;
                }
                else if (ch.Kind == SyntaxKind.ArrayLiteralExpression)
                {
                    var checkthisalso = true;
                }
                else if (ch is LiteralExpression literalExpression)
                {
                    typeName = literalExpression.Kind.ToString()[..^7].ToLower();
                }
                else if (ch.Kind == SyntaxKind.UnionType)
                {
                    var unionTypes = ReadTypes(ch.Children);
                    var isNullable = unionTypes.Contains("null");
                    if (isNullable) unionTypes = unionTypes.Where(o => o != "null").ToList();
                    if (unionTypes.Count == 1)
                    {
                        var isNullableString = isNullable ? "?" : "";
                        typeName = $"{unionTypes.First()}{isNullableString}";
                    }
                    else
                    {
                        foreach (var uType in unionTypes)
                        {
                            var isNullableString = isNullable ? "?" : "";
                            var uTypeName = $"{unionTypes.First()}{isNullableString}";
                            if (uTypeName.StartsWith("(") || uTypeName.Contains("|") || uTypeName.Contains(" "))
                            {
                                var gg = true;
                            }
                            typesFound.Add(uTypeName);
                        }
                        continue;
                    }
                }
                else if (ch.Kind == SyntaxKind.FunctionType)
                {
                    // TODO
                    typeName = ch.GetText();
                    continue;
                }
                else if (ch.Kind == SyntaxKind.ParenthesizedType)
                {
                    if (ch.Children.Count > 0)
                    {
                        var isFunctionType = ch.First.Kind == SyntaxKind.FunctionType;
                        if (isFunctionType)
                        {
                            var skip = true;
                            continue;
                        }
                    }
                    var tmpTxt = ch.GetText();
                    var paranTypes = tmpTxt.Substring(1);
                    paranTypes = paranTypes.Substring(0, paranTypes.Length - 1);
                    var paranTypesL = paranTypes.Split("|").Select(o => o.Trim()).ToList();
                    var isNullable = paranTypesL.Contains("null");
                    if (isNullable) paranTypesL = paranTypesL.Where(o => o != "null").ToList();
                    if (paranTypesL.Count == 1)
                    {
                        var isNullableString = isNullable ? "?" : "";
                        typeName = $"{paranTypesL.First()}{isNullableString}";
                        if (typeName.StartsWith("(") || typeName.Contains("|") || typeName.Contains(" "))
                        {
                            var gg = true;
                        }
                    }
                    else
                    {
                        var checkthis = true;
                        continue;
                    }
                }
                else
                {
                    typeName = ch.GetText();
                }
                if (!string.IsNullOrEmpty(typeName))
                {
                    if (typeName.StartsWith("(") || typeName.Contains("|") || typeName.Contains(" "))
                    {
                        var gg = true;
                    }
                    typesFound.Add(typeName);
                }
            }
            typesFound = typesFound.ToList();
            return typesFound;
        }
    }
}
