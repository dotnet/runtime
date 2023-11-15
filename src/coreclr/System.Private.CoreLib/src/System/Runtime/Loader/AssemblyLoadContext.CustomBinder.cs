// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        private int BindAssemblyByNameWorker(BinderAssemblyName assemblyName, out BinderAssembly? coreCLRFoundAssembly)
        {
            // CoreLib should be bound using BindToSystem
            Debug.Assert(!assemblyName.IsCoreLib);

            int hr = AssemblyBinderCommon.BindAssembly(this, assemblyName, excludeAppPaths: false, out coreCLRFoundAssembly);

            if (hr >= 0)
            {
                Debug.Assert(coreCLRFoundAssembly != null);
                coreCLRFoundAssembly.Binder = this;
            }

            return hr;
        }

        internal virtual int BindUsingAssemblyName(BinderAssemblyName assemblyName, out BinderAssembly? assembly)
        {
            int hr;
            BinderAssembly? coreCLRFoundAssembly;

            // When LoadContext needs to resolve an assembly reference, it will go through the following lookup order:
            //
            // 1) Lookup the assembly within the LoadContext itself. If assembly is found, use it.
            // 2) Invoke the LoadContext's Load method implementation. If assembly is found, use it.
            // 3) Lookup the assembly within DefaultBinder (except for satellite requests). If assembly is found, use it.
            // 4) Invoke the LoadContext's ResolveSatelliteAssembly method (for satellite requests). If assembly is found, use it.
            // 5) Invoke the LoadContext's Resolving event. If assembly is found, use it.
            // 6) Raise exception.
            //
            // This approach enables a LoadContext to override assemblies that have been loaded in TPA context by loading
            // a different (or even the same!) version.

            {
                // Step 1 - Try to find the assembly within the LoadContext.
                hr = BindAssemblyByNameWorker(assemblyName, out coreCLRFoundAssembly);
                if (hr is HResults.E_FILENOTFOUND or AssemblyBinderCommon.FUSION_E_APP_DOMAIN_LOCKED or HResults.FUSION_E_REF_DEF_MISMATCH)
                {
                    // If we are here, one of the following is possible:
                    //
                    // 1) The assembly has not been found in the current binder's application context (i.e. it has not already been loaded), OR
                    // 2) An assembly with the same simple name was already loaded in the context of the current binder but we ran into a Ref/Def
                    //    mismatch (either due to version difference or strong-name difference).
                    //
                    // Thus, if default binder has been overridden, then invoke it in an attempt to perform the binding for it make the call
                    // of what to do next. The host-overridden binder can either fail the bind or return reference to an existing assembly
                    // that has been loaded.

                    hr = BindUsingHostAssemblyResolver(assemblyName, Default, this, out coreCLRFoundAssembly);

                    if (hr >= 0)
                    {
                        // We maybe returned an assembly that was bound to a different AssemblyBinder instance.
                        // In such a case, we will not overwrite the binder (which would be wrong since the assembly would not
                        // be present in the cache of the current binding context).
                        Debug.Assert(coreCLRFoundAssembly != null);
                        coreCLRFoundAssembly.Binder ??= this;
                    }
                }
            }

            assembly = hr < 0 ? null : coreCLRFoundAssembly;
            return hr;
        }

        // called by vm
        internal virtual int BindUsingPEImage(nint pPEImage, bool excludeAppPaths, out BinderAssembly? assembly)
        {
            assembly = null;
            int hr;

            try
            {
                BinderAssembly? coreCLRFoundAssembly;
                BinderAssemblyName assemblyName = new BinderAssemblyName(pPEImage);

                // Validate architecture
                if (!AssemblyBinderCommon.IsValidArchitecture(assemblyName.ProcessorArchitecture))
                    return AssemblyBinderCommon.CLR_E_BIND_ARCHITECTURE_MISMATCH;

                // Disallow attempt to bind to the core library. Aside from that,
                // the LoadContext can load any assembly (even if it was in a different LoadContext like TPA).
                if (assemblyName.IsCoreLib)
                    return HResults.E_FILENOTFOUND;

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
    }
}
