// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

public class Test
{
    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr Marshal_In([In]UIntPtr uintPtr);

    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr Marshal_InOut([In, Out]UIntPtr uintPtr);

    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr Marshal_Out([Out]UIntPtr uintPtr);

    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr MarshalPointer_In([In]ref UIntPtr puintPtr);

    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr MarshalPointer_InOut(ref UIntPtr puintPtr);

    [DllImport(@"UIntPtrNative", CallingConvention = CallingConvention.StdCall)]
    private static extern UIntPtr MarshalPointer_Out(out UIntPtr puintPtr);


    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void TestEntryPoint()
    {
        UIntPtr uintPtrManaged = (UIntPtr)1000;
        UIntPtr uintPtrNative = (UIntPtr)2000;
        UIntPtr uintPtrReturn = (UIntPtr)3000;

        UIntPtr uintPtr1 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, Marshal_In(uintPtr1));

        UIntPtr uintPtr2 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, Marshal_InOut(uintPtr2));
        Assert.Equal(uintPtrManaged, uintPtr2);

        UIntPtr uintPtr3 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, Marshal_Out(uintPtr3));
        Assert.Equal(uintPtrManaged, uintPtr3);

        UIntPtr uintPtr4 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, MarshalPointer_In(ref uintPtr4));
        Assert.Equal(uintPtrManaged, uintPtr4);

        UIntPtr uintPtr5 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, MarshalPointer_InOut(ref uintPtr5));
        Assert.Equal(uintPtrNative, uintPtr5);

        UIntPtr uintPtr6 = uintPtrManaged;
        Assert.Equal(uintPtrReturn, MarshalPointer_Out(out uintPtr6));
        Assert.Equal(uintPtrNative, uintPtr6);
    }
}
