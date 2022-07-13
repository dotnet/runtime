// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Program
{
    public static int Main()
    {
        AssertEquals(typeof(sbyte),  Enum.GetUnderlyingType(typeof(SByteEnum)));
        AssertEquals(typeof(byte),   Enum.GetUnderlyingType(typeof(ByteEnum)));
        AssertEquals(typeof(short),  Enum.GetUnderlyingType(typeof(ShortEnum)));
        AssertEquals(typeof(ushort), Enum.GetUnderlyingType(typeof(UShortEnum)));
        AssertEquals(typeof(int),    Enum.GetUnderlyingType(typeof(IntEnum)));
        AssertEquals(typeof(uint),   Enum.GetUnderlyingType(typeof(UIntEnum)));
        AssertEquals(typeof(long),   Enum.GetUnderlyingType(typeof(LongEnum)));
        AssertEquals(typeof(ulong),  Enum.GetUnderlyingType(typeof(ULongEnum)));

        AssertEquals(typeof(char),   Enum.GetUnderlyingType(typeof(CharEnum)));
        AssertEquals(typeof(bool),   Enum.GetUnderlyingType(typeof(BoolEnum)));
        AssertEquals(typeof(float),  Enum.GetUnderlyingType(typeof(FloatEnum)));
        AssertEquals(typeof(double), Enum.GetUnderlyingType(typeof(DoubleEnum)));
        AssertEquals(typeof(nint),   Enum.GetUnderlyingType(typeof(IntPtrEnum)));
        AssertEquals(typeof(nuint),  Enum.GetUnderlyingType(typeof(UIntPtrEnum)));

        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(typeof(int)));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(typeof(nint)));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(typeof(Enum)));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(typeof(object)));
        AssertThrowsArgumentNullException(() => Enum.GetUnderlyingType(null));

        AssertEquals(typeof(sbyte),  Enum.GetUnderlyingType(NoInline(typeof(SByteEnum))));
        AssertEquals(typeof(byte),   Enum.GetUnderlyingType(NoInline(typeof(ByteEnum))));
        AssertEquals(typeof(short),  Enum.GetUnderlyingType(NoInline(typeof(ShortEnum))));
        AssertEquals(typeof(ushort), Enum.GetUnderlyingType(NoInline(typeof(UShortEnum))));
        AssertEquals(typeof(int),    Enum.GetUnderlyingType(NoInline(typeof(IntEnum))));
        AssertEquals(typeof(uint),   Enum.GetUnderlyingType(NoInline(typeof(UIntEnum))));
        AssertEquals(typeof(long),   Enum.GetUnderlyingType(NoInline(typeof(LongEnum))));
        AssertEquals(typeof(ulong),  Enum.GetUnderlyingType(NoInline(typeof(ULongEnum))));

        AssertEquals(typeof(char),   Enum.GetUnderlyingType(NoInline(typeof(CharEnum))));
        AssertEquals(typeof(bool),   Enum.GetUnderlyingType(NoInline(typeof(BoolEnum))));
        AssertEquals(typeof(float),  Enum.GetUnderlyingType(NoInline(typeof(FloatEnum))));
        AssertEquals(typeof(double), Enum.GetUnderlyingType(NoInline(typeof(DoubleEnum))));
        AssertEquals(typeof(nint),   Enum.GetUnderlyingType(NoInline(typeof(IntPtrEnum))));
        AssertEquals(typeof(nuint),  Enum.GetUnderlyingType(NoInline(typeof(UIntPtrEnum))));

        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(NoInline(typeof(int))));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(NoInline(typeof(nint))));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(NoInline(typeof(Enum))));
        AssertThrowsArgumentException(() => Enum.GetUnderlyingType(NoInline(typeof(object))));
        AssertThrowsArgumentNullException(() => Enum.GetUnderlyingType(NoInline(null)));

        return 100;
    }

    public enum SByteEnum : sbyte {}
    public enum ByteEnum : byte {}
    public enum ShortEnum : short {}
    public enum UShortEnum : ushort {}
    public enum IntEnum {}
    public enum UIntEnum : uint {}
    public enum LongEnum : long {}
    public enum ULongEnum : ulong {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NoInline(Type type) => type;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEquals(Type expected, Type actual, [CallerLineNumber] int l = 0)
    {
        if (expected != actual)
            throw new InvalidOperationException($"Invalid type, expected {expected.FullName}, got {actual.FullName} at line {l}");
    }

    private static void AssertThrowsArgumentException(Action a, [CallerLineNumber] int l = 0)
    {
        try
        {
            a();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException($"Expected ArgumentException at line {l}");
    }

    private static void AssertThrowsArgumentNullException(Action a, [CallerLineNumber] int l = 0)
    {
        try
        {
            a();
        }
        catch (ArgumentNullException)
        {
            return;
        }
        throw new InvalidOperationException($"Expected ArgumentNullException at line {l}");
    }
}
