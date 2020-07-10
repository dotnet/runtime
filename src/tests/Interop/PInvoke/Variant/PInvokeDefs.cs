// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable CS0612, CS0618
class VariantNative
{
    public struct CustomStruct
    {
    }

    public struct ObjectWrapper
    {
        [MarshalAs(UnmanagedType.Struct)]
        public object value;
    }

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Byte(object obj, byte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_SByte(object obj, sbyte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Int16(object obj, short expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_UInt16(object obj, ushort expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Int32(object obj, int expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_UInt32(object obj, uint expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Int64(object obj, long expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_UInt64(object obj, ulong expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Single(object obj, float expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Double(object obj, double expected);
    [DllImport(nameof(VariantNative), CharSet = CharSet.Unicode)]
    public static extern bool Marshal_ByValue_String(object obj, [MarshalAs(UnmanagedType.BStr)] string expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Char(object obj, char expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Boolean(object obj, [MarshalAs(UnmanagedType.VariantBool)] bool expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_DateTime(object obj, DateTime expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Decimal(object obj, decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Currency(object obj, [MarshalAs(UnmanagedType.Currency)] decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Missing(object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Object(object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Object_IUnknown(object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Empty(object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Null(object obj);

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByValue_Invalid(object obj);

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Byte(ref object obj, byte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_SByte(ref object obj, sbyte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Int16(ref object obj, short expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_UInt16(ref object obj, ushort expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Int32(ref object obj, int expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_UInt32(ref object obj, uint expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Int64(ref object obj, long expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_UInt64(ref object obj, ulong expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Single(ref object obj, float expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Double(ref object obj, double expected);
    [DllImport(nameof(VariantNative), CharSet = CharSet.Unicode)]
    public static extern bool Marshal_ByRef_String(ref object obj, [MarshalAs(UnmanagedType.BStr)] string expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Char(ref object obj, char expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Boolean(ref object obj, [MarshalAs(UnmanagedType.VariantBool)] bool expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_DateTime(ref object obj, DateTime expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Decimal(ref object obj, decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Currency(ref object obj, [MarshalAs(UnmanagedType.Currency)] decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Missing(ref object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Object(ref object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Object_IUnknown(ref object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Empty(ref object obj);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ByRef_Null(ref object obj);

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_ChangeVariantType(ref object obj, int expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Out(out object obj, int expected);

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Byte(ObjectWrapper wrapper, byte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_SByte(ObjectWrapper wrapper, sbyte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Int16(ObjectWrapper wrapper, short expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_UInt16(ObjectWrapper wrapper, ushort expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Int32(ObjectWrapper wrapper, int expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_UInt32(ObjectWrapper wrapper, uint expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Int64(ObjectWrapper wrapper, long expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_UInt64(ObjectWrapper wrapper, ulong expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Single(ObjectWrapper wrapper, float expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Double(ObjectWrapper wrapper, double expected);
    [DllImport(nameof(VariantNative), CharSet = CharSet.Unicode)]
    public static extern bool Marshal_Struct_ByValue_String(ObjectWrapper wrapper, [MarshalAs(UnmanagedType.BStr)] string expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Char(ObjectWrapper wrapper, char expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Boolean(ObjectWrapper wrapper, [MarshalAs(UnmanagedType.VariantBool)] bool expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_DateTime(ObjectWrapper wrapper, DateTime expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Decimal(ObjectWrapper wrapper, decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Currency(ObjectWrapper wrapper, [MarshalAs(UnmanagedType.Currency)] decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Missing(ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Object(ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Object_IUnknown(ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Empty(ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByValue_Null(ObjectWrapper wrapper);

    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Byte(ref ObjectWrapper wrapper, byte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_SByte(ref ObjectWrapper wrapper, sbyte expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Int16(ref ObjectWrapper wrapper, short expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_UInt16(ref ObjectWrapper wrapper, ushort expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Int32(ref ObjectWrapper wrapper, int expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_UInt32(ref ObjectWrapper wrapper, uint expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Int64(ref ObjectWrapper wrapper, long expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_UInt64(ref ObjectWrapper wrapper, ulong expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Single(ref ObjectWrapper wrapper, float expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Double(ref ObjectWrapper wrapper, double expected);
    [DllImport(nameof(VariantNative), CharSet = CharSet.Unicode)]
    public static extern bool Marshal_Struct_ByRef_String(ref ObjectWrapper wrapper, [MarshalAs(UnmanagedType.BStr)] string expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Char(ref ObjectWrapper wrapper, char expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Boolean(ref ObjectWrapper wrapper, [MarshalAs(UnmanagedType.VariantBool)] bool expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_DateTime(ref ObjectWrapper wrapper, DateTime expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Decimal(ref ObjectWrapper wrapper, decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Currency(ref ObjectWrapper wrapper, [MarshalAs(UnmanagedType.Currency)] decimal expected);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Missing(ref ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Object(ref ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Object_IUnknown(ref ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Empty(ref ObjectWrapper wrapper);
    [DllImport(nameof(VariantNative))]
    public static extern bool Marshal_Struct_ByRef_Null(ref ObjectWrapper wrapper);
}
