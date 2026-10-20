using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// The version check guards the call against CA1416, since the call site itself is reachable on
// all browser versions while the pinvoke is annotated as supported on browser 1.0 and later.
if (OperatingSystem.IsOSPlatformVersionAtLeast("browser", 1, 0))
{
    Console.WriteLine($"TestOutput -> sum: {VersionedBrowserInterop.versioned_browser_add(19, 23)}");
}
return 42;

// Regression coverage for https://github.com/dotnet/runtime/issues/132297:
// a versioned platform attribute like [SupportedOSPlatform("browser1.0")] must still match
// TargetOS=browser, so this pinvoke must be included in the generated pinvoke table and
// callable at runtime.
[SupportedOSPlatform("browser1.0")]
internal static class VersionedBrowserInterop
{
    [DllImport("versioned-osplatform")]
    public static extern int versioned_browser_add(int a, int b);
}

// A versioned platform attribute for a different OS must not match TargetOS=browser, so this
// pinvoke must be skipped and must not appear in the generated pinvoke table.
[SupportedOSPlatform("windows1.0")]
internal static class VersionedWindowsInterop
{
    [DllImport("versioned-osplatform")]
    public static extern int versioned_windows_add(int a, int b);
}
