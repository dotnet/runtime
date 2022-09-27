// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable 169

namespace ContainsGCPointers
{
    public struct NoPointers
    {
        public int int1;
        public byte byte1;
        public char char1;
    }

    public struct StillNoPointers
    {
        public NoPointers noPointers1;
        public bool bool1;
    }

    public class ClassNoPointers
    {
        public char char1;
    }

    public struct HasPointers
    {
        public string string1;
    }

    public struct FieldHasPointers
    {
        public HasPointers hasPointers1;
    }

    public class ClassHasPointers
    {
        public ClassHasPointers classHasPointers1;
    }

    public class BaseClassHasPointers : ClassHasPointers
    {
    }

    public class ClassHasIntArray
    {
        public int[] intArrayField;
    }

    public class ClassHasArrayOfClassType
    {
        public ClassNoPointers[] classTypeArray;
    }
}

namespace Explicit
{
    [StructLayout(LayoutKind.Explicit)]
    class Class1
    {
        public static int Stat;
        [FieldOffset(4)]
        public bool Bar;
        [FieldOffset(10)]
        public char Baz;
    }

    [StructLayout(LayoutKind.Explicit)]
    class Class2 : Class1
    {
        [FieldOffset(0)]
        public int Lol;
        [FieldOffset(20)]
        public byte Omg;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    class ExplicitSize
    {
        [FieldOffset(0)]
        public int Lol;
        [FieldOffset(20)]
        public byte Omg;
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
    public class Class1
    {
        public int MyInt;
        public bool MyBool;
        public char MyChar;
        public string MyString;
        public byte[] MyByteArray;
        public Class1 MyClass1SelfRef;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Class2 : Class1
    {
        public int MyInt2;
    }

    // [StructLayout(LayoutKind.Sequential)] is applied by default by the C# compiler
    public struct Struct0
    {
        public bool b1;
        public bool b2;
        public bool b3;
        public int i1;
        public string s1;
    }

    // [StructLayout(LayoutKind.Sequential)] is applied by default by the C# compiler
    public struct Struct1
    {
        public Struct0 MyStruct0;
        public bool MyBool;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassDoubleBool
    {
        public double double1;
        public bool bool1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassBoolDoubleBool
    {
        public bool bool1;
        public double double1;
        public bool bool2;
    }

    public struct StructByte
    {
        public byte fld1;
    }

    public struct StructStructByte_StructByteAuto
    {
        public StructByte fld1;
        public Auto.StructByte fld2;
    }
    public struct StructStructByte_Struct2BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct2Bytes fld2;
    }
    public struct StructStructByte_Struct3BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct3Bytes fld2;
    }
    public struct StructStructByte_Struct4BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct4Bytes fld2;
    }
    public struct StructStructByte_Struct5BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct5Bytes fld2;
    }
    public struct StructStructByte_Struct8BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct8Bytes fld2;
    }
    public struct StructStructByte_Struct9BytesAuto
    {
        public StructByte fld1;
        public Auto.Struct9Bytes fld2;
    }

    public struct StructStructByte_Int128StructAuto
    {
        public StructByte fld1;
        public Auto.Int128Struct fld2;
    }

    public struct StructStructByte_UInt128StructAuto
    {
        public StructByte fld1;
        public Auto.UInt128Struct fld2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Class16Align
    {
        Vector128<byte> vector16Align;
    }
}

namespace Auto
{
    [StructLayout(LayoutKind.Auto)]
    public struct StructWithBool
    {
        public bool MyStructBool;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct StructWithIntChar
    {
        public char MyStructChar;
        public int MyStructInt;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct StructWithChar
    {
        public char MyStructChar;
    }

    public class ClassContainingStructs
    {
        public static int MyStaticInt;

        public StructWithBool MyStructWithBool;
        public bool MyBool1;
        public char MyChar1;
        public int MyInt;
        public double MyDouble;
        public long MyLong;
        public byte[] MyByteArray;
        public string MyString1;
        public bool MyBool2;
        public StructWithIntChar MyStructWithIntChar;
        public StructWithChar MyStructWithChar;
    }

    public class BaseClass7BytesRemaining
    {
        public bool MyBool1;
        public double MyDouble1;
        public long MyLong1;
        public byte[] MyByteArray1;
        public string MyString1;
    }

    public class BaseClass4BytesRemaining
    {
        public long MyLong1;
        public uint MyUint1;
    }

    public class BaseClass3BytesRemaining
    {
        public int MyInt1;
        public string MyString1;
        public bool MyBool1;
    }

    public class OptimizePartial : BaseClass7BytesRemaining
    {
        public bool OptBool;
        public char OptChar;
        public long NoOptLong;
        public string NoOptString;
    }

    public class Optimize7Bools : BaseClass7BytesRemaining
    {
        public bool OptBool1;
        public bool OptBool2;
        public bool OptBool3;
        public bool OptBool4;
        public bool OptBool5;
        public bool OptBool6;
        public bool OptBool7;
        public bool NoOptBool8;
        public string NoOptString;
    }

    public class OptimizeAlignedFields : BaseClass7BytesRemaining
    {
        public bool OptBool1;
        public bool OptBool2;
        public bool OptBool3;
        public bool NoOptBool4;
        public char OptChar1;
        public char OptChar2;
        public string NoOptString;
    }

    public class OptimizeLargestField : BaseClass4BytesRemaining
    {
        public bool NoOptBool;
        public char NoOptChar;
        public int OptInt;
        public string NoOptString;
    }

    public class NoOptimizeMisaligned : BaseClass3BytesRemaining
    {
        public char NoOptChar;
        public int NoOptInt;
        public string NoOptString;
    }

    public class NoOptimizeCharAtSize2Alignment : BaseClass3BytesRemaining
    {
        public char NoOptChar;
    }

    [StructLayout(LayoutKind.Auto, Pack = 1)]
    public struct MinPacking<T>
    {
        public byte _byte;
        public T _value;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct int8x16x2
    {
        public Vector128<byte> _0;
        public Vector128<byte> _1;
    }

    public struct Wrapper_int8x16x2
    {
        public int8x16x2 fld;
    }

    public struct Wrapper_int8x16x2_2
    {
        public bool fld1;
        public int8x16x2 fld2;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct StructByte
    {
        public byte fld1;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct2Bytes
    {
        public byte fld1;
        public byte fld2;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct3Bytes
    {
        public byte fld1;
        public byte fld2;
        public byte fld3;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct4Bytes
    {
        public byte fld1;
        public byte fld2;
        public byte fld3;
        public byte fld4;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct5Bytes
    {
        public byte fld1;
        public byte fld2;
        public byte fld3;
        public byte fld4;
        public byte fld5;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct8Bytes
    {
        public byte fld1;
        public byte fld2;
        public byte fld3;
        public byte fld4;
        public byte fld5;
        public byte fld6;
        public byte fld7;
        public byte fld8;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Struct9Bytes
    {
        public byte fld1;
        public byte fld2;
        public byte fld3;
        public byte fld4;
        public byte fld5;
        public byte fld6;
        public byte fld7;
        public byte fld8;
        public byte fld9;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct UInt128Struct
    {
        UInt128 fld1;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Int128Struct
    {
        Int128 fld1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Class16Align
    {
        Vector128<byte> vector16Align;
    }
}

namespace IsByRefLike
{
    public ref struct ByRefLikeStruct
    {
        public ref object ByRef;
    }

    public struct NotByRefLike
    {
        public int X;
    }
}

namespace EnumAlignment
{
    public enum ByteEnum : byte { Val }
    public enum ShortEnum : short { Val }
    public enum IntEnum : int { Val }
    public enum LongEnum : long { Val }

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
