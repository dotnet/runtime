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

		static IntPtr InternalLoadUnmanagedDllFromPath (string unmanagedDllPath)
		{
			throw new NotImplementedException ();
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

		public static Assembly[] GetLoadedAssemblies ()
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
				if (ptrAssemblyLoadContext == IntPtr.Zero)
				{
					// If the load context is returned null, then the assembly was bound using the TPA binder
					// and we shall return reference to the active "Default" binder - which could be the TPA binder
					// or an overridden CLRPrivBinderAssemblyLoadContext instance.
					loadContextForAssembly = AssemblyLoadContext.Default;
				} else {
					loadContextForAssembly = (AssemblyLoadContext) (GCHandle.FromIntPtr (ptrAssemblyLoadContext).Target)!;
				}
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
		private static Assembly? MonoResolveUsingLoad (IntPtr gchALC, string assemblyName)
		{
			return Resolve (gchALC, new AssemblyName (assemblyName));
		}

		// Invoked by Mono to resolve using the Resolving event after
		// trying the Load override and default load context without
		// success.
		private static Assembly? MonoResolveUsingResolvingEvent (IntPtr gchALC, string assemblyName)
		{
			return ResolveUsingResolvingEvent (gchALC, new AssemblyName (assemblyName));
		}

		// Invoked by Mono to resolve requests to load satellite assemblies. 
		private static Assembly? MonoResolveUsingResolveSatelliteAssembly (IntPtr gchALC, string assemblyName)
		{
			return ResolveSatelliteAssembly (gchALC, new AssemblyName (assemblyName));
		}


#region Copied from AssemblyLoadContext.CoreCLR.cs
		// WISH: let's share this code

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

			Func<AssemblyLoadContext, AssemblyName, Assembly>? assemblyResolveHandler = _resolving;

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
#endregion
	}
}
