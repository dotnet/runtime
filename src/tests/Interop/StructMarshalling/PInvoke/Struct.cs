using System;
using System.Runtime.InteropServices;

public class Common
{
    public const int NumArrElements = 2;
}
//////////////////////////////struct definition///////////////////////////
[StructLayout(LayoutKind.Sequential)]
public struct InnerSequential
{
    public int f1;
    public float f2;
    public String f3;
}

[StructLayout(LayoutKind.Sequential)]
struct IntWithInnerSequential
{
    public int i1;
    public InnerSequential sequential;
}

[StructLayout(LayoutKind.Sequential)]
struct SequentialWrapper
{
    public InnerSequential sequential;
}

[StructLayout(LayoutKind.Sequential)]
struct SequentialDoubleWrapper
{
    public SequentialWrapper wrapper;
}

[StructLayout(LayoutKind.Sequential)]
struct AggregateSequentialWrapper
{
    public SequentialWrapper wrapper1;
    public InnerSequential sequential;
    public SequentialWrapper wrapper2;
}

[StructLayout(LayoutKind.Sequential)]
public struct ComplexStruct
{
    public int i;
    [MarshalAs(UnmanagedType.I1)]
    public bool b;
    [MarshalAs(UnmanagedType.LPStr)]
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
    [MarshalAs(UnmanagedType.LPStr)]
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
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string fullName;
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
public struct IntegerStructSequential
{
    public int i;
}
[StructLayout(LayoutKind.Sequential)]
public struct OuterIntegerStructSequential
{
    public int i;
    public IntegerStructSequential s_int;
}
[StructLayout(LayoutKind.Sequential)]
public struct IncludeOuterIntegerStructSequential
{
    public OuterIntegerStructSequential s;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct S11
{
    public int* i32;
    public int i;
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

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct AutoString
{
    public string str;
}

[StructLayout(LayoutKind.Sequential)]
public struct HFA
{
    public float f1;
    public float f2;
    public float f3;
    public float f4;
}

[StructLayout(LayoutKind.Explicit)]
public struct ExplicitHFA
{
    [FieldOffset(0)]
    public float f1;
    [FieldOffset(4)]
    public float f2;
    [FieldOffset(8)]
    public float f3;
    [FieldOffset(12)]
    public float f4;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct ExplicitFixedHFA
{
    [FieldOffset(0)]
    public float f1;
    [FieldOffset(4)]
    public float f2;
    [FieldOffset(8)]
    public fixed float fs[2];
}

[StructLayout(LayoutKind.Explicit)]
public struct OverlappingHFA
{
    [FieldOffset(0)]
    public HFA hfa;

    [FieldOffset(0)]
    public ExplicitHFA explicitHfa;

    [FieldOffset(0)]
    public ExplicitFixedHFA explicitFixedHfa;
}

[StructLayout(LayoutKind.Sequential)]
public struct DoubleHFA
{
    public double d1;
    public double d2;
}

[StructLayout(LayoutKind.Sequential)]
public struct ManyInts
{
    public int i1;
    public int i2;
    public int i3;
    public int i4;
    public int i5;
    public int i6;
    public int i7;
    public int i8;
    public int i9;
    public int i10;
    public int i11;
    public int i12;
    public int i13;
    public int i14;
    public int i15;
    public int i16;
    public int i17;
    public int i18;
    public int i19;
    public int i20;

    public System.Collections.Generic.IEnumerator<int> GetEnumerator()
    {
        yield return i1;
        yield return i2;
        yield return i3;
        yield return i4;
        yield return i5;
        yield return i6;
        yield return i7;
        yield return i8;
        yield return i9;
        yield return i10;
        yield return i11;
        yield return i12;
        yield return i13;
        yield return i14;
        yield return i15;
        yield return i16;
        yield return i17;
        yield return i18;
        yield return i19;
        yield return i20;
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct MultipleBool
{
    public bool b1;
    public bool b2;
}

[StructLayout(LayoutKind.Explicit)]
public struct OverlappingLongFloat
{
    [FieldOffset(0)]
    public long l;

    [FieldOffset(4)]
    public float f;
}

[StructLayout(LayoutKind.Explicit)]
public struct OverlappingLongFloat2
{
    [FieldOffset(4)]
    public float f;
    [FieldOffset(0)]
    public long l;
}

[StructLayout(LayoutKind.Explicit)]
public struct OverlappingMultipleEightbyte
{
    [FieldOffset(8)]
    public int i;
    [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] arr;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FixedBufferClassificationTestBlittable
{
    public fixed int arr[3];
    public float f;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FixedBufferClassificationTest
{
    public fixed int arr[3];
    public NonBlittableFloat f;
}

// A non-blittable wrapper for a float value.
// Used to force a type with a float field to be non-blittable
// and take a different code path.
[StructLayout(LayoutKind.Sequential)]
public struct NonBlittableFloat
{
    public NonBlittableFloat(float f)
    {
        arr = new []{f};
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    private float[] arr;

    public float F => arr[0];
}

public struct Int32Wrapper
{
    public int i;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FixedArrayClassificationTest
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public Int32Wrapper[] arr;
    public float f;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct UnicodeCharArrayClassification
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public char[] arr;
    public float f;
}

delegate int IntIntDelegate(int a);

[StructLayout(LayoutKind.Sequential)]
public struct DelegateFieldMarshaling
{
    public Delegate IntIntFunction;
}

public struct Int32CLongStruct
{
    public int i;
    public CLong l;
}
