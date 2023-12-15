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
    public static unsafe int StructWithCtorTest(StructWithCtor* ptrStruct, ref StructWithCtor refStruct)
    {
        if (ptrStruct->_instanceField != 1)
            return 1;
        if (refStruct._instanceField != 2)
            return 2;

        if (StructWithCtor.CopyCtorCallCount != 2)
            return 3;
        if (StructWithCtor.DtorCallCount != 2)
            return 4;


        return 100;
    }

    [Fact]
    [SkipOnMono("Not supported on Mono")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static unsafe void ValidateCopyConstructorAndDestructorCalled()
    {
        CopyCtorUtil.TestDelegate del = (CopyCtorUtil.TestDelegate)Delegate.CreateDelegate(typeof(CopyCtorUtil.TestDelegate), typeof(CopyCtor).GetMethod("StructWithCtorTest"));
        StructWithCtor s1 = new StructWithCtor();
        StructWithCtor s2 = new StructWithCtor();
        s1._instanceField = 1;
        s2._instanceField = 2;
        Assert.Equal(100, FunctionPointer.Call_FunctionPointer(Marshal.GetFunctionPointerForDelegate(del), &s1, ref s2));

        GC.KeepAlive(del);
    }
}
