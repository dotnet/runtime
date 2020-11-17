// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 169

namespace ContainsGCPointers
{
    struct NoPointers
    {
        int int1;
        byte byte1;
        char char1;
    }

    struct StillNoPointers
    {
        NoPointers noPointers1;
        bool bool1;
    }

    class ClassNoPointers
    {
        char char1;
    }

    struct HasPointers
    {
        string string1;
    }

    struct FieldHasPointers
    {
        HasPointers hasPointers1;
    }

    class ClassHasPointers
    {
        ClassHasPointers classHasPointers1;
    }

    class BaseClassHasPointers : ClassHasPointers
    {
    }

    public class ClassHasIntArray
    {
        int[] intArrayField;
    }

    public class ClassHasArrayOfClassType
    {
        ClassNoPointers[] classTypeArray;
    }
}

namespace Explicit
{
    [StructLayout(LayoutKind.Explicit)]
    class Class1
    {
        static int Stat;
        [FieldOffset(4)]
        bool Bar;
        [FieldOffset(10)]
        char Baz;
    }

    [StructLayout(LayoutKind.Explicit)]
    class Class2 : Class1
    {
        [FieldOffset(0)]
        int Lol;
        [FieldOffset(20)]
        byte Omg;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    class ExplicitSize
    {
        [FieldOffset(0)]
        int Lol;
        [FieldOffset(20)]
        byte Omg;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitEmptyClass
    {
    }

    [StructLayout(LayoutKind.Explicit, Size = 0)]
    public class ExplicitEmptyClassSize0
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ExplicitEmptyStruct
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    ref struct MisalignedPointer
    {
        [FieldOffset(2)]
        public object O;
    }

    [StructLayout(LayoutKind.Explicit)]
    ref struct MisalignedByRef
    {
        [FieldOffset(2)]
        public ByRefStruct O;
    }

    ref struct ByRefStruct
    {
    }
}

namespace Sequential
{
    [StructLayout(LayoutKind.Sequential)]
    class Class1
    {
        int MyInt;
        bool MyBool;
        char MyChar;
        string MyString;
        byte[] MyByteArray;
        Class1 MyClass1SelfRef;
    }

    [StructLayout(LayoutKind.Sequential)]
    class Class2 : Class1
    {
        int MyInt2;
    }

    // [StructLayout(LayoutKind.Sequential)] is applied by default by the C# compiler
    struct Struct0
    {
        bool b1;
        bool b2;
        bool b3;
        int i1;
        string s1;
    }

    // [StructLayout(LayoutKind.Sequential)] is applied by default by the C# compiler
    struct Struct1
    {
        Struct0 MyStruct0;
        bool MyBool;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassDoubleBool
    {
        double double1;
        bool bool1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassBoolDoubleBool
    {
        bool bool1;
        double double1;
        bool bool2;
    }
}

namespace Auto
{
    [StructLayout(LayoutKind.Auto)]
    struct StructWithBool
    {
        bool MyStructBool;
    }

    [StructLayout(LayoutKind.Auto)]
    struct StructWithIntChar
    {
        char MyStructChar;
        int MyStructInt;
    }

    [StructLayout(LayoutKind.Auto)]
    struct StructWithChar
    {
        char MyStructChar;
    }

    class ClassContainingStructs
    {
        static int MyStaticInt;

        StructWithBool MyStructWithBool;
        bool MyBool1;
        char MyChar1;
        int MyInt;
        double MyDouble;
        long MyLong;
        byte[] MyByteArray;
        string MyString1;
        bool MyBool2;
        StructWithIntChar MyStructWithIntChar;
        StructWithChar MyStructWithChar;
    }

    class BaseClass7BytesRemaining
    {
        bool MyBool1;
        double MyDouble1;
        long MyLong1;
        byte[] MyByteArray1;
        string MyString1;
    }

    class BaseClass4BytesRemaining
    {
        long MyLong1;
        uint MyUint1;
    }

    class BaseClass3BytesRemaining
    {
        int MyInt1;
        string MyString1;
        bool MyBool1;
    }

    class OptimizePartial : BaseClass7BytesRemaining
    {
        bool OptBool;
        char OptChar;
        long NoOptLong;
        string NoOptString;
    }

    class Optimize7Bools : BaseClass7BytesRemaining
    {
        bool OptBool1;
        bool OptBool2;
        bool OptBool3;
        bool OptBool4;
        bool OptBool5;
        bool OptBool6;
        bool OptBool7;
        bool NoOptBool8;
        string NoOptString;
    }

    class OptimizeAlignedFields : BaseClass7BytesRemaining
    {
        bool OptBool1;
        bool OptBool2;
        bool OptBool3;
        bool NoOptBool4;
        char OptChar1;
        char OptChar2;
        string NoOptString;
    }

    class OptimizeLargestField : BaseClass4BytesRemaining
    {
        bool NoOptBool;
        char NoOptChar;
        int OptInt;
        string NoOptString;
    }

    class NoOptimizeMisaligned : BaseClass3BytesRemaining
    {
        char NoOptChar;
        int NoOptInt;
        string NoOptString;
    }

    class NoOptimizeCharAtSize2Alignment : BaseClass3BytesRemaining
    {
        char NoOptChar;
    }

    [StructLayout(LayoutKind.Auto, Pack = 1)]
    struct MinPacking<T>
    {
        public byte _byte;
        public T _value;
    }
}

namespace IsByRefLike
{
    public ref struct ByRefLikeStruct
    {
        ByReference<object> ByRef;
    }

    public struct NotByRefLike
    {
        int X;
    }
}

namespace EnumAlignment
{
    public enum ByteEnum : byte {}
    public enum ShortEnum : short {}
    public enum IntEnum : int {}
    public enum LongEnum : long {}

    public struct LongIntEnumStruct
    {
        public LongEnum _1;
        public IntEnum _2;
        public LongEnum _3;
        public IntEnum _4;
    }

    public struct LongIntEnumStructFieldStruct
    {
        public byte _0;
        public LongIntEnumStruct _struct;
    }

    public struct IntShortEnumStruct
    {
        public IntEnum _1;
        public ShortEnum _2;
        public IntEnum _3;
        public ShortEnum _4;
    }

    public struct IntShortEnumStructFieldStruct
    {
        public byte _0;
        public IntShortEnumStruct _struct;
    }

    public struct ShortByteEnumStruct
    {
        public ShortEnum _1;
        public ByteEnum _2;
        public ShortEnum _3;
        public ByteEnum _4;
    }

    public struct ShortByteEnumStructFieldStruct
    {
        public byte _0;
        public ShortByteEnumStruct _struct;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct LongIntEnumStructAuto
    {
        public LongEnum _1;
        public IntEnum _2;
        public LongEnum _3;
        public IntEnum _4;
    }

    public struct LongIntEnumStructAutoFieldStruct
    {
        public byte _0;
        public LongIntEnumStructAuto _struct;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct IntShortEnumStructAuto
    {
        public IntEnum _1;
        public ShortEnum _2;
        public IntEnum _3;
        public ShortEnum _4;
    }

    public struct IntShortEnumStructAutoFieldStruct
    {
        public byte _0;
        public IntShortEnumStructAuto _struct;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct ShortByteEnumStructAuto
    {
        public ShortEnum _1;
        public ByteEnum _2;
        public ShortEnum _3;
        public ByteEnum _4;
    }

    public struct ShortByteEnumStructAutoFieldStruct
    {
        public byte _0;
        public ShortByteEnumStructAuto _struct;
    }
}
