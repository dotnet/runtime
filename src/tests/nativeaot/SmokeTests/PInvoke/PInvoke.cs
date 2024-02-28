// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MULTIMODULE_BUILD && !DEBUG
// Some tests won't work if we're using optimizing codegen, but scanner doesn't run.
// This currently happens in optimized multi-obj builds.
#define OPTIMIZED_MODE_WITHOUT_SCANNER
#endif

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// Make sure the interop data are present even without reflection
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All)]
    internal class __BlockAllReflectionAttribute : Attribute { }
}

// Name of namespace matches the name of the assembly on purpose to
// ensure that we can handle this (mostly an issue for C++ code generation).
namespace PInvokeTests
{
    internal class Program
    {
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int Square(int intValue);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int IsTrue(bool boolValue);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int CheckIncremental(int[] array, int sz);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int CheckIncremental_Foo(Foo[] array, int sz);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int Inc(ref int value);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern int VerifyByRefFoo(ref Foo value);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        private static extern ref Foo VerifyByRefFooReturn();

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern bool GetNextChar(ref char c);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiString(string str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiStringOut(out string str);

        [DllImport("PInvokeNative", EntryPoint = "VerifyAnsiString", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyUTF8String([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("PInvokeNative", EntryPoint = "VerifyAnsiStringOut", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyUTF8StringOut([Out, MarshalAs(UnmanagedType.LPUTF8Str)] out string str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiStringRef(ref string str);

        [DllImport("PInvokeNative", EntryPoint = "VerifyAnsiStringRef", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiStringInRef([In]ref string str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int VerifyUnicodeString(string str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int VerifyUnicodeStringOut(out string str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int VerifyUnicodeStringRef(ref string str);

        [DllImport("PInvokeNative", EntryPoint = "VerifyUnicodeStringRef", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int VerifyUnicodeStringInRef([In]ref string str);

        [DllImport("PInvokeNative", CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiStringArray([In, MarshalAs(UnmanagedType.LPArray)]string[] str);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern bool VerifyAnsiCharArrayIn(char[] a);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern bool VerifyAnsiCharArrayOut([Out]char[] a);

        [DllImport("PInvokeNative", CharSet = CharSet.Ansi)]
        private static extern void ToUpper([In, Out, MarshalAs(UnmanagedType.LPArray)]string[] str);

        [DllImport("PInvokeNative", CharSet = CharSet.Ansi)]
        private static extern bool VerifySizeParamIndex(
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte[] arrByte, out byte arrSize);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "VerifyUnicodeStringBuilder")]
        private static extern int VerifyUnicodeStringBuilder(StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "VerifyUnicodeStringBuilder")]
        private static extern int VerifyUnicodeStringBuilderIn([In]StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int VerifyUnicodeStringBuilderOut([Out]StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "VerifyAnsiStringBuilder")]
        private static extern int VerifyAnsiStringBuilder(StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "VerifyAnsiStringBuilder")]
        private static extern int VerifyAnsiStringBuilderIn([In]StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int VerifyAnsiStringBuilderOut([Out]StringBuilder sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "SafeHandleTest")]
        public static extern bool HandleRefTest(HandleRef hr, Int64 hrValue);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        public static extern bool SafeHandleTest(SafeMemoryHandle sh1, Int64 sh1Value);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        public static extern int SafeHandleOutTest(out SafeMemoryHandle sh1);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        public static extern int SafeHandleRefTest(ref SafeMemoryHandle sh1, bool change);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool LastErrorTest();

        delegate int Delegate_Int(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j);
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_Int(Delegate_Int del);

        delegate int Delegate_Int_AggressiveInlining(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j);
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "ReversePInvoke_Int")]
#if OPTIMIZED_MODE_WITHOUT_SCANNER
        [MethodImpl(MethodImplOptions.NoInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        static extern bool ReversePInvoke_Int_AggressiveInlining(Delegate_Int_AggressiveInlining del);


        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet=CharSet.Ansi)]
        delegate bool Delegate_String(string s);
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_String(Delegate_String del);

        [DllImport("PInvokeNative", EntryPoint="ReversePInvoke_String", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_String_Delegate(Delegate del);

        [DllImport("PInvokeNative", EntryPoint="ReversePInvoke_String", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_String_MulticastDelegate(MulticastDelegate del);

        struct FieldDelegate
        {
            public Delegate d;
        }

        struct FieldMulticastDelegate
        {
            public MulticastDelegate d;
        }

        [DllImport("PInvokeNative", EntryPoint="ReversePInvoke_DelegateField", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_Field_Delegate(FieldDelegate del);

        [DllImport("PInvokeNative", EntryPoint="ReversePInvoke_DelegateField", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_Field_MulticastDelegate(FieldMulticastDelegate del);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate bool Delegate_OutString([MarshalAs(0x30)] out string s);
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_OutString(Delegate_OutString del);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        delegate bool Delegate_Array([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] array, IntPtr sz);
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool ReversePInvoke_Array(Delegate_Array del);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern Delegate_String GetDelegate();

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool Callback(ref Delegate_String d);

        delegate void Delegate_Unused();
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern unsafe int* ReversePInvoke_Unused(Delegate_Unused del);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "StructTest")]
        static extern bool StructTest_Auto(AutoStruct ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool StructTest_Sequential2(NesterOfSequentialStruct.SequentialStruct ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool StructTest(SequentialStruct ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern void StructTest_ByRef(ref SequentialStruct ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "StructTest_ByRef")]
        static extern bool ClassTest([In, Out] SequentialClass ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "StructTest_ByRef")]
        static extern bool AsAnyTest([In, Out, MarshalAs(40 /* UnmanagedType.AsAny */)] object o);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern void StructTest_ByOut(out SequentialStruct ss);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool StructTest_Explicit(ExplicitStruct es);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool StructTest_Nested(NestedStruct ns);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, EntryPoint = "StructTest_Nested")]
        static extern bool StructTest_NestedClass(NestedClass nc);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool StructTest_Array(SequentialStruct []ns, int length);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        static extern bool IsNULL(char[] a);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        static extern bool IsNULL(String sb);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool IsNULL(Foo[] foo);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool IsNULL(SequentialStruct[] foo);

        [StructLayout(LayoutKind.Sequential, CharSet= CharSet.Ansi, Pack = 4)]
        public unsafe struct InlineArrayStruct
        {
            public int f0;
            public int f1;
            public int f2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public short[] inlineArray;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
            public string inlineString;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        public unsafe struct InlineUnicodeStruct
        {
            public int f0;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
            public string inlineString;
        }

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool InlineArrayTest(ref InlineArrayStruct ias, ref InlineUnicodeStruct ius);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public unsafe delegate void SetLastErrorFuncDelegate(int errorCode);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GetFunctionPointer();

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GetNativeFuncFunctionPointer();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal unsafe struct InlineString
        {
            internal uint size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string name;
        }

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool InlineStringTest(ref InlineString ias);

        internal delegate int Callback0();
        internal delegate int Callback1();
        internal delegate int Callback2();

        [DllImport("PInvokeNative")]
        internal static extern bool RegisterCallbacks(ref Callbacks callbacks);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Callbacks
        {
            public Callback0 callback0;
            public Callback1 callback1;
            public Callback2 callback2;
        }

        public static int callbackFunc0() { return 0; }
        public static int callbackFunc1() { return 1; }
        public static int callbackFunc2() { return 2; }

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        static extern void ValidateSuccessCall(int errorCode);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        static extern int ValidateIntResult(int errorCode);

        [DllImport("PInvokeNative", EntryPoint = "ValidateIntResult", CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        static extern MagicEnum ValidateEnumResult(int errorCode);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        internal static extern decimal DecimalTest(decimal value);

        [UnmanagedCallersOnly]
        internal unsafe static void UnmanagedMethod(byte* address, byte value) => *address = value;

        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall)})]
        internal unsafe static void StdcallMethod(byte* address, byte value) => *address = value;

        internal enum MagicEnum
        {
            MagicResult = 42,
        }

        public static int Main()
        {
            TestBlittableType();
            TestBoolean();
            TestUnichar();
            TestArrays();
            TestByRef();
            TestString();
            TestStringBuilder();
            TestLastError();
            TestHandleRef();
            TestSafeHandle();
            TestStringArray();
            TestSizeParamIndex();
            TestDelegate();
            TestStruct();
            TestLayoutClassPtr();
            TestLayoutClass();
            TestAsAny();
            TestMarshalStructAPIs();
            TestWithoutPreserveSig();
            TestForwardDelegateWithUnmanagedCallersOnly();
            TestDecimal();
            TestDifferentModopts();
            TestGenericCaller<string>();
            TestFunctionPointers();

            return 100;
        }

        public static void ThrowIfNotEquals<T>(T expected, T actual, string message)
        {
            if (!expected.Equals(actual))
            {
                message += "\nExpected: " + expected.ToString() + "\n";
                message += "Actual: " + actual.ToString() + "\n";
                throw new Exception(message);
            }
        }

        public static void ThrowIfNotEquals(bool expected, bool actual, string message)
        {
            ThrowIfNotEquals(expected ? 1 : 0, actual ? 1 : 0, message);
        }

        private static void TestBlittableType()
        {
            Console.WriteLine("Testing marshalling blittable types");
            ThrowIfNotEquals(100, Square(10), "Int marshalling failed");
        }

        private static void TestBoolean()
        {
            Console.WriteLine("Testing marshalling boolean");
            ThrowIfNotEquals(1, IsTrue(true), "Bool marshalling failed");
            ThrowIfNotEquals(0, IsTrue(false), "Bool marshalling failed");
        }

        private static void TestUnichar()
        {
            Console.WriteLine("Testing Unichar");
            char c = 'a';
            ThrowIfNotEquals(true, GetNextChar(ref c), "Unichar marshalling failed.");
            ThrowIfNotEquals('b', c, "Unichar marshalling failed.");
        }

        struct Foo
        {
            public int a;
            public float b;
        }

        private static void TestArrays()
        {
            Console.WriteLine("Testing marshalling int arrays");

            const int ArraySize = 100;
            int[] arr = new int[ArraySize];
            for (int i = 0; i < ArraySize; i++)
                arr[i] = i;

            ThrowIfNotEquals(0, CheckIncremental(arr, ArraySize), "Array marshalling failed");

            Console.WriteLine("Testing marshalling blittable struct arrays");

            Foo[] arr_foo = null;
            ThrowIfNotEquals(true, IsNULL(arr_foo), "Blittable array null check failed");

            arr_foo = new Foo[ArraySize];
            for (int i = 0; i < ArraySize; i++)
            {
                arr_foo[i].a = i;
                arr_foo[i].b = i;
            }

            ThrowIfNotEquals(0, CheckIncremental_Foo(arr_foo, ArraySize), "Array marshalling failed");

            char[] a = "Hello World".ToCharArray();
            ThrowIfNotEquals(true, VerifyAnsiCharArrayIn(a), "Ansi Char Array In failed");

            char[] b = new char[12];
            ThrowIfNotEquals(true, VerifyAnsiCharArrayOut(b), "Ansi Char Array Out failed");
            ThrowIfNotEquals("Hello World!", new String(b), "Ansi Char Array Out failed2");

            char[] c = null;
            ThrowIfNotEquals(true, IsNULL(c), "AnsiChar Array null check failed");
        }

        private static void TestByRef()
        {
            Console.WriteLine("Testing marshalling by ref");

            int value = 100;
            ThrowIfNotEquals(0, Inc(ref value), "By ref marshalling failed");
            ThrowIfNotEquals(101, value, "By ref marshalling failed");

            Foo foo = new Foo();
            foo.a = 10;
            foo.b = 20;
            int ret = VerifyByRefFoo(ref foo);
            ThrowIfNotEquals(0, ret, "By ref struct marshalling failed");

            ThrowIfNotEquals(foo.a, 11, "By ref struct unmarshalling failed");
            ThrowIfNotEquals(foo.b, 21.0f, "By ref struct unmarshalling failed");

            ref Foo retfoo = ref VerifyByRefFooReturn();
            ThrowIfNotEquals(retfoo.a, 42, "By ref struct return failed");
            ThrowIfNotEquals(retfoo.b, 43.0, "By ref struct return failed");
        }

        private static void TestString()
        {
            Console.WriteLine("Testing marshalling string");
            ThrowIfNotEquals(1, VerifyAnsiString("Hello World"), "Ansi String marshalling failed.");
            ThrowIfNotEquals(1, VerifyUnicodeString("Hello World"), "Unicode String marshalling failed.");
            string s;
            ThrowIfNotEquals(1, VerifyAnsiStringOut(out s), "Out Ansi String marshalling failed");
            ThrowIfNotEquals("Hello World", s, "Out Ansi String marshalling failed");

            VerifyAnsiStringInRef(ref s);
            ThrowIfNotEquals("Hello World", s, "In Ref ansi String marshalling failed");

            VerifyAnsiStringRef(ref s);
            ThrowIfNotEquals("Hello World!", s, "Ref ansi String marshalling failed");

            ThrowIfNotEquals(1, VerifyUnicodeStringOut(out s), "Out Unicode String marshalling failed");
            ThrowIfNotEquals("Hello World", s, "Out Unicode String marshalling failed");

            VerifyUnicodeStringInRef(ref s);
            ThrowIfNotEquals("Hello World", s, "In Ref Unicode String marshalling failed");

            VerifyUnicodeStringRef(ref s);
            ThrowIfNotEquals("Hello World!", s, "Ref Unicode String marshalling failed");

            string ss = null;
            ThrowIfNotEquals(true, IsNULL(ss), "Ansi String null check failed");

            ThrowIfNotEquals(1, VerifyUTF8String("Hello World"), "UTF8 String marshalling failed.");
            ThrowIfNotEquals(1, VerifyUTF8StringOut(out s), "Out UTF8 String marshalling failed");
            ThrowIfNotEquals("Hello World", s, "Out UTF8 String marshalling failed");
        }

        private static void TestStringBuilder()
        {
            Console.WriteLine("Testing marshalling string builder");
            StringBuilder sb = new StringBuilder("Hello World");
            ThrowIfNotEquals(1, VerifyUnicodeStringBuilder(sb), "Unicode StringBuilder marshalling failed");
            ThrowIfNotEquals("HELLO WORLD", sb.ToString(), "Unicode StringBuilder marshalling failed.");

            StringBuilder sb1 = null;
            // for null stringbuilder it should return -1
            ThrowIfNotEquals(-1, VerifyUnicodeStringBuilder(sb1), "Null unicode StringBuilder marshalling failed");

            StringBuilder sb2 = new StringBuilder("Hello World");
            ThrowIfNotEquals(1, VerifyUnicodeStringBuilderIn(sb2), "In unicode StringBuilder marshalling failed");
            // Only [In] should change stringbuilder value
            ThrowIfNotEquals("Hello World", sb2.ToString(), "In unicode StringBuilder marshalling failed");

            StringBuilder sb3 = new StringBuilder();
            ThrowIfNotEquals(1, VerifyUnicodeStringBuilderOut(sb3), "Out Unicode string marshalling failed");
            ThrowIfNotEquals("Hello World", sb3.ToString(), "Out Unicode StringBuilder marshalling failed");

            StringBuilder sb4 = new StringBuilder("Hello World");
            ThrowIfNotEquals(1, VerifyAnsiStringBuilder(sb4), "Ansi StringBuilder marshalling failed");
            ThrowIfNotEquals("HELLO WORLD", sb4.ToString(), "Ansi StringBuilder marshalling failed.");

            StringBuilder sb5 = null;
            // for null stringbuilder it should return -1
            ThrowIfNotEquals(-1, VerifyAnsiStringBuilder(sb5), "Null Ansi StringBuilder marshalling failed");

            StringBuilder sb6 = new StringBuilder("Hello World");
            ThrowIfNotEquals(1, VerifyAnsiStringBuilderIn(sb6), "In unicode StringBuilder marshalling failed");
            // Only [In] should change stringbuilder value
            ThrowIfNotEquals("Hello World", sb6.ToString(), "In unicode StringBuilder marshalling failed");

            StringBuilder sb7 = new StringBuilder();
            ThrowIfNotEquals(1, VerifyAnsiStringBuilderOut(sb7), "Out Ansi string marshalling failed");
            ThrowIfNotEquals("Hello World!", sb7.ToString(), "Out Ansi StringBuilder marshalling failed");
        }


        private static void TestStringArray()
        {
            Console.WriteLine("Testing marshalling string array");
            string[] strArray = new string[] { "Hello", "World" };
            ThrowIfNotEquals(1, VerifyAnsiStringArray(strArray), "Ansi string array in marshalling failed.");
            ToUpper(strArray);

            ThrowIfNotEquals(true, "HELLO" == strArray[0] && "WORLD" == strArray[1], "Ansi string array  out marshalling failed.");
        }

        private static void TestLastError()
        {
            Console.WriteLine("Testing last error");
            ThrowIfNotEquals(true, LastErrorTest(), "GetLastWin32Error is not zero");
            ThrowIfNotEquals(12345, Marshal.GetLastWin32Error(), "Last Error test failed");
        }

        private static void TestHandleRef()
        {
            Console.WriteLine("Testing marshalling HandleRef");

            ThrowIfNotEquals(true, HandleRefTest(new HandleRef(new object(), (IntPtr)2018), 2018), "HandleRef marshalling failed");
        }

        private static void TestSafeHandle()
        {
            Console.WriteLine("Testing marshalling SafeHandle");

            SafeMemoryHandle hnd = SafeMemoryHandle.AllocateMemory(1000);

            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            long val = hndIntPtr.ToInt64(); //return the 64-bit value associated with hnd

            ThrowIfNotEquals(true, SafeHandleTest(hnd, val), "SafeHandle marshalling failed.");

            Console.WriteLine("Testing marshalling out SafeHandle");
            SafeMemoryHandle hnd2;
            int actual = SafeHandleOutTest(out hnd2);
            int expected = unchecked((int)hnd2.DangerousGetHandle().ToInt64());
            ThrowIfNotEquals(actual, expected, "SafeHandle out marshalling failed");

            Console.WriteLine("Testing marshalling ref SafeHandle");
            SafeMemoryHandle hndOriginal = hnd2;
            SafeHandleRefTest(ref hnd2, false);
            ThrowIfNotEquals(hndOriginal, hnd2, "SafeHandle no-op ref marshalling failed");

            int actual3 = SafeHandleRefTest(ref hnd2, true);
            int expected3 = unchecked((int)hnd2.DangerousGetHandle().ToInt64());
            ThrowIfNotEquals(actual3, expected3, "SafeHandle ref marshalling failed");

            hndOriginal.Dispose();
            hnd2.Dispose();
        }

        private static void TestSizeParamIndex()
        {
            Console.WriteLine("Testing SizeParamIndex");
            byte byte_Array_Size;
            byte[] arrByte;

            VerifySizeParamIndex(out arrByte, out byte_Array_Size);
            ThrowIfNotEquals(10, byte_Array_Size, "out size failed.");
            bool pass = true;
            for (int i = 0; i < byte_Array_Size; i++)
            {
                if (arrByte[i] != i)
                {
                    pass = false;
                    break;
                }
            }
            ThrowIfNotEquals(true, pass, "SizeParamIndex failed.");
        }

        private class ClosedDelegateCLass
        {
            public int Sum(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
            {
                return a + b + c + d + e + f + g + h + i + j;
            }

            public bool GetString(String s)
            {
                return s == "Hello World";
            }

            public bool CheckArray(int[] a, IntPtr sz)
            {
                if (sz != new IntPtr(42))
                    return false;

                for (int i = 0; i < (int)sz; i++)
                {
                    if (a[i] != i)
                        return false;
                }
                return true;
            }
        }

        private static void TestDelegate()
        {
            Console.WriteLine("Testing Delegate");

            Delegate_Int del = new Delegate_Int(Sum);
            ThrowIfNotEquals(true, ReversePInvoke_Int(del), "Delegate marshalling failed.");

            Delegate_Int_AggressiveInlining del_aggressive = new Delegate_Int_AggressiveInlining(Sum);
            ThrowIfNotEquals(true, ReversePInvoke_Int_AggressiveInlining(del_aggressive), "Delegate marshalling with aggressive inlining failed.");

            unsafe
            {
                //
                // We haven't instantiated Delegate_Unused and nobody
                // allocates it. If a EEType is not constructed for Delegate_Unused
                // it will fail during linking.
                //
                ReversePInvoke_Unused(null);
            }

            Delegate_Int closed = new Delegate_Int((new ClosedDelegateCLass()).Sum);
            ThrowIfNotEquals(true, ReversePInvoke_Int(closed), "Closed Delegate marshalling failed.");

            Delegate_String ret = GetDelegate();
            ThrowIfNotEquals(true, ret("Hello World!"), "Delegate as P/Invoke return failed");

            Delegate_String d = new Delegate_String(new ClosedDelegateCLass().GetString);
            ThrowIfNotEquals(true, Callback(ref d), "Delegate IN marshalling failed");
            ThrowIfNotEquals(true, d("Hello World!"), "Delegate OUT marshalling failed");

            Delegate_String ds = new Delegate_String((new ClosedDelegateCLass()).GetString);
            ThrowIfNotEquals(true, ReversePInvoke_String(ds), "Delegate marshalling failed.");

            ThrowIfNotEquals(true, ReversePInvoke_String_Delegate(ds), "Delegate marshalling failed.");
            ThrowIfNotEquals(true, ReversePInvoke_String_MulticastDelegate(ds), "Delegate marshalling failed.");

            FieldDelegate fd;
            fd.d = ds;
            ThrowIfNotEquals(true, ReversePInvoke_Field_Delegate(fd), "Delegate marshalling failed.");
            FieldMulticastDelegate fmd;
            fmd.d = ds;
            ThrowIfNotEquals(true, ReversePInvoke_Field_MulticastDelegate(fmd), "Delegate marshalling failed.");

            Delegate_OutString dos = new Delegate_OutString((out string x) =>
            {
                x = "Hello there!";
                return true;
            });
            ThrowIfNotEquals(true, ReversePInvoke_OutString(dos), "Delegate string out marshalling failed.");

            Delegate_Array da = new Delegate_Array((new ClosedDelegateCLass()).CheckArray);
            ThrowIfNotEquals(true, ReversePInvoke_Array(da), "Delegate array marshalling failed.");

            IntPtr procAddress = GetFunctionPointer();
            SetLastErrorFuncDelegate funcDelegate =
                Marshal.GetDelegateForFunctionPointer<SetLastErrorFuncDelegate>(procAddress);
            funcDelegate(0x204);
            ThrowIfNotEquals(0x204, Marshal.GetLastWin32Error(), "Not match");
		}
		
		private static unsafe void TestFunctionPointers()
		{
            IntPtr procAddress = GetNativeFuncFunctionPointer();
            delegate* unmanaged[Cdecl] <int, int> unmanagedFuncDelegate =
                (delegate* unmanaged[Cdecl] <int, int>)procAddress;
            var result = unmanagedFuncDelegate(100);
            ThrowIfNotEquals(1422, result, "Function pointer did not set expected error code");
        }

        static int Sum(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
        {
            return a + b + c + d + e + f + g + h + i + j;
        }
        [StructLayout(LayoutKind.Auto)]
        public struct AutoStruct
        {
            public short f0;
            public int f1;
            public float f2;
            [MarshalAs(UnmanagedType.LPStr)]
            public String f3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SequentialStruct
        {
            // NOTE: Same members as SequentialClass below
            public short f0;
            public int f1;
            public float f2;
            [MarshalAs(UnmanagedType.LPStr)]
            public String f3;
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            public String f4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SequentialClass
        {
            // NOTE: Same members as SequentialStruct above
            public short f0;
            public int f1;
            public float f2;
            [MarshalAs(UnmanagedType.LPStr)]
            public String f3;
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            public String f4;
        }

        // A second struct with the same name but nested. Regression test against native types being mangled into
        // the compiler-generated type and losing fully qualified type name information.
        class NesterOfSequentialStruct
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SequentialStruct
            {
                public float f1;
                public int f2;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ExplicitStruct
        {
            // NOTE: Same layout as ExplicitClass
            [FieldOffset(0)]
            public int f1;

            [FieldOffset(12)]
            public float f2;

            [FieldOffset(24)]
            [MarshalAs(UnmanagedType.LPStr)]
            public String f3;
        }

        [StructLayout(LayoutKind.Explicit)]
        public class ExplicitClass
        {
            // NOTE: Same layout as ExplicitStruct
            [FieldOffset(0)]
            public int f1;

            [FieldOffset(12)]
            public float f2;

            [FieldOffset(24)]
            [MarshalAs(UnmanagedType.LPStr)]
            public String f3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NestedStruct
        {
            public int f1;

            public ExplicitStruct f2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NestedClass
        {
            public int f1;

            public ExplicitClass f2;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct BlittableStruct
        {
            [FieldOffset(4)]
            public float FirstField;
            [FieldOffset(12)]
            public float SecondField;
            [FieldOffset(32)]
            public long ThirdField;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NonBlittableStruct
        {
            public int f1;
            public bool f2;
            public bool f3;
            public bool f4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class BlittableClass
        {
            public long f1;
            public int f2;
            public int f3;
            public long f4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class NonBlittableClass
        {
            public bool f1;
            public bool f2;
            public int f3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ClassForTestingFlowAnalysis
        {
            public int Field;
        }

        private static void TestStruct()
        {
            Console.WriteLine("Testing Structs");
            SequentialStruct ss = new SequentialStruct();
            ss.f0 = 100;
            ss.f1 = 1;
            ss.f2 = 10.0f;
            ss.f3 = "Hello";
            ss.f4 = "Hola";

            ThrowIfNotEquals(true, StructTest(ss), "Struct marshalling scenario1 failed.");

            StructTest_ByRef(ref ss);
            ThrowIfNotEquals(true,  ss.f1 == 2 && ss.f2 == 11.0 && ss.f3.Equals("Ifmmp") && ss.f4.Equals("Ipmb"), "Struct marshalling scenario2 failed.");

            SequentialStruct ss2 = new SequentialStruct();
            StructTest_ByOut(out ss2);
            ThrowIfNotEquals(true, ss2.f0 == 1 && ss2.f1 == 1.0 &&  ss2.f2 == 1.0 && ss2.f3.Equals("0123456") && ss2.f4.Equals("789"), "Struct marshalling scenario3 failed.");

            NesterOfSequentialStruct.SequentialStruct ss3 = new NesterOfSequentialStruct.SequentialStruct();
            ss3.f1 = 10.0f;
            ss3.f2 = 123;

            ThrowIfNotEquals(true, StructTest_Sequential2(ss3), "Struct marshalling scenario1 failed.");

            ExplicitStruct es = new ExplicitStruct();
            es.f1 = 100;
            es.f2 = 100.0f;
            es.f3 = "Hello";
            ThrowIfNotEquals(true, StructTest_Explicit(es), "Struct marshalling scenario4 failed.");

            NestedStruct ns = new NestedStruct();
            ns.f1 = 100;
            ns.f2 = es;
            ThrowIfNotEquals(true, StructTest_Nested(ns), "Struct marshalling scenario5 failed.");

            SequentialStruct[] ssa = null;
            ThrowIfNotEquals(true, IsNULL(ssa), "Non-blittable array null check failed");

            ssa = new SequentialStruct[3];
            for (int i = 0; i < 3; i++)
            {
                ssa[i].f1 = 0;
                ssa[i].f1 = i;
                ssa[i].f2 = i*i;
                ssa[i].f3 = i.LowLevelToString();
                ssa[i].f4 = "u8" + i.LowLevelToString();
            }
            ThrowIfNotEquals(true, StructTest_Array(ssa, ssa.Length), "Array of struct marshalling failed");

            InlineString ils = new InlineString();
            InlineStringTest(ref ils);
            ThrowIfNotEquals("Hello World!", ils.name, "Inline string marshalling failed");

            InlineArrayStruct ias = new InlineArrayStruct();
            ias.inlineArray = new short[128];

            for (short i = 0; i < 128; i++)
            {
                ias.inlineArray[i] = i;
            }

            ias.inlineString = "Hello";

            InlineUnicodeStruct ius = new InlineUnicodeStruct();
            ius.inlineString = "Hello World";

            ThrowIfNotEquals(true, InlineArrayTest(ref ias, ref ius), "inline array marshalling failed");
            bool pass = true;
            for (short i = 0; i < 128; i++)
            {
                if (ias.inlineArray[i] != i + 1)
                {
                    pass = false;
                }
            }
            ThrowIfNotEquals(true, pass, "inline array marshalling failed");

            ThrowIfNotEquals("Hello World", ias.inlineString, "Inline ByValTStr Ansi marshalling failed");

            ThrowIfNotEquals("Hello World", ius.inlineString, "Inline ByValTStr Unicode marshalling failed");

            pass = false;
            AutoStruct autoStruct = new AutoStruct();
            try
            {
                // passing struct with Auto layout should throw exception.
                StructTest_Auto(autoStruct);
            }
            catch (Exception)
            {
                pass = true;
            }
            ThrowIfNotEquals(true, pass, "Struct marshalling scenario6 failed.");

            Callbacks callbacks = new Callbacks();
            callbacks.callback0 = new Callback0(callbackFunc0);
            callbacks.callback1 = new Callback1(callbackFunc1);
            callbacks.callback2 = new Callback2(callbackFunc2);
            ThrowIfNotEquals(true,  RegisterCallbacks(ref callbacks), "Scenario 7: Struct with delegate marshalling failed");
        }

        private static void TestLayoutClassPtr()
        {
            SequentialClass ss = new SequentialClass();
            ss.f0 = 100;
            ss.f1 = 1;
            ss.f2 = 10.0f;
            ss.f3 = "Hello";
            ss.f4 = "Hola";

            ClassTest(ss);
            ThrowIfNotEquals(true, ss.f1 == 2 && ss.f2 == 11.0 && ss.f3.Equals("Ifmmp") && ss.f4.Equals("Ipmb"), "LayoutClassPtr marshalling scenario1 failed.");
        }

#if OPTIMIZED_MODE_WITHOUT_SCANNER
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void Workaround()
        {
            // Ensure there's a standalone method body for these two - this method is marked NoOptimization+NoInlining.
            Marshal.SizeOf<SequentialClass>();
            Marshal.SizeOf<SequentialStruct>();
        }
#endif

        private static void TestAsAny()
        {
            if (String.Empty.Length > 0)
            {
                // Make sure we saw these types being used in marshalling
                Marshal.SizeOf<SequentialClass>();
                Marshal.SizeOf<SequentialStruct>();
#if OPTIMIZED_MODE_WITHOUT_SCANNER
                Workaround();
#endif
            }

            SequentialClass sc = new SequentialClass();
            sc.f0 = 100;
            sc.f1 = 1;
            sc.f2 = 10.0f;
            sc.f3 = "Hello";
            sc.f4 = "Hola";

            AsAnyTest(sc);
            ThrowIfNotEquals(true, sc.f1 == 2 && sc.f2 == 11.0 && sc.f3.Equals("Ifmmp") && sc.f4.Equals("Ipmb"), "AsAny marshalling scenario1 failed.");

            SequentialStruct ss = new SequentialStruct();
            ss.f0 = 100;
            ss.f1 = 1;
            ss.f2 = 10.0f;
            ss.f3 = "Hello";
            ss.f4 = "Hola";

            object o = ss;
            AsAnyTest(o);
            ss = (SequentialStruct)o;
            ThrowIfNotEquals(true, ss.f1 == 2 && ss.f2 == 11.0 && ss.f3.Equals("Ifmmp") && sc.f4.Equals("Ipmb"), "AsAny marshalling scenario2 failed.");
        }

        private static void TestLayoutClass()
        {
            ExplicitClass es = new ExplicitClass();
            es.f1 = 100;
            es.f2 = 100.0f;
            es.f3 = "Hello";

            NestedClass ns = new NestedClass();
            ns.f1 = 100;
            ns.f2 = es;
            ThrowIfNotEquals(true, StructTest_NestedClass(ns), "LayoutClass marshalling scenario1 failed.");
        }

        private static unsafe void TestMarshalStructAPIs()
        {
            Console.WriteLine("Testing Marshal APIs for structs");

            BlittableStruct bs = new BlittableStruct() { FirstField = 1.0f, SecondField = 2.0f, ThirdField = 3 };
            int bs_size = Marshal.SizeOf<BlittableStruct>(bs);
            ThrowIfNotEquals(40, bs_size, "Marshal.SizeOf<BlittableStruct> failed");
            IntPtr bs_memory = Marshal.AllocHGlobal(bs_size);
            try
            {
                Marshal.StructureToPtr<BlittableStruct>(bs, bs_memory, false);
                // Marshal.PtrToStructure uses reflection
                // BlittableStruct bs2 = Marshal.PtrToStructure<BlittableStruct>(bs_memory);
                BlittableStruct bs2 = *(BlittableStruct*)bs_memory;
                ThrowIfNotEquals(true, bs2.FirstField == 1.0f && bs2.SecondField == 2.0f && bs2.ThirdField == 3 , "BlittableStruct marshalling Marshal API failed");

                IntPtr offset = Marshal.OffsetOf<BlittableStruct>("SecondField");
                ThrowIfNotEquals(new IntPtr(12), offset, "Struct marshalling OffsetOf failed.");
            }
            finally
            {
                Marshal.FreeHGlobal(bs_memory);
            }

            NonBlittableStruct ts = new NonBlittableStruct() { f1 = 100, f2 = true, f3 = false, f4 = true };
            int size = Marshal.SizeOf<NonBlittableStruct>(ts);
            ThrowIfNotEquals(16, size, "Marshal.SizeOf<NonBlittableStruct> failed");
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr<NonBlittableStruct>(ts, memory, false);
                // Marshal.PtrToStructure uses reflection
                // NonBlittableStruct ts2 = Marshal.PtrToStructure<NonBlittableStruct>(memory);
                // ThrowIfNotEquals(true, ts2.f1 == 100 && ts2.f2 == true && ts2.f3 == false && ts2.f4 == true, "NonBlittableStruct marshalling Marshal API failed");

                IntPtr offset = Marshal.OffsetOf<NonBlittableStruct>("f2");
                ThrowIfNotEquals(new IntPtr(4), offset, "Struct marshalling OffsetOf failed.");
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            BlittableClass bc = new BlittableClass() { f1 = 100, f2 = 12345678, f3 = 999, f4 = -4 };
            int bc_size = Marshal.SizeOf<BlittableClass>(bc);
            ThrowIfNotEquals(24, bc_size, "Marshal.SizeOf<BlittableClass> failed");
            IntPtr bc_memory = Marshal.AllocHGlobal(bc_size);
            try
            {
                Marshal.StructureToPtr<BlittableClass>(bc, bc_memory, false);
                BlittableClass bc2 = new BlittableClass();
                Marshal.PtrToStructure<BlittableClass>(bc_memory, bc2);
                ThrowIfNotEquals(true, bc2.f1 == 100 && bc2.f2 == 12345678 && bc2.f3 == 999 && bc2.f4 == -4, "BlittableClass marshalling Marshal API failed");
            }
            finally
            {
                Marshal.FreeHGlobal(bc_memory);
            }

            NonBlittableClass nbc = new NonBlittableClass() { f1 = false, f2 = true, f3 = 42 };
            int nbc_size = Marshal.SizeOf<NonBlittableClass>(nbc);
            ThrowIfNotEquals(12, nbc_size, "Marshal.SizeOf<NonBlittableClass> failed");
            IntPtr nbc_memory = Marshal.AllocHGlobal(nbc_size);
            try
            {
                Marshal.StructureToPtr<NonBlittableClass>(nbc, nbc_memory, false);
                NonBlittableClass nbc2 = new NonBlittableClass();
                Marshal.PtrToStructure<NonBlittableClass>(nbc_memory, nbc2);
                ThrowIfNotEquals(true, nbc2.f1 == false && nbc2.f2 == true && nbc2.f3 == 42, "NonBlittableClass marshalling Marshal API failed");
            }
            finally
            {
                Marshal.FreeHGlobal(nbc_memory);
            }

            int cftf_size = Marshal.SizeOf(typeof(ClassForTestingFlowAnalysis));
            ThrowIfNotEquals(4, cftf_size, "ClassForTestingFlowAnalysis marshalling Marshal API failed");
        }

        private unsafe static void TestDecimal()
        {
            Console.WriteLine("Testing Decimals");
            var d = new decimal(100, 101, 102, false, 1);
            var ret = DecimalTest(d);
            var expected = new decimal(99, 98, 97, true, 2);
            ThrowIfNotEquals(ret, expected, "Decimal marshalling failed.");
        }

        [UnmanagedCallersOnly]
        static void UnmanagedCallback()
        {
            GC.Collect();
        }

        private static void TestWithoutPreserveSig()
        {
            Console.WriteLine("Testing with PreserveSig = false");
            ValidateSuccessCall(0);

            try
            {
                const int E_NOTIMPL = -2147467263;
                ValidateSuccessCall(E_NOTIMPL);
                throw new Exception("Exception should be thrown for E_NOTIMPL error code");
            }
            catch (NotImplementedException)
            {
            }

            var intResult = ValidateIntResult(0);
            ThrowIfNotEquals(intResult, 42, "Int32 marshalling failed.");

            try
            {
                const int E_NOTIMPL = -2147467263;
                intResult = ValidateIntResult(E_NOTIMPL);
                throw new Exception("Exception should be thrown for E_NOTIMPL error code");
            }
            catch (NotImplementedException)
            {
            }

            var enumResult = ValidateEnumResult(0);
            ThrowIfNotEquals(enumResult, MagicEnum.MagicResult, "Enum marshalling failed.");
        }

        public static unsafe void TestForwardDelegateWithUnmanagedCallersOnly()
        {
            Console.WriteLine("Testing Forward Delegate with UnmanagedCallersOnly");
            Action a = Marshal.GetDelegateForFunctionPointer<Action>((IntPtr)(void*)(delegate* unmanaged<void>)&UnmanagedCallback);
            a();
        }

        public static unsafe void TestDifferentModopts()
        {
            byte storage;

            delegate* unmanaged<byte*, byte, void> unmanagedMethod = &UnmanagedMethod;

            var outUnmanagedMethod = (delegate* unmanaged<out byte, byte, void>)unmanagedMethod;
            outUnmanagedMethod(out storage, 12);
            ThrowIfNotEquals(storage, 12, "Out unmanaged call failed.");

            var refUnmanagedMethod = (delegate* unmanaged<ref byte, byte, void>)unmanagedMethod;
            refUnmanagedMethod(ref storage, 34);
            ThrowIfNotEquals(storage, 34, "Ref unmanaged call failed.");

            delegate* unmanaged[Stdcall]<byte*, byte, void> stdcallMethod = &StdcallMethod;

            var outStdcallMethod = (delegate* unmanaged[Stdcall]<out byte, byte, void>)stdcallMethod;
            outStdcallMethod(out storage, 12);
            ThrowIfNotEquals(storage, 12, "Out unmanaged stdcall failed.");

            var refStdcallMethod = (delegate* unmanaged[Stdcall]<ref byte, byte, void>)stdcallMethod;
            refStdcallMethod(ref storage, 34);
            ThrowIfNotEquals(storage, 34, "Ref unmanaged stdcall failed.");
            var refStdcallSuppressTransition = (delegate* unmanaged[Stdcall, SuppressGCTransition]<ref byte, byte, void>)stdcallMethod;
            if (string.Empty.Length > 0)
            {
                // Do not actually call this because the calling convention is wrong. We just check the compiler didn't crash.
                refStdcallSuppressTransition(ref storage, 56);
            }
        }

        public static unsafe void TestGenericCaller<T>()
        {
            byte storage;

            delegate* unmanaged<byte*, byte, void> unmanagedMethod = &UnmanagedMethod;

            var outUnmanagedMethod = (delegate* unmanaged<out byte, byte, void>)unmanagedMethod;
            outUnmanagedMethod(out storage, 12);
            ThrowIfNotEquals(storage, 12, "Out unmanaged call failed.");
        }
    }

    public class SafeMemoryHandle : SafeHandle //SafeHandle subclass
    {
        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        public static extern SafeMemoryHandle AllocateMemory(int size);

        [DllImport("PInvokeNative", CallingConvention = CallingConvention.StdCall)]
        public static extern bool ReleaseMemory(IntPtr handle);

        public SafeMemoryHandle()
            : base(IntPtr.Zero, true)
        {
        }

        private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
        }

        override protected bool ReleaseHandle()
        {
            return ReleaseMemory(handle);
        }
    } //end of SafeMemoryHandle class

    public static class LowLevelExtensions
    {
        // Int32.ToString() calls into glob/loc garbage that hits CppCodegen limitations
        public static string LowLevelToString(this int i)
        {
            char[] digits = new char[11];
            int numDigits = 0;

            if (i == int.MinValue)
                return "-2147483648";

            bool negative = i < 0;
            if (negative)
                i = -i;

            do
            {
                digits[numDigits] = (char)('0' + (i % 10));
                numDigits++;
                i /= 10;
            }
            while (i != 0);
            if (negative)
            {
                digits[numDigits] = '-';
                numDigits++;
            }
            Array.Reverse(digits);
            return new string(digits, digits.Length - numDigits, numDigits);
        }
    }
}
