// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Internal.Runtime.Binder
{
    internal sealed class CustomAssemblyBinder : AssemblyBinder
    {
        private readonly DefaultAssemblyBinder _defaultBinder;
        private System.Reflection.LoaderAllocator? _loaderAllocator;
        private GCHandle _loaderAllocatorHandle;

        // A strong GC handle to the managed AssemblyLoadContext. This handle is set when the unload of the AssemblyLoadContext is initiated
        // to keep the managed AssemblyLoadContext alive until the unload is finished.
        // We still keep the weak handle pointing to the same managed AssemblyLoadContext so that native code can use the handle above
        // to refer to it during the whole lifetime of the AssemblyLoadContext.
        private GCHandle _ptrManagedStrongAssemblyLoadContext;

        public override bool IsDefault => false;

        public override System.Reflection.LoaderAllocator? GetLoaderAllocator() => _loaderAllocator;

        public CustomAssemblyBinder(
            DefaultAssemblyBinder defaultBinder,
            System.Reflection.LoaderAllocator? loaderAllocator,
            GCHandle loaderAllocatorHandle,
            GCHandle ptrAssemblyLoadContext)
        {
            // Save reference to the DefaultBinder that is required to be present.
            _defaultBinder = defaultBinder;

            // Save the reference to the IntPtr for GCHandle for the managed
            // AssemblyLoadContext instance
            ManagedAssemblyLoadContext = ptrAssemblyLoadContext;

            if (loaderAllocator != null)
            {
                // Link to LoaderAllocator, keep a reference to it
                // VERIFY(pLoaderAllocator->AddReferenceIfAlive());

                // ((AssemblyLoaderAllocator*)pLoaderAllocator)->RegisterBinder(pBinder);
                var thisHandle = GCHandle.Alloc(this);
                loaderAllocator.RegisterBinder(thisHandle);
            }

            _loaderAllocator = loaderAllocator;
            _loaderAllocatorHandle = loaderAllocatorHandle;
        }

        public void PrepareForLoadContextRelease(GCHandle ptrManagedStrongAssemblyLoadContext)
        {
            // Add a strong handle so that the managed assembly load context stays alive until the
            // CustomAssemblyBinder::ReleaseLoadContext is called.
            // We keep the weak handle as well since this method can be running on one thread (e.g. the finalizer one)
            // and other thread can be using the weak handle.
            _ptrManagedStrongAssemblyLoadContext = ptrManagedStrongAssemblyLoadContext;

            Debug.Assert(_loaderAllocator != null);
            Debug.Assert(_loaderAllocatorHandle.IsAllocated);

            // We cannot delete the binder here as it is used indirectly when comparing assemblies with the same binder
            // It will be deleted when the LoaderAllocator will be deleted
            // We need to keep the LoaderAllocator pointer set as it still may be needed for creating references between the
            // native LoaderAllocators of two collectible contexts in case the AssemblyLoadContext.Unload was called on the current
            // context before returning from its AssemblyLoadContext.Load override or the context's Resolving event.
            // But we need to release the LoaderAllocator so that it doesn't prevent completion of the final phase of unloading in
            // some cases. It is safe to do as the AssemblyLoaderAllocator is guaranteed to be alive at least until the
            // CustomAssemblyBinder::ReleaseLoadContext is called, where we NULL this pointer.

            _loaderAllocator = null!;

            // Destroy the strong handle to the LoaderAllocator in order to let it reach its finalizer
            _loaderAllocatorHandle.Free();
            _loaderAllocatorHandle = default;
        }

        ~CustomAssemblyBinder()
        {
            // CustomAssemblyBinder::ReleaseLoadContext

            Debug.Assert(ManagedAssemblyLoadContext.IsAllocated);
            Debug.Assert(_ptrManagedStrongAssemblyLoadContext.IsAllocated);

            // This method is called to release the weak and strong handles on the managed AssemblyLoadContext
            // once the Unloading event has been fired
            ManagedAssemblyLoadContext.Free();
            _ptrManagedStrongAssemblyLoadContext.Free();
            ManagedAssemblyLoadContext = default;

            // The AssemblyLoaderAllocator is in a process of shutdown and should not be used
            // after this point.
            _ptrManagedStrongAssemblyLoadContext.Free();
        }

        private int BindAssemblyByNameWorker(AssemblyName assemblyName, out Assembly? coreCLRFoundAssembly)
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

        public override int BindUsingAssemblyName(AssemblyName assemblyName, out Assembly? assembly)
        {
            int hr;
            Assembly? coreCLRFoundAssembly;

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

                    hr = AssemblyBinderCommon.BindUsingHostAssemblyResolver(ManagedAssemblyLoadContext, assemblyName, _defaultBinder, this, out coreCLRFoundAssembly);

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
