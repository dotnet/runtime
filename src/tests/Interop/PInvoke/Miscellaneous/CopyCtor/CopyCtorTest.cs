// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Miscellaneous;

public static unsafe class CopyCtor
{
    private static void ResetCounters()
    {
        StructWithCtor.CopyCtorCallCount = 0;
        StructWithCtor.DtorCallCount = 0;
    }

    public static unsafe int StructWithCtorTest(StructWithCtor* ptrStruct, ref StructWithCtor refStruct)
    {
        if (ptrStruct->_instanceField != 1)
        {
            Console.WriteLine($"Fail: {ptrStruct->_instanceField} != {1}");
            return 1;
        }
        if (refStruct._instanceField != 2)
        {
            Console.WriteLine($"Fail: {refStruct._instanceField} != {2}");
            return 2;
        }

        int expectedCallCount = RuntimeInformation.ProcessArchitecture == Architecture.X86 ? 2 : 0;

        if (StructWithCtor.CopyCtorCallCount != expectedCallCount)
        {
            Console.WriteLine($"Fail: {StructWithCtor.CopyCtorCallCount} != {expectedCallCount}");
            return 3;
        }
        if (StructWithCtor.DtorCallCount != expectedCallCount)
        {
            Console.WriteLine($"Fail: {StructWithCtor.DtorCallCount} != {expectedCallCount}");
            return 4;
        }

        return 100;
    }

    public static unsafe int StructWithCtorWithPrecedingParameterTest(int* ptrInt, StructWithCtor* ptrStruct)
    {
        if (*ptrInt != 42)
        {
            Console.WriteLine($"Fail: {*ptrInt} != {42}");
            return 1;
        }
        if (ptrStruct->_instanceField != 3)
        {
            Console.WriteLine($"Fail: {ptrStruct->_instanceField} != {3}");
            return 2;
        }

        int expectedCallCount = RuntimeInformation.ProcessArchitecture == Architecture.X86 ? 1 : 0;

        if (StructWithCtor.CopyCtorCallCount != expectedCallCount)
        {
            Console.WriteLine($"Fail: {StructWithCtor.CopyCtorCallCount} != {expectedCallCount}");
            return 3;
        }
        if (StructWithCtor.DtorCallCount != expectedCallCount)
        {
            Console.WriteLine($"Fail: {StructWithCtor.DtorCallCount} != {expectedCallCount}");
            return 4;
        }

        return 100;
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsWindows))]
    [SkipOnMono("Not supported on Mono")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [SkipOnCoreClr("JitStress can introduce extra copies", RuntimeTestModes.JitStress)]
    [Xunit.SkipOnCoreClrAttribute("Depends on marshalled calli", RuntimeTestModes.InterpreterActive)]
    public static unsafe void ValidateCopyConstructorAndDestructorCalled()
    {
        ResetCounters();

        CopyCtorUtil.TestDelegate del = (CopyCtorUtil.TestDelegate)Delegate.CreateDelegate(typeof(CopyCtorUtil.TestDelegate), typeof(CopyCtor).GetMethod(nameof(StructWithCtorTest)));
        StructWithCtor s1 = new StructWithCtor();
        StructWithCtor s2 = new StructWithCtor();
        s1._instanceField = 1;
        s2._instanceField = 2;
        Assert.Equal(100, FunctionPointer.Call_FunctionPointer(Marshal.GetFunctionPointerForDelegate(del), &s1, ref s2));

        GC.KeepAlive(del);
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsWindows))]
    [SkipOnMono("Not supported on Mono")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [SkipOnCoreClr("JitStress can introduce extra copies", RuntimeTestModes.JitStress)]
    [Xunit.SkipOnCoreClrAttribute("Depends on marshalled calli", RuntimeTestModes.InterpreterActive)]
    public static unsafe void ValidateCopyConstructorCalledWithMissingParameterMetadata()
    {
        ResetCounters();

        CopyCtorUtil.DelegateWithMissingParameterMetadata del = (CopyCtorUtil.DelegateWithMissingParameterMetadata)Delegate.CreateDelegate(
            typeof(CopyCtorUtil.DelegateWithMissingParameterMetadata),
            typeof(CopyCtor).GetMethod(nameof(StructWithCtorWithPrecedingParameterTest)));
        int i = 42;
        StructWithCtor s = new StructWithCtor();
        s._instanceField = 3;
        Assert.Equal(100, FunctionPointer.Call_FunctionPointer_MissingParameterMetadata(Marshal.GetFunctionPointerForDelegate(del), &i, &s));

        GC.KeepAlive(del);
    }
}
