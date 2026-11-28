// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135394
{
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void TestEntryPoint()
    {
        Assert.Equal(-1, Test(new ShortField { Value = -1 }));
        Assert.Equal(short.MinValue, Test(new ShortField { Value = short.MinValue }));
        Assert.Equal(-1, Test(new SByteField { Value = -1 }));
        Assert.Equal(sbyte.MinValue, Test(new SByteField { Value = sbyte.MinValue }));
        Assert.Equal(-2, TestFieldCopy(new ShortField { Value = -1 }));
        Assert.Equal(2 * short.MinValue, TestFieldCopy(new ShortField { Value = short.MinValue }));
        Assert.Equal(-2, TestFieldCopy(new SByteField { Value = -1 }));
        Assert.Equal(2 * sbyte.MinValue, TestFieldCopy(new SByteField { Value = sbyte.MinValue }));
        Assert.Equal(-2, TestScalarCopy((short)-1));
        Assert.Equal(-2, TestScalarCopy((sbyte)-1));
        Assert.Equal(-1, InvokeShortPrimitiveCopyWithDirtyBits());
        Assert.Equal(-1, InvokeSBytePrimitiveCopyWithDirtyBits());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(ShortField value)
    {
        short result = 0;
        Unsafe.As<short, ShortField>(ref result) = value;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(SByteField value)
    {
        sbyte result = 0;
        Unsafe.As<sbyte, SByteField>(ref result) = value;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestFieldCopy(ShortField value)
    {
        ShortField result = value;
        return Test(result) + result.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestFieldCopy(SByteField value)
    {
        SByteField result = value;
        return Test(result) + result.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestScalarCopy(short value)
    {
        ShortField result = Unsafe.As<short, ShortField>(ref value);
        return Test(result) + result.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestScalarCopy(sbyte value)
    {
        SByteField result = Unsafe.As<sbyte, SByteField>(ref value);
        return Test(result) + result.Value;
    }

    // Non-promoted, non-struct-field locals are normalized on load, so the JIT may read them
    // with undefined upper bits. A plain call always sign/zero-extends the argument, so this
    // path needs an irregular, mismatched-signature call to expose unnormalized bits in the
    // argument register, the way a native caller legally could.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestPrimitiveCopy(short value)
    {
        short result = 0;
        Unsafe.As<short, ShortField>(ref result) = Unsafe.As<short, ShortField>(ref value);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestPrimitiveCopy(sbyte value)
    {
        sbyte result = 0;
        Unsafe.As<sbyte, SByteField>(ref result) = Unsafe.As<sbyte, SByteField>(ref value);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int InvokeShortPrimitiveCopyWithDirtyBits()
    {
        delegate*<short, int> typed = &TestPrimitiveCopy;
        delegate*<int, int> dirty = (delegate*<int, int>)typed;
        return dirty(0x1FFFF);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int InvokeSBytePrimitiveCopyWithDirtyBits()
    {
        delegate*<sbyte, int> typed = &TestPrimitiveCopy;
        delegate*<int, int> dirty = (delegate*<int, int>)typed;
        return dirty(0x1FF);
    }

    private struct ShortField
    {
        public short Value;
    }

    private struct SByteField
    {
        public sbyte Value;
    }
}
