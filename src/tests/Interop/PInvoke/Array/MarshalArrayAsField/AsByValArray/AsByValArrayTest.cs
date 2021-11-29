// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

#region Sequential
#region sequential struct definition
[StructLayout(LayoutKind.Sequential)]
public struct S_INTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_UINTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_SHORTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_WORDArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LONG64Array_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_ULONG64Array_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_DOUBLEArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_FLOATArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BYTEArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_CHARArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LPSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_LPCSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
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
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public struct S_BOOLArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}

public enum TestEnum
{
    Red = 1,
    Green,
    Blue
}

[StructLayout(LayoutKind.Sequential)]
public struct EnregisterableNonBlittable_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public TestEnum[] arr;
}

public struct SimpleStruct
{
    public int fld;
}

[StructLayout(LayoutKind.Sequential)]
public struct EnregisterableUserType
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public SimpleStruct[] arr;
}
#endregion

#region sequential class definition
[StructLayout(LayoutKind.Sequential)]
public class C_INTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_UINTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_SHORTArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_WORDArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LONG64Array_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_ULONG64Array_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_DOUBLEArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_FLOATArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BYTEArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_CHARArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LPSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_LPCSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BSTRArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a class
[StructLayout(LayoutKind.Sequential)]
public class C_StructArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public class C_BOOLArray_Seq
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion
#endregion

#region Explicit

#region explicit stuct definition
[StructLayout(LayoutKind.Explicit)]
public struct S_INTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_UINTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_SHORTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_WORDArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.I8)]
    public long[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_ULONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_DOUBLEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_FLOATArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BYTEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_CHARArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LPSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_LPCSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a struct
[StructLayout(LayoutKind.Explicit)]
public struct S_StructArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public struct S_BOOLArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion

#region explicit class definition
[StructLayout(LayoutKind.Explicit)]
public class C_INTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public int[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_UINTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public uint[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_SHORTArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public short[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_WORDArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ushort[] arr;
}

[StructLayout(LayoutKind.Explicit, Pack = 8)]
public class C_LONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public long[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_ULONG64Array_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public ulong[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_DOUBLEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public double[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_FLOATArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public float[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BYTEArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public byte[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_CHARArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public char[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_LPSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_LPCSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public string[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BSTRArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE, ArraySubType = UnmanagedType.BStr)]
    public string[] arr;
}

//struct array in a class
[StructLayout(LayoutKind.Explicit)]
public class C_StructArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public TestStruct[] arr;
}

[StructLayout(LayoutKind.Explicit)]
public class C_BOOLArray_Exp
{
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Test.ARRAY_SIZE)]
    public bool[] arr;
}
#endregion

#endregion

class Test
{
    //for RunTest1
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArraySeqStructByVal([In]S_INTArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArraySeqStructByVal([In]S_UINTArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArraySeqStructByVal([In]S_SHORTArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArraySeqStructByVal([In]S_WORDArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArraySeqStructByVal([In]S_LONG64Array_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArraySeqStructByVal([In]S_ULONG64Array_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArraySeqStructByVal([In]S_DOUBLEArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArraySeqStructByVal([In]S_FLOATArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArraySeqStructByVal([In]S_BYTEArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArraySeqStructByVal([In]S_CHARArray_Seq s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArraySeqStructByVal([In]S_LPSTRArray_Seq s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArraySeqStructByVal([In]S_LPCSTRArray_Seq s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArraySeqStructByVal([In]S_BSTRArray_Seq s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArraySeqStructByVal([In]S_StructArray_Seq s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeEnregistrableNonBlittableSeqStructByVal(EnregisterableNonBlittable_Seq s, TestEnum[] values);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeEnregisterableUserTypeStructByVal(EnregisterableUserType s, SimpleStruct[] values);

    //for RunTest2
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArraySeqClassByVal([In]C_INTArray_Seq c, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArraySeqClassByVal([In]C_UINTArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArraySeqClassByVal([In]C_SHORTArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArraySeqClassByVal([In]C_WORDArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArraySeqClassByVal([In]C_LONG64Array_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArraySeqClassByVal([In]C_ULONG64Array_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArraySeqClassByVal([In]C_DOUBLEArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArraySeqClassByVal([In]C_FLOATArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArraySeqClassByVal([In]C_BYTEArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArraySeqClassByVal([In]C_CHARArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArraySeqClassByVal([In]C_LPSTRArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArraySeqClassByVal([In]C_LPCSTRArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArraySeqClassByVal([In]C_BSTRArray_Seq s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArraySeqClassByVal([In]C_StructArray_Seq s, int size);


    //for RunTest3
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArrayExpStructByVal([In]S_INTArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArrayExpStructByVal([In]S_UINTArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArrayExpStructByVal([In]S_SHORTArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArrayExpStructByVal([In]S_WORDArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArrayExpStructByVal([In]S_LONG64Array_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArrayExpStructByVal([In]S_ULONG64Array_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArrayExpStructByVal([In]S_DOUBLEArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArrayExpStructByVal([In]S_FLOATArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArrayExpStructByVal([In]S_BYTEArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArrayExpStructByVal([In]S_CHARArray_Exp s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArrayExpStructByVal([In]S_LPSTRArray_Exp s, int size);
    
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArrayExpStructByVal([In]S_LPCSTRArray_Exp s, int size);
    
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArrayExpStructByVal([In]S_BSTRArray_Exp s, int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArrayExpStructByVal([In]S_StructArray_Exp s, int size);

    //for RunTest4
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArrayExpClassByVal([In]C_INTArray_Exp c, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArrayExpClassByVal([In]C_UINTArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArrayExpClassByVal([In]C_SHORTArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArrayExpClassByVal([In]C_WORDArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArrayExpClassByVal([In]C_LONG64Array_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArrayExpClassByVal([In]C_ULONG64Array_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArrayExpClassByVal([In]C_DOUBLEArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArrayExpClassByVal([In]C_FLOATArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArrayExpClassByVal([In]C_BYTEArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArrayExpClassByVal([In]C_CHARArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArrayExpClassByVal([In]C_LPSTRArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArrayExpClassByVal([In]C_LPCSTRArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArrayExpClassByVal([In]C_BSTRArray_Exp s, int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArrayExpClassByVal([In]C_StructArray_Exp s, int size);

    //for RunTest5
    //get struct on C++ side as sequential class
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern S_INTArray_Seq S_INTArray_Ret_ByValue();

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_INTArray_Seq S_INTArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_UINTArray_Seq S_UINTArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_SHORTArray_Seq S_SHORTArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_WORDArray_Seq S_WORDArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_LONG64Array_Seq S_LONG64Array_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_ULONG64Array_Seq S_ULONG64Array_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_DOUBLEArray_Seq S_DOUBLEArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_FLOATArray_Seq S_FLOATArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_BYTEArray_Seq S_BYTEArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_CHARArray_Seq S_CHARArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_LPSTRArray_Seq S_LPSTRArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_BSTRArray_Seq S_BSTRArray_Ret();
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern C_StructArray_Seq S_StructArray_Ret();

    //for RunTest6
    //get struct on C++ side as explicit class
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_INTArray_Ret")]
    static extern C_INTArray_Exp S_INTArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_UINTArray_Ret")]
    static extern C_UINTArray_Exp S_UINTArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_SHORTArray_Ret")]
    static extern C_SHORTArray_Exp S_SHORTArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_WORDArray_Ret")]
    static extern C_WORDArray_Exp S_WORDArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_LONG64Array_Ret")]
    static extern C_LONG64Array_Exp S_LONG64Array_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_ULONG64Array_Ret")]
    static extern C_ULONG64Array_Exp S_ULONG64Array_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_DOUBLEArray_Ret")]
    static extern C_DOUBLEArray_Exp S_DOUBLEArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_FLOATArray_Ret")]
    static extern C_FLOATArray_Exp S_FLOATArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_BYTEArray_Ret")]
    static extern C_BYTEArray_Exp S_BYTEArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_CHARArray_Ret")]
    static extern C_CHARArray_Exp S_CHARArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_LPSTRArray_Ret")]
    static extern C_LPSTRArray_Exp S_LPSTRArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_BSTRArray_Ret")]
    static extern C_BSTRArray_Exp S_BSTRArray_Ret2();
    [DllImport("MarshalArrayByValArrayNative", EntryPoint = "S_StructArray_Ret")]
    static extern C_StructArray_Exp S_StructArray_Ret2();

    #region Helper

    internal const int ARRAY_SIZE = 100;

    static T[] InitArray<T>(int size)
    {
        T[] array = new T[size];

        for (int i = 0; i < array.Length; i++)
            array[i] = (T)Convert.ChangeType(i, typeof(T));

        return array;
    }

    static TestStruct[] InitStructArray(int size)
    {
        TestStruct[] array = new TestStruct[size];

        for (int i = 0; i < array.Length; i++)
        {
            array[i].x = i;
            array[i].d = i;
            array[i].l = i;
            array[i].str = i.ToString();
        }

        return array;
    }

    static bool[] InitBoolArray(int size)
    {
        bool[] array = new bool[size];

        for (int i = 0; i < array.Length; i++)
        {
            if (i % 2 == 0)
                array[i] = true;
            else
                array[i] = false;
        }

        return array;
    }

    static bool Equals<T>(T[] arr1, T[] arr2)
    {
        if (arr1 == null && arr2 == null)
            return true;
        else if (arr1 == null && arr2 != null)
            return false;
        else if (arr1 != null && arr2 == null)
            return false;
        else if (arr1.Length != arr2.Length)
            return false;

        for (int i = 0; i < arr2.Length; ++i)
        {
            if (!Object.Equals(arr1[i], arr2[i]))
            {
                Console.WriteLine("Array marshaling error, when type is {0}", typeof(T));
                Console.WriteLine("Expected: {0}, Actual: {1}", arr1[i], arr2[i]);
                return false;
            }
        }

        return true;
    }

    static bool TestStructEquals(TestStruct[] tsArr1, TestStruct[] tsArr2)
    {
        if (tsArr1 == null && tsArr2 == null)
            return true;
        else if (tsArr1 == null && tsArr2 != null)
            return false;
        else if (tsArr1 != null && tsArr2 == null)
            return false;
        else if (tsArr1.Length != tsArr2.Length)
            return false;

        bool result = true;
        for (int i = 0; i < tsArr2.Length; i++)
        {
            result = (tsArr1[i].x == tsArr2[i].x &&
                       tsArr1[i].d == tsArr2[i].d &&
                       tsArr1[i].l == tsArr2[i].l &&
                       tsArr1[i].str == tsArr2[i].str) && result;
        }

        return result;
    }

    #endregion

    static void RunTest1(string report)
    {
        Console.WriteLine(report);

        S_INTArray_Seq s1 = new S_INTArray_Seq();
        s1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.IsTrue(TakeIntArraySeqStructByVal(s1, s1.arr.Length), "TakeIntArraySeqStructByVal");

        S_UINTArray_Seq s2 = new S_UINTArray_Seq();
        s2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.IsTrue(TakeUIntArraySeqStructByVal(s2, s2.arr.Length), "TakeUIntArraySeqStructByVal");

        S_SHORTArray_Seq s3 = new S_SHORTArray_Seq();
        s3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.IsTrue(TakeShortArraySeqStructByVal(s3, s3.arr.Length), "TakeShortArraySeqStructByVal");

        S_WORDArray_Seq s4 = new S_WORDArray_Seq();
        s4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.IsTrue(TakeWordArraySeqStructByVal(s4, s4.arr.Length), "TakeWordArraySeqStructByVal");

        S_LONG64Array_Seq s5 = new S_LONG64Array_Seq();
        s5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.IsTrue(TakeLong64ArraySeqStructByVal(s5, s5.arr.Length), "TakeLong64ArraySeqStructByVal");

        S_ULONG64Array_Seq s6 = new S_ULONG64Array_Seq();
        s6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.IsTrue(TakeULong64ArraySeqStructByVal(s6, s6.arr.Length), "TakeULong64ArraySeqStructByVal");

        S_DOUBLEArray_Seq s7 = new S_DOUBLEArray_Seq();
        s7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.IsTrue(TakeDoubleArraySeqStructByVal(s7, s7.arr.Length), "TakeDoubleArraySeqStructByVal");

        S_FLOATArray_Seq s8 = new S_FLOATArray_Seq();
        s8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.IsTrue(TakeFloatArraySeqStructByVal(s8, s8.arr.Length), "TakeFloatArraySeqStructByVal");

        S_BYTEArray_Seq s9 = new S_BYTEArray_Seq();
        s9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.IsTrue(TakeByteArraySeqStructByVal(s9, s9.arr.Length), "TakeByteArraySeqStructByVal");

        S_CHARArray_Seq s10 = new S_CHARArray_Seq();
        s10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.IsTrue(TakeCharArraySeqStructByVal(s10, s10.arr.Length), "TakeCharArraySeqStructByVal");

        S_LPSTRArray_Seq s11 = new S_LPSTRArray_Seq();
        s11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPSTRArraySeqStructByVal(s11, s11.arr.Length),"TakeLPSTRArraySeqStructByVal");

        S_LPCSTRArray_Seq s12 = new S_LPCSTRArray_Seq();
        s12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPCSTRArraySeqStructByVal(s12, s12.arr.Length),"TakeLPCSTRArraySeqStructByVal");

        if (OperatingSystem.IsWindows())
        {
            S_BSTRArray_Seq s13 = new S_BSTRArray_Seq();
            s13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.IsTrue(TakeBSTRArraySeqStructByVal(s13, s13.arr.Length),"TakeBSTRArraySeqStructByVal");
        }

        S_StructArray_Seq s14 = new S_StructArray_Seq();
        s14.arr = InitStructArray(ARRAY_SIZE);
        Assert.IsTrue(TakeStructArraySeqStructByVal(s14, s14.arr.Length),"TakeStructArraySeqStructByVal");

        EnregisterableNonBlittable_Seq s15 = new EnregisterableNonBlittable_Seq
        {
            arr = new TestEnum[3]
            {
                TestEnum.Red,
                TestEnum.Green,
                TestEnum.Blue
            }
        };

        Assert.IsTrue(TakeEnregistrableNonBlittableSeqStructByVal(s15, s15.arr), "EnregisterableNonBlittableSeqStructByVal");

        EnregisterableUserType s16 = new EnregisterableUserType
        {
            arr = new SimpleStruct[3]
            {
                new SimpleStruct { fld = 10 },
                new SimpleStruct { fld = 25 },
                new SimpleStruct { fld = 40 }
            }
        };

        Assert.IsTrue(TakeEnregisterableUserTypeStructByVal(s16, s16.arr), "TakeEnregisterableUserTypeStructByVal");
    }

    static void RunTest2(string report)
    {
        Console.WriteLine(report);

        C_INTArray_Seq c1 = new C_INTArray_Seq();
        c1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.IsTrue(TakeIntArraySeqClassByVal(c1, c1.arr.Length));

        C_UINTArray_Seq c2 = new C_UINTArray_Seq();
        c2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.IsTrue(TakeUIntArraySeqClassByVal(c2, c2.arr.Length));

        C_SHORTArray_Seq c3 = new C_SHORTArray_Seq();
        c3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.IsTrue(TakeShortArraySeqClassByVal(c3, c3.arr.Length));

        C_WORDArray_Seq c4 = new C_WORDArray_Seq();
        c4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.IsTrue(TakeWordArraySeqClassByVal(c4, c4.arr.Length));

        C_LONG64Array_Seq c5 = new C_LONG64Array_Seq();
        c5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.IsTrue(TakeLong64ArraySeqClassByVal(c5, c5.arr.Length));

        C_ULONG64Array_Seq c6 = new C_ULONG64Array_Seq();
        c6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.IsTrue(TakeULong64ArraySeqClassByVal(c6, c6.arr.Length));

        C_DOUBLEArray_Seq c7 = new C_DOUBLEArray_Seq();
        c7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.IsTrue(TakeDoubleArraySeqClassByVal(c7, c7.arr.Length));

        C_FLOATArray_Seq c8 = new C_FLOATArray_Seq();
        c8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.IsTrue(TakeFloatArraySeqClassByVal(c8, c8.arr.Length));

        C_BYTEArray_Seq c9 = new C_BYTEArray_Seq();
        c9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.IsTrue(TakeByteArraySeqClassByVal(c9, c9.arr.Length));

        C_CHARArray_Seq c10 = new C_CHARArray_Seq();
        c10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.IsTrue(TakeCharArraySeqClassByVal(c10, c10.arr.Length));

        C_LPSTRArray_Seq c11 = new C_LPSTRArray_Seq();
        c11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPSTRArraySeqClassByVal(c11, c11.arr.Length));

        C_LPCSTRArray_Seq c12 = new C_LPCSTRArray_Seq();
        c12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPCSTRArraySeqClassByVal(c12, c12.arr.Length));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Seq c13 = new C_BSTRArray_Seq();
            c13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.IsTrue(TakeBSTRArraySeqClassByVal(c13, c13.arr.Length));
        }

        C_StructArray_Seq c14 = new C_StructArray_Seq();
        c14.arr = InitStructArray(ARRAY_SIZE);
        Assert.IsTrue(TakeStructArraySeqClassByVal(c14, c14.arr.Length));
    }

    static void RunTest3(string report)
    {
        Console.WriteLine(report);

        S_INTArray_Exp s1 = new S_INTArray_Exp();
        s1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.IsTrue(TakeIntArrayExpStructByVal(s1, s1.arr.Length), "TakeIntArrayExpStructByVal");

        S_UINTArray_Exp s2 = new S_UINTArray_Exp();
        s2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.IsTrue(TakeUIntArrayExpStructByVal(s2, s2.arr.Length), "TakeUIntArrayExpStructByVal");

        S_SHORTArray_Exp s3 = new S_SHORTArray_Exp();
        s3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.IsTrue(TakeShortArrayExpStructByVal(s3, s3.arr.Length), "TakeShortArrayExpStructByVal");

        S_WORDArray_Exp s4 = new S_WORDArray_Exp();
        s4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.IsTrue(TakeWordArrayExpStructByVal(s4, s4.arr.Length), "TakeWordArrayExpStructByVal");

        S_LONG64Array_Exp s5 = new S_LONG64Array_Exp();
        s5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.IsTrue(TakeLong64ArrayExpStructByVal(s5, s5.arr.Length), "TakeLong64ArrayExpStructByVal");

        S_ULONG64Array_Exp s6 = new S_ULONG64Array_Exp();
        s6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.IsTrue(TakeULong64ArrayExpStructByVal(s6, s6.arr.Length), "TakeULong64ArrayExpStructByVal");

        S_DOUBLEArray_Exp s7 = new S_DOUBLEArray_Exp();
        s7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.IsTrue(TakeDoubleArrayExpStructByVal(s7, s7.arr.Length), "TakeDoubleArrayExpStructByVal");

        S_FLOATArray_Exp s8 = new S_FLOATArray_Exp();
        s8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.IsTrue(TakeFloatArrayExpStructByVal(s8, s8.arr.Length), "TakeFloatArrayExpStructByVal");

        S_BYTEArray_Exp s9 = new S_BYTEArray_Exp();
        s9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.IsTrue(TakeByteArrayExpStructByVal(s9, s9.arr.Length), "TakeByteArrayExpStructByVal");

        S_CHARArray_Exp s10 = new S_CHARArray_Exp();
        s10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.IsTrue(TakeCharArrayExpStructByVal(s10, s10.arr.Length), "TakeCharArrayExpStructByVal");

        S_LPSTRArray_Exp s11 = new S_LPSTRArray_Exp();
        s11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPSTRArrayExpStructByVal(s11, s11.arr.Length));

        S_LPCSTRArray_Exp s12 = new S_LPCSTRArray_Exp();
        s12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPCSTRArrayExpStructByVal(s12, s12.arr.Length));

        if (OperatingSystem.IsWindows())
        {
            S_BSTRArray_Exp c13 = new S_BSTRArray_Exp();
            c13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.IsTrue(TakeBSTRArrayExpStructByVal(c13, c13.arr.Length));
        }

        S_StructArray_Exp s14 = new S_StructArray_Exp();
        s14.arr = InitStructArray(ARRAY_SIZE);
        Assert.IsTrue(TakeStructArrayExpStructByVal(s14, s14.arr.Length));
    }

    static void RunTest4(string report)
    {
        Console.WriteLine(report);

        C_INTArray_Exp c1 = new C_INTArray_Exp();
        c1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.IsTrue(TakeIntArrayExpClassByVal(c1, c1.arr.Length));

        C_UINTArray_Exp c2 = new C_UINTArray_Exp();
        c2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.IsTrue(TakeUIntArrayExpClassByVal(c2, c2.arr.Length));

        C_SHORTArray_Exp c3 = new C_SHORTArray_Exp();
        c3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.IsTrue(TakeShortArrayExpClassByVal(c3, c3.arr.Length));

        C_WORDArray_Exp c4 = new C_WORDArray_Exp();
        c4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.IsTrue(TakeWordArrayExpClassByVal(c4, c4.arr.Length));

        C_LONG64Array_Exp c5 = new C_LONG64Array_Exp();
        c5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.IsTrue(TakeLong64ArrayExpClassByVal(c5, c5.arr.Length));

        C_ULONG64Array_Exp c6 = new C_ULONG64Array_Exp();
        c6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.IsTrue(TakeULong64ArrayExpClassByVal(c6, c6.arr.Length));

        C_DOUBLEArray_Exp c7 = new C_DOUBLEArray_Exp();
        c7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.IsTrue(TakeDoubleArrayExpClassByVal(c7, c7.arr.Length));

        C_FLOATArray_Exp c8 = new C_FLOATArray_Exp();
        c8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.IsTrue(TakeFloatArrayExpClassByVal(c8, c8.arr.Length));

        C_BYTEArray_Exp c9 = new C_BYTEArray_Exp();
        c9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.IsTrue(TakeByteArrayExpClassByVal(c9, c9.arr.Length));

        C_CHARArray_Exp c10 = new C_CHARArray_Exp();
        c10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.IsTrue(TakeCharArrayExpClassByVal(c10, c10.arr.Length));

        C_LPSTRArray_Exp c11 = new C_LPSTRArray_Exp();
        c11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPSTRArrayExpClassByVal(c11, c11.arr.Length));

        C_LPCSTRArray_Exp c12 = new C_LPCSTRArray_Exp();
        c12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.IsTrue(TakeLPCSTRArrayExpClassByVal(c12, c12.arr.Length));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Exp c13 = new C_BSTRArray_Exp();
            c13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.IsTrue(TakeBSTRArrayExpClassByVal(c13, c13.arr.Length));
        }

        C_StructArray_Exp c14 = new C_StructArray_Exp();
        c14.arr = InitStructArray(ARRAY_SIZE);
        Assert.IsTrue(TakeStructArrayExpClassByVal(c14, c14.arr.Length));
    }

    static void RunTest5(string report)
    {
        Console.WriteLine(report);
        
        S_INTArray_Seq retval = S_INTArray_Ret_ByValue();
        Assert.IsTrue(Equals(InitArray<int>(ARRAY_SIZE), retval.arr));

        C_INTArray_Seq retval1 = S_INTArray_Ret();
        Assert.IsTrue(Equals(InitArray<int>(ARRAY_SIZE), retval1.arr));

        C_UINTArray_Seq retval2 = S_UINTArray_Ret();
        Assert.IsTrue(Equals(InitArray<uint>(ARRAY_SIZE), retval2.arr));

        C_SHORTArray_Seq retval3 = S_SHORTArray_Ret();
        Assert.IsTrue(Equals(InitArray<short>(ARRAY_SIZE), retval3.arr));

        C_WORDArray_Seq retval4 = S_WORDArray_Ret();
        Assert.IsTrue(Equals(InitArray<ushort>(ARRAY_SIZE), retval4.arr));

        C_LONG64Array_Seq retval5 = S_LONG64Array_Ret();
        Assert.IsTrue(Equals(InitArray<long>(ARRAY_SIZE), retval5.arr));

        C_ULONG64Array_Seq retval6 = S_ULONG64Array_Ret();
        Assert.IsTrue(Equals(InitArray<ulong>(ARRAY_SIZE), retval6.arr));

        C_DOUBLEArray_Seq retval7 = S_DOUBLEArray_Ret();
        Assert.IsTrue(Equals(InitArray<double>(ARRAY_SIZE), retval7.arr));

        C_FLOATArray_Seq retval8 = S_FLOATArray_Ret();
        Assert.IsTrue(Equals(InitArray<float>(ARRAY_SIZE), retval8.arr));

        C_BYTEArray_Seq retval9 = S_BYTEArray_Ret();
        Assert.IsTrue(Equals(InitArray<byte>(ARRAY_SIZE), retval9.arr));

        C_CHARArray_Seq retval10 = S_CHARArray_Ret();
        Assert.IsTrue(Equals(InitArray<char>(ARRAY_SIZE), retval10.arr));

        C_LPSTRArray_Seq retval11 = S_LPSTRArray_Ret();
        Assert.IsTrue(Equals(InitArray<string>(ARRAY_SIZE), retval11.arr));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Seq retval12 = S_BSTRArray_Ret();
            Assert.IsTrue(Equals(InitArray<string>(ARRAY_SIZE), retval12.arr));
        }

        C_StructArray_Seq retval13 = S_StructArray_Ret();
        Assert.IsTrue(TestStructEquals(InitStructArray(ARRAY_SIZE), retval13.arr));
    }

    static void RunTest6(string report)
    {
        Console.WriteLine(report);

        C_INTArray_Exp retval1 = S_INTArray_Ret2();
        Assert.IsTrue(Equals(InitArray<int>(ARRAY_SIZE), retval1.arr));

        C_UINTArray_Exp retval2 = S_UINTArray_Ret2();
        Assert.IsTrue(Equals(InitArray<uint>(ARRAY_SIZE), retval2.arr));

        C_SHORTArray_Exp retval3 = S_SHORTArray_Ret2();
        Assert.IsTrue(Equals(InitArray<short>(ARRAY_SIZE), retval3.arr));

        C_WORDArray_Exp retval4 = S_WORDArray_Ret2();
        Assert.IsTrue(Equals(InitArray<ushort>(ARRAY_SIZE), retval4.arr));

        C_LONG64Array_Exp retval5 = S_LONG64Array_Ret2();
        Assert.IsTrue(Equals(InitArray<long>(ARRAY_SIZE), retval5.arr));

        C_ULONG64Array_Exp retval6 = S_ULONG64Array_Ret2();
        Assert.IsTrue(Equals(InitArray<ulong>(ARRAY_SIZE), retval6.arr));

        C_DOUBLEArray_Exp retval7 = S_DOUBLEArray_Ret2();
        Assert.IsTrue(Equals(InitArray<double>(ARRAY_SIZE), retval7.arr));

        C_FLOATArray_Exp retval8 = S_FLOATArray_Ret2();
        Assert.IsTrue(Equals(InitArray<float>(ARRAY_SIZE), retval8.arr));

        C_BYTEArray_Exp retval9 = S_BYTEArray_Ret2();
        Assert.IsTrue(Equals(InitArray<byte>(ARRAY_SIZE), retval9.arr));

        C_CHARArray_Exp retval10 = S_CHARArray_Ret2();
        Assert.IsTrue(Equals(InitArray<char>(ARRAY_SIZE), retval10.arr));

        C_LPSTRArray_Exp retval11 = S_LPSTRArray_Ret2();
        Assert.IsTrue(Equals(InitArray<string>(ARRAY_SIZE), retval11.arr));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Exp retval12 = S_BSTRArray_Ret2();
            Assert.IsTrue(Equals(InitArray<string>(ARRAY_SIZE), retval12.arr));
        }

        C_StructArray_Exp retval13 = S_StructArray_Ret2();
        Assert.IsTrue(TestStructEquals(InitStructArray(ARRAY_SIZE), retval13.arr));
    }

    static int Main(string[] args)
    {
        try
        {
            RunTest1("RunTest1 : Marshal array as field as ByValArray in sequential struct as parameter.");
            RunTest2("RunTest2 : Marshal array as field as ByValArray in sequential class as parameter.");
            RunTest3("RunTest3 : Marshal array as field as ByValArray in explicit struct as parameter.");
            RunTest4("RunTest4 : Marshal array as field as ByValArray in explicit class as parameter.");
            RunTest5("RunTest5 : Marshal array as field as ByValArray in sequential class as return type.");
            RunTest6("RunTest6 : Marshal array as field as ByValArray in explicit class as return type.");
        
            Console.WriteLine("\nTest PASS.");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nTEST FAIL: {e.Message}");
            return 101;
        }
    }
}
