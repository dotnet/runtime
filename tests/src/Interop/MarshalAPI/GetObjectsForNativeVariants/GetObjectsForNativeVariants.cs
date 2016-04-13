using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;
#pragma warning disable 618
public class GetObjectsForNativeVariantsTest 
{  
    [StructLayout(LayoutKind.Sequential)]
    public struct Record {
            private IntPtr _record;
            private IntPtr _recordInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UnionTypes {
            [FieldOffset(0)] internal SByte _i1;
            [FieldOffset(0)] internal Int16 _i2;
            [FieldOffset(0)] internal Int32 _i4;
            [FieldOffset(0)] internal Int64 _i8;
            [FieldOffset(0)] internal Byte _ui1;
            [FieldOffset(0)] internal UInt16 _ui2;
            [FieldOffset(0)] internal UInt32 _ui4;
            [FieldOffset(0)] internal UInt64 _ui8;
            [FieldOffset(0)] internal Int32 _int;
            [FieldOffset(0)] internal UInt32 _uint;
            [FieldOffset(0)] internal Single _r4;
            [FieldOffset(0)] internal Double _r8;
            [FieldOffset(0)] internal Int64 _cy;
            [FieldOffset(0)] internal double _date;
            [FieldOffset(0)] internal IntPtr _bstr;
            [FieldOffset(0)] internal IntPtr _unknown;
            [FieldOffset(0)] internal IntPtr _dispatch;
            [FieldOffset(0)] internal IntPtr _pvarVal;
            [FieldOffset(0)] internal IntPtr _byref;
            [FieldOffset(0)] internal Record _record;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeUnion
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public UnionTypes _unionTypes;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Variant
    {
       [FieldOffset(0)] public TypeUnion  m_Variant;
       [FieldOffset(0)]  public decimal m_decimal;
    }
        
    public static void NullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => Marshal.GetObjectsForNativeVariants(IntPtr.Zero, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Marshal.GetObjectsForNativeVariants<int>(new IntPtr(100), -1));
    }
        
    public static void UshortType()
    {
        
        Variant v = new Variant();

        IntPtr pNative = Marshal.AllocHGlobal(2 * Marshal.SizeOf(v));        
        Marshal.GetNativeVariantForObject<ushort>(99, pNative);
        Marshal.GetNativeVariantForObject<ushort>(100, pNative +Marshal.SizeOf(v));


        ushort[] actual = Marshal.GetObjectsForNativeVariants<ushort>(pNative,2);
        Assert.AreEqual(99, actual[0]);
        Assert.AreEqual(100, actual[1]);

        Marshal.FreeHGlobal(pNative);

    }
    public static int Main(String[] args)
    {
        UshortType();
        NullParameter();
        return 100;
    }
}
#pragma warning restore 618