// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_GetDefaultAssemblyBinder")]
        internal static partial IntPtr GetDefaultAssemblyBinder();

        // Foo
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern Internal.Runtime.Binder.IVMAssembly AssemblyNative_LoadFromPEImage(Internal.Runtime.Binder.AssemblyBinder pBinder, IntPtr pPEImage, bool excludeAppPaths = false);

        private static Internal.Runtime.Binder.AssemblyBinder InitializeAssemblyLoadContext(GCHandle ptrAssemblyLoadContext, bool representsTPALoadContext, bool isCollectible)
        {
            // We do not need to take a lock since this method is invoked from the ctor of AssemblyLoadContext managed type and
            // only one thread is ever executing a ctor for a given instance.

            // Initialize the assembly binder instance in the VM
            GCHandle pDefaultBinder = GCHandle.FromIntPtr(GetDefaultAssemblyBinder());
            var defaultBinder = pDefaultBinder.Target as Internal.Runtime.Binder.DefaultAssemblyBinder;
            Debug.Assert(defaultBinder != null);
            if (!representsTPALoadContext)
            {
                // Initialize a custom assembly binder

                LoaderAllocator? loaderAllocator = null;
                GCHandle loaderAllocatorHandle = default;

                if (isCollectible)
                {
                    // Create a new AssemblyLoaderAllocator for an AssemblyLoadContext
                }

                return new Internal.Runtime.Binder.CustomAssemblyBinder(defaultBinder, loaderAllocator, loaderAllocatorHandle, ptrAssemblyLoadContext);
            }
            else
            {
                // We are initializing the managed instance of Assembly Load Context that would represent the TPA binder.
                // First, confirm we do not have an existing managed ALC attached to the TPA binder.
                Debug.Assert(!defaultBinder.ManagedAssemblyLoadContext.IsAllocated);

                // Attach the managed TPA binding context with the native one.
                defaultBinder.ManagedAssemblyLoadContext = ptrAssemblyLoadContext;
                return defaultBinder;
            }
        }

        private static void PrepareForAssemblyLoadContextRelease(Internal.Runtime.Binder.AssemblyBinder ptrNativeAssemblyBinder, GCHandle ptrAssemblyLoadContextStrong)
        {
            ((Internal.Runtime.Binder.CustomAssemblyBinder)ptrNativeAssemblyBinder).PrepareForLoadContextRelease(ptrAssemblyLoadContextStrong);
        }

        private static unsafe RuntimeAssembly LoadFromStream(Internal.Runtime.Binder.AssemblyBinder assemblyBinder, byte* ptrAssemblyArray, int iAssemblyArrayLen, byte* ptrSymbols, int iSymbolArrayLen)
        {
            IntPtr pILImage = 0;

            try
            {
                pILImage = PEImage_CreateFromByteArray(ptrAssemblyArray, iAssemblyArrayLen);

                // Need to verify that this is a valid CLR assembly.
                if (!PEImage_CheckILFormat(pILImage))
                    throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);

                LoaderAllocator? loaderAllocator = assemblyBinder.GetLoaderAllocator();
                if (loaderAllocator != null && loaderAllocator.IsCollectible() && !PEImage_IsILOnly(pILImage))
                {
                    // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
                    throw new BadImageFormatException("Cannot load a mixed assembly into a collectible AssemblyLoadContext");
                }

                // Pass the stream based assembly as IL in an attempt to bind and load it
                IntPtr loadedAssembly = AssemblyNative_LoadFromPEImage(assemblyBinder, pILImage);
                RuntimeAssembly? retAssembly = null;
                Internal.Runtime.Binder.AssemblyBinder.Assembly_GetExposedObject(loadedAssembly, ObjectHandleOnStack.Create(ref retAssembly));
                Debug.Assert(retAssembly != null);

                // LOG((LF_CLASSLOADER,
                //       LL_INFO100,
                //       "\tLoaded assembly from a file\n"));

                // In order to assign the PDB image (if present),
                // the resulting assembly's image needs to be exactly the one
                // we created above. We need pointer comparison instead of pe image equivalence
                // to avoid mixed binaries/PDB pairs of other images.
                // This applies to both Desktop CLR and CoreCLR, with or without fusion.
                bool isSameAssembly = Internal.Runtime.Binder.AssemblyBinder.Assembly_GetPEImage(loadedAssembly) == pILImage;

                // Setting the PDB info is only applicable for our original assembly.
                // This applies to both Desktop CLR and CoreCLR, with or without fusion.
                if (isSameAssembly)
                {
                    // #ifdef DEBUGGING_SUPPORTED
                    if (ptrSymbols != null)
                    {
                        Internal.Runtime.Binder.AssemblyBinder.Assembly_SetSymbolBytes(loadedAssembly, ptrAssemblyArray, iSymbolArrayLen);
                    }
                    // #endif
                }

                return retAssembly;
            }
            finally
            {
                if (pILImage != 0)
                    PEImage_Release(pILImage);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MultiCoreJIT_InternalSetProfileRoot", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void InternalSetProfileRoot(string directoryPath);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MultiCoreJIT_InternalStartProfile", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void InternalStartProfile(string? profile, IntPtr ptrNativeAssemblyBinder);

        // Foo
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalStartProfile(string? profile, Internal.Runtime.Binder.AssemblyBinder ptrNativeAssemblyBinder);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
#pragma warning disable IDE0060 // niPath is unused
        private static RuntimeAssembly LoadFromPath(Internal.Runtime.Binder.AssemblyBinder assemblyBinder, string? ilPath, string? niPath)
#pragma warning restore IDE0060
        {
            // Form the PEImage for the ILAssembly. In case of an exception, the holder will ensure
            // the release of the image.
            IntPtr pILImage = 0;

            try
            {
                if (ilPath != null)
                {
                    pILImage = PEImage_OpenImage(ilPath, 0, default);

                    // Need to verify that this is a valid CLR assembly.
                    if (!PEImage_CheckILFormat(pILImage))
                        throw new BadImageFormatException($"{SR.BadImageFormat_BadILFormat} The format of the file '{ilPath}' is invalid.");

                    LoaderAllocator? loaderAllocator = assemblyBinder.GetLoaderAllocator();
                    if (loaderAllocator != null && loaderAllocator.IsCollectible() && !PEImage_IsILOnly(pILImage))
                    {
                        // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
                        throw new BadImageFormatException($"Cannot load a mixed assembly into a collectible AssemblyLoadContext. The format of the file '{ilPath}' is invalid.");
                    }

                    Internal.Runtime.Binder.IVMAssembly loadedAssembly = AssemblyNative_LoadFromPEImage(assemblyBinder, pILImage);
                    return loadedAssembly.GetExposedObject();

                    // LOG((LF_CLASSLOADER,
                    //       LL_INFO100,
                    //       "\tLoaded assembly from a file\n"));
                }

                // The behavior was inherited from native implementation.
                return null!;
            }
            finally
            {
                if (pILImage != 0)
                    PEImage_Release(pILImage);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Assembly[] GetLoadedAssemblies();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsTracingEnabled();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_TraceResolvingHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TraceResolvingHandlerInvoked(string assemblyName, string handlerName, string? alcName, string? resultAssemblyName, string? resultAssemblyPath);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_TraceAssemblyResolveHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TraceAssemblyResolveHandlerInvoked(string assemblyName, string handlerName, string? resultAssemblyName, string? resultAssemblyPath);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TraceAssemblyLoadFromResolveHandlerInvoked(string assemblyName, [MarshalAs(UnmanagedType.Bool)] bool isTrackedAssembly, string requestingAssemblyPath, string? requestedAssemblyPath);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_TraceSatelliteSubdirectoryPathProbed", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TraceSatelliteSubdirectoryPathProbed(string filePath, int hResult);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        private RuntimeAssembly InternalLoadFromPath(string? assemblyPath, string? nativeImagePath)
        {
            return LoadFromPath(_assemblyBinder, assemblyPath, nativeImagePath);
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        internal unsafe Assembly InternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols)
        {
            fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
            {
                return LoadFromStream(_assemblyBinder, ptrAssembly, arrAssembly.Length,
                    ptrSymbols, arrSymbols.Length);
            }
        }

#if TARGET_WINDOWS
        private static RuntimeAssembly LoadFromInMemoryModuleInternal(Internal.Runtime.Binder.AssemblyBinder assemblyBinder, IntPtr hModule)
        {
            IntPtr pILImage = 0;

            try
            {
                pILImage = PEImage_CreateFromHMODULE(hModule);

                // Need to verify that this is a valid CLR assembly.
                if (!PEImage_HasCorHeader(pILImage))
                    throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);

                // Pass the in memory module as IL in an attempt to bind and load it
                Internal.Runtime.Binder.IVMAssembly loadedAssembly = AssemblyNative_LoadFromPEImage(assemblyBinder, pILImage);
                return loadedAssembly.GetExposedObject();

                // LOG((LF_CLASSLOADER,
                //       LL_INFO100,
                //       "\tLoaded assembly from a file\n"));
            }
            finally
            {
                if (pILImage != 0)
                    PEImage_Release(pILImage);
            }
        }

        /// <summary>
        /// Load a module that has already been loaded into memory by the OS loader as a .NET assembly.
        /// </summary>
        internal Assembly LoadFromInMemoryModule(IntPtr moduleHandle)
        {
            ArgumentNullException.ThrowIfNull(moduleHandle);

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return LoadFromInMemoryModuleInternal(_assemblyBinder, moduleHandle);
            }
        }
#endif

        // This method is invoked by the VM to resolve a satellite assembly reference
        // after trying assembly resolution via Load override without success.
        internal static Assembly? ResolveSatelliteAssembly(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;

            // Invoke the ResolveSatelliteAssembly method
            return context.ResolveSatelliteAssembly(assemblyName);
        }

        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static IntPtr ResolveUnmanagedDll(string unmanagedDllName, IntPtr gchManagedAssemblyLoadContext)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;
            return context.LoadUnmanagedDll(unmanagedDllName);
        }

        // This method is invoked by the VM to resolve a native library using the ResolvingUnmanagedDll event
        // after trying all other means of resolution.
        private static IntPtr ResolveUnmanagedDllUsingEvent(string unmanagedDllName, Assembly assembly, IntPtr gchManagedAssemblyLoadContext)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;
            return context.GetResolvedUnmanagedDll(assembly, unmanagedDllName);
        }

        // This method is invoked by the VM to resolve an assembly reference using the Resolving event
        // after trying assembly resolution via Load override and TPA load context without success.
        internal static Assembly? ResolveUsingResolvingEvent(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;
            // Invoke the AssemblyResolve event callbacks if wired up
            return context.ResolveUsingEvent(assemblyName);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_GetLoadContextForAssembly")]
        private static partial IntPtr GetLoadContextForAssembly(QCallAssembly assembly);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_OpenImage", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr PEImage_OpenImage(string path, int mdInternalImportFlags, Internal.Runtime.Binder.BundleFileLocation bundleFileLocation = default);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_CreateFromByteArray")]
        private static unsafe partial IntPtr PEImage_CreateFromByteArray(byte* ptrArray, int size);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_CheckILFormat")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PEImage_CheckILFormat(IntPtr pPEImage);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_IsILOnly")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PEImage_IsILOnly(IntPtr pPEImage);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_Release")]
        internal static partial void PEImage_Release(IntPtr pPEImage);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_GetMVID")]
        internal static partial void PEImage_GetMVID(IntPtr pPEImage, out Guid mvid);

#if TARGET_WINDOWS
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_CreateFromHMODULE")]
        private static unsafe partial IntPtr PEImage_CreateFromHMODULE(IntPtr hMod);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_HasCorHeader")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PEImage_HasCorHeader(IntPtr pPEImage);
#endif

        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            RuntimeAssembly? rtAsm = GetRuntimeAssembly(assembly);

            // We only support looking up load context for runtime assemblies.
            AssemblyLoadContext? loadContextForAssembly = null;
            if (rtAsm != null)
            {
                RuntimeAssembly runtimeAssembly = rtAsm;
                IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly(new QCallAssembly(ref runtimeAssembly));
                if (ptrAssemblyLoadContext == IntPtr.Zero)
                {
                    // If the load context is returned null, then the assembly was bound using the TPA binder
                    // and we shall return reference to the "Default" binder.
                    loadContextForAssembly = Default;
                }
                else
                {
                    loadContextForAssembly = (AssemblyLoadContext)(GCHandle.FromIntPtr(ptrAssemblyLoadContext).Target)!;
                }
            }

            return loadContextForAssembly;
        }

        // Set the root directory path for profile optimization.
        public void SetProfileOptimizationRoot(string directoryPath)
        {
            InternalSetProfileRoot(directoryPath);
        }

        // Start profile optimization for the specified profile name.
        public void StartProfileOptimization(string? profile)
        {
            InternalStartProfile(profile, _assemblyBinder);
        }

        internal static RuntimeAssembly? GetRuntimeAssembly(Assembly? asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is System.Reflection.Emit.RuntimeAssemblyBuilder ab ? ab.InternalAssembly :
                null;
        }

        // Assembly load runtime activity name
        private const string AssemblyLoadName = "AssemblyLoad";

        /// <summary>
        /// Called by the runtime to start an assembly load activity for tracing
        /// </summary>
        private static void StartAssemblyLoad(ref Guid activityId, ref Guid relatedActivityId)
        {
            // Make sure ActivityTracker is enabled
            ActivityTracker.Instance.Enable();

            // Don't use trace to TPL event source in ActivityTracker - that event source is a singleton and its instantiation may have triggered the load.
            ActivityTracker.Instance.OnStart(NativeRuntimeEventSource.Log.Name, AssemblyLoadName, 0, ref activityId, ref relatedActivityId, EventActivityOptions.Recursive, useTplSource: false);
        }

        /// <summary>
        /// Called by the runtime to stop an assembly load activity for tracing
        /// </summary>
        private static void StopAssemblyLoad(ref Guid activityId)
        {
            // Don't use trace to TPL event source in ActivityTracker - that event source is a singleton and its instantiation may have triggered the load.
            ActivityTracker.Instance.OnStop(NativeRuntimeEventSource.Log.Name, AssemblyLoadName, 0, ref activityId, useTplSource: false);
        }

        /// <summary>
        /// Called by the runtime to make sure the default ALC is initialized
        /// </summary>
        internal static void InitializeDefaultContext()
        {
            _ = Default;
        }
    }
}
