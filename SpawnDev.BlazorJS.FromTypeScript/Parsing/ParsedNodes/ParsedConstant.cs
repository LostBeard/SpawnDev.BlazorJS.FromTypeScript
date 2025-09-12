namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedConstant : Parsed
    {
        public string Value { get; set; } = "";
        public string Name { get; set; } = "";
        public ParsedType Type { get; set; }
    }
}
