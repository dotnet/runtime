// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.Binder
{
    internal sealed class DefaultAssemblyBinder : AssemblyBinder
    {
        // TODO: AdHoc
        internal static DefaultAssemblyBinder Default { get; } = new DefaultAssemblyBinder();

        public override bool IsDefault => true;

        // Not supported by this binder
        public override System.Reflection.LoaderAllocator? GetLoaderAllocator() => null;

        // Helper functions
        private int BindAssemblyByNameWorker(AssemblyName assemblyName, out Assembly? coreCLRFoundAssembly, bool excludeAppPaths)
        {
            // CoreLib should be bound using BindToSystem
            Debug.Assert(!assemblyName.IsCoreLib);

            int hr = AssemblyBinderCommon.BindAssembly(this, assemblyName, excludeAppPaths, out coreCLRFoundAssembly);

            if (hr >= 0)
            {
                Debug.Assert(coreCLRFoundAssembly != null);
                coreCLRFoundAssembly.Binder = this;
            }

            return hr;
        }

        public override int BindUsingAssemblyName(AssemblyName assemblyName, out Assembly? assembly)
        {
            assembly = null;

            int hr = BindAssemblyByNameWorker(assemblyName, out Assembly? coreCLRFoundAssembly, excludeAppPaths: false);

            if (hr is HResults.E_FILENOTFOUND or AssemblyBinderCommon.FUSION_E_APP_DOMAIN_LOCKED or HResults.FUSION_E_REF_DEF_MISMATCH)
            {
                // If we are here, one of the following is possible:
                //
                // 1) The assembly has not been found in the current binder's application context (i.e. it has not already been loaded), OR
                // 2) An assembly with the same simple name was already loaded in the context of the current binder but we ran into a Ref/Def
                //    mismatch (either due to version difference or strong-name difference).
                //
                // Attempt to resolve the assembly via managed ALC instance. This can either fail the bind or return reference to an existing
                // assembly that has been loaded
                GCHandle pManagedAssemblyLoadContext = ManagedAssemblyLoadContext;
                if (!pManagedAssemblyLoadContext.IsAllocated)
                {
                    // For satellite assemblies, the managed ALC has additional resolution logic (defined by the runtime) which
                    // should be run even if the managed default ALC has not yet been used. (For non-satellite assemblies, any
                    // additional logic comes through a user-defined event handler which would have initialized the managed ALC,
                    // so if the managed ALC is not set yet, there is no additional logic to run)
                    if (!assemblyName.IsNeutralCulture)
                    {
                        // Make sure the managed default ALC is initialized.
                        AssemblyLoadContext.InitializeDefaultContext();
                    }

                    pManagedAssemblyLoadContext = ManagedAssemblyLoadContext;
                    Debug.Assert(pManagedAssemblyLoadContext.IsAllocated);
                }

                if (pManagedAssemblyLoadContext.IsAllocated)
                {
                    hr = AssemblyBinderCommon.BindUsingHostAssemblyResolver(pManagedAssemblyLoadContext, assemblyName, null, this, out coreCLRFoundAssembly);

                    if (hr >= 0)
                    {
                        // We maybe returned an assembly that was bound to a different AssemblyLoadContext instance.
                        // In such a case, we will not overwrite the binding context (which would be wrong since it would not
                        // be present in the cache of the current binding context).
                        Debug.Assert(coreCLRFoundAssembly != null);
                        coreCLRFoundAssembly.Binder ??= this;
                    }
                }
            }

            if (hr >= 0)
                assembly = coreCLRFoundAssembly;

            return hr;
        }

        public override int BindUsingPEImage(nint pPEImage, bool excludeAppPaths, out Assembly? assembly)
        {
            assembly = null;
            int hr;

            try
            {
                Assembly? coreCLRFoundAssembly;
                AssemblyName assemblyName = new AssemblyName(pPEImage);

                // Validate architecture
                if (!AssemblyBinderCommon.IsValidArchitecture(assemblyName.ProcessorArchitecture))
                    return AssemblyBinderCommon.CLR_E_BIND_ARCHITECTURE_MISMATCH;

                // Easy out for CoreLib
                if (assemblyName.IsCoreLib)
                    return HResults.E_FILENOTFOUND;

                {
                    // Ensure we are not being asked to bind to a TPA assembly

                    Debug.Assert(AppContext.TrustedPlatformAssemblyMap != null);

                    if (AppContext.TrustedPlatformAssemblyMap.ContainsKey(assemblyName.SimpleName))
                    {
                        // The simple name of the assembly being requested to be bound was found in the TPA list.
                        // Now, perform the actual bind to see if the assembly was really in the TPA assembly list or not.
                        hr = BindAssemblyByNameWorker(assemblyName, out coreCLRFoundAssembly, excludeAppPaths: true);
                        if (hr >= 0)
                        {
                            Debug.Assert(coreCLRFoundAssembly != null);
                            if (coreCLRFoundAssembly.IsInTPA)
                            {
                                assembly = coreCLRFoundAssembly;
                                return hr;
                            }
                        }
                    }
                }

                hr = AssemblyBinderCommon.BindUsingPEImage(this, assemblyName, pPEImage, excludeAppPaths, out coreCLRFoundAssembly);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(coreCLRFoundAssembly != null);
                    coreCLRFoundAssembly.Binder = this;
                    assembly = coreCLRFoundAssembly;
                }
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return hr;
        }

        public void SetupBindingPaths(string trustedPlatformAssemblies, string platformResourceRoots, string appPaths)
        {
            AppContext.SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, acquireLock: true);
        }

        // BindToSystem
    }
}
