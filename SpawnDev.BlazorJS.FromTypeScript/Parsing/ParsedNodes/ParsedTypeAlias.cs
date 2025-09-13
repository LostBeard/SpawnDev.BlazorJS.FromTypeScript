namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedTypeAlias : Parsed
    {
        public string Name { get; set; } = "";
        public string AliasFor { get; set; } = "";
        public string[] UnionTypes { get; set; } = Array.Empty<string>();
    }
}
