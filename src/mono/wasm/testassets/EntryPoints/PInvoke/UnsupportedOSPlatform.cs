using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public class Test
{
    public static int Main(string[] argv)
    {
        Console.WriteLine("TestOutput -> Main running");
        return 42;
    }
}

// Regression coverage for https://github.com/dotnet/runtime/issues/110870:
// a Windows-only P/Invoke with non-blittable parameters must be skipped by the wasm
// pinvoke collector when building for the browser, and must not emit WASM0060/WASM0062/WASM0001.
[SupportedOSPlatform("windows")]
internal static class Win32Interop
{
    [DllImport("gdi32.dll")]
    public static extern int GetCharABCWidthsFloat(HandleRef hdc, uint iFirst, uint iLast, [Out] ABCFLOAT[] lpABCF);

    [DllImport("user32.dll")]
    public static extern int MethodWithNonBlittableObject(object arg, HandleRef handle);
}

internal struct ABCFLOAT
{
#pragma warning disable CS0649 // fields are only used to describe the native struct layout
    public float abcfA;
    public float abcfB;
    public float abcfC;
#pragma warning restore CS0649
}
