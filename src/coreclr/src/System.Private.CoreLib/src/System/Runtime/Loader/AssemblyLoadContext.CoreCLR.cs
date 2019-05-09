// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext, bool isCollectible);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void PrepareForAssemblyLoadContextRelease(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyLoadContextStrong);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadFromStream(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void InternalSetProfileRoot(string directoryPath);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContext, string? ilPath, string? niPath, ObjectHandleOnStack retAssembly);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Assembly[] GetLoadedAssemblies();

        private Assembly InternalLoadFromPath(string? assemblyPath, string? nativeImagePath)
        {
            RuntimeAssembly? loadedAssembly = null;
            LoadFromPath(_nativeAssemblyLoadContext, assemblyPath, nativeImagePath, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            return loadedAssembly!;
        }

        internal unsafe Assembly InternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols)
        {
            RuntimeAssembly? loadedAssembly = null;

            fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
            {
                LoadFromStream(_nativeAssemblyLoadContext, new IntPtr(ptrAssembly), arrAssembly.Length,
                    new IntPtr(ptrSymbols), arrSymbols.Length, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            }

            return loadedAssembly!;
        }
        
#if !FEATURE_PAL
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadFromInMemoryModuleInternal(IntPtr ptrNativeAssemblyLoadContext, IntPtr hModule, ObjectHandleOnStack retAssembly);


        /// <summary>
        /// Load a module that has already been loaded into memory by the OS loader as a .NET assembly.
        /// </summary>
        internal Assembly LoadFromInMemoryModule(IntPtr moduleHandle)
        {
            if (moduleHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(moduleHandle));
            }
            lock (_unloadLock)
            {
                VerifyIsAlive();

                RuntimeAssembly? loadedAssembly = null;
                LoadFromInMemoryModuleInternal(
                    _nativeAssemblyLoadContext,
                    moduleHandle,
                    JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                return loadedAssembly!;
            }
        }
#endif

        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static Assembly? Resolve(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;

            return context.ResolveUsingLoad(assemblyName);
        }

        // This method is invoked by the VM to resolve an assembly reference using the Resolving event
        // after trying assembly resolution via Load override and TPA load context without success.
        private static Assembly? ResolveUsingResolvingEvent(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;

            // Invoke the AssemblyResolve event callbacks if wired up
            return context.ResolveUsingEvent(assemblyName);
        }

        // This method is invoked by the VM to resolve a satellite assembly reference
        // after trying assembly resolution via Load override without success.
        private static Assembly? ResolveSatelliteAssembly(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;

            // Invoke the ResolveSatelliteAssembly method
            return context.ResolveSatelliteAssembly(assemblyName);
        }

        private Assembly? GetFirstResolvedAssembly(AssemblyName assemblyName)
        {
            Assembly? resolvedAssembly = null;

            Func<AssemblyLoadContext, AssemblyName, Assembly> assemblyResolveHandler = _resolving;

            if (assemblyResolveHandler != null)
            {
                // Loop through the event subscribers and return the first non-null Assembly instance
                foreach (Func<AssemblyLoadContext, AssemblyName, Assembly> handler in assemblyResolveHandler.GetInvocationList())
                {
                    resolvedAssembly = handler(this, assemblyName);
                    if (resolvedAssembly != null)
                    {
                        return resolvedAssembly;
                    }
                }
            }

            return null;
        }

        private Assembly ValidateAssemblyNameWithSimpleName(Assembly assembly, string? requestedSimpleName)
        {
            // Get the name of the loaded assembly
            string? loadedSimpleName = null;

            // Derived type's Load implementation is expected to use one of the LoadFrom* methods to get the assembly
            // which is a RuntimeAssembly instance. However, since Assembly type can be used build any other artifact (e.g. AssemblyBuilder),
            // we need to check for RuntimeAssembly.
            RuntimeAssembly? rtLoadedAssembly = assembly as RuntimeAssembly;
            if (rtLoadedAssembly != null)
            {
                loadedSimpleName = rtLoadedAssembly.GetSimpleName();
            }

            // The simple names should match at the very least
            if (string.IsNullOrEmpty(requestedSimpleName))
            {
                throw new ArgumentException(SR.ArgumentNull_AssemblyNameName);
            }
            if (string.IsNullOrEmpty(loadedSimpleName) || !requestedSimpleName.Equals(loadedSimpleName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException(SR.Argument_CustomAssemblyLoadContextRequestedNameMismatch);
            }

            return assembly;
        }

        private Assembly? ResolveUsingLoad(AssemblyName assemblyName)
        {
            string? simpleName = assemblyName.Name;
            Assembly? assembly = Load(assemblyName);

            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }

            return assembly;
        }

        private Assembly? ResolveUsingEvent(AssemblyName assemblyName)
        {
            string? simpleName = assemblyName.Name;

            // Invoke the AssemblyResolve event callbacks if wired up
            Assembly? assembly = GetFirstResolvedAssembly(assemblyName);
            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }

            return assembly;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr InternalLoadUnmanagedDllFromPath(string unmanagedDllPath);

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

        private IntPtr GetResolvedUnmanagedDll(Assembly assembly, string unmanagedDllName)
        {
            IntPtr resolvedDll = IntPtr.Zero;

            Func<Assembly, string, IntPtr> dllResolveHandler = _resolvingUnmanagedDll;

            if (dllResolveHandler != null)
            {
                // Loop through the event subscribers and return the first non-null native library handle
                foreach (Func<Assembly, string, IntPtr>  handler in dllResolveHandler.GetInvocationList())
                {
                    resolvedDll = handler(assembly, unmanagedDllName);
                    if (resolvedDll != IntPtr.Zero)
                    {
                        return resolvedDll;
                    }
                }
            }

            return IntPtr.Zero;
        }
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void LoadTypeForWinRTTypeNameInContextInternal(IntPtr ptrNativeAssemblyLoadContext, string typeName, ObjectHandleOnStack loadedType);

        internal Type LoadTypeForWinRTTypeNameInContext(string typeName)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                Type? type = null;
                LoadTypeForWinRTTypeNameInContextInternal(_nativeAssemblyLoadContext, typeName, JitHelpers.GetObjectHandleOnStack(ref type));
                return type!;
            }
        }
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly assembly);

        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            AssemblyLoadContext? loadContextForAssembly = null;

            RuntimeAssembly? rtAsm = assembly as RuntimeAssembly;

            // We only support looking up load context for runtime assemblies.
            if (rtAsm != null)
            {
                IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly(rtAsm);
                if (ptrAssemblyLoadContext == IntPtr.Zero)
                {
                    // If the load context is returned null, then the assembly was bound using the TPA binder
                    // and we shall return reference to the active "Default" binder - which could be the TPA binder
                    // or an overridden CLRPrivBinderAssemblyLoadContext instance.
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
        public void StartProfileOptimization(string profile)
        {
            InternalStartProfile(profile, _nativeAssemblyLoadContext);
        }

        // This method is called by the VM.
        private static void OnAssemblyLoad(RuntimeAssembly assembly)
        {
            AssemblyLoad?.Invoke(null /* AppDomain */, new AssemblyLoadEventArgs(assembly));
        }

        // This method is called by the VM.
        private static RuntimeAssembly? OnResourceResolve(RuntimeAssembly assembly, string resourceName)
        {
            return InvokeResolveEvent(ResourceResolve, assembly, resourceName);
        }

        // This method is called by the VM
        private static RuntimeAssembly? OnTypeResolve(RuntimeAssembly assembly, string typeName)
        {
            return InvokeResolveEvent(TypeResolve, assembly, typeName);
        }

        // This method is called by the VM.
        private static RuntimeAssembly? OnAssemblyResolve(RuntimeAssembly assembly, string assemblyFullName)
        {
            return InvokeResolveEvent(AssemblyResolve, assembly, assemblyFullName);
        }

        private static RuntimeAssembly? InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, string name)
        {
            if (eventHandler == null)
                return null;

            var args = new ResolveEventArgs(name, assembly);

            foreach (ResolveEventHandler handler in eventHandler.GetInvocationList())
            {
                Assembly asm = handler(null /* AppDomain */, args);
                RuntimeAssembly? ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        private static RuntimeAssembly? GetRuntimeAssembly(Assembly asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is System.Reflection.Emit.AssemblyBuilder ab ? ab.InternalAssembly :
                null;
        }
    }
}
