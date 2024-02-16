// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace MarshalArrayAsField.LPArray;

#region Sequential
#region sequential struct definition
[StructLayout(LayoutKind.Sequential)]
public struct S_INTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_UINTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_SHORTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_WORDArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LONG64Array_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_ULONG64Array_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_DOUBLEArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_FLOATArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BYTEArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_CHARArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LPSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LPCSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a struct
[StructLayout(LayoutKind.Sequential)]
public struct TestStruct
{
    public int x;
    public double d;
    public long l;
    public string str;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_StructArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BOOLArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion

#region sequential class definition
[StructLayout(LayoutKind.Sequential)]
public class C_INTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_UINTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_SHORTArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_WORDArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LONG64Array_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_ULONG64Array_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_DOUBLEArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_FLOATArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BYTEArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_CHARArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LPSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LPCSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BSTRArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a class
[StructLayout(LayoutKind.Sequential)]
public class C_StructArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BOOLArray_Seq
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion
#endregion

#region Explicit

#region explicit struct definition
[StructLayout(LayoutKind.Explicit)]
public struct S_INTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_UINTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_SHORTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_WORDArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.I8)]
    public long[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_ULONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_DOUBLEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_FLOATArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BYTEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_CHARArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LPSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LPCSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a struct
[StructLayout(LayoutKind.Explicit)]
public struct S_StructArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BOOLArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion

#region explicit class definition
[StructLayout(LayoutKind.Explicit)]
public class C_INTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_UINTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_SHORTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_WORDArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Explicit, Pack = 8)]
public class C_LONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_ULONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_DOUBLEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_FLOATArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BYTEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_CHARArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_LPSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_LPCSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a class
[StructLayout(LayoutKind.Explicit)]
public class C_StructArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BOOLArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.LPArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion

#endregion
