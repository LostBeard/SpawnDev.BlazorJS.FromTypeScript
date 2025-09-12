namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedInterfaceOrClass : Parsed
    {
        public bool IsAbstract { get; set; }
        public bool IsClass { get; set; }
        public string ProjectPath { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> Extends { get; set; }
        public List<string> TypeParameters { get; set; }
        //public List<ParsedConstructor> Constructors { get; set; } = new List<ParsedConstructor>();
        public List<ParsedConstant> Constants { get; set; } = new List<ParsedConstant>();
        public List<ParsedProperty> Properties { get; set; } = new List<ParsedProperty>();
        public List<ParsedMethod> Methods { get; set; } = new List<ParsedMethod>();
    }
}
