﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyModel;
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

        public static IEnumerable<RuntimeFile> GetDefaultRuntimeFileAssets(this IEnumerable<RuntimeAssetGroup> self) => GetRuntimeFiles(self, string.Empty);
        public static IEnumerable<RuntimeFile> GetRuntimeFileAssets(this IEnumerable<RuntimeAssetGroup> self, string runtime)
        {
            if (string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            return GetRuntimeFiles(self, runtime);
        }

        private static IEnumerable<RuntimeFile> GetRuntimeFiles(IEnumerable<RuntimeAssetGroup> groups, string runtime)
        {
            return groups
                .Where(a => string.Equals(a.Runtime, runtime, StringComparison.Ordinal))
                .SelectMany(a => a.RuntimeFiles);
        }
    }
}
