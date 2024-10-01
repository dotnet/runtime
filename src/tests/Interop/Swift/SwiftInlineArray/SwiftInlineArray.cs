// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class SwiftInlineArray
{
    private const string SwiftLib = "libSwiftInlineArray.dylib";

    [InlineArray(32)]
    struct F0
    {
        private byte _element0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftInlineArray10swiftFunc02a0SiAA2F0V_tF")]
    private static extern nint SwiftFunc0(F0 a0);

    [Fact]
    public static unsafe void TestFuncWithByteInlineArray()
    {
        F0 a0 = default;
        byte* ptr = (byte*)&a0;

        ptr[0] = (byte)122;
        ptr[1] = (byte)223;
        ptr[2] = (byte)66;
        ptr[3] = (byte)1;
        ptr[4] = (byte)135;
        ptr[5] = (byte)209;
        ptr[6] = (byte)54;
        ptr[7] = (byte)221;
        ptr[8] = (byte)24;
        ptr[9] = (byte)104;
        ptr[10] = (byte)21;
        ptr[11] = (byte)222;
        ptr[12] = (byte)156;
        ptr[13] = (byte)241;
        ptr[14] = (byte)97;
        ptr[15] = (byte)141;
        ptr[16] = (byte)239;
        ptr[17] = (byte)184;
        ptr[18] = (byte)69;
        ptr[19] = (byte)247;
        ptr[20] = (byte)134;
        ptr[21] = (byte)121;
        ptr[22] = (byte)204;
        ptr[23] = (byte)45;
        ptr[24] = (byte)112;
        ptr[25] = (byte)166;
        ptr[26] = (byte)220;
        ptr[27] = (byte)221;
        ptr[28] = (byte)86;
        ptr[29] = (byte)197;
        ptr[30] = (byte)178;
        ptr[31] = (byte)29;


        long result = SwiftFunc0(a0);
        Assert.Equal(8091295595945034296, result);
        Console.WriteLine("OK");
    }


    [InlineArray(8)]
    struct F1
    {
        private int _element0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftInlineArray10swiftFunc12a0SiAA2F1V_tF")]
    private static extern nint SwiftFunc1(F1 a0);

    [Fact]
    public static unsafe void TestFuncWithIntInlineArray()
    {
        F1 a0 = default;
        int* ptr = (int*)&a0;

        ptr[0] = (int)-1172606642;
        ptr[1] = (int)2004011304;
        ptr[2] = (int)-1751053775;
        ptr[3] = (int)-1361536584;
        ptr[4] = (int)1578364919;
        ptr[5] = (int)1205365715;
        ptr[6] = (int)-883274792;
        ptr[7] = (int)-550660826;


        long result = SwiftFunc1(a0);
        Assert.Equal(8444261314257660732, result);
        Console.WriteLine("OK");
    }

    [InlineArray(6)]
    struct F2
    {
        private ulong _element0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftInlineArray10swiftFunc22a0SiAA2F2V_tF")]
    private static extern nint SwiftFunc2(F2 a0);

    [Fact]
    public static unsafe void TestFuncWithLargeInlineArray()
    {
        F2 a0 = default;
        ulong* ptr = (ulong*)&a0;

        ptr[0] = (ulong)163054281557578879;
        ptr[1] = (ulong)3715665182263428629;
        ptr[2] = (ulong)15352099497683712058;
        ptr[3] = (ulong)9456667702469177637;
        ptr[4] = (ulong)5768234261922277852;
        ptr[5] = (ulong)17154681812528174574;


        long result = SwiftFunc2(a0);
        Assert.Equal(-627554439188077294, result);
        Console.WriteLine("OK");
    }

    [InlineArray(1)]
    struct F3
    {
        private byte _element0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftInlineArray10swiftFunc32a0SiAA2F3V_tF")]
    private static extern nint SwiftFunc3(F3 a0);

    [Fact]
    public static unsafe void TestFuncWithSingleElementInlineArray()
    {
        F3 a0 = default;
        byte* ptr = (byte*)&a0;

        ptr[0] = (byte)177;


        long result = SwiftFunc3(a0);
        Assert.Equal(-5808468912223652740, result);
        Console.WriteLine("OK");
    }
}
