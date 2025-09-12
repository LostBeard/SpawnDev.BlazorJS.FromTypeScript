namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedMethod : Parsed
    {
        public bool IsConstructor { get; set; }
        public bool IsStatic { get; set; }
        public string Name { get; set; } = "";
        public ParsedType ReturnType { get; set; }
        public List<ParsedMethodParameter> Parameters { get; set; } = new List<ParsedMethodParameter>();
    }
}
