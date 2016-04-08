using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;

public class GetNativeVariantForObjectTest
{   
    internal struct Variant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr bstrVal;
        public IntPtr pRecInfo;
    }    
    
    public static void NullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => Marshal.GetNativeVariantForObject(new object(),IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => Marshal.GetNativeVariantForObject<int>(1, IntPtr.Zero));
    }
    
    public static void EmptyObject()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject(null, pNative);
        object o = Marshal.GetObjectForNativeVariant(pNative);
        Assert.AreEqual(null, o);
    }
    
    public static void PrimitiveType()
    {
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<ushort>(99, pNative);
        ushort actual = Marshal.GetObjectForNativeVariant<ushort>(pNative);
        Assert.AreEqual(99, actual);
    }
    
    public static void Char()
    {
        // GetNativeVariantForObject supports char, but internally recognizes it the same as ushort
        // because the native variant type uses mscorlib type VarEnum to store what type it contains.
        // To get back the original char, use GetObjectForNativeVariant<ushort> and cast to char.
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<char>('a', pNative);
        ushort actual = Marshal.GetObjectForNativeVariant<ushort>(pNative);
        char actualChar = (char)actual;
        Assert.AreEqual('a', actual);
    }
    
    public static void CharNegative()
    {
        // While GetNativeVariantForObject supports taking chars, GetObjectForNativeVariant will
        // never return a char. The internal type is ushort, as mentioned above. This behavior
        // is the same on ProjectN and Desktop CLR.
        Variant v = new Variant();
        IntPtr pNative = Marshal.AllocHGlobal(Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<char>('a', pNative);
        Assert.Throws<InvalidCastException>(() =>
        {
            char actual = Marshal.GetObjectForNativeVariant<char>(pNative);
            Assert.AreEqual('a', actual);
        });
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

    public static int Main(String[] unusedArgs)
    {
        EmptyObject();
        PrimitiveType();
        Char();
        CharNegative();
        StringType();
        DoubleType();
        return 100;
    }
}
