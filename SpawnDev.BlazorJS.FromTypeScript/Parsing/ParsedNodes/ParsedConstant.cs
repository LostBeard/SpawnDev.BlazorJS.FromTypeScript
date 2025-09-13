using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedConstant : Parsed
    {
        public string Value { get; set; } = "";
        public string Name { get; set; } = "";
        public ParsedType Type { get; set; }
        public string CSReturnType
        {
            get
            {
                var ret = Type?.Name.Trim() ?? "";
                if (ret.StartsWith("Promise<"))
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                else if (ret == "Promise")
                {
                    ret = Regex.Replace(ret, "^Promise", "Task");
                }
                return ret;
            }
        }
    }
}
