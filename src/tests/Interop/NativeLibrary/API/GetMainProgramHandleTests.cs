// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using static TestHelpers;

class GetMainProgramHandleTests
{
    // Mobile test runs aren't hosted by corerun, so we don't have a well-known export to test here
    [ConditionalFact(nameof(IsHostedByCoreRun))]
    public static void CanAccessCoreRunExportFromMainProgramHandle()
    {
        EXPECT(GetSymbolFromMainProgramHandle("HostExecutable", "GetCurrentClrDetails"));
        EXPECT(GetSymbolFromMainProgramHandle("HostExecutable", "NonExistentCoreRunExport"), TestResult.ReturnFailure);
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Apple platforms load library symbols globally by default.")]
    public static void NativeLibraryLoadDoesNotLoadSymbolsGlobally()
    {
        IntPtr handle = NativeLibrary.Load(NativeLibraryToLoad.GetFullPath());
        try
        {
            // NativeLibrary does not load symbols globally, so we shouldn't be able to discover symbols from libaries loaded
            // with NativeLibary.Load.
            EXPECT(GetSymbolFromMainProgramHandle("LocallyLoadedNativeLib", TestLibrary.Utilities.IsX86 ? "_NativeSum@8" : "NativeSum"),  TestResult.ReturnFailure);
            EXPECT(GetSymbolFromMainProgramHandle("LocallyLoadedNativeLib", "NonNativeSum"), TestResult.ReturnFailure);

        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotX86))]
    [SkipOnPlatform(TestPlatforms.Windows, "Windows does not have a concept of globally loaded symbols")]
    public static void GloballyLoadedLibrarySymbolsVisibleFromMainProgramHandle()
    {
        // On non-Windows platforms, symbols from globally loaded shared libraries will also be discoverable.
        // Globally loading symbols is not the .NET default, so we use a call to dlopen in native code
        // with the right flags to test the scenario.
        IntPtr handle = LoadLibraryGlobally(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), NativeLibraryToLoad.GetLibraryFileName("GloballyLoadedNativeLibrary")));

        try
        {
            EXPECT(GetSymbolFromMainProgramHandle("GloballyLoadedNativeLib", "NativeMultiply"));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotX86))]
    [SkipOnPlatform(TestPlatforms.Windows, "Windows does not have a concept of globally loaded symbols")]
    public static void InvalidSymbolName_Fails()
    {
        // On non-Windows platforms, symbols from globally loaded shared libraries will also be discoverable.
        // Globally loading symbols is not the .NET default, so we use a call to dlopen in native code
        // with the right flags to test the scenario.
        IntPtr handle = LoadLibraryGlobally(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), NativeLibraryToLoad.GetLibraryFileName("GloballyLoadedNativeLibrary")));

        try
        {
            EXPECT(GetSymbolFromMainProgramHandle("GloballyLoadedNativeLib", "NonNativeMultiply"), TestResult.ReturnFailure);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsX86))]
    [SkipOnPlatform(TestPlatforms.Windows, "Windows does not have a concept of globally loaded symbols")]
    public static void GloballyLoadedLibrarySymbolsVisibleFromMainProgramHandle_Mangling()
    {
        // On non-Windows platforms, symbols from globally loaded shared libraries will also be discoverable.
        // Globally loading symbols is not the .NET default, so we use a call to dlopen in native code
        // with the right flags to test the scenario.
        IntPtr handle = LoadLibraryGlobally(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), NativeLibraryToLoad.GetLibraryFileName("GloballyLoadedNativeLibrary")));

        try
        {
            EXPECT(GetSymbolFromMainProgramHandle("GloballyLoadedNativeLib", "_NativeMultiply@8"));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [Fact]
    public static void FreeMainProgramHandle()
    {
        NativeLibrary.Free(NativeLibrary.GetMainProgramHandle());
        Assert.True(true);
    }

    public static bool IsHostedByCoreRun { get; } = Process.GetCurrentProcess().MainModule.ModuleName is "corerun" or "corerun.exe";

    static TestResult GetSymbolFromMainProgramHandle(string scenarioName, string symbolToLoadFromHandle)
    {
        return Run(() => {
            IntPtr moduleHandle = NativeLibrary.GetMainProgramHandle();
            bool success = NativeLibrary.TryGetExport(moduleHandle, symbolToLoadFromHandle, out IntPtr address);
            if (!success)
                return TestResult.ReturnFailure;
            if (address == IntPtr.Zero)
                return TestResult.ReturnNull;
            return TestResult.Success;
        });
    }

    static IntPtr LoadLibraryGlobally(string name)
    {
        IntPtr handle = LoadLibraryGloballyNative(name);

        if (handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        return handle;

        [DllImport("GlobalLoadHelper", EntryPoint = "LoadLibraryGlobally", SetLastError = true)]
        static extern IntPtr LoadLibraryGloballyNative(string name);
    }
}
