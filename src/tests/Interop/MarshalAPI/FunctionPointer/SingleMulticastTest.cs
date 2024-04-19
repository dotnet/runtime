// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using Xunit;

partial class FunctionPtr
{
    public delegate bool DelegateWithLong(long l); //Singlecast delegate
    public delegate void MultiDelegateWithLong(long l); //Multicast delegate

    public static DelegateWithLong s_DelWithLongBool = new DelegateWithLong(MethodWithLongBool);
    public static MultiDelegateWithLong s_MultidelWithLong = new MultiDelegateWithLong(MethodWithLong);

    [Fact]
    public static void RunGetFcnPtrSingleMulticastTest()
    {
        Console.WriteLine($"Running {nameof(RunGetFcnPtrSingleMulticastTest)}...");

        {
            IntPtr fcnptr = Marshal.GetFunctionPointerForDelegate<DelegateWithLong>(s_DelWithLongBool);
            Assert.True(FunctionPointerNative.CheckFcnPtr(fcnptr));
        }

        {
            IntPtr fcnptr = Marshal.GetFunctionPointerForDelegate<MultiDelegateWithLong>(s_MultidelWithLong);
            FunctionPointerNative.CheckFcnPtr(fcnptr);
        }
    }

    private static bool MethodWithLongBool(long l)
    {
        if (l != 999999999999)
            return false;
        else
            return true;
    }

    private static void MethodWithLong(long l)
    {
        if (l != 999999999999)
            throw new Exception("Failed multicast call");
    }
}
