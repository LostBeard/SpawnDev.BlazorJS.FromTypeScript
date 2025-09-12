using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing.ParsedNodes
{
    public class ParsedType : Parsed
    {
        public bool IsNullable { get; set; }
        public string Name { get; set; } = "";
        public bool IsTypedType => TypeArguments.Any();
        public bool IsPromise => Name.StartsWith("Promise<") || Name == "Promise";
        public string? AsTask
        {
            get
            {
                if (!IsPromise) return null;
                var args = TypeArguments;
                return args.Any() ? $"Task<{string.Join(", ", args)}>" : "Task";
            }
        }
        //public string TypeArgumentsDelimited => string.Join(", ", TypeArguments);
        public string GetTypeArgumentsDelimited(string thisReplacement) => string.Join(", ", TypeArguments.Select(o => o == "this" ? thisReplacement : o));
        public string[] TypeArguments
        {
            get
            {
                // VERY basic type splitting. will fail if any types are also Typed types (regex shouldwork better...)
                var p0 = Name.IndexOf("<");
                if (p0 > -1)
                {
                    var p1 = Name.LastIndexOf(">");
                    var sub = Name.Substring(p0 + 1, p1 - p0 - 1);
                    var ret = sub.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
                    ret.Remove("void");
                    return ret.ToArray();
                }
                return Array.Empty<string>();

            }
        }
    }
}
