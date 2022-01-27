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
        [MarshalAs(UnmanagedType.U1)]
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
        [MarshalAs(UnmanagedType.U2)]
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
        [MarshalAs(UnmanagedType.U2)]
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

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBool str, short s, bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.Bool)] bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShort str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<char> str, short s, char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithShortAndGeneric<short> str, short s, short c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShortWithMarshalAs str, short s, [MarshalAs(UnmanagedType.U1)] char c);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern delegate* unmanaged<StructWithShortAndBool, short, bool, bool> GetStructWithShortAndBoolCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CallCheckStructWithShortAndBoolCallback(delegate*<StructWithShortAndBool, short, bool, bool> cb, StructWithShortAndBool str, short s, bool b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern delegate* unmanaged<StructWithShortAndBool, short, bool, bool> GetStructWithShortAndBoolWithVariantBoolCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool GetByteAsBool(byte b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(AutoLayoutStruct s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutField s);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "Invalid")]
    public static extern void CallWithAutoLayoutStruct(SequentialWithAutoLayoutNestedField s);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBoolWithVariantBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);

    // Apply the UnmanagedFunctionPointer attributes with the default calling conventions so that Mono's AOT compiler
    // recognizes that these delegate types are used in interop and should have managed->native thunks generated for them.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolCallback(StructWithShortAndBool str, short s, bool b);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool CheckStructWithShortAndBoolWithMarshalAsAndVariantBoolCallback(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);

    [UnmanagedCallersOnly]
    public static bool CheckStructWithShortAndBoolManaged(StructWithShortAndBool str, short s, bool b)
    {
        return str.s == s && str.b == b;
    }
}
