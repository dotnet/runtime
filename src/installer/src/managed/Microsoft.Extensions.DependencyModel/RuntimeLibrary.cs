// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeLibrary : Library
    {
        public RuntimeLibrary(string type,
            string name,
            string version,
            string hash,
            IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups,
            IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : this(type,
                  name,
                  version,
                  hash,
                  runtimeAssemblyGroups,
                  nativeLibraryGroups,
                  resourceAssemblies,
                  dependencies,
                  serviceable,
                  path: null,
                  hashPath: null)
        {
        }

        public RuntimeLibrary(string type,
            string name,
            string version,
            string hash,
            IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups,
            IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string path,
            string hashPath)
            : this(type,
                  name,
                  version,
                  hash,
                  runtimeAssemblyGroups,
                  nativeLibraryGroups,
                  resourceAssemblies,
                  dependencies,
                  serviceable,
                  path,
                  hashPath,
                  runtimeStoreManifestName : null)
         {
         }

        public RuntimeLibrary(string type,
            string name,
            string version,
            string hash,
            IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups,
            IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string path,
            string hashPath,
            string runtimeStoreManifestName)
            : base(type,
                  name,
                  version,
                  hash,
                  dependencies,
                  serviceable,
                  path,
                  hashPath,
                  runtimeStoreManifestName)
        {
            if (runtimeAssemblyGroups == null)
            {
                throw new ArgumentNullException(nameof(runtimeAssemblyGroups));
            }
            if (nativeLibraryGroups == null)
            {
                throw new ArgumentNullException(nameof(nativeLibraryGroups));
            }
            if (resourceAssemblies == null)
            {
                throw new ArgumentNullException(nameof(resourceAssemblies));
            }
            RuntimeAssemblyGroups = runtimeAssemblyGroups;
            ResourceAssemblies = resourceAssemblies.ToArray();
            NativeLibraryGroups = nativeLibraryGroups;
        }

        public IReadOnlyList<RuntimeAssetGroup> RuntimeAssemblyGroups { get; }

        public IReadOnlyList<RuntimeAssetGroup> NativeLibraryGroups { get; }

        public IReadOnlyList<ResourceAssembly> ResourceAssemblies { get; }
    }
}