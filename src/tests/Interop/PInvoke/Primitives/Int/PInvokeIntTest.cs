// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class ClientPInvokeIntNativeTest
{
    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_In([In]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_InOut([In, Out]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_Out([Out]int intValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_In([In]ref int pintValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_InOut(ref int pintValue);

    [DllImport("PInvokeIntNative")]
    private static extern int MarshalPointer_Out(out int pintValue);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_InMany([In]short i1, [In]short i2, [In]short i3, [In]short i4, [In]short i5, [In]short i6, [In]short i7, [In]short i8, [In]short i9, [In]short i10, [In]short i11, [In]byte i12, [In]byte i13, [In]int i14, [In]short i15);

    [DllImport("PInvokeIntNative")]
    private static extern int Marshal_InMany_InOutPointer([In]short i1, [In]short i2, [In]short i3, [In]short i4, [In]short i5, [In]short i6, [In]short i7, [In]short i8, [In]short i9, [In]short i10, [In]short i11, [In]byte i12, [In]byte i13, [In]int i14, [In]short i15, ref int pintValue);


    [Fact]
    public static int TestEntryPoint()
    {
        int failures = 0;
        int intManaged = (int)1000;
        int intNative = (int)2000;
        int intReturn = (int)3000;

        int int1 = intManaged;
        if(intReturn != Marshal_In(int1))
        {
            failures++;
            Console.WriteLine("In return value is wrong");
        }

        int int2 = intManaged;
        if (intReturn != Marshal_InOut(int2))
        {
            failures++;
            Console.WriteLine("InOut return value is wrong");
        }
        if (intManaged != int2)
        {
            failures++;
            Console.WriteLine("InOut byval parameter value changed.");
        }

        int int3 = intManaged;
        if (intReturn != Marshal_Out(int3))
        {
            failures++;
            Console.WriteLine("Out return value is wrong");
        }
        if (intManaged != int3)
        {
            failures++;
            Console.WriteLine("Out byval parameter value changed.");
        }

        int int4 = intManaged;
        if (intReturn != MarshalPointer_In(ref int4))
        {
            failures++;
            Console.WriteLine("RefIn return value is wrong");
        }
        if (intManaged != int4)
        {
            failures++;
            Console.WriteLine("In byref parameter value changed.");
        }

        int int5 = intManaged;
        if (intReturn != MarshalPointer_InOut(ref int5))
        {
            failures++;
            Console.WriteLine("RefInOut return value is wrong");
        }
        if (intNative != int5)
        {
            failures++;
            Console.WriteLine("InOut byref value is wrong.");
        }

        int int6 = intManaged;
        if (intReturn != MarshalPointer_Out(out int6))
        {
            failures++;
            Console.WriteLine("RefOut return value is wrong");
        }
        if (intNative != int6)
        {
            failures++;
            Console.WriteLine("Out byref value is wrong.");
        }

        if(120 != Marshal_InMany(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15))
        {
            failures++;
            Console.WriteLine("InMany return value is wrong");
        }

        int int7 = intManaged;
        if(120 != Marshal_InMany_InOutPointer(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, ref int7))
        {
            failures++;
            Console.WriteLine("InMany_InOutPointer return value is wrong");
        }
        if (intNative != int7)
        {
            failures++;
            Console.WriteLine("InMany_InOutPointer byref value is wrong.");
        }

        return 100 + failures;
    }
}
