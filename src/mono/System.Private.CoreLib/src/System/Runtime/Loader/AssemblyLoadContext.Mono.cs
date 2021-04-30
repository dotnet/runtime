// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Internal.Runtime.CompilerServices;
using Mono;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        internal IntPtr NativeALC
        {
            get
            {
                return _nativeAssemblyLoadContext;
            }
        }

        [DynamicDependency(nameof(_nativeAssemblyLoadContext))]
        private IntPtr InitializeAssemblyLoadContext(IntPtr thisHandlePtr, bool representsTPALoadContext, bool isCollectible)
        {
            using (SafeStringMarshal handle = RuntimeMarshal.MarshalString(Name))
            {
                return InternalInitializeNativeALC(thisHandlePtr, handle.Value, representsTPALoadContext, isCollectible);
            }
        }

        [MethodImplAttribute (MethodImplOptions.InternalCall)]
        private static extern void PrepareForAssemblyLoadContextRelease (IntPtr nativeAssemblyLoadContext, IntPtr assemblyLoadContextStrong);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        private Assembly InternalLoadFromPath(string? assemblyPath, string? nativeImagePath)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            assemblyPath = assemblyPath?.Replace('\\', Path.DirectorySeparatorChar);
            // TODO: Handle nativeImagePath
            return InternalLoadFile(NativeALC, assemblyPath, ref stackMark);
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        internal Assembly InternalLoad(byte[] arrAssembly, byte[]? arrSymbols)
        {
            unsafe
            {
                int symbolsLength = arrSymbols?.Length ?? 0;
                fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
                {
                    return InternalLoadFromStream(NativeALC, new IntPtr(ptrAssembly), arrAssembly.Length,
                                       new IntPtr(ptrSymbols), symbolsLength);
                }
            }
        }

        internal static Assembly[] GetLoadedAssemblies()
        {
            return InternalGetLoadedAssemblies();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly rtAsm);

        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            AssemblyLoadContext? loadContextForAssembly = null;

            RuntimeAssembly? rtAsm = assembly as RuntimeAssembly;

            // We only support looking up load context for runtime assemblies.
            if (rtAsm != null)
            {
                RuntimeAssembly runtimeAssembly = rtAsm;
                IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly(runtimeAssembly);
                loadContextForAssembly = GetAssemblyLoadContext(ptrAssemblyLoadContext);
            }

            return loadContextForAssembly;
        }

        public void SetProfileOptimizationRoot(string directoryPath)
        {
        }

        public void StartProfileOptimization(string? profile)
        {
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Assembly InternalLoadFile(IntPtr nativeAssemblyLoadContext, string? assemblyFile, ref StackCrawlMark stackMark);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalInitializeNativeALC(IntPtr thisHandlePtr, IntPtr name, bool representsTPALoadContext, bool isCollectible);

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Assembly InternalLoadFromStream(IntPtr nativeAssemblyLoadContext, IntPtr assm, int assmLength, IntPtr symbols, int symbolsLength);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Assembly[] InternalGetLoadedAssemblies();

        internal static Assembly? DoAssemblyResolve(string name)
        {
            return AssemblyResolve?.Invoke(null, new ResolveEventArgs(name));
        }

        // Invoked by Mono to resolve using the load method.
        private static Assembly? MonoResolveUsingLoad(IntPtr gchALC, string assemblyName)
        {
            return Resolve(gchALC, new AssemblyName(assemblyName));
        }

        // Invoked by Mono to resolve using the Resolving event after
        // trying the Load override and default load context without
        // success.
        private static Assembly? MonoResolveUsingResolvingEvent(IntPtr gchALC, string assemblyName)
        {
            AssemblyLoadContext context;
            // This check exists because the function can be called early in startup, before the default ALC is initialized
            if (gchALC == IntPtr.Zero)
                context = Default;
            else
                context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchALC).Target)!;
            return context.ResolveUsingEvent(new AssemblyName(assemblyName));
        }

        // Invoked by Mono to resolve requests to load satellite assemblies.
        private static Assembly? MonoResolveUsingResolveSatelliteAssembly(IntPtr gchALC, string assemblyName)
        {
            AssemblyLoadContext context;
            // This check exists because the function can be called early in startup, before the default ALC is initialized
            if (gchALC == IntPtr.Zero)
                context = Default;
            else
                context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchALC).Target)!;
            return context.ResolveSatelliteAssembly(new AssemblyName(assemblyName));
        }

        private static AssemblyLoadContext GetAssemblyLoadContext(IntPtr gchManagedAssemblyLoadContext)
        {
            AssemblyLoadContext context;
            // This check exists because the function can be called early in startup, before the default ALC is initialized
            if (gchManagedAssemblyLoadContext == IntPtr.Zero)
                context = Default;
            else
                context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;
            return context;
        }

        private static void MonoResolveUnmanagedDll(string unmanagedDllName, IntPtr gchManagedAssemblyLoadContext, ref IntPtr dll)
        {
            AssemblyLoadContext context = GetAssemblyLoadContext(gchManagedAssemblyLoadContext);
            dll = context.LoadUnmanagedDll(unmanagedDllName);
        }

        private static void MonoResolveUnmanagedDllUsingEvent(string unmanagedDllName, Assembly assembly, IntPtr gchManagedAssemblyLoadContext, ref IntPtr dll)
        {
            AssemblyLoadContext context = GetAssemblyLoadContext(gchManagedAssemblyLoadContext);
            dll = context.GetResolvedUnmanagedDll(assembly, unmanagedDllName);
        }

        private static RuntimeAssembly? GetRuntimeAssembly(Assembly? asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is System.Reflection.Emit.AssemblyBuilder ab ? Unsafe.As<RuntimeAssembly>(ab) : // Mono AssemblyBuilder is also a RuntimeAssembly, see AssemblyBuilder.Mono.cs
                null;
        }
    }
}
