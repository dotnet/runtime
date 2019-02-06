// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

namespace NativeDefs
{    
    public delegate string Del_Marshal_Out(string s);
    public delegate string Del_MarshalPointer_InOut(ref string s);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, BestFitMapping = true)]
    [return: MarshalAs(UnmanagedType.LPTStr)]
    public delegate StringBuilder Del_MarshalStrB_InOut([In, Out][MarshalAs(UnmanagedType.LPTStr)]StringBuilder s);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode, BestFitMapping = true)]
    [return: MarshalAs(UnmanagedType.LPTStr)]
    public delegate StringBuilder Del_MarshalStrB_Out([Out][MarshalAs(UnmanagedType.LPTStr)] out StringBuilder s);

    public static class PInvokeDef
    {
        public const string NativeBinaryName = "LPTSTRTestNative";

        [DllImport(NativeBinaryName)]
        public static extern bool ReverseP_MarshalStrB_Out(Del_MarshalStrB_Out d);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern string Marshal_Out([Out][MarshalAs(UnmanagedType.LPTStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern string Marshal_In([In][MarshalAs(UnmanagedType.LPTStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern string Marshal_InOut([In, Out][MarshalAs(UnmanagedType.LPTStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern string MarshalPointer_InOut([MarshalAs(UnmanagedType.LPTStr)]ref string s);
                

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern string MarshalPointer_Out([MarshalAs(UnmanagedType.LPTStr)]out string s);

        [DllImport(NativeBinaryName, EntryPoint = "Marshal_InOut")]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern StringBuilder MarshalStrB_InOut([In, Out][MarshalAs(UnmanagedType.LPTStr)]StringBuilder s);

        [DllImport(NativeBinaryName, EntryPoint = "MarshalPointer_Out")]
        [return: MarshalAs(UnmanagedType.LPTStr)]
        public static extern StringBuilder MarshalStrB_Out([MarshalAs(UnmanagedType.LPTStr)]out StringBuilder s);

        [DllImport(NativeBinaryName, EntryPoint = "Marshal_InOut")]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern StringBuilder MarshalStrWB_InOut([In, Out][MarshalAs(UnmanagedType.LPWStr)]StringBuilder s);

        [DllImport(NativeBinaryName, EntryPoint = "MarshalPointer_Out")]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern StringBuilder MarshalStrWB_Out([MarshalAs(UnmanagedType.LPWStr)]out StringBuilder s);

        [DllImport(NativeBinaryName)]
        public static extern bool ReverseP_MarshalStrB_InOut(Del_MarshalStrB_InOut d, [MarshalAs(UnmanagedType.LPTStr)]string s);

        [DllImport(NativeBinaryName, CharSet = CharSet.Unicode)]
        public static extern bool Verify_NullTerminators_PastEnd(StringBuilder builder, int length);

        [DllImport(NativeBinaryName, EntryPoint = "Verify_NullTerminators_PastEnd", CharSet = CharSet.Unicode)]
        public static extern bool Verify_NullTerminators_PastEnd_Out([Out] StringBuilder builder, int length);
    }
}
