// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using TestLibrary;

class Test
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


    public static int Main(string[] args)
    {
        UIntPtr uintPtrManaged = (UIntPtr)1000;
        UIntPtr uintPtrNative = (UIntPtr)2000;
        UIntPtr uintPtrReturn = (UIntPtr)3000;

        UIntPtr uintPtr1 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, Marshal_In(uintPtr1), "The return value is wrong");

        UIntPtr uintPtr2 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, Marshal_InOut(uintPtr2), "The return value is wrong");
        Assert.AreEqual(uintPtrManaged, uintPtr2, "The parameter value is changed");

        UIntPtr uintPtr3 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, Marshal_Out(uintPtr3), "The return value is wrong");
        Assert.AreEqual(uintPtrManaged, uintPtr3, "The parameter value is changed");

        UIntPtr uintPtr4 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, MarshalPointer_In(ref uintPtr4), "The return value is wrong");
        Assert.AreEqual(uintPtrManaged, uintPtr4, "The parameter value is changed");

        UIntPtr uintPtr5 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, MarshalPointer_InOut(ref uintPtr5), "The return value is wrong");
        Assert.AreEqual(uintPtrNative, uintPtr5, "The passed value is wrong");

        UIntPtr uintPtr6 = uintPtrManaged;
        Assert.AreEqual(uintPtrReturn, MarshalPointer_Out(out uintPtr6), "The return value is wrong");
        Assert.AreEqual(uintPtrNative, uintPtr6, "The passed value is wrong");

        return 100;
    }
}