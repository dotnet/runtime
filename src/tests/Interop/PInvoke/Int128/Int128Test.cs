// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;


public struct StructJustInt128
{
    public StructJustInt128(Int128 val) { value = val; }
    public Int128 value;
}

public struct StructWithInt128
{
    public StructWithInt128(Int128 val) { value = val; messUpPadding = 0x10; }
    public byte messUpPadding;
    public Int128 value;
}

unsafe partial class Int128Native
{
    [DllImport(nameof(Int128Native))]
    public static extern Int128 GetInt128(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern void GetInt128Out(ulong upper, ulong lower, Int128* value);

    [DllImport(nameof(Int128Native))]
    public static extern void GetInt128Out(ulong upper, ulong lower, out Int128 value);

    [DllImport(nameof(Int128Native))]
    public static extern void GetInt128Out(ulong upper, ulong lower, out StructJustInt128 value);

    [DllImport(nameof(Int128Native))]
    public static extern ulong GetInt128Lower_S(StructJustInt128 value);

    [DllImport(nameof(Int128Native))]
    public static extern ulong GetInt128Lower(Int128 value);

    [DllImport(nameof(Int128Native))]
    public static extern Int128* GetInt128Ptr(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native), EntryPoint = "GetInt128Ptr")]
    public static extern ref readonly Int128 GetInt128Ref(ulong upper, ulong lower);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128(Int128 lhs, Int128 rhs);


    [DllImport(nameof(Int128Native))]
    public static extern void AddStructWithInt128_ByRef(ref StructWithInt128 lhs, ref StructWithInt128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern StructWithInt128 AddStructWithInt128(StructWithInt128 lhs, StructWithInt128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern StructWithInt128 AddStructWithInt128_1(long dummy1, StructWithInt128 lhs, StructWithInt128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern StructWithInt128 AddStructWithInt128_9(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, long dummy6, long dummy7, long dummy8, long dummy9, StructWithInt128 lhs, StructWithInt128 rhs);

    // Test alignment and proper register usage for Int128 with various amounts of other registers in use. These tests are designed to stress the calling convention of Arm64 and Unix X64.
    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_1(long dummy1, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_2(long dummy1, long dummy2, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_3(long dummy1, long dummy2, long dummy3, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_4(long dummy1, long dummy2, long dummy3, long dummy4, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_5(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_6(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, long dummy6, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_7(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, long dummy6, long dummy7, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_8(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, long dummy6, long dummy7, long dummy8, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128_9(long dummy1, long dummy2, long dummy3, long dummy4, long dummy5, long dummy6, long dummy7, long dummy8, long dummy9, Int128 lhs, Int128 rhs);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s(Int128* pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int128[] pValues, int count);

    [DllImport(nameof(Int128Native))]
    public static extern Int128 AddInt128s(in Int128 pValues, int count);
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe partial class Int128Native
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69399", TestRuntimes.Mono)]
    public static void TestInt128FieldLayout()
    {
        // This test checks that the alignment rules of Int128 structs match the native compiler
        StructWithInt128 lhs = new StructWithInt128(new Int128(11, 12));
        StructWithInt128 rhs = new StructWithInt128(new Int128(13, 14));

        Int128Native.AddStructWithInt128_ByRef(ref lhs, ref rhs);
        Assert.Equal(new StructWithInt128(new Int128(24, 26)), lhs);

        Int128 value2;
        Int128Native.GetInt128Out(3, 4, &value2);
        Assert.Equal(new Int128(3, 4), value2);

        Int128Native.GetInt128Out(5, 6, out Int128 value3);
        Assert.Equal(new Int128(5, 6), value3);

        StructJustInt128 value4;
        Int128Native.GetInt128Out(7, 8, out value4);
        Assert.Equal(new StructJustInt128(new Int128(7, 8)), value4);

        // Until we implement the correct abi for Int128, validate that we don't marshal to native

        // Checking return value
        Assert.Throws<System.Runtime.InteropServices.MarshalDirectiveException>(() => GetInt128(0, 1));

        // Checking input value as Int128 itself
        Assert.Throws<System.Runtime.InteropServices.MarshalDirectiveException>(() => GetInt128Lower(default(Int128)));

        // Checking input value as structure wrapping Int128
        Assert.Throws<System.Runtime.InteropServices.MarshalDirectiveException>(() => GetInt128Lower_S(default(StructJustInt128)));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/74209")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69531", TestRuntimes.Mono)]
    public static void TestInt128()
    {
        Int128 value1 = Int128Native.GetInt128(1, 2);
        Assert.Equal(new Int128(1, 2), value1);


        Int128* value4 = Int128Native.GetInt128Ptr(7, 8);
        Assert.Equal(new Int128(7, 8), *value4);

        ref readonly Int128 value5 = ref Int128Native.GetInt128Ref(9, 10);
        Assert.Equal(new Int128(9, 10), value5);

        Int128 value6 = Int128Native.AddInt128(new Int128(11, 12), new Int128(13, 14));
        Assert.Equal(new Int128(24, 26), value6);

        Int128[] values = new Int128[] {
            new Int128(15, 16),
            new Int128(17, 18),
            new Int128(19, 20),
            new Int128(21, 22),
            new Int128(23, 24),
        };

        fixed (Int128* pValues = &values[0])
        {
            Int128 value7 = Int128Native.AddInt128s(pValues, values.Length);
            Assert.Equal(new Int128(95, 100), value7);
        }

        Int128 value8 = Int128Native.AddInt128s(values, values.Length);
        Assert.Equal(new Int128(95, 100), value8);

        Int128 value9 = Int128Native.AddInt128s(in values[0], values.Length);
        Assert.Equal(new Int128(95, 100), value9);

        // Test ABI alignment issues on Arm64 and Unix X64, both enregistered and while spilled to the stack
        Int128 value10 = Int128Native.AddInt128_1(1, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value10);

        Int128 value11 = Int128Native.AddInt128_2(1, 2, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value11);

        Int128 value12 = Int128Native.AddInt128_3(1, 2, 3, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value12);

        Int128 value13 = Int128Native.AddInt128_4(1, 2, 3, 4, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value13);

        Int128 value14 = Int128Native.AddInt128_5(1, 2, 3, 4, 5, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value14);

        Int128 value15 = Int128Native.AddInt128_6(1, 2, 3, 4, 5, 6, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value15);

        Int128 value16 = Int128Native.AddInt128_7(1, 2, 3, 4, 5, 6, 7, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value16);

        Int128 value17 = Int128Native.AddInt128_8(1, 2, 3, 4, 5, 6, 7, 8, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value17);

        Int128 value18 = Int128Native.AddInt128_9(1, 2, 3, 4, 5, 6, 7, 8, 9, new Int128(25, 26), new Int128(27, 28));
        Assert.Equal(new Int128(52, 54), value18);

        // Test alignment within a structure
        StructWithInt128 value19 = Int128Native.AddStructWithInt128(new StructWithInt128(new Int128(29, 30)), new StructWithInt128(new Int128(31, 32)));
        Assert.Equal(new StructWithInt128(new Int128(60, 62)), value19);

        // Test abi register alignment within a structure
        StructWithInt128 value20 = Int128Native.AddStructWithInt128_1(1, new StructWithInt128(new Int128(29, 30)), new StructWithInt128(new Int128(31, 32)));
        Assert.Equal(new StructWithInt128(new Int128(60, 62)), value20);

        // Test abi alignment when spilled to the stack
        StructWithInt128 value21 = Int128Native.AddStructWithInt128_9(1, 2, 3, 4, 5, 6, 7, 8, 9, new StructWithInt128(new Int128(29, 30)), new StructWithInt128(new Int128(31, 32)));
        Assert.Equal(new StructWithInt128(new Int128(60, 62)), value21);
    }
}
