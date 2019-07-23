// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
			throw new NotImplementedException ();
		}

		public static AssemblyLoadContext GetLoadContext (Assembly assembly)
		{
			throw new NotImplementedException ();
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

		internal static Assembly DoAssemblyResolve (string name)
		{
			return AssemblyResolve (null, new ResolveEventArgs (name));
		}
	}
}
