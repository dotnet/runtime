// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public delegate int Dele();

#region Part 1 : Marshaled As FunctionPtr
[StructLayout(LayoutKind.Sequential)]
public struct Struct1_FuncPtrAsField1_Seq
{
    public bool verification;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Dele dele;
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct1_FuncPtrAsField2_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Dele dele;
}

[StructLayout(LayoutKind.Sequential)]
public class Class1_FuncPtrAsField3_Seq
{
    public bool verification;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Dele dele;
}

[StructLayout(LayoutKind.Explicit)]
public class Class1_FuncPtrAsField4_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Dele dele;
}
#endregion

#region Part 2 : Marshaled As Default
[StructLayout(LayoutKind.Sequential)]
public struct Struct2_FuncPtrAsField1_Seq
{
    public bool verification;

    public Dele dele;
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct2_FuncPtrAsField2_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    public Dele dele;
}

[StructLayout(LayoutKind.Sequential)]
public class Class2_FuncPtrAsField3_Seq
{
    public bool verification;

    public Dele dele;
}

[StructLayout(LayoutKind.Explicit)]
public class Class2_FuncPtrAsField4_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    public Dele dele;
}
#endregion

#region Part 3 : Marshaled As Interface
public delegate void Dele2();

[StructLayout(LayoutKind.Sequential)]
public struct Struct3_InterfacePtrAsField1_Seq
{
    public bool verification;

    [MarshalAs(UnmanagedType.Interface)]
    public Dele2 dele;
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct3_InterfacePtrAsField2_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.Interface)]
    public Dele2 dele;
}

[StructLayout(LayoutKind.Sequential)]
public class Class3_InterfacePtrAsField3_Seq
{
    public bool verification;

    [MarshalAs(UnmanagedType.Interface)]
    public Dele2 dele;
}

[StructLayout(LayoutKind.Explicit)]
public class Class3_InterfacePtrAsField4_Exp
{
    [FieldOffset(0)]
    public bool verification;

    [FieldOffset(4)]
    public Int32 Padding;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.Interface)]
    public Dele2 dele;
}
#endregion