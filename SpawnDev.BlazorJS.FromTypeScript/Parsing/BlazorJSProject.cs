using Radzen;
using Sdcb.TypeScript;
using Sdcb.TypeScript.TsTypes;
using SpawnDev.BlazorJS.AsyncFileSystem;
using SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes;
using SpawnDev.BlazorJS.FromTypeScript.Services;
using SpawnDev.BlazorJS.JSObjects;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Array = System.Array;
using Node = Sdcb.TypeScript.TsTypes.Node;
using Type = System.Type;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing
{
    // https://ts-ast-viewer.com/
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
        IAsyncFS FS;
        public event Action OnProgress = default!;
        public long TypeScriptDeclarationFileCount { get; private set; }
        public BlazorJSProject(IAsyncFS fs, string srcPath, string projectName, string jsModuleNamespace, bool nameSpaceFromPath = false)
        {
            FS = fs;
            ProjectName = projectName;
            SourcePath = srcPath;
            //OutPath = outPath;
            NameSpaceFromPath = nameSpaceFromPath;
            JSModuleNamespace = jsModuleNamespace;
        }
        public async Task ProcessDir()
        {
            Modules.Clear();
            var files = await FS.GetInfos(SourcePath, true);
            var indexFile = files.FirstOrDefault(o => o.Name == "index.d.ts");
            var tsdFiles = files.Where(o => o.Name.EndsWith(".d.ts")).ToList();
            TypeScriptDeclarationFileCount = tsdFiles.Count;
            if (indexFile != null)
            {
                await ParseTypeScriptDefinitionsFile(indexFile.FullPath);
                OnProgress?.Invoke();
            }
            else
            {
                foreach (var file in tsdFiles)
                {
                    await ParseTypeScriptDefinitionsFile(file.FullPath);
                    OnProgress?.Invoke();
                }
            }
        }
        string ResolveTypeNameToFullName(string typeName)
        {
            var fndClass = Interfaces.FirstOrDefault(o => o.CSClassName == typeName);
            if (fndClass != null)
            {
                return fndClass.ModuleNamespace;
            }
            var fndEnum = Enums.FirstOrDefault(o => o.CSEnumName == typeName);
            if (fndEnum != null)
            {
                return $"{ProjectName}.{fndEnum.CSEnumName}";
            }
            var fndConstant = Constants.FirstOrDefault(o => o.Name == typeName);
            if (fndConstant != null)
            {
                return $"{ProjectName}.{fndConstant.Name}";
            }
            var type = Type.GetType($"SpawnDev.BlazorJS.JSObjects.{typeName}");
            if (type != null)
            {
                return $"SpawnDev.BlazorJS.JSObjects.{typeName}";
            }
            return typeName;
        }
        string AliasForToTypeNameArray(string[] typeNames)
        {
            var typeFullNames = typeNames.Select(ResolveTypeNameToFullName).ToList();
            var ret = string.Join(", ", typeFullNames);
            return ret;
        }
        public bool IgnoreUnderscoreMembers = true;
        public bool UseCSNaming { get; set; } = true;
        public bool ExportInterfacesAsClasses { get; set; } = false;
        public async Task WriteViews(string OutPath, Action<int, int> progressCallback)
        {
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            await FS.Remove(OutPath);
            await FS.CreateDirectory(OutPath);

            var files = await FS.GetInfos(SourcePath, true);
            var indexFile = files.FirstOrDefault(o => o.Name == "index.d.ts");
            var tsdFiles = files.Where(o => o.Name.EndsWith(".d.ts")).ToList();
            TypeScriptDeclarationFileCount = tsdFiles.Count;
            var i = 0;
            foreach (var file in tsdFiles)
            {
                var subPath = string.Join('/', file.FullPath.Split('/').Skip(SourcePath.Trim('/').Split('/').Length));
                var destFile = IOPath.Combine(OutPath, subPath);
                destFile = Regex.Replace(destFile, @"\.[^.]+$", ".view", RegexOptions.IgnoreCase);
                var text = await FS.ReadText(file.FullPath);
                var x = new TypeScriptAST(text, file.FullPath);
                var view = x.RootNode.GetView();
                await FS.Write(destFile, view);
                i++;
                progressCallback?.Invoke(tsdFiles.Count, i);
            }
        }
        public async Task WriteSignatures(string OutPath, Action<int, int> progressCallback)
        {
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            await FS.Remove(OutPath);
            await FS.CreateDirectory(OutPath);

            var files = await FS.GetInfos(SourcePath, true);
            var indexFile = files.FirstOrDefault(o => o.Name == "index.d.ts");
            var tsdFiles = files.Where(o => o.Name.EndsWith(".d.ts")).ToList();
            TypeScriptDeclarationFileCount = tsdFiles.Count;
            var i = 0;
            var sigs = new Dictionary<string, List<string>>();
            foreach (var file in tsdFiles)
            {
                var subPath = string.Join('/', file.FullPath.Split('/').Skip(SourcePath.Trim('/').Split('/').Length));
                var destFile = IOPath.Combine(OutPath, subPath);
                destFile = Regex.Replace(destFile, @"\.[^.]+$", ".view", RegexOptions.IgnoreCase);
                var text = await FS.ReadText(file.FullPath);
                var x = new TypeScriptAST(text, file.FullPath);
                var view = x.RootNode.GetView();
                foreach (var c in x.RootNode.OfKind(SyntaxKind.ClassDeclaration))
                {
                    foreach (var m in c.OfKind(SyntaxKind.PropertyDeclaration))
                    {
                        var sig = m.GetNodeSignature2();
                        if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                        {
                            typeSigs = new List<string>();
                            sigs[m.Kind.ToString()] = typeSigs;
                        }
                        if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                    }
                    foreach (var n in c.OfKind(SyntaxKind.MethodDeclaration))
                    {
                        foreach (var m in n.OfKind(SyntaxKind.Parameter))
                        {
                            var sig = m.GetNodeSignature2();
                            if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                            {
                                typeSigs = new List<string>();
                                sigs[m.Kind.ToString()] = typeSigs;
                            }
                            if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                        }
                    }
                }
                foreach (var c in x.RootNode.OfKind(SyntaxKind.InterfaceDeclaration))
                {
                    foreach (var m in c.OfKind(SyntaxKind.PropertySignature))
                    {
                        var sig = m.GetNodeSignature2();
                        if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                        {
                            typeSigs = new List<string>();
                            sigs[m.Kind.ToString()] = typeSigs;
                        }
                        if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                    }
                    foreach (var m in c.OfKind(SyntaxKind.MethodSignature))
                    {
                        var sig = m.GetNodeSignature2();
                        if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                        {
                            typeSigs = new List<string>();
                            sigs[m.Kind.ToString()] = typeSigs;
                        }
                        if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                    }
                }
                foreach (var m in x.RootNode.OfKind(SyntaxKind.VariableDeclaration))
                {
                    var sig = m.GetNodeSignature2();
                    if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                    {
                        typeSigs = new List<string>();
                        sigs[m.Kind.ToString()] = typeSigs;
                    }
                    if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                }
                foreach (var c in x.RootNode.OfKind(SyntaxKind.EnumDeclaration))
                {
                    foreach (var m in c.OfKind(SyntaxKind.EnumMember))
                    {
                        var sig = m.GetNodeSignature2();
                        if (!sigs.TryGetValue(m.Kind.ToString(), out var typeSigs))
                        {
                            typeSigs = new List<string>();
                            sigs[m.Kind.ToString()] = typeSigs;
                        }
                        if (!typeSigs.Contains(sig)) typeSigs.Add(sig);
                    }
                }
                await FS.Write(destFile, view);
                i++;
                progressCallback?.Invoke(tsdFiles.Count, i);
            }
            var nmtart = true;
            foreach (var kvp in sigs)
            {
                var lines = kvp.Value;
                var outFile = IOPath.Combine(OutPath, $"{kvp.Key}.txt");
                await FS.Write(outFile, string.Join("\n", lines));
            }
        }
        public async Task WriteViewsCondensed(string OutPath, Action<int, int> progressCallback)
        {
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            await FS.Remove(OutPath);
            await FS.CreateDirectory(OutPath);

            var files = await FS.GetInfos(SourcePath, true);
            var indexFile = files.FirstOrDefault(o => o.Name == "index.d.ts");
            var tsdFiles = files.Where(o => o.Name.EndsWith(".d.ts")).ToList();
            TypeScriptDeclarationFileCount = tsdFiles.Count;
            var i = 0;
            var classes = new List<Node>();
            var interfaces = new List<Node>();
            var typeAliases = new List<Node>();
            var enums = new List<Node>();
            // (global constants. constants inside classes will be handled there.)
            var constants = new List<Node>();
            foreach (var file in tsdFiles)
            {
                var subPath = string.Join('/', file.FullPath.Split('/').Skip(SourcePath.Split('/').Length));
                var destFile = IOPath.Combine(OutPath, subPath);
                var destDir = IOPath.GetDirectoryName(destFile);
                destFile = Regex.Replace(destFile, @"\.[^.]+$", ".view", RegexOptions.IgnoreCase);
                var text = await FS.ReadText(file.FullPath);
                var x = new TypeScriptAST(text, file.FullPath);
                var view = x.RootNode.GetView();

                classes.AddRange(x.RootNode.OfKind(SyntaxKind.ClassDeclaration));
                interfaces.AddRange(x.RootNode.OfKind(SyntaxKind.InterfaceDeclaration));
                typeAliases.AddRange(x.RootNode.OfKind(SyntaxKind.TypeAliasDeclaration));
                enums.AddRange(x.RootNode.OfKind(SyntaxKind.EnumDeclaration));
                // (global constants. constants inside classes will be handled there.)
                constants.AddRange(x.RootNode.OfKind(SyntaxKind.VariableDeclaration));
                //await FS.Write(destFile, view);
                i++;
                progressCallback?.Invoke(tsdFiles.Count, i);
            }

            var sb = new StringBuilder();
            // classes
            foreach (var c in classes.OrderBy(o => o.IdentifierStr))
            {
                sb.AppendLine(c.GetView());
            }
            await FS.Write(IOPath.Combine(OutPath, "classes.views"), sb.ToString());
            // interfaces
            sb.Clear();
            foreach (var c in interfaces.OrderBy(o => o.IdentifierStr))
            {
                sb.AppendLine(c.GetView());
            }
            await FS.Write(IOPath.Combine(OutPath, "interfaces.views"), sb.ToString());
            // enums
            sb.Clear();
            foreach (var c in enums.OrderBy(o => o.IdentifierStr))
            {
                sb.AppendLine(c.GetView());
            }
            await FS.Write(IOPath.Combine(OutPath, "enums.views"), sb.ToString());
            // type aliases
            sb.Clear();
            foreach (var c in typeAliases.OrderBy(o => o.IdentifierStr))
            {
                sb.AppendLine(c.GetView());
            }
            await FS.Write(IOPath.Combine(OutPath, "typealiases.views"), sb.ToString());
            // constants
            sb.Clear();
            foreach (var c in constants.OrderBy(o => o.IdentifierStr))
            {
                sb.AppendLine(c.GetView());
            }
            await FS.Write(IOPath.Combine(OutPath, "constants.views"), sb.ToString());
        }
        public async Task WriteProject2(string OutPath, Action<int, int> progressCallback)
        {
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            await FS.Remove(OutPath);
            await FS.CreateDirectory(OutPath);

            // A basic .csproj file
            var proj = GetCSProjStr();
            await FS.Write(IOPath.Combine(OutPath, $"{ProjectName}.csproj"), proj);

            //
            var total = Modules.Count;
            var done = 0;
            foreach (var m in Modules.Values)
            {
                foreach (var c in m.Interfaces)
                {
                    if (!c.IsClass && !ExportInterfacesAsClasses) continue;
                    var fileNameP = IOPath.Combine(OutPath, m.SubPath, $"{c.Name}.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = $@"
using System.Text;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    public class {c.CSClassName}{(c.Extends.Any() ? $" : {c.Extends.First()}" : " : JSObject")}
    {{
        #region Constructors
        /// <inheritdoc/>
        public {c.CSClassName}(IJSInProcessObjectReference _ref) : base(_ref) {{ }}
{string.Join("\r\n", c.Methods.Where(o => o.IsConstructor).Select(m => m.ToString("        ")))}
        #endregion

        #region Properties
{string.Join("\r\n", c.Properties.Where(o => !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderBy(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion

        #region Methods
{string.Join("\r\n", c.Methods.Where(o => !o.IsConstructor && !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderByDescending(o => o.Name).Select(m => m.ToString("        ")))}
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
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    /// <summary>
    /// {c.SourceText?.Replace("\n", "\n    /// ")}
    /// </summary>
    public enum {c.CSEnumName}
    {{
        {string.Join("\n        ", c.Values.Select(o => $"{o.Name} = {o.Value},"))}
    }}
}}
";
                    await FS.Write(fileName, code);
                }
                if (m.TypeAliases.Any())
                {
                    // create a file for all type aliases
                    var fileNameP = IOPath.Combine(OutPath, m.SubPath, $"TypeDefinitions.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = string.Join("\n", m.TypeAliases.Select(o =>
                    {
                        return $@"
/// <summary>
/// {o.SourceText?.Replace("\n", "\n/// ")}
/// </summary>
global using {o.Name} = SpawnDev.BlazorJS.Union<{AliasForToTypeNameArray(o.UnionTypes)}>;
".Trim();
                    }));
                    await FS.Write(fileName, code);
                }
                if (m.Constants.Any())
                {
                    // create a file for all type constants
                    var fileNameP = IOPath.Combine(OutPath, m.SubPath, $"Constants.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = string.Join("\n", m.Constants.Select(o =>
                    {
                        return $@"
        /// <summary>
        /// {o.SourceText?.Replace("\n", "\n        /// ")}
        /// </summary>
        public const {o.CSReturnType} {o.Name} = {o.Value};
".Trim();
                    }));

                    code = $@"
using System.Text;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    public static partial class Constants
    {{
        {code}
    }}
}}
";
                    await FS.Write(fileName, code);
                }
                done++;
                progressCallback?.Invoke(total, done);
            }
        }
        public delegate void PostProcessBeforeWriteDelegate(string file, ref string cont);
        public PostProcessBeforeWriteDelegate? PostProcessBeforeWrite = null;
        public async Task WriteProject(string OutPath, Action<int, int> progressCallback, bool flatPath = true)
        {
            if (await FS.FileExists(OutPath)) throw new Exception($"{nameof(OutPath)} should be a directory. File was found.");
            await FS.Remove(OutPath);
            await FS.CreateDirectory(OutPath);

            // A basic .csproj file
            var proj = GetCSProjStr();
            await FS.Write(IOPath.Combine(OutPath, $"{ProjectName}.csproj"), proj);


            var allowGenerics = false;
            //
            var total = Modules.Count;
            var done = 0;
            foreach (var m in Modules.Values)
            {
                foreach (var c in m.Interfaces)
                {
                    var fileNameP = flatPath ? IOPath.Combine(OutPath, $"{c.Name}.cs") : IOPath.Combine(OutPath, m.SubPath, $"{c.Name}.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    if (!c.IsClass && !ExportInterfacesAsClasses)
                    {
                        var code = $@"
using System.Text;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    public interface {c.CSClassName}{(c.Extends.Any() ? $" : {string.Join(", ", c.Extends)}" : "")}
    {{
        #region Properties
{string.Join("\r\n", c.Properties.Where(o => !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderBy(o => o.Name).Select(m => m.ToInterfaceString("        ")))}
        #endregion

        #region Methods
{string.Join("\r\n", c.Methods.Where(o => !o.IsConstructor && !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderByDescending(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion
    }}
}}
";
                        PostProcessBeforeWrite?.Invoke(fileName, ref code);
                        await FS.Write(fileName, code);
                    }
                    else
                    {
                        string getClassTypeParameterString(bool enabled)
                        {
                            return enabled && c.TypeParameters?.Any() == true ? $"<{string.Join(", ", c.TypeParameters)}>" : "";
                        }
                        var code = $@"
using System.Text;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    public class {c.CSClassName}{getClassTypeParameterString(allowGenerics)}{(c.Extends.Any() ? $" : {c.Extends.First()}" : " : JSObject")}
    {{
        #region Constructors
        /// <inheritdoc/>
        public {c.CSClassName}(IJSInProcessObjectReference _ref) : base(_ref) {{ }}
{string.Join("\r\n", c.Methods.Where(o => o.IsConstructor).Select(m => m.ToString("        ")))}
        #endregion

        #region Properties
{string.Join("\r\n", c.Properties.Where(o => !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderBy(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion

        #region Methods
{string.Join("\r\n", c.Methods.Where(o => !o.IsConstructor && !string.IsNullOrWhiteSpace(o.Name) && (!IgnoreUnderscoreMembers || !o.Name.StartsWith("_"))).OrderByDescending(o => o.Name).Select(m => m.ToString("        ")))}
        #endregion
    }}
}}
";
                        PostProcessBeforeWrite?.Invoke(fileName, ref code);
                        await FS.Write(fileName, code);
                    }
                }
                foreach (var c in m.Enums)
                {
                    var fileNameP = flatPath ? IOPath.Combine(OutPath, $"{c.Name}.cs") : IOPath.Combine(OutPath, m.SubPath, $"{c.Name}.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    //var fileName = IOPath.GetFullPath(IOPath.Combine(m.DestDir, $"{c.Name}.cs"));
                    if (c.IsNumeric)
                    {
                        var code = $@"
using System.Text;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace {m.ModuleNamespace}
{{
    /// <summary>
    /// {c.SourceText?.Replace("\n", "\n    /// ")}
    /// </summary>
    public enum {c.CSEnumName}
    {{
        {string.Join("\n        ", c.Values.Select(o => $"{o.Name} = {o.Value},"))}
    }}
}}
";
                        PostProcessBeforeWrite?.Invoke(fileName, ref code);
                        await FS.Write(fileName, code);
                    }
                    else if (c.IsString)
                    {
                        var code = $@"
using SpawnDev.BlazorJS.JsonConverters;
using System.Text.Json.Serialization;

namespace {m.ModuleNamespace}
{{
    /// <summary>
    /// {c.SourceText?.Replace("\n", "\n    /// ")}
    /// </summary>
    [JsonConverter(typeof(EnumStringConverterFactory))]
    public enum {c.CSEnumName}
    {{
        {string.Join("\n        ", c.Values.Select(o => $"[JsonPropertyName({o.Value})]\n        {o.Name},"))}
    }}
}}
";
                        PostProcessBeforeWrite?.Invoke(fileName, ref code);
                        await FS.Write(fileName, code);
                    }
                }
                if (m.TypeAliases.Any())
                {
                    // create a file for all type aliases
                    var fileNameP = flatPath ? IOPath.Combine(OutPath, $"TypeDefinitions.cs") : IOPath.Combine(OutPath, m.SubPath, $"TypeDefinitions.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = string.Join("\n", m.TypeAliases.Select(o =>
                    {
                        return $@"
/// <summary>
/// {o.SourceText?.Replace("\n", "\n/// ")}
/// </summary>
global using {o.Name} = SpawnDev.BlazorJS.Union<{AliasForToTypeNameArray(o.UnionTypes)}>;
".Trim();
                    }));
                    PostProcessBeforeWrite?.Invoke(fileName, ref code);
                    await FS.Append(fileName, code + "\n");
                }
                if (m.Constants.Any())
                {
                    // create a file for all type constants
                    var fileNameP = flatPath ? IOPath.Combine(OutPath, $"Constants.cs") : IOPath.Combine(OutPath, m.SubPath, $"Constants.cs");
                    var fileName = IOPath.GetFullPath(fileNameP)!;
                    var code = string.Join("\n", m.Constants.Select(o =>
                    {
                        if (string.IsNullOrEmpty(o.Value))
                        {
                            return $@"
        ///// <summary>
        ///// {o.SourceText?.Replace("\n", "\n        /// ")}
        ///// </summary>
        //public const {o.CSReturnType} {o.Name} = {o.Value};
".Trim();
                        }
                        else
                        {
                            return $@"
        /// <summary>
        /// {o.SourceText?.Replace("\n", "\n        /// ")}
        /// </summary>
        public const {o.CSReturnType} {o.Name} = {o.Value};
".Trim();
                        }
                    }));

                    PostProcessBeforeWrite?.Invoke(fileName, ref code);
                    await FS.Append(fileName, code);
                }
                done++;
                progressCallback?.Invoke(total, done);
            }
        }
        bool UseTitleCaseNaming = true;
        async Task ParseTypeScriptDefinitionsFile(string file)
        {
            file = IOPath.GetFullPath(file)!;
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
                //Console.WriteLine($"Reading: {file}");
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
                Project = this,
                SubPath = subPath,
                SourceFile = file,
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
        async Task ParseTypeScriptDefinitions(ParsedModule parsedModule, string text)
        {
            var x = new TypeScriptAST(text, parsedModule.SourceFile);
            await ParseNodeChildren(parsedModule, x.RootNode.Children);
        }
        List<Node> GetAncestors(Node node)
        {
            var ret = new List<Node>();
            while (node.Parent != null)
            {
                ret.Add((Node)node.Parent);
                node = (Node)node.Parent;
            }
            return ret;
        }

        async Task ParseNode(ParsedModule parsedModule, Node child)
        {
            var ancestors = GetAncestors(child);
            var inClass = ancestors.Any(o => o.Kind == SyntaxKind.ClassDeclaration);
            var inInterface = ancestors.Any(o => o.Kind == SyntaxKind.InterfaceDeclaration);

            var fileDir = IOPath.GetDirectoryName(parsedModule.SourceFile);
            var str = child.GetTextSafe();
            if (child is ModuleDeclaration moduleDeclaration)
            {
                var moduleName = moduleDeclaration.IdentifierStr;
                var moduleBlock = moduleDeclaration.Children.OfType<ModuleBlock>().FirstOrDefault();
                if (moduleBlock != null)
                {
                    await ParseNodeChildren(parsedModule, moduleBlock.Children);
                }
            }
            else if (child is VariableStatement variableStatement)
            {
                foreach (var variableDeclartion in variableStatement.DeclarationList.Declarations)
                {
                    var name = variableDeclartion.IdentifierStr;
                    if (string.IsNullOrEmpty(name))
                    {
                        var nmt22 = true;
                        continue;
                    }
                    if (name == "NodeShaderStage")
                    {
                        var nmtt = true;
                    }
                    var typeNode = variableDeclartion.GetNonModifiers().Last();
                    var m = new ParsedConstant();
                    m.SourceText = variableDeclartion.GetTextSafe();
                    m.Name = variableDeclartion.IdentifierStr;
                    var numericLiteral = typeNode.OfKind(SyntaxKind.NumericLiteral).FirstOrDefault();
                    var stringLiteral = typeNode.OfKind(SyntaxKind.StringLiteral).FirstOrDefault();
                    m.Value = numericLiteral?.GetTextSafe() ?? stringLiteral?.GetTextSafe() ?? "";
                    if (numericLiteral != null)
                    {
                        var nmtt = numericLiteral.IdentifierStr;
                    }
                    else if (stringLiteral != null)
                    {
                        var nmtt = stringLiteral.IdentifierStr;
                    }
                    m.Type = new ParsedType
                    {
                        Name = typeNode.GetTypeString(""),
                        SourceText = typeNode.GetTextSafe(),
                    };
                    parsedModule.Constants.Add(m);
                }
            }
            else if (child is FunctionDeclaration functionDeclaration)
            {
                var nmt = true;
            }
            else if (child is TypeAliasDeclaration typeAliasDeclaration)
            {
                var nodeHasErrors = (child.Flags & NodeFlags.ThisNodeHasError) != 0;
                var nodeOrSubHasErrors = (child.Flags & NodeFlags.ThisNodeOrAnySubNodesHasError) != 0;
                if (nodeHasErrors || nodeOrSubHasErrors)
                {
                    return;
                }
                var aliasName = typeAliasDeclaration.IdentifierStr;
                var unionNode = child.Children.FirstOrDefault(o => o.Kind == SyntaxKind.UnionType);
                if (unionNode != null && unionNode.Children.Any() && unionNode.Children.All(o => o.Kind == SyntaxKind.LiteralType))
                {
                    // EnumString
                    var m = new ParsedEnum();
                    m.SourceText = child.GetTextSafe();
                    m.Name = aliasName;
                    // this could be an Enum or EnumString
                    var literalType = unionNode.Children.First().Children.First().Kind;
                    var values = unionNode.Children.Select(o => o.GetTextSafe()).ToList();
                    if (literalType == SyntaxKind.StringLiteral)
                    {

                    }
                    else if (literalType == SyntaxKind.NumericLiteral)
                    {
                        // Enum

                    }
                    foreach (var n in unionNode.Children)
                    {
                        var c = n.Children.First();
                        var enuMentry = new ParsedEnumEntry();
                        // not a key, value type. only has a value
                        var value = c.GetTextSafe();
                        switch (c.Kind)
                        {
                            case SyntaxKind.StringLiteral:
                                enuMentry.Name = value.Trim('"').TitleCaseInvariant();
                                enuMentry.Value = value;
                                m.IsString = true;
                                break;
                            case SyntaxKind.NumericLiteral:
                                enuMentry.Name = $"Opt_{m.Values.Count}";
                                enuMentry.Value = value;
                                m.IsNumeric = true;
                                break;
                            default:
                                var nmt22 = true;
                                break;
                        }
                        m.Values.Add(enuMentry);
                    }
                    parsedModule.Enums.Add(m);
                    var nmtt1222 = true;
                }
                else
                {
                    var unionType = typeAliasDeclaration.OfKind<UnionTypeNode>().ToList().FirstOrDefault();
                    var m = new ParsedTypeAlias();
                    m.SourceText = child.GetTextSafe();
                    m.Name = typeAliasDeclaration.IdentifierStr;
                    var unionTypes = unionType?.Children.Select(o => o.IdentifierStr).ToArray() ?? Array.Empty<string>();
                    m.UnionTypes = unionTypes;
                    parsedModule.TypeAliases.Add(m);
                }
                //if (m.Name == "PixelFormatGPU")
                //{
                //    // this type is essentially an EnumString
                //    var nmt11 = true;
                //}
                var nmt = true;
            }
            else if (child is InterfaceDeclaration interfaceDeclaration)
            {
#if DEBUG
                var nmt = child.GetView();
                Console.WriteLine(nmt);
#endif
                var parsedInterface = interfaceDeclaration.ParseInterfaceOrClass(parsedModule, JSModuleNamespace);
                var existing = Interfaces.FirstOrDefault(o => o.JSNameSpaceName == parsedInterface.JSNameSpaceName);
                if (existing != null)
                {
                    if (existing.IsClass)
                    {
                        //  class trumps interface
                        return;
                    }
                    else
                    {
                        var existingMemberCount = existing.Methods.Count + existing.Properties.Count;
                        var parsedMemberCount = parsedInterface.Methods.Count + parsedInterface.Properties.Count;
                        if (existingMemberCount > parsedMemberCount)
                        {
                            // existing interface definition has more members
                            return;
                        }
                        //  remove the previous parsed  one so the new  one will be used.
                        //  may need to warn the used... the project may need/use namespaceing if they are really different object types with the same name
                        parsedModule.Interfaces.Remove(existing);
                    }
                }
                parsedModule.Interfaces.Add(parsedInterface);
            }
            else if (child is ClassDeclaration classDeclaration)
            {
#if DEBUG
                var nmt = child.GetView();
                Console.WriteLine(nmt);
#endif
                var parsedClass = classDeclaration.ParseInterfaceOrClass(parsedModule, JSModuleNamespace);
                var existing = Interfaces.FirstOrDefault(o => o.JSNameSpaceName == parsedClass.JSNameSpaceName);
                if (existing != null)
                {
                    if (existing.IsClass)
                    {
                        var existingMemberCount = existing.Methods.Count + existing.Properties.Count;
                        var parsedMemberCount = parsedClass.Methods.Count + parsedClass.Properties.Count;
                        if (existingMemberCount > parsedMemberCount)
                        {
                            return;
                        }
                        //  remove the previous parsed  one so the new  one will be used.
                        //  may need to warn the used... the project may need/use namespaceing if they are really different object types with the same name
                        parsedModule.Interfaces.Remove(existing);
                    }
                    else
                    {
                        // class definition trumps interface
                        parsedModule.Interfaces.Remove(existing);
                    }
                }
                parsedModule.Interfaces.Add(parsedClass);
                //sb.AppendLine(interfaceStr);
                var nmt2 = true;
            }
            else if (child is EnumDeclaration enumDeclaration)
            {
                var m = new ParsedEnum();
                m.SourceText = child.GetTextSafe();
                m.Name = enumDeclaration.IdentifierStr;
                foreach (var c in enumDeclaration.OfKind<EnumMember>())
                {
                    var enuMentry = new ParsedEnumEntry();
                    enuMentry.Name = c.IdentifierStr;

                    var valueNode = c.Children.First(o => o.Kind != SyntaxKind.Identifier);
                    switch (valueNode.Kind)
                    {
                        case SyntaxKind.StringLiteral:
                            enuMentry.Value = valueNode.GetTextSafe();
                            m.IsString = true;
                            break;
                        case SyntaxKind.NumericLiteral:
                            enuMentry.Value = valueNode.GetTextSafe();
                            m.IsNumeric = true;
                            break;
                        default:
                            var nmt22 = true;
                            break;
                    }
                    m.Values.Add(enuMentry);
                }
                parsedModule.Enums.Add(m);
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
                    var importName = importDeclaration.Children.First().GetTextSafe();
                    var nmt = true;
                }
                else if (importDeclaration.Children.Count == 2)
                {
                    try
                    {
                        var importClause = importDeclaration.Children[0].IdentifierStr;
                        var strLit = (importDeclaration.Children[1] as StringLiteral)!.Text;
                        var scriptPath = IOPath.GetFullPath(IOPath.Combine(fileDir, strLit));
                        var filePath = IOPath.GetFullPath(scriptPath);
                        await ParseTypeScriptDefinitionsFile(filePath);
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
        async Task ParseNodeChildren(ParsedModule parsedModule, List<Node> children)
        {
            foreach (var child in children)
            {
                await ParseNode(parsedModule, child);
            }
        }
        string GetInterfaceName(Node x)
        {
            var name = x.IdentifierStr;
            if (string.IsNullOrEmpty(name))
            {
                name = x.GetTextSafe();
            }
            if (string.IsNullOrEmpty(name) && x.First is LiteralExpression literal)
            {
                name = x.First.GetTextSafe();
            }
            if (string.IsNullOrEmpty(name))
            {
                var bbb = false;
            }
            return name;
        }
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
                    typeName = ch.GetTextSafe();
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
                    var tmpTxt = ch.GetTextSafe();
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
                    typeName = ch.GetTextSafe();
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
        string GetCSProjStr()
        {
            return $@"
<Project Sdk=""Microsoft.NET.Sdk.Razor"">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
		<Version>0.0.1</Version>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""8.0.19"" />
		<PackageReference Include=""System.Text.Json"" Version=""8.0.6"" />
		<PackageReference Include=""SpawnDev.BlazorJS"" Version=""2.30.0-preview.1"" />
	</ItemGroup>

	<ItemGroup>
		<SupportedPlatform Include=""browser"" />
	</ItemGroup>

</Project>
".Trim();
        }
    }

}
