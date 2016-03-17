using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public static class DependencyContextExtensions
    {
        private const string NativeImageSufix = ".ni";

        public static IEnumerable<string> GetDefaultNativeAssets(this DependencyContext self)
        {
            return ResolveAssets(self, string.Empty, l => l.NativeLibraryGroups);
        }

        public static IEnumerable<string> GetRuntimeNativeAssets(this DependencyContext self, string runtimeIdentifier)
        {
            return ResolveAssets(self, runtimeIdentifier, l => l.NativeLibraryGroups);
        }

        public static IEnumerable<string> GetDefaultAssemblyNames(this DependencyContext self)
        {
            return ResolveAssets(self, string.Empty, l => l.RuntimeAssemblyGroups).Select(GetAssemblyName);
        }

        public static IEnumerable<string> GetRuntimeAssemblyNames(this DependencyContext self, string runtimeIdentifier)
        {
            return ResolveAssets(self, runtimeIdentifier, l => l.RuntimeAssemblyGroups).Select(GetAssemblyName);
        }

        private static string GetAssemblyName(string assetPath)
        {
            var name = Path.GetFileNameWithoutExtension(assetPath);
            if (name == null)
            {
                throw new ArgumentException($"Provided path has empty file name '{assetPath}'", nameof(assetPath));
            }

            if (name.EndsWith(NativeImageSufix))
            {
                name = name.Substring(0, name.Length - NativeImageSufix.Length);
            }

            return name;
        }

        private static IEnumerable<string> ResolveAssets(DependencyContext context, string runtimeIdentifier, Func<RuntimeLibrary, IEnumerable<RuntimeAssetGroup>> groupSelector)
        {
            var fallbacks = context.RuntimeGraph.FirstOrDefault(f => f.Runtime == runtimeIdentifier);
            var rids = Enumerable.Concat(new[] { runtimeIdentifier }, fallbacks?.Fallbacks ?? Enumerable.Empty<string>());
            return context.RuntimeLibraries.SelectMany(l => SelectAssets(rids, groupSelector(l)));
        }

        private static IEnumerable<string> SelectAssets(IEnumerable<string> rids, IEnumerable<RuntimeAssetGroup> groups)
        {
            foreach (var rid in rids)
            {
                var group = groups.FirstOrDefault(g => g.Runtime == rid);
                if (group != null)
                {
                    return group.AssetPaths;
                }
            }

            // Return the RID-agnostic group
            return groups.GetDefaultAssets();
        }

    }
}
