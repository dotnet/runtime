using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Cli.Build
{
    public class BranchInfo
    {
        private static readonly string s_branchInfoFileName = "branchinfo.txt";

        private string _repoRoot;
        private string _branchInfoFile;

        public IDictionary<string, string> Entries { get; set; }

        public BranchInfo(string repoRoot)
        {
            _repoRoot = repoRoot;
            _branchInfoFile = Path.Combine(_repoRoot, s_branchInfoFileName);

            Entries = ReadBranchInfo(_branchInfoFile);
        }

        private IDictionary<string, string> ReadBranchInfo(string path)
        {
            var lines = File.ReadAllLines(path);
            var dict = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (!line.Trim().StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                {
                    var splat = line.Split(new[] { '=' }, 2);
                    dict[splat[0]] = splat[1];
                }
            }
            return dict;
        }
    }
}
