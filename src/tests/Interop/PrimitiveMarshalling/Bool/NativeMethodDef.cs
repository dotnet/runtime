// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

public class BoolNative
{
    [DllImport(nameof(BoolNative))]
    public static extern bool Marshal_In([In]bool boolValue);

    [DllImport(nameof(BoolNative))]
    public static extern bool Marshal_InOut([In, Out]bool boolValue);

    [DllImport(nameof(BoolNative))]
    public static extern bool Marshal_Out([Out]bool boolValue);

    [DllImport(nameof(BoolNative))]
    public static extern bool MarshalPointer_In([In]ref bool pboolValue);

    [DllImport(nameof(BoolNative))]
    public static extern bool MarshalPointer_InOut(ref bool pboolValue);

    [DllImport(nameof(BoolNative))]
    public static extern bool MarshalPointer_Out(out bool pboolValue);

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_In(
      [In, MarshalAs(UnmanagedType.U1)]bool boolValue);

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_InOut(
      [In, Out, MarshalAs(UnmanagedType.U1)]bool boolValue);

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_Out(
      [Out, MarshalAs(UnmanagedType.U1)]bool boolValue);

#pragma warning disable CS0612, CS0618
    public struct ContainsVariantBool
    {
        [MarshalAs(UnmanagedType.VariantBool)]
        public bool value;
    }

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_ByValue_Variant(
        [MarshalAs(UnmanagedType.VariantBool)] bool value,
        [MarshalAs(UnmanagedType.U1)] bool expected);

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_Ref_Variant(
        [MarshalAs(UnmanagedType.VariantBool)] ref bool value);
    
    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_ByValue_Struct_Variant(
        ContainsVariantBool value,
        [MarshalAs(UnmanagedType.U1)] bool expected);

    [DllImport(nameof(BoolNative))]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_Ref_Struct_Variant(ref ContainsVariantBool value);

#pragma warning restore CS0612, CS0618

}
