using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;

public class GetObjectForNativeVariantTest 
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
        Assert.Throws<ArgumentNullException>(() => Marshal.GetObjectForNativeVariant(IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => Marshal.GetObjectForNativeVariant<int>(IntPtr.Zero));
    }
    
    public static void Decimal()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject(3.14m, pNative);
        decimal d = Marshal.GetObjectForNativeVariant<decimal>(pNative);
        Assert.AreEqual(3.14m, d);
    }
        
    public static void PrimitiveType()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<ushort>(99, pNative);
        ushort actual = Marshal.GetObjectForNativeVariant<ushort>(pNative);
        Assert.AreEqual(99, actual);
    }
        
    public static void StringType()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<string>("99", pNative);
        string actual = Marshal.GetObjectForNativeVariant<string>(pNative);
        Assert.AreEqual("99", actual);
    }
        
    public static void DoubleType()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<double>(3.14, pNative);
        double actual = Marshal.GetObjectForNativeVariant<double>(pNative);
        Assert.AreEqual(3.14, actual);
    }
        
    public static void IUnknownType()
    {
        Variant v = new Variant();
        IntPtr pObj = Marshal.GetIUnknownForObject(new object());
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<IntPtr>(pObj, pNative);
        IntPtr pActualObj = Marshal.GetObjectForNativeVariant<IntPtr>(pNative);
        Assert.AreEqual(pObj, pActualObj);
    }

    public static int Main(String[] unusedArgs)
    {
        //IUnknownType();
        DoubleType();
        StringType();
        PrimitiveType();
        Decimal();
        NullParameter();
        return 100;
    }
}
