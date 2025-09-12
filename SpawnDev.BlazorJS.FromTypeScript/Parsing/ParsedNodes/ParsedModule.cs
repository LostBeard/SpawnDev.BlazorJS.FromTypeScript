namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedModule : Parsed
    {
        //public string ProjectDir { get; set; } = "";
        //public string DestDir { get; set; } = "";

        public string ProjectNamespace { get; set; }
        public string ModuleNamespaceSub { get; set; }
        public string ModuleNamespace => $"{ProjectNamespace}.{ModuleNamespaceSub}".Trim('.');
        public string ProjectPath { get; set; }
        public string DestDir { get; set; } = "";
        public string DestFile { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public string Name { get; set; } = "";
        public List<ParsedConstant> Constants { get; set; } = new List<ParsedConstant>();
        public List<ParsedInterfaceOrClass> Interfaces { get; set; } = new List<ParsedInterfaceOrClass>();
        public List<ParsedEnum> Enums { get; set; } = new List<ParsedEnum>();
        public List<ParsedTypeAlias> TypeAliases { get; set; } = new List<ParsedTypeAlias>();
    }
}
