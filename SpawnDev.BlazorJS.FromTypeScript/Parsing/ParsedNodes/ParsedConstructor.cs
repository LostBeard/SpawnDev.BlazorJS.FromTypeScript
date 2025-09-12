namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedConstructor : Parsed
    {
        public string Name { get; set; } = "";
        public List<ParsedMethodParameter> Parameters { get; set; } = new List<ParsedMethodParameter>();
    }
}
