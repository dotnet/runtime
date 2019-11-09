// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Loader
{
	partial class AssemblyLoadContext
	{
		internal IntPtr NativeALC {
			get {
				return _nativeAssemblyLoadContext;
			}
		}

		static IntPtr InitializeAssemblyLoadContext (IntPtr thisHandlePtr, bool representsTPALoadContext, bool isCollectible)
		{
			return InternalInitializeNativeALC (thisHandlePtr, representsTPALoadContext, isCollectible);
		}

		static void PrepareForAssemblyLoadContextRelease (IntPtr nativeAssemblyLoadContext, IntPtr assemblyLoadContextStrong)
		{
		}

		[System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
		Assembly InternalLoadFromPath (string assemblyPath, string nativeImagePath)
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

			assemblyPath = assemblyPath.Replace ('\\', Path.DirectorySeparatorChar);
			// TODO: Handle nativeImagePath
			return InternalLoadFile (NativeALC, assemblyPath, ref stackMark);
		}

		internal Assembly InternalLoad (byte[] arrAssembly, byte[] arrSymbols)
		{
			unsafe {
				int symbolsLength = arrSymbols?.Length ?? 0;
				fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
				{
					return InternalLoadFromStream (NativeALC, new IntPtr (ptrAssembly), arrAssembly.Length,
									   new IntPtr (ptrSymbols), symbolsLength);
				}
			}
		}

		internal static Assembly[] GetLoadedAssemblies ()
		{
			return InternalGetLoadedAssemblies ();
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static IntPtr GetLoadContextForAssembly (RuntimeAssembly rtAsm);

		// Returns the load context in which the specified assembly has been loaded
		public static AssemblyLoadContext? GetLoadContext (Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException (nameof (assembly));

			AssemblyLoadContext? loadContextForAssembly = null;

			RuntimeAssembly? rtAsm = assembly as RuntimeAssembly;

			// We only support looking up load context for runtime assemblies.
			if (rtAsm != null) {
				RuntimeAssembly runtimeAssembly = rtAsm;
				IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly (runtimeAssembly);
				loadContextForAssembly = GetAssemblyLoadContext (ptrAssemblyLoadContext);
			}

			return loadContextForAssembly;
		}

		public void SetProfileOptimizationRoot (string directoryPath)
		{
		}

		public void StartProfileOptimization (string profile)
		{
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Assembly InternalLoadFile (IntPtr nativeAssemblyLoadContext, string assemblyFile, ref StackCrawlMark stackMark);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static IntPtr InternalInitializeNativeALC (IntPtr thisHandlePtr, bool representsTPALoadContext, bool isCollectible);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Assembly InternalLoadFromStream (IntPtr nativeAssemblyLoadContext, IntPtr assm, int assmLength, IntPtr symbols, int symbolsLength);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Assembly[] InternalGetLoadedAssemblies ();

		internal static Assembly DoAssemblyResolve (string name)
		{
			return AssemblyResolve (null, new ResolveEventArgs (name));
		}

		// Invoked by Mono to resolve using the load method.
		static Assembly? MonoResolveUsingLoad (IntPtr gchALC, string assemblyName)
		{
			return Resolve (gchALC, new AssemblyName (assemblyName));
		}

		// Invoked by Mono to resolve using the Resolving event after
		// trying the Load override and default load context without
		// success.
		static Assembly? MonoResolveUsingResolvingEvent (IntPtr gchALC, string assemblyName)
		{
			return ResolveUsingResolvingEvent (gchALC, new AssemblyName (assemblyName));
		}

		// Invoked by Mono to resolve requests to load satellite assemblies. 
		static Assembly? MonoResolveUsingResolveSatelliteAssembly (IntPtr gchALC, string assemblyName)
		{
			return ResolveSatelliteAssembly (gchALC, new AssemblyName (assemblyName));
		}

		private static AssemblyLoadContext GetAssemblyLoadContext (IntPtr gchManagedAssemblyLoadContext)
		{
			AssemblyLoadContext context;
			// This check exists because the function can be called early in startup, before the default ALC is initialized
			if (gchManagedAssemblyLoadContext == IntPtr.Zero)
				context = AssemblyLoadContext.Default;
			else
				context = (AssemblyLoadContext)(GCHandle.FromIntPtr (gchManagedAssemblyLoadContext).Target)!;
			return context;
		}

		private static void MonoResolveUnmanagedDll (string unmanagedDllName, IntPtr gchManagedAssemblyLoadContext, ref IntPtr dll)
		{
			AssemblyLoadContext context = GetAssemblyLoadContext (gchManagedAssemblyLoadContext);
			dll = context.LoadUnmanagedDll(unmanagedDllName);
		}

		private static void MonoResolveUnmanagedDllUsingEvent (string unmanagedDllName, Assembly assembly, IntPtr gchManagedAssemblyLoadContext, ref IntPtr dll)
		{
			AssemblyLoadContext context = GetAssemblyLoadContext (gchManagedAssemblyLoadContext);
			dll = context.GetResolvedUnmanagedDll(assembly, unmanagedDllName);
		}

#region Copied from AssemblyLoadContext.CoreCLR.cs, modified until our AssemblyBuilder implementation is functional
		private static RuntimeAssembly? GetRuntimeAssembly(Assembly? asm)
		{
			return
				asm == null ? null :
				asm is RuntimeAssembly rtAssembly ? rtAssembly :
				//asm is System.Reflection.Emit.AssemblyBuilder ab ? ab.InternalAssembly :
				null;
		}
#endregion
	}
}
