// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyModel
{
    public static class DependencyContextExtensions
    {
        private const string NativeImageSufix = ".ni";

        public static IEnumerable<string> GetDefaultNativeAssets(this DependencyContext self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetDefaultNativeAssets(self));
        }

        public static IEnumerable<RuntimeFile> GetDefaultNativeRuntimeFileAssets(this DependencyContext self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetDefaultNativeRuntimeFileAssets(self));
        }

        public static IEnumerable<string> GetRuntimeNativeAssets(this DependencyContext self, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetRuntimeNativeAssets(self, runtimeIdentifier));
        }

        public static IEnumerable<RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this DependencyContext self, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetRuntimeNativeRuntimeFileAssets(self, runtimeIdentifier));
        }

        public static IEnumerable<string> GetDefaultNativeAssets(this RuntimeLibrary self, DependencyContext context)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return ResolveAssets(context, string.Empty, self.NativeLibraryGroups);
        }

        public static IEnumerable<RuntimeFile> GetDefaultNativeRuntimeFileAssets(this RuntimeLibrary self, DependencyContext context)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return ResolveRuntimeFiles(context, string.Empty, self.NativeLibraryGroups);
        }

        public static IEnumerable<string> GetRuntimeNativeAssets(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return ResolveAssets(context, runtimeIdentifier, self.NativeLibraryGroups);
        }

        public static IEnumerable<RuntimeFile> GetRuntimeNativeRuntimeFileAssets(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return ResolveRuntimeFiles(context, runtimeIdentifier, self.NativeLibraryGroups);
        }

        public static IEnumerable<AssemblyName> GetDefaultAssemblyNames(this DependencyContext self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetDefaultAssemblyNames(self));
        }

        public static IEnumerable<AssemblyName> GetRuntimeAssemblyNames(this DependencyContext self, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return self.RuntimeLibraries.SelectMany(library => library.GetRuntimeAssemblyNames(self, runtimeIdentifier));
        }

        public static IEnumerable<AssemblyName> GetDefaultAssemblyNames(this RuntimeLibrary self, DependencyContext context)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return ResolveAssets(context, string.Empty, self.RuntimeAssemblyGroups).Select(GetAssemblyName);
        }

        public static IEnumerable<AssemblyName> GetRuntimeAssemblyNames(this RuntimeLibrary self, DependencyContext context, string runtimeIdentifier)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }
            return ResolveAssets(context, runtimeIdentifier, self.RuntimeAssemblyGroups).Select(GetAssemblyName);
        }

        private static AssemblyName GetAssemblyName(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (name == null)
            {
                throw new ArgumentException($"Provided path has empty file name '{assetPath}'", nameof(assetPath));
            }

            if (name.EndsWith(NativeImageSufix))
            {
                name = name.Substring(0, name.Length - NativeImageSufix.Length);
            }

            return new AssemblyName(name);
        }

        private static IEnumerable<string> ResolveAssets(
            DependencyContext context,
            string runtimeIdentifier,
            IEnumerable<RuntimeAssetGroup> assets)
        {
            RuntimeFallbacks? fallbacks = context.RuntimeGraph.FirstOrDefault(f => f.Runtime == runtimeIdentifier);
            IEnumerable<string?> rids = Enumerable.Concat(new[] { runtimeIdentifier }, fallbacks?.Fallbacks ?? Enumerable.Empty<string?>());
            return SelectAssets(rids, assets);
        }

        private static IEnumerable<RuntimeFile> ResolveRuntimeFiles(
            DependencyContext context,
            string runtimeIdentifier,
            IEnumerable<RuntimeAssetGroup> assets)
        {
            RuntimeFallbacks? fallbacks = context.RuntimeGraph.FirstOrDefault(f => f.Runtime == runtimeIdentifier);
            IEnumerable<string?> rids = Enumerable.Concat(new[] { runtimeIdentifier }, fallbacks?.Fallbacks ?? Enumerable.Empty<string?>());
            return SelectRuntimeFiles(rids, assets);
        }

        private static IEnumerable<string> SelectAssets(IEnumerable<string?> rids, IEnumerable<RuntimeAssetGroup> groups)
        {
            foreach (string? rid in rids)
            {
                RuntimeAssetGroup? group = groups.FirstOrDefault(g => g.Runtime == rid);
                if (group != null)
                {
                    return group.AssetPaths;
                }
            }

            // Return the RID-agnostic group
            return groups.GetDefaultAssets();
        }

        private static IEnumerable<RuntimeFile> SelectRuntimeFiles(IEnumerable<string?> rids, IEnumerable<RuntimeAssetGroup> groups)
        {
            foreach (string? rid in rids)
            {
                RuntimeAssetGroup? group = groups.FirstOrDefault(g => g.Runtime == rid);
                if (group != null)
                {
                    return group.RuntimeFiles;
                }
            }

            // Return the RID-agnostic group
            return groups.GetDefaultRuntimeFileAssets();
        }
    }
}
