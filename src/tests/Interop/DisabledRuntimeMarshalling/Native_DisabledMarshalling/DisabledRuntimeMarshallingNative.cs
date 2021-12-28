// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class DisabledRuntimeMarshallingNative
{
    public struct StructWithShortAndBool
    {
        short s;
        bool b;

        public StructWithShortAndBool(short s, bool b)
        {
            this.s = s;
            this.b = b;
        }
    }

    public struct StructWithShortAndBoolWithMarshalAs
    {
        short s;
        [MarshalAs(UnmanagedType.VariantBool)]
        bool b;

        public StructWithShortAndBoolWithMarshalAs(short s, bool b)
        {
            this.s = s;
            this.b = b;
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

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBool str, short s, bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithShortAndBool(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.I4)] bool b);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShort str, short s, char c);
    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern bool CheckStructWithWCharAndShort(StructWithWCharAndShortWithMarshalAs str, short s, [MarshalAs(UnmanagedType.U1)] char c);
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

    [DllImport(nameof(DisabledRuntimeMarshallingNative))]
    public static extern delegate*<StructWithShortAndBool, short, bool, bool> GetStructWithShortAndBoolCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "GetStructWithShortAndBoolCallback")]
    public static extern delegate*<StructWithShortAndBoolWithMarshalAs, short, bool, bool> GetStructWithShortAndBoolWithMarshalAsCallback();

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "PassThrough")]
    public static extern bool GetByteAsBool(byte b);

    [DllImport(nameof(DisabledRuntimeMarshallingNative), EntryPoint = "CheckStructWithShortAndBoolWithVariantBool")]
    [return:MarshalAs(UnmanagedType.U1)]
    public static extern bool CheckStructWithShortAndBoolWithVariantBool_FailureExpected(StructWithShortAndBool str, short s, [MarshalAs(UnmanagedType.VariantBool)] bool b);

    public delegate bool CheckStructWithShortAndBoolCallback(StructWithShortAndBool str, short s, bool b);
    public delegate bool CheckStructWithShortAndBoolWithMarshalAsCallback(StructWithShortAndBoolWithMarshalAs str, short s, [MarshalAs(UnmanagedType.I4)] bool b);
}
