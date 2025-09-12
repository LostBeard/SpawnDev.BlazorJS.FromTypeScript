namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedMethodParameter : Parsed
    {
        public bool IsParamsArray { get; set; }
        public bool Optional { get; set; }
        public bool HasDefaultValue { get; set; }
        public string DefaultValue { get; set; } = "";
        public string Name { get; set; } = "";
        public ParsedType Type { get; set; }
    }
}
