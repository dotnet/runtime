// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.Binder
{
    // System.Reflection.TypeLoading.AssemblyNameData
    internal readonly unsafe struct AssemblyNameData
    {
        public readonly void* Name;
        public readonly void* Culture;

        public readonly byte* PublicKeyOrToken;
        public readonly int PublicKeyOrTokenLength;

        public readonly int MajorVersion;
        public readonly int MinorVersion;
        public readonly int BuildNumber;
        public readonly int RevisionNumber;

        public readonly PEKind ProcessorArchitecture;
        public readonly System.Reflection.AssemblyContentType ContentType;

        public readonly AssemblyIdentityFlags IdentityFlags;
    }

    internal abstract class AssemblyBinder
    {
        public int BindAssemblyByName(in AssemblyNameData pAssemblyNameData, out Assembly? assembly)
        {
            return BindUsingAssemblyName(new AssemblyName(pAssemblyNameData), out assembly);
        }

        public abstract int BindUsingPEImage(IntPtr pPEImage, bool excludeAppPaths, out Assembly? assembly);

        public abstract int BindUsingAssemblyName(AssemblyName assemblyName, out Assembly? assembly);

        /// <summary>
        /// Get LoaderAllocator for binders that contain it. For other binders, return NULL.
        /// </summary>
        public abstract System.Reflection.LoaderAllocator? GetLoaderAllocator();

        public abstract bool IsDefault { get; }

        public ApplicationContext AppContext { get; } = new ApplicationContext();

        // A GC handle to the managed AssemblyLoadContext.
        // It is a long weak handle for collectible AssemblyLoadContexts and strong handle for non-collectible ones.
        public GCHandle ManagedAssemblyLoadContext { get; set; }

        // NativeImage* LoadNativeImage(Module* componentModule, LPCUTF8 nativeImageName);

        public void AddLoadedAssembly(IVMAssembly loadedAssembly)
        {
            // BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
            // TODO: is the lock shared outside this type?
            lock (_loadLock)
            {
                _loadedAssemblies.Add(loadedAssembly);

                // #ifdef FEATURE_READYTORUN
                DeclareLoadedAssembly(loadedAssembly);
                // #endif // FEATURE_READYTORUN
            }
        }

        public string GetNameForDiagnostics() => IsDefault ? "Default" : GetNameForDiagnosticsFromManagedALC(ManagedAssemblyLoadContext);

        public static string GetNameForDiagnosticsFromManagedALC(GCHandle managedALC)
        {
            if (managedALC.Target == GCHandle.FromIntPtr(AssemblyLoadContext.GetDefaultAssemblyBinder()).Target)
            {
                return "Default";
            }

            var alc = managedALC.Target as AssemblyLoadContext;
            Debug.Assert(alc != null);
            return alc.ToString();
        }

        // static void GetNameForDiagnosticsFromSpec(AssemblySpec* spec, /*out*/ SString& alcName);

        //# ifdef FEATURE_READYTORUN

        private static void MvidMismatchFatalError(Guid mvidActual, Guid mvidExpected, string simpleName, bool compositeComponent, string assemblyRequirementName)
        {
            string message;

            if (compositeComponent)
            {
                message = $"MVID mismatch between loaded assembly '{simpleName}' (MVID = {mvidActual}) and an assembly with the same simple name embedded in the native image '{assemblyRequirementName}' (MVID = {mvidExpected})";
            }
            else
            {
                message = $"MVID mismatch between loaded assembly '{simpleName}' (MVID = {mvidActual}) and version of assembly '{simpleName}' expected by assembly '{assemblyRequirementName}' (MVID = {mvidExpected})";
            }

            Environment.FailFast(message);
        }

        // Must be called under the LoadLock
        public void DeclareDependencyOnMvid(string simpleName, Guid mvid, bool compositeComponent, string imageName)
        {
            // If the table is empty, then we didn't fill it with all the loaded assemblies as they were loaded. Record this detail, and fix after adding the dependency
            bool addAllLoadedModules = false;
            if (_assemblySimpleNameMvidCheckHash.Count == 0)
            {
                addAllLoadedModules = true;
            }

            ref SimpleNameToExpectedMVIDAndRequiringAssembly entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_assemblySimpleNameMvidCheckHash, simpleName, out bool found);
            if (!found)
            {
                entry = new SimpleNameToExpectedMVIDAndRequiringAssembly
                {
                    Mvid = mvid,
                    CompositeComponent = compositeComponent,
                    AssemblyRequirementName = imageName
                };
            }
            else
            {
                // Elem already exists. Determine if the existing elem is another one with the same mvid, in which case just record that a dependency is in play.
                // If the existing elem has a different mvid, fail.
                if (entry.Mvid == mvid)
                {
                    // Mvid matches exactly.
                    if (entry.AssemblyRequirementName == null)
                    {
                        entry.AssemblyRequirementName = imageName;
                        entry.CompositeComponent = compositeComponent;
                    }
                    else
                    {
                        MvidMismatchFatalError(entry.Mvid, mvid, simpleName, compositeComponent, imageName);
                    }
                }
            }

            if (addAllLoadedModules)
            {
                foreach (IVMAssembly assembly in _loadedAssemblies)
                {
                    DeclareLoadedAssembly(assembly);
                }
            }
        }

        // Must be called under the LoadLock
        private void DeclareLoadedAssembly(IVMAssembly loadedAssembly)
        {
            // If table is empty, then no mvid dependencies have been declared, so we don't need to record this information
            if (_assemblySimpleNameMvidCheckHash.Count == 0)
            {
                return;
            }

            loadedAssembly.GetMDImport().GetScopeProps(out Guid mvid);
            string simpleName = loadedAssembly.SimpleName;

            ref SimpleNameToExpectedMVIDAndRequiringAssembly entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_assemblySimpleNameMvidCheckHash, simpleName, out bool found);
            if (!found)
            {
                entry = new SimpleNameToExpectedMVIDAndRequiringAssembly
                {
                    Mvid = mvid,
                    CompositeComponent = false,
                    AssemblyRequirementName = null
                };
            }
            else
            {
                // Elem already exists. Determine if the existing elem is another one with the same mvid, in which case do nothing. Everything is fine here.
                // If the existing elem has a different mvid, but isn't a dependency on exact mvid elem, then set the mvid to all 0.
                // If the existing elem has a different mvid, and is a dependency on exact mvid elem, then we've hit a fatal error.
                if (entry.Mvid == mvid)
                {
                    // Mvid matches exactly.
                }
                else if (entry.AssemblyRequirementName == null)
                {
                    // Another loaded assembly, set the stored Mvid to all zeroes to indicate that it isn't a unique mvid
                    entry.Mvid = Guid.Empty;
                }
                else
                {
                    MvidMismatchFatalError(entry.Mvid, mvid, simpleName, entry.CompositeComponent, entry.AssemblyRequirementName);
                }
            }
        }
        //#endif // FEATURE_READYTORUN

        private struct SimpleNameToExpectedMVIDAndRequiringAssembly
        {
            // When an assembly is loaded, this Mvid value will be set to the mvid of the assembly. If there are multiple assemblies
            // with different mvid's loaded with the same simple name, then the Mvid value will be set to all zeroes.
            public Guid Mvid;

            // If an assembly of this simple name is not yet loaded, but a depedency on an exact mvid is registered, then this field will
            // be filled in with the simple assembly name of the first assembly loaded with an mvid dependency.
            public string? AssemblyRequirementName;

            // To disambiguate between component images of a composite image and requirements from a non-composite --inputbubble assembly, use this bool
            public bool CompositeComponent;
        }

        // Use a case senstive comparison here even though
        // assembly name matching should be case insensitive. Case insensitive
        // comparisons are slow and have throwing scenarios, and this hash table
        // provides a best-effort match to prevent problems, not perfection
        private readonly Dictionary<string, SimpleNameToExpectedMVIDAndRequiringAssembly> _assemblySimpleNameMvidCheckHash = new Dictionary<string, SimpleNameToExpectedMVIDAndRequiringAssembly>();
        private readonly List<IVMAssembly> _loadedAssemblies = new List<IVMAssembly>();

        private static readonly object _loadLock = new object();
    }

    // Foo
    internal interface IVMAssembly // coreclr/vm/assembly
    {
        public System.Reflection.MetadataImport GetMDImport();

        public string SimpleName { get; }

        public System.Reflection.RuntimeAssembly GetExposedObject();

        public IntPtr GetPEAssembly_GetPEImage();

        public unsafe void GetModule_SetSymbolBytes(byte* ptrSymbolArray, int cbSymbolArrayLength);
    }
}
