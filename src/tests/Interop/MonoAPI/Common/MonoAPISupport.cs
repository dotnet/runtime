using System;
using System.Runtime.InteropServices;

namespace MonoAPI.Tests;

public class MonoAPISupport
{
    public const string TestLibName = "mono-embedding-api-test";

    [DllImport(TestLibName)]
    private static extern byte libtest_initialize_runtime_symbols(IntPtr libcoreclr_name);

    public static void Setup()
    {
	string libName = TestLibrary.XPlatformUtils.GetStandardNativeLibraryFileName("coreclr");
	if (!SetupSymbols(libName))
	    throw new Exception ($"Native library could not probe for runtime embedding API symbols in {libName}");
    }

    private static bool SetupSymbols(string libName)
    {
	IntPtr ptr = IntPtr.Zero;
	byte res = 0;
	try {
	    ptr = Marshal.StringToHGlobalAnsi(libName);
	    res = libtest_initialize_runtime_symbols(ptr);
	} finally {
	    Marshal.FreeHGlobal(ptr);
	}
	return res != 0;
    }
}
