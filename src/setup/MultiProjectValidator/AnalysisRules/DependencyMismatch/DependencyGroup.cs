using System;
using System.Collections.Generic;

namespace ProjectSanity.AnalysisRules.DependencyMismatch
{
    internal class DependencyGroup
    {
        public static DependencyGroup CreateWithEntry(DependencyInfo dependencyInfo)
        {
            var dependencyGroup = new DependencyGroup
            {
                DependencyName = dependencyInfo.Name,
                VersionDependencyInfoMap = new Dictionary<string, List<DependencyInfo>>()
            };

            dependencyGroup.AddEntry(dependencyInfo);

            return dependencyGroup;
        }

        public string DependencyName { get; private set; }
        public Dictionary<string, List<DependencyInfo>> VersionDependencyInfoMap { get; private set; }

        public bool HasConflict
        {
            get
            {
                return VersionDependencyInfoMap.Count > 1;
            }
        }

        public void AddEntry(DependencyInfo dependencyInfo)
        {
            if (!dependencyInfo.Name.Equals(DependencyName, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Added dependency does not match group");
            }

            if (VersionDependencyInfoMap.ContainsKey(dependencyInfo.Version))
            {
                VersionDependencyInfoMap[dependencyInfo.Version].Add(dependencyInfo);
            }
            else
            {
                VersionDependencyInfoMap[dependencyInfo.Version] = new List<DependencyInfo>()
                {
                    dependencyInfo
                };
            }
        }

    }
}
