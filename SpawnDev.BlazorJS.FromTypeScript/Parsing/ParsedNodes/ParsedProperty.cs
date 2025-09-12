namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedProperty : Parsed
    {
        public bool IsNullable { get; set; }
        public bool IsStatic { get; set; }
        public bool IsOverride { get; set; }
        public string Name { get; set; } = "";
        public bool HasGet { get; set; }
        public bool HasSet { get; set; }
        public bool ReadOnly { get; set; }
        public bool ShouldHaveSetter => HasSet || (!HasGet && !ReadOnly);
        public ParsedType Type { get; set; }
    }
}
