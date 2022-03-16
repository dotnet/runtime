// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using static TestHelpers;

class GetLibraryExportTests : IDisposable
{
    private readonly IntPtr handle;

    public GetLibraryExportTests()
    {
        handle = NativeLibrary.Load(NativeLibraryToLoad.GetFullPath());
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsX86))]
    public void GetValidExport_ManualMangling()
    {
        EXPECT(GetLibraryExport(handle, "_NativeSum@8"));
        EXPECT(TryGetLibraryExport(handle, "_NativeSum@8"));
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotX86))]
    public void GetValidExport()
    {
        EXPECT(GetLibraryExport(handle, "NativeSum"));
        EXPECT(TryGetLibraryExport(handle, "NativeSum"));
    }

    [Fact]
    public void NullHandle()
    {
        EXPECT(GetLibraryExport(IntPtr.Zero, "NativeSum"), TestResult.ArgumentNull);
        EXPECT(TryGetLibraryExport(IntPtr.Zero, "NativeSum"), TestResult.ArgumentNull);
    }

    [Fact]
    public void NullExport()
    {
        EXPECT(GetLibraryExport(handle, null), TestResult.ArgumentNull);
        EXPECT(TryGetLibraryExport(handle, null), TestResult.ArgumentNull);
    }

    [Fact]
    public void ExportDoesNotExist()
    {
        EXPECT(GetLibraryExport(handle, "NonNativeSum"), TestResult.EntryPointNotFound);
        EXPECT(TryGetLibraryExport(handle, "NonNativeSum"), TestResult.ReturnFailure);
    }


    public void Dispose() => NativeLibrary.Free(handle);
    
    static TestResult GetLibraryExport(IntPtr handle, string name)
    {
        return Run(() => {
            IntPtr address = NativeLibrary.GetExport(handle, name);
            if (address == IntPtr.Zero)
                return TestResult.ReturnNull;
            if (RunExportedFunction(address, 1, 1) != 2)
                return TestResult.IncorrectEvaluation;
            return TestResult.Success;
        });
    }

    static TestResult TryGetLibraryExport(IntPtr handle, string name)
    {
        return Run(() => {
            IntPtr address = IntPtr.Zero;
            bool success = NativeLibrary.TryGetExport(handle, name, out address);
            if (!success)
                return TestResult.ReturnFailure;
            if (address == IntPtr.Zero)
                return TestResult.ReturnNull;
            if (RunExportedFunction(address, 1, 1) != 2)
                return TestResult.IncorrectEvaluation;
            return TestResult.Success;
        });
    }

    private static unsafe int RunExportedFunction(IntPtr address, int arg1, int arg2)
    {
        return ((delegate* unmanaged<int, int, int>)address)(arg1, arg2);
    }
}
