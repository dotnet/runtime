// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class EnumIntrinsics
{
    public enum SByteEnum : sbyte { Min = sbyte.MinValue, Neg = -1, Zero = 0, Max = sbyte.MaxValue }
    public enum ByteEnum : byte { Min = 0, Max = 255 }
    public enum ShortEnum : short { Min = short.MinValue, Max = short.MaxValue }
    public enum UShortEnum : ushort { Min = 0, Max = ushort.MaxValue }
    public enum IntEnum : int { Min = int.MinValue, Zero = 0, Max = int.MaxValue }
    public enum UIntEnum : uint { Min = 0, Max = uint.MaxValue }
    public enum LongEnum : long { Min = long.MinValue, Max = long.MaxValue }
    public enum ULongEnum : ulong { Min = 0, Max = ulong.MaxValue }
    [Flags] public enum FlagsEnum { None = 0, A = 1, B = 2, All = 3 }

    [Fact]
    public static void TestEntryPoint()
    {
        TestSimpleEnums();
        TestGenericEnums();
        TestDifferentUnderlyingTypes();
        TestCornerCases();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestSimpleEnums()
    {
        // Testing bit-pattern identities
        Assert.True(SByteEnum.Neg.Equals((SByteEnum)(-1)));
        Assert.True(ByteEnum.Max.Equals((ByteEnum)255));
        Assert.False(SByteEnum.Max.Equals(SByteEnum.Min));

        // Flags behavior (bitwise equality)
        FlagsEnum flags = FlagsEnum.A | FlagsEnum.B;
        Assert.True(flags.Equals(FlagsEnum.All));
        Assert.False(flags.Equals(FlagsEnum.A));

        // 64-bit boundaries
        Assert.True(ULongEnum.Max.Equals((ULongEnum)ulong.MaxValue));
        Assert.True(LongEnum.Min.Equals((LongEnum)long.MinValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestGenericEnums()
    {
        // Testing generic instantiation for every width
        Assert.True(CheckGenericEquals(SByteEnum.Max, SByteEnum.Max));
        Assert.True(CheckGenericEquals(UShortEnum.Max, UShortEnum.Max));
        Assert.True(CheckGenericEquals(UIntEnum.Max, UIntEnum.Max));
        Assert.True(CheckGenericEquals(ULongEnum.Max, ULongEnum.Max));

        var container = new GenericEnumClass<IntEnum> { field = IntEnum.Min };
        Assert.True(CheckGenericEquals(container.field, IntEnum.Min));
        Assert.False(CheckGenericEquals(container.field, IntEnum.Max));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckGenericEquals<T>(T left, T right) where T : Enum => left.Equals(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestDifferentUnderlyingTypes()
    {
        // 0xFF pattern
        Assert.False(((SByteEnum)(-1)).Equals((ByteEnum)255));

        // 0x00 pattern
        Assert.False(SByteEnum.Zero.Equals(ByteEnum.Min));

        // 0xFFFF pattern
        Assert.False(((ShortEnum)(-1)).Equals((UShortEnum)ushort.MaxValue));

        // 0xFFFFFFFF pattern
        Assert.False(((IntEnum)(-1)).Equals((UIntEnum)uint.MaxValue));

        // Signed vs Unsigned same width
        Assert.False(IntEnum.Max.Equals((UIntEnum)int.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestCornerCases()
    {
        // Ensure no false positives with primitive types (boxing checks)
        Assert.False(IntEnum.Zero.Equals(0));
        Assert.False(LongEnum.Max.Equals(long.MaxValue));

        // Different enum types entirely
        Assert.False(SimpleEnum.A.Equals(FlagsEnum.A));

        // Null and Object references
        object obj = new object();
        Assert.False(SimpleEnum.B.Equals(obj));
        Assert.False(SimpleEnum.C.Equals(null));

        // Double boxing scenarios
        object boxedA = SimpleEnum.A;
        object boxedB = SimpleEnum.A;
        Assert.True(boxedA.Equals(boxedB));
        Assert.True(SimpleEnum.A.Equals(boxedB));
    }

    public class GenericEnumClass<T> where T : Enum { public T field; }
    public enum SimpleEnum { A, B, C }
}
