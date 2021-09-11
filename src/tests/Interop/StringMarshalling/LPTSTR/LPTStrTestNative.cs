// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

class LPTStrTestNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ByValStringInStructAnsi
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string str;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ByValStringInStructSplitAnsi
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string str1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string str2;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ByValStringInStructUnicode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string str;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ByValStringInStructSplitUnicode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string str1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string str2;
    }

    [DllImport(nameof(LPTStrTestNative), CharSet = CharSet.Unicode)]
    public static extern bool Verify_NullTerminators_PastEnd(StringBuilder builder, int length);

    [DllImport(nameof(LPTStrTestNative), EntryPoint = "Verify_NullTerminators_PastEnd", CharSet = CharSet.Unicode)]
    public static extern bool Verify_NullTerminators_PastEnd_Out([Out] StringBuilder builder, int length);

    [DllImport(nameof(LPTStrTestNative))]
    public static extern bool MatchFuncNameAnsi(ByValStringInStructAnsi str);
    [DllImport(nameof(LPTStrTestNative))]
    public static extern bool MatchFuncNameUni(ByValStringInStructUnicode str);

    [DllImport(nameof(LPTStrTestNative))]
    public static extern void ReverseByValStringAnsi(ref ByValStringInStructAnsi str);
    [DllImport(nameof(LPTStrTestNative))]
    public static extern void ReverseByValStringUni(ref ByValStringInStructUnicode str);

    [DllImport(nameof(LPTStrTestNative))]
    public static extern void ReverseCopyByValStringAnsi(ByValStringInStructAnsi str, out ByValStringInStructSplitAnsi strOut);
    [DllImport(nameof(LPTStrTestNative))]
    public static extern void ReverseCopyByValStringUni(ByValStringInStructUnicode str, out ByValStringInStructSplitUnicode strOut);
}
