// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	partial class NativeLibrary
	{
		const string SystemLibrary = "libdl";

		[DllImport (SystemLibrary)]
		static extern IntPtr dlopen (string libName, int flags);

		[DllImport (SystemLibrary)]
		static extern IntPtr dlsym (IntPtr handle, string symbol);

		[DllImport (SystemLibrary)]
		static extern void dlclose (IntPtr handle);

		static IntPtr LoadLibraryByName (string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
		{
			if (searchPath != null) {
				throw new NotImplementedException ($"LoadLibraryByName+DllImportSearchPath is not implemented");
			}
			return LoadFromPath (libraryName, throwOnError);
		}

		static IntPtr LoadFromPath (string libraryName, bool throwOnError)
		{
			const int RTLD_LAZY = 0x001;
			
			IntPtr ptr = dlopen (libraryName, RTLD_LAZY);
			if (ptr == IntPtr.Zero && throwOnError) {
				throw new DllNotFoundException ();
			}
			return ptr;
		}

		static IntPtr LoadByName (string libraryName, RuntimeAssembly callingAssembly, bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, bool throwOnError)
		{
			if (hasDllImportSearchPathFlag) {
				throw new NotImplementedException ($"LoadByName+DllImportSearchPath is not implemented");
			}
			return LoadLibraryByName (libraryName, null, null, throwOnError);
		}

		static void FreeLib (IntPtr handle) => dlclose (handle);

		static IntPtr GetSymbol (IntPtr handle, string symbolName, bool throwOnError)
		{
			IntPtr symbol = dlsym (handle, symbolName);
			if (symbol == IntPtr.Zero && throwOnError) {
				throw new Exception ($"{symbolName} was not found (handle={handle})");
			}
			return symbol;
		}
	}
}
