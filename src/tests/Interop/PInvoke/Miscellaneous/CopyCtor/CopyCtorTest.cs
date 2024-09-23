// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

static unsafe class CopyCtor
{
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

        int expectedCallCount = 2;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
        {
            expectedCallCount = 4;
        }

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

    public static unsafe int Main()
    {
        if (!TestLibrary.PlatformDetection.IsWindows)
        {
            return 100;
        }

        TestDelegate del = (TestDelegate)Delegate.CreateDelegate(typeof(TestDelegate), typeof(CopyCtor).GetMethod("StructWithCtorTest"));
        StructWithCtor s1 = new StructWithCtor();
        StructWithCtor s2 = new StructWithCtor();
        s1._instanceField = 1;
        s2._instanceField = 2;
        int returnVal = FunctionPointer.Call_FunctionPointer(Marshal.GetFunctionPointerForDelegate(del), &s1, ref s2);

        GC.KeepAlive(del);

        return returnVal;
    }
}
