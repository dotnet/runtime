// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public class Common
{
    public const int NumArrElements = 2;
}

[StructLayout(LayoutKind.Sequential)]
public struct InnerSequential
{
    public int f1;
    public float f2;
    public String f3;
}

[StructLayout(LayoutKind.Explicit)]
public struct INNER2
{
    [FieldOffset(0)]
    public int f1;
    [FieldOffset(4)]
    public float f2;
    [FieldOffset(8)]
    public String f3;
}

[StructLayout(LayoutKind.Explicit)]
public struct InnerExplicit
{
    [FieldOffset(0)]
    public int f1;
    [FieldOffset(0)]
    public float f2;
    [FieldOffset(8)]
    public String f3;
}

[StructLayout(LayoutKind.Sequential)]//struct containing one field of array type
public struct InnerArraySequential
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Common.NumArrElements)]
    public InnerSequential[] arr;
}

[StructLayout(LayoutKind.Explicit, Pack = 8)]
public struct InnerArrayExplicit
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Common.NumArrElements)]
    public InnerSequential[] arr;

    [FieldOffset(8)]
    public string f4;
}

[StructLayout(LayoutKind.Explicit)]
public struct OUTER3
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Common.NumArrElements)]
    public InnerSequential[] arr;

    [FieldOffset(24)]
    public string f4;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct CharSetAnsiSequential
{
    public string f1;
    public char f2;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct CharSetUnicodeSequential
{
    public string f1;
    public char f2;
}

[StructLayout(LayoutKind.Sequential)]
public struct NumberSequential
{
    public Int64 i64;
    public UInt64 ui64;
    public Double d;
    public int i32;
    public uint ui32;
    public short s1;
    public ushort us1;
    public Int16 i16;
    public UInt16 ui16;
    public Single sgl;
    public Byte b;
    public SByte sb;
}

[StructLayout(LayoutKind.Sequential)]
public struct S3
{
    public bool flag;
    public string str;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public int[] vals;
}

[StructLayout(LayoutKind.Sequential)]
public struct S4
{
    public int age;
    public string name;
}

public enum Enum1 { e1 = 1, e2 = 3 };

[StructLayout(LayoutKind.Sequential)]
public struct S5
{
    public S4 s4;
    public Enum1 ef;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct StringStructSequentialAnsi
{
    public string first;
    public string last;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct StringStructSequentialUnicode
{
    public string first;
    public string last;
}

[StructLayout(LayoutKind.Sequential)]
public struct S8
{
    public string name;
    public bool gender;
    [MarshalAs(UnmanagedType.Error)]
    public int i32;
    [MarshalAs(UnmanagedType.Error)]
    public uint ui32;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 jobNum;
    [MarshalAs(UnmanagedType.I1)]
    public sbyte mySByte;
}

public struct S9
{
    [MarshalAs(UnmanagedType.Error)]
    public int i32;
    public TestDelegate1 myDelegate1;
}

public delegate void TestDelegate1(S9 myStruct);

[StructLayout(LayoutKind.Sequential)]
public struct IntergerStructSequential
{
    public int i;
}

[StructLayout(LayoutKind.Sequential)]
public struct OuterIntergerStructSequential
{
    public int i;
    public IntergerStructSequential s_int;
}

[StructLayout(LayoutKind.Sequential)]
public struct IncludeOuterIntergerStructSequential
{
    public OuterIntergerStructSequential s;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct S11
{
    public int* i32;
    public int i;
}

[StructLayout(LayoutKind.Sequential)]
public struct ByteStruct3Byte
{
    public byte b1;
    public byte b2;
    public byte b3;
}

[StructLayout(LayoutKind.Explicit)]
public struct U
{
    [FieldOffset(0)]
    public int i32;
    [FieldOffset(0)]
    public uint ui32;
    [FieldOffset(0)]
    public IntPtr iPtr;
    [FieldOffset(0)]
    public UIntPtr uiPtr;
    [FieldOffset(0)]
    public short s;
    [FieldOffset(0)]
    public ushort us;
    [FieldOffset(0)]
    public Byte b;
    [FieldOffset(0)]
    public SByte sb;
    [FieldOffset(0)]
    public long l;
    [FieldOffset(0)]
    public ulong ul;
    [FieldOffset(0)]
    public float f;
    [FieldOffset(0)]
    public Double d;
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct ByteStructPack2Explicit
{
    [FieldOffset(0)]
    public byte b1;
    [FieldOffset(1)]
    public byte b2;
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct ShortStructPack4Explicit
{
    [FieldOffset(0)]
    public short s1;
    [FieldOffset(2)]
    public short s2;
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct IntStructPack8Explicit
{
    [FieldOffset(0)]
    public int i1;
    [FieldOffset(4)]
    public int i2;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct LongStructPack16Explicit
{
    [FieldOffset(0)]
    public long l1;
    [FieldOffset(8)]
    public long l2;
}

[StructLayout(LayoutKind.Sequential)]
public struct ComplexStruct
{
    public int i;
    [MarshalAs(UnmanagedType.I1)]
    public bool b;
    public string str;
    public IntPtr pedding;
    public ScriptParamType type;
}

[StructLayout(LayoutKind.Explicit)]
public struct ScriptParamType
{
    [FieldOffset(0)]
    public int idata;
    [FieldOffset(8)]
    public bool bdata;
    [FieldOffset(8)]
    public double ddata;
    [FieldOffset(8)]
    public IntPtr ptrdata;
}
