namespace Melanzana.CodeSign
{
    public class ResourceBuilder
    {
        private readonly HashSet<string> exclusions = new();
        private readonly List<ResourceRule> rules = new();

        public ResourceBuilder()
        {
        }

        public void AddRule(ResourceRule rule)
        {
            rules.Add(rule);
        }

        public void AddExclusion(string path)
        {
            exclusions.Add(path);
        }

        public ResourceRule? FindRule(string path)
        {
            ResourceRule? bestRule = null;

            foreach (var candidateRule in rules)
            {
                if (bestRule == null || candidateRule.Weight >= bestRule.Weight)
                {
                    if (candidateRule.IsMatch(path))
                    {
                        if (bestRule == null || candidateRule.Weight > bestRule.Weight)
                        {
                            bestRule = candidateRule;
                        }
                        else if (bestRule.Weight == candidateRule.Weight && !bestRule.IsOmitted && candidateRule.IsOmitted)
                        {
                            // In case of matching weight compare by IsOmitted. Apple tools do this only
                            // for watchOS but we opt to do it everywhere.
                            bestRule = candidateRule;
                        }
                    }
                }
            }

            return bestRule;
        }

        private IEnumerable<(string Path, FileSystemInfo Info, ResourceRule Rule)> Scan(string path, string relativePath)
        {
            var rootDirectoryInfo = new DirectoryInfo(path);

            foreach (var fsEntryInfo in rootDirectoryInfo.EnumerateFileSystemInfos().OrderBy(i => i.Name, StringComparer.Ordinal))
            {
                string fsEntryPath = Path.Combine(relativePath, fsEntryInfo.Name);

                if (exclusions.Contains(fsEntryPath, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rule = FindRule(fsEntryPath);

                // TODO: Check symlinks (should be treated as files)
                if (fsEntryInfo is FileInfo)
                {
                    if (rule != null && !rule.IsOmitted)
                    {
                        yield return (fsEntryPath, fsEntryInfo, rule);
                    }
                }
                else if (fsEntryInfo is DirectoryInfo)
                {
                    // Directories cannot be matched with omit/optional rules but
                    // they can be matched with a rule for nested bundle.
                    if (rule != null && rule.IsNested && fsEntryInfo.Extension.Length > 0)
                    {
                        yield return (fsEntryPath, fsEntryInfo, rule);
                    }
                    else
                    {
                        foreach (var nestedResult in Scan(fsEntryInfo.FullName, fsEntryPath))
                        {
                            yield return nestedResult;
                        }
                    }
                }
            }
        }

        public IEnumerable <(string Path, FileSystemInfo Info, ResourceRule Rule)> Scan(string bundlePath)
        {
            return Scan(bundlePath, "");
        }

        public IEnumerable<ResourceRule> Rules => rules;
    }
}
