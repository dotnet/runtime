// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

public unsafe class DisabledRuntimeMarshallingNative
{
    public struct StructWithShortAndBool
    {
        public bool b;
        public short s;
        int padding;

        public StructWithShortAndBool(short s, bool b)
        {
            this.s = s;
            this.b = b;
            this.padding = 0xdeadbee;
        }
    }

    public struct StructWithShortAndBoolWithMarshalAs
    {
#if DISABLE_RUNTIME_MARSHALLING
        [MarshalAs(UnmanagedType.VariantBool)]
#else
        [MarshalAs(UnmanagedType.U1)]
#endif
        bool b;
        short s;
        int padding;

        public StructWithShortAndBoolWithMarshalAs(short s, bool b)
        {
            this.s = s;
            this.b = b;
            this.padding = 0xdeadbee;
        }
    }

    public struct StructWithWCharAndShort
    {
        short s;
#if DISABLE_RUNTIME_MARSHALLING
#else
        [MarshalAs(UnmanagedType.U2)]
#endif
        char c;

        public StructWithWCharAndShort(short s, char c)
        {
            this.s = s;
            this.c = c;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithWCharAndShortWithMarshalAs
    {
        short s;
#if DISABLE_RUNTIME_MARSHALLING
        [MarshalAs(UnmanagedType.U1)]
#else
        [MarshalAs(UnmanagedType.U2)]
#endif
        char c;

        public StructWithWCharAndShortWithMarshalAs(short s, char c)
        {
            this.s = s;
            this.c = c;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithShortAndGeneric<T>
    {
        short s;
        T t;
        public StructWithShortAndGeneric(short s, T t)
        {
            this.s = s;
            this.t = t;
        }
    }

#if DISABLE_RUNTIME_MARSHALLING
    public struct StructWithString
    {
        string s;

        public StructWithString(string s)
        {
            this.s = s;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class LayoutClass
    {}
#endif

    [StructLayout(LayoutKind.Auto)]
    public struct AutoLayoutStruct
    {
        int i;
    }

    public struct SequentialWithAutoLayoutField
    {
        AutoLayoutStruct auto;
    }

    public struct SequentialWithAutoLayoutNestedField
    {
        SequentialWithAutoLayoutField field;
    }

#if DISABLE_RUNTIME_MARSHALLING
    public enum ByteEnum : byte
    {
        Value = 42
    }

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    public static extern byte GetEnumUnderlyingValue(ByteEnum b);
#endif

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if !DISABLE_RUNTIME_MARSHALLING
    [return:MarshalAs(UnmanagedType.U1)]
#endif
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBool str, short s, bool b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if DISABLE_RUNTIME_MARSHALLING
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBoolWithMarshalAs str, short s, bool b);
#else
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.Bool)] bool b);
#endif

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if !DISABLE_RUNTIME_MARSHALLING
    [return:MarshalAs(UnmanagedType.U1)]
#endif
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShort str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if !DISABLE_RUNTIME_MARSHALLING
    [return:MarshalAs(UnmanagedType.U1)]
#endif
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShortWithMarshalAs str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if !DISABLE_RUNTIME_MARSHALLING
    [return:MarshalAs(UnmanagedType.U1)]
#endif
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<char> str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
#if !DISABLE_RUNTIME_MARSHALLING
    [return:MarshalAs(UnmanagedType.U1)]
#endif
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<short> str, short s, short c);

#if DISABLE_RUNTIME_MARSHALLING
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CallCheckStructWithShortAndBoolCallback(delegate* unmanaged<StructWithShortAndBool, short, bool, bool> cb, StructWithShortAndBool str, short s, bool b);
#endif

    public static IntPtr GetStructWithShortAndBoolCallback()
    {
#if DISABLE_RUNTIME_MARSHALLING
        return GetStructWithShortAndBoolCallback(false);
#else
        return GetStructWithShortAndBoolCallback(true);
#endif
        [DllImport(nameof(DisabledRuntimeMarshallingNative))]
        static extern IntPtr GetStructWithShortAndBoolCallback([MarshalAs(UnmanagedType.U1)] bool marshalSupported);
    }

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern IntPtr GetStructWithShortAndBoolWithVariantBoolCallback();

#if DISABLE_RUNTIME_MARSHALLING
    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    public static extern bool GetByteAsBool(byte b);
#endif

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(AutoLayoutStruct s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutField s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutNestedField s);

#if DISABLE_RUNTIME_MARSHALLING
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBoolWithVariantBool(StructWithShortAndBool str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);
#else
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBoolWithVariantBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);
#endif

    // Apply the UnmanagedFunctionPointer attributes with the default calling conventions so that Mono's AOT compiler
    // recognizes that these delegate types are used in interop and should have managed->native thunks generated for them.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolCallback(StructWithShortAndBool str, short s, bool b);

#if DISABLE_RUNTIME_MARSHALLING
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolWithVariantBoolCallback(StructWithShortAndBool str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);
#else
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolWithMarshalAsAndVariantBoolCallback(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);
#endif

    [UnmanagedCallersOnly]
    public static bool CheckStructWithShortAndBoolManaged(StructWithShortAndBool str, short s, bool b)
    {
        return str.s == s && str.b == b;
    }

#if DISABLE_RUNTIME_MARSHALLING
    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid", CharSet = CharSet.Ansi)]
    public static extern void CheckStringWithAnsiCharSet(string s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid", CharSet = CharSet.Unicode)]
    public static extern void CheckStringWithUnicodeCharSet(string s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid", CharSet = CharSet.Unicode)]
    public static extern string GetStringWithUnicodeCharSet();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CheckStructWithStructWithString(StructWithString s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CheckLayoutClass(LayoutClass c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid", SetLastError = true)]
    public static extern void CallWithSetLastError();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    [LCIDConversion(0)]
    public static extern void CallWithLCID();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid", PreserveSig = false)]
    public static extern int CallWithHResultSwap();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithByRef(ref int i);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithVarargs(__arglist);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithInt128(Int128 i);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithUInt128(UInt128 i);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Nullable<int> s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Span<int> s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(ReadOnlySpan<int> ros);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Vector64<int> v);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Vector128<int> v);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Vector256<int> v);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWith(Vector<int> v);
#endif
}
