using System.Text.RegularExpressions;

namespace Melanzana.CodeSign
{
    public class ResourceRule
    {
        /// <summary>Resource may be absent at runtime</summary>
        public bool IsOptional { get; init; }

        /// <summary>Resource is not sealed even if present in the bundle</summary> 
        public bool IsOmitted { get; init; }

        /// <summary>Recursively signed code</summary>
        public bool IsNested { get; init; }

        /// <summary>Precedence weight of the rule (rules with higher weight override rules with lower weight)</summary>
        public uint Weight { get; init; } = 1;

        /// <summary>Regular expression pattern for match full path in the bundle</summary>
        public string Pattern { get; private set; }

        private readonly Regex regex;

        public ResourceRule(string pattern)
        {
            Pattern = pattern;
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public bool IsMatch(string path) => regex.IsMatch(path);
    }
}
