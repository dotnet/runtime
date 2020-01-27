// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	partial class NativeLibrary
	{
		static IntPtr LoadLibraryByName (string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
		{
			return LoadByName (libraryName,
			                   (RuntimeAssembly)assembly,
			                   searchPath.HasValue,
			                   (uint)searchPath.GetValueOrDefault(),
			                   throwOnError);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static IntPtr LoadFromPath (string libraryName, bool throwOnError);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static IntPtr LoadByName (string libraryName, RuntimeAssembly callingAssembly, bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, bool throwOnError);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void FreeLib (IntPtr handle);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static IntPtr GetSymbol (IntPtr handle, string symbolName, bool throwOnError);

		private static void MonoLoadLibraryCallbackStub (string libraryName, Assembly assembly, bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags, ref IntPtr dll)
		{
			dll = LoadLibraryCallbackStub (libraryName, assembly, hasDllImportSearchPathFlags, dllImportSearchPathFlags);
		}
	}
}
