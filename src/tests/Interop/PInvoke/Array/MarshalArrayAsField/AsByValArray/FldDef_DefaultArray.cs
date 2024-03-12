// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace MarshalArrayAsField.ByValArray;
#pragma warning disable 618

#region Struct Definition
[StructLayout(LayoutKind.Sequential)]
public struct S2
{
    public int i32;
    public uint ui32;
    public short s1;
    public ushort us1;
    public Byte b;
    public SByte sb;
    public Int16 i16;
    public UInt16 ui16;
    public Int64 i64;
    public UInt64 ui64;
    public Single sgl;
    public Double d;
}

[StructLayout(LayoutKind.Sequential)]
public struct Struct_Sequential
{
    public int[] longArr;

    public uint[] ulongArr;

    public short[] shortArr;

    public ushort[] ushortArr;

    public long[] long64Arr;

    public ulong[] ulong64Arr;

    public double[] doubleArr;

    public float[] floatArr;

    public byte[] byteArr;

    //not default
    //helper field definition, just match structure member verification func on C++ side
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
    public string[] bstrArr;

    public bool[] boolArr;
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct_Explicit
{
    [FieldOffset(0 * 8)]
    public int[] longArr;

    [FieldOffset(1 * 8)]
    public uint[] ulongArr;

    [FieldOffset(2 * 8)]
    public short[] shortArr;

    [FieldOffset(3 * 8)]
    public ushort[] ushortArr;

    [FieldOffset(4 * 8)]
    public long[] long64Arr;

    [FieldOffset(5 * 8)]
    public ulong[] ulong64Arr;

    [FieldOffset(6 * 8)]
    public double[] doubleArr;

    [FieldOffset(7 * 8)]
    public float[] floatArr;

    [FieldOffset(8 * 8)]
    public byte[] byteArr;

    [FieldOffset(9 * 8)]
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
    public string[] bstrArr;

    [FieldOffset(10 * 8)]
    public bool[] boolArr;
}

[StructLayout(LayoutKind.Sequential)]
public struct Struct_SeqWithArrOfStr
{
    public S2[] arrS2;
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct_ExpWithArrOfStr
{
    [FieldOffset(0)]
    public S2[] arrS2;
}
#endregion

#region Class Definition
[StructLayout(LayoutKind.Sequential)]
public class Class_Sequential
{
    public int[] longArr;

    public uint[] ulongArr;

    public short[] shortArr;

    public ushort[] ushortArr;

    public long[] long64Arr;

    public ulong[] ulong64Arr;

    public double[] doubleArr;

    public float[] floatArr;

    public byte[] byteArr;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
    public string[] bstrArr;

    public bool[] boolArr;
}

[StructLayout(LayoutKind.Explicit)]
public class Class_Explicit
{
    [FieldOffset(0 * 8)]
    public int[] longArr;

    [FieldOffset(1 * 8)]
    public uint[] ulongArr;

    [FieldOffset(2 * 8)]
    public short[] shortArr;

    [FieldOffset(3 * 8)]
    public ushort[] ushortArr;

    [FieldOffset(4 * 8)]
    public long[] long64Arr;

    [FieldOffset(5 * 8)]
    public ulong[] ulong64Arr;

    [FieldOffset(6 * 8)]
    public double[] doubleArr;

    [FieldOffset(7 * 8)]
    public float[] floatArr;

    [FieldOffset(8 * 8)]
    public byte[] byteArr;

    [FieldOffset(9 * 8)]
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
    public string[] bstrArr;

    [FieldOffset(10 * 8)]
    public bool[] boolArr;
}

[StructLayout(LayoutKind.Sequential)]
public class Class_SeqWithArrOfStr
{
    public S2[] arrS2;
}

[StructLayout(LayoutKind.Explicit)]
public class Class_ExpWithArrOfStr
{
    [FieldOffset(0)]
    public S2[] arrS2;
}
#endregion

#pragma warning restore 618
