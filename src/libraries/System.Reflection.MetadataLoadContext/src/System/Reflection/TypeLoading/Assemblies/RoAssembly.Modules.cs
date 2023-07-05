// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoAssembly
    {
        public sealed override Module? GetModule(string name) => GetRoModule(name);
        public sealed override Module[] GetModules(bool getResourceModules) => ComputeRoModules(getResourceModules).CloneArray<Module>();

#if NETCOREAPP
        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
#endif
        public sealed override FileStream? GetFile(string name)
        {
            Module? m = GetModule(name);
            if (m == null)
                return null;
            return new FileStream(m.FullyQualifiedName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

#if NETCOREAPP
        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
#endif
        public sealed override FileStream[] GetFiles(bool getResourceModules)
        {
            Module[] m = GetModules(getResourceModules);
            FileStream[] fs = new FileStream[m.Length];
            for (int i = 0; i < fs.Length; i++)
            {
                fs[i] = new FileStream(m[i].FullyQualifiedName, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return fs;
        }

        public sealed override Module[] GetLoadedModules(bool getResourceModules)
        {
            List<Module> modules = new List<Module>(_loadedModules.Length + 1)
            {
                GetRoManifestModule()
            };
            for (int i = 0; i < _loadedModules.Length; i++)
            {
                RoModule? module = Volatile.Read(ref _loadedModules[i]);
                if (module != null && (getResourceModules || !module.IsResource()))
                    modules.Add(module);
            }
            return modules.ToArray();
        }

        public abstract override event ModuleResolveEventHandler? ModuleResolve;

        internal RoModule? GetRoModule(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (!TryGetAssemblyFileInfo(name, includeManifestModule: true, out AssemblyFileInfo afi))
                return null;

            return GetRoModule(afi);
        }

        private RoModule GetRoModule(in AssemblyFileInfo afi)
        {
            if (afi.RowIndex == 0)
                return GetRoManifestModule();

            int loadedModulesIndex = afi.RowIndex - 1;
            string moduleName = afi.Name;
            RoModule? prior = Volatile.Read(ref _loadedModules[loadedModulesIndex]);
            if (prior != null)
                return prior;

            RoModule newModule = LoadModule(moduleName, afi.ContainsMetadata);
            return Interlocked.CompareExchange(ref _loadedModules[loadedModulesIndex], newModule, null) ?? newModule;
        }

        internal RoModule[] ComputeRoModules(bool getResourceModules)
        {
            List<RoModule> modules = new List<RoModule>(_loadedModules.Length + 1);
            foreach (AssemblyFileInfo afi in GetAssemblyFileInfosFromManifest(includeManifestModule: true, includeResourceModules: getResourceModules))
            {
                RoModule module = GetRoModule(afi);
                modules.Add(module);
            }
            return modules.ToArray();
        }

#pragma warning disable CS8995 // Nullable type is null-checked and will throw if null.
        public sealed override Module LoadModule(string moduleName, byte[]? rawModule, byte[]? rawSymbolStore)
#pragma warning restore CS8995
        {
            if (moduleName is null)
                throw new ArgumentNullException(nameof(moduleName));
            if (rawModule is null)
                throw new ArgumentNullException(nameof(rawModule));
            if (!TryGetAssemblyFileInfo(moduleName, includeManifestModule: false, out AssemblyFileInfo afi))
                throw new ArgumentException(SR.Format(SR.SpecifiedFileNameInvalid, moduleName)); // Name not in manifest.

            Debug.Assert(afi.RowIndex != 0); // Since we excluded the manifest module from the search.

            int loadedModuleIndex = afi.RowIndex - 1;
            RoModule newModule = CreateModule(new MemoryStream(rawModule), afi.ContainsMetadata);
            Interlocked.CompareExchange(ref _loadedModules[loadedModuleIndex], newModule, null);

            // Somewhat shockingly, the compatible behavior is to return the newly created module always rather than the Module that
            // actually won the race to be resolved!
            return newModule;
        }

        private bool TryGetAssemblyFileInfo(string name, bool includeManifestModule, out AssemblyFileInfo afi)
        {
            foreach (AssemblyFileInfo candidate in GetAssemblyFileInfosFromManifest(includeManifestModule: includeManifestModule, includeResourceModules: true))
            {
                if (name.Equals(candidate.Name, StringComparison.OrdinalIgnoreCase))
                {
                    afi = candidate;
                    return true;
                }
            }

            afi = default;
            return false;
        }

        protected abstract RoModule LoadModule(string moduleName, bool containsMetadata);
        protected abstract RoModule CreateModule(Stream peStream, bool containsMetadata);
        protected abstract IEnumerable<AssemblyFileInfo> GetAssemblyFileInfosFromManifest(bool includeManifestModule, bool includeResourceModules);
    }
}
