// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            string? hash,
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
            string? hash,
            IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups,
            IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string? path,
            string? hashPath)
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


        /// <summary>
        /// Initializes a new <see cref="RuntimeLibrary"/>.
        /// </summary>
        /// <param name="type">The library's type.</param>
        /// <param name="name">The library's name.</param>
        /// <param name="version">The library's version.</param>
        /// <param name="hash">The library package's hash.</param>
        /// <param name="runtimeAssemblyGroups">The library's runtime assemblies.</param>
        /// <param name="nativeLibraryGroups">The library's native libraries.</param>
        /// <param name="resourceAssemblies">The library's resource assemblies.</param>
        /// <param name="dependencies">The library's dependencies.</param>
        /// <param name="serviceable">Whether the library is serviceable.</param>
        /// <param name="path">The library package's path.</param>
        /// <param name="hashPath">The library package's hash path.</param>
        /// <param name="runtimeStoreManifestName">The library's runtime store manifest name.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="type"/> argument is null.
        /// The <paramref name="name"/> argument is null.
        /// The <paramref name="version"/> argument is null.
        /// The <paramref name="runtimeAssemblyGroups"/> argument is null.
        /// The <paramref name="nativeLibraryGroups"/> argument is null.
        /// The <paramref name="resourceAssemblies"/> argument is null.
        /// The <paramref name="dependencies"/> argument is null.
        /// </exception>
        public RuntimeLibrary(string type,
            string name,
            string version,
            string? hash,
            IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups,
            IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string? path,
            string? hashPath,
            string? runtimeStoreManifestName)
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
            ThrowHelper.ThrowIfNull(runtimeAssemblyGroups);
            ThrowHelper.ThrowIfNull(nativeLibraryGroups);
            ThrowHelper.ThrowIfNull(resourceAssemblies);

            RuntimeAssemblyGroups = runtimeAssemblyGroups;
            ResourceAssemblies = resourceAssemblies.ToArray();
            NativeLibraryGroups = nativeLibraryGroups;
        }

        public IReadOnlyList<RuntimeAssetGroup> RuntimeAssemblyGroups { get; }

        public IReadOnlyList<RuntimeAssetGroup> NativeLibraryGroups { get; }

        public IReadOnlyList<ResourceAssembly> ResourceAssemblies { get; }
    }
}
