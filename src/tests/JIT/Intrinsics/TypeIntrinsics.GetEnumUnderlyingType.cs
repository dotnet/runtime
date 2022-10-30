// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class GetEnumUnderlyingType
{
    public static void TestGetEnumUnderlyingType()
    {
        AssertEquals(typeof(sbyte),  typeof(SByteEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(byte),   typeof(ByteEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(short),  typeof(ShortEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(ushort), typeof(UShortEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(int),    typeof(IntEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(uint),   typeof(UIntEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(long),   typeof(LongEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(ulong),  typeof(ULongEnum).GetEnumUnderlyingType());

        AssertEquals(typeof(char),   typeof(CharEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(bool),   typeof(BoolEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(float),  typeof(FloatEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(double), typeof(DoubleEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(nint),   typeof(IntPtrEnum).GetEnumUnderlyingType());
        AssertEquals(typeof(nuint),  typeof(UIntPtrEnum).GetEnumUnderlyingType());

        AssertThrowsArgumentException(() => typeof(int).GetEnumUnderlyingType());
        AssertThrowsArgumentException(() => typeof(nint).GetEnumUnderlyingType());
        AssertThrowsArgumentException(() => typeof(Enum).GetEnumUnderlyingType());
        AssertThrowsArgumentException(() => typeof(object).GetEnumUnderlyingType());
        AssertThrowsNullReferenceException(() => ((Type)null).GetEnumUnderlyingType());

        AssertEquals(typeof(sbyte),  NoInline(typeof(SByteEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(byte),   NoInline(typeof(ByteEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(short),  NoInline(typeof(ShortEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(ushort), NoInline(typeof(UShortEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(int),    NoInline(typeof(IntEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(uint),   NoInline(typeof(UIntEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(long),   NoInline(typeof(LongEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(ulong),  NoInline(typeof(ULongEnum).GetEnumUnderlyingType()));

        AssertEquals(typeof(char),   NoInline(typeof(CharEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(bool),   NoInline(typeof(BoolEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(float),  NoInline(typeof(FloatEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(double), NoInline(typeof(DoubleEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(nint),   NoInline(typeof(IntPtrEnum).GetEnumUnderlyingType()));
        AssertEquals(typeof(nuint),  NoInline(typeof(UIntPtrEnum).GetEnumUnderlyingType()));

        AssertThrowsArgumentException(() => NoInline(typeof(int).GetEnumUnderlyingType()));
        AssertThrowsArgumentException(() => NoInline(typeof(nint).GetEnumUnderlyingType()));
        AssertThrowsArgumentException(() => NoInline(typeof(Enum).GetEnumUnderlyingType()));
        AssertThrowsArgumentException(() => NoInline(typeof(object).GetEnumUnderlyingType()));
        AssertThrowsNullReferenceException(() => NoInline(null).GetEnumUnderlyingType());

        AssertThrowsArgumentException(() => typeof(GenericEnumClass<>).GetGenericArguments()[0].GetEnumUnderlyingType());
    }

    public enum SByteEnum : sbyte {}
    public enum ByteEnum : byte {}
    public enum ShortEnum : short {}
    public enum UShortEnum : ushort {}
    public enum IntEnum {}
    public enum UIntEnum : uint {}
    public enum LongEnum : long {}
    public enum ULongEnum : ulong {}

    public class GenericEnumClass<T> where T : Enum
    {
        public T field;
    }

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

    private static void AssertThrowsNullReferenceException(Action a, [CallerLineNumber] int l = 0)
    {
        try
        {
            a();
        }
        catch (NullReferenceException)
        {
            return;
        }
        throw new InvalidOperationException($"Expected NullReferenceException at line {l}");
    }
}
