// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_InitializeAssemblyLoadContext")]
        private static partial IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, [MarshalAs(UnmanagedType.Bool)] bool fRepresentsTPALoadContext, [MarshalAs(UnmanagedType.Bool)] bool isCollectible);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_PrepareForAssemblyLoadContextRelease")]
        private static partial void PrepareForAssemblyLoadContextRelease(IntPtr ptrNativeAssemblyBinder, IntPtr ptrAssemblyLoadContextStrong);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_LoadFromStream")]
        private static partial void LoadFromStream(IntPtr ptrNativeAssemblyBinder, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MultiCoreJIT_InternalSetProfileRoot", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void InternalSetProfileRoot(string directoryPath);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MultiCoreJIT_InternalStartProfile", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void InternalStartProfile(string? profile, IntPtr ptrNativeAssemblyBinder);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_LoadFromPath", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void LoadFromPath(IntPtr ptrNativeAssemblyBinder, string? ilPath, string? niPath, ObjectHandleOnStack retAssembly);

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
        private Assembly InternalLoadFromPath(string? assemblyPath, string? nativeImagePath)
        {
            RuntimeAssembly? loadedAssembly = null;
            LoadFromPath(_nativeAssemblyLoadContext, assemblyPath, nativeImagePath, ObjectHandleOnStack.Create(ref loadedAssembly));
            return loadedAssembly!;
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        internal unsafe Assembly InternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols)
        {
            RuntimeAssembly? loadedAssembly = null;

            fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
            {
                LoadFromStream(_nativeAssemblyLoadContext, new IntPtr(ptrAssembly), arrAssembly.Length,
                    new IntPtr(ptrSymbols), arrSymbols.Length, ObjectHandleOnStack.Create(ref loadedAssembly));
            }

            return loadedAssembly!;
        }

#if TARGET_WINDOWS
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_LoadFromInMemoryModule")]
        private static partial IntPtr LoadFromInMemoryModuleInternal(IntPtr ptrNativeAssemblyBinder, IntPtr hModule, ObjectHandleOnStack retAssembly);


        /// <summary>
        /// Load a module that has already been loaded into memory by the OS loader as a .NET assembly.
        /// </summary>
        internal Assembly LoadFromInMemoryModule(IntPtr moduleHandle)
        {
            ArgumentNullException.ThrowIfNull(moduleHandle);

            lock (_unloadLock)
            {
                VerifyIsAlive();

                RuntimeAssembly? loadedAssembly = null;
                LoadFromInMemoryModuleInternal(
                    _nativeAssemblyLoadContext,
                    moduleHandle,
                    ObjectHandleOnStack.Create(ref loadedAssembly));
                return loadedAssembly!;
            }
        }
#endif

        // This method is invoked by the VM to resolve a satellite assembly reference
        // after trying assembly resolution via Load override without success.
        private static Assembly? ResolveSatelliteAssembly(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
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
        private static Assembly? ResolveUsingResolvingEvent(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;
            // Invoke the AssemblyResolve event callbacks if wired up
            return context.ResolveUsingEvent(assemblyName);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_GetLoadContextForAssembly")]
        private static partial IntPtr GetLoadContextForAssembly(QCallAssembly assembly);

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
                    loadContextForAssembly = AssemblyLoadContext.Default;
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
            InternalStartProfile(profile, _nativeAssemblyLoadContext);
        }

        private static RuntimeAssembly? GetRuntimeAssembly(Assembly? asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is System.Reflection.Emit.AssemblyBuilder ab ? ab.InternalAssembly :
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
        private static void InitializeDefaultContext()
        {
            _ = AssemblyLoadContext.Default;
        }
    }
}
