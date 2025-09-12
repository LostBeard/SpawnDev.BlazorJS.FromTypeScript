namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedEnum : Parsed
    {
        public string Name { get; set; } = "";
        public List<ParsedEnumEntry> Values { get; set; } = new List<ParsedEnumEntry>();
    }
}
