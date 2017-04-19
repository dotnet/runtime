// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

namespace NativeDefs
{
    
    public delegate string Del_MarshalPointer_Out(out string s);
    public delegate string Del_Marshal_InOut(string s);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public delegate string DelMarshalPointer_Out([MarshalAs(UnmanagedType.LPStr)][Out] out string s);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.LPTStr)]
    public delegate string DelMarshal_InOut([MarshalAs(UnmanagedType.LPTStr)][In, Out]string s);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, BestFitMapping = true)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public delegate StringBuilder Del_MarshalStrB_InOut([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode, BestFitMapping = true)]
    [return: MarshalAs(UnmanagedType.LPTStr)]
    public delegate StringBuilder Del_MarshalStrB_Out([Out][MarshalAs(UnmanagedType.LPTStr)] out StringBuilder s);

    public static class PInvokeDef
    {
        public const string NativeBinaryName = "LPSTRTestNative";

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string Marshal_InOut([In, Out][MarshalAs(UnmanagedType.LPStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string Marshal_Out([Out][MarshalAs(UnmanagedType.LPStr)]string s);

        [DllImport(NativeBinaryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Writeline(string format, int i, char c, double d, short s, uint u);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string MarshalPointer_InOut([MarshalAs(UnmanagedType.LPStr)]ref string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string MarshalPointer_Out([MarshalAs(UnmanagedType.LPStr)]out string s);

        [DllImport(NativeBinaryName, EntryPoint = "Marshal_InOut")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern StringBuilder MarshalStrB_InOut([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

        [DllImport(NativeBinaryName, EntryPoint = "MarshalPointer_Out")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern StringBuilder MarshalStrB_Out([MarshalAs(UnmanagedType.LPStr)]out StringBuilder s);

        [DllImport(NativeBinaryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool RPinvoke_DelMarshal_InOut(DelMarshal_InOut d, [MarshalAs(UnmanagedType.LPTStr)]string s);

        [DllImport(NativeBinaryName, CallingConvention = CallingConvention.StdCall)]
        public static extern bool RPinvoke_DelMarshalPointer_Out(DelMarshalPointer_Out d);

        [DllImport(NativeBinaryName)]
        public static extern bool ReverseP_MarshalStrB_InOut(Del_MarshalStrB_InOut d, [MarshalAs(UnmanagedType.LPStr)]string s);
        [DllImport(NativeBinaryName)]
        public static extern bool ReverseP_MarshalStrB_Out(Del_MarshalStrB_Out d);
    }
}
