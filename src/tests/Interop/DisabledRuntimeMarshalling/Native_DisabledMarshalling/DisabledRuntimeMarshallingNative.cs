// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
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
        [MarshalAs(UnmanagedType.VariantBool)]
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
        [MarshalAs(UnmanagedType.U1)]
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

    public enum ByteEnum : byte
    {
        Value = 42
    }

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBool str, short s, bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.I4)] bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShort str, short s, char c);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShortWithMarshalAs str, short s, char c);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<char> str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<short> str, short s, short c);

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
    public static extern void CallWithAutoLayoutStruct(AutoLayoutStruct s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutField s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutNestedField s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithByRef(ref int i);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithVarargs(__arglist);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern delegate* unmanaged<StructWithShortAndBool, short, bool, bool> GetStructWithShortAndBoolCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern delegate* unmanaged<StructWithShortAndBool, short, bool, bool> GetStructWithShortAndBoolWithVariantBoolCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CallCheckStructWithShortAndBoolCallback(delegate* unmanaged<StructWithShortAndBool, short, bool, bool> cb, StructWithShortAndBool str, short s, bool b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    public static extern bool GetByteAsBool(byte b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    public static extern byte GetEnumUnderlyingValue(ByteEnum b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "CheckStructWithShortAndBoolWithVariantBool")]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBoolWithVariantBool_FailureExpected(StructWithShortAndBool str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);

    // Apply the UnmanagedFunctionPointer attributes with the default calling conventions so that Mono's AOT compiler
    // recognizes that these delegate types are used in interop and should have managed->native thunks generated for them.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolCallback(StructWithShortAndBool str, short s, bool b);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolWithVariantBoolCallback(StructWithShortAndBool str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);

    [UnmanagedCallersOnly]
    public static bool CheckStructWithShortAndBoolManaged(StructWithShortAndBool str, short s, bool b)
    {
        return str.s == s && str.b == b;
    }
}
