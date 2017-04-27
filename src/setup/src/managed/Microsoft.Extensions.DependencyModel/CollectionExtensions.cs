﻿using Microsoft.Extensions.DependencyModel;
using System.Linq;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static RuntimeAssetGroup GetDefaultGroup(this IEnumerable<RuntimeAssetGroup> self) => GetGroup(self, string.Empty);
        public static RuntimeAssetGroup GetRuntimeGroup(this IEnumerable<RuntimeAssetGroup> self, string runtime)
        {
            if(string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            return GetGroup(self, runtime);
        }

        private static RuntimeAssetGroup GetGroup(IEnumerable<RuntimeAssetGroup> groups, string runtime)
        {
            return groups.FirstOrDefault(g => g.Runtime == runtime);
        }

        public static IEnumerable<string> GetDefaultAssets(this IEnumerable<RuntimeAssetGroup> self) => GetAssets(self, string.Empty);
        public static IEnumerable<string> GetRuntimeAssets(this IEnumerable<RuntimeAssetGroup> self, string runtime)
        {
            if(string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            return GetAssets(self, runtime);
        }

        private static IEnumerable<string> GetAssets(IEnumerable<RuntimeAssetGroup> groups, string runtime)
        {
            return groups
                .Where(a => string.Equals(a.Runtime, runtime, StringComparison.Ordinal))
                .SelectMany(a => a.AssetPaths);
        }
    }
}
