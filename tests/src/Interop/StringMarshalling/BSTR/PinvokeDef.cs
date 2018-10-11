// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

namespace NativeDefs
{
    public struct Person
    {
        public int age;
        public int _padding;
        [MarshalAs(UnmanagedType.BStr)]
        public string name;
    }

    [return: MarshalAs(UnmanagedType.BStr)]
    public delegate string Del_MarshalPointer_Out([MarshalAs(UnmanagedType.BStr)] out string s);
    
    [return: MarshalAs(UnmanagedType.BStr)]
    public delegate string Del_Marshal_InOut([MarshalAs(UnmanagedType.BStr)]string s);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public delegate string DelMarshalPointer_Out([MarshalAs(UnmanagedType.BStr)][Out] out string s);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public delegate string DelMarshal_InOut([MarshalAs(UnmanagedType.BStr)][In, Out]string s);

    public delegate bool DelMarshal_Struct_In(Person person);

    public delegate bool DelMarshalPointer_Struct_InOut(ref Person person);

    public static class PInvokeDef
    {
        public const string NativeBinaryName = "BSTRTestNative";

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string Marshal_InOut([In, Out][MarshalAs(UnmanagedType.BStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string Marshal_Out([Out][MarshalAs(UnmanagedType.BStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string MarshalPointer_InOut([MarshalAs(UnmanagedType.BStr)]ref string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string MarshalPointer_Out([MarshalAs(UnmanagedType.BStr)]out string s);

        [DllImport(NativeBinaryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool RPinvoke_DelMarshal_InOut(DelMarshal_InOut d, [MarshalAs(UnmanagedType.BStr)]string s);

        [DllImport(NativeBinaryName, CallingConvention = CallingConvention.StdCall)]
        public static extern bool RPinvoke_DelMarshalPointer_Out(DelMarshalPointer_Out d);
        
        [DllImport(NativeBinaryName)]
        public static extern bool Marshal_Struct_In(Person person);

        [DllImport(NativeBinaryName)]
        public static extern bool MarshalPointer_Struct_InOut(ref Person person);

        [DllImport(NativeBinaryName)]
        public static extern bool RPInvoke_DelMarshal_Struct_In(DelMarshal_Struct_In d);

        [DllImport(NativeBinaryName)]
        public static extern bool RPInvoke_DelMarshalStructPointer_InOut(DelMarshalPointer_Struct_InOut d);
    }
}
