// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;

class VBByRefStrNative
{
    
    [DllImport(nameof(VBByRefStrNative), CharSet = CharSet.Ansi)]
    public static extern bool Marshal_Ansi(string expected, [MarshalAs(UnmanagedType.VBByRefStr)] ref string actual, string newValue);
    [DllImport(nameof(VBByRefStrNative), CharSet = CharSet.Unicode)]
    public static extern bool Marshal_Unicode(string expected, [MarshalAs(UnmanagedType.VBByRefStr)] ref string actual, string newValue);

    [DllImport(nameof(VBByRefStrNative), EntryPoint = "Marshal_Invalid")]
    public static extern bool Marshal_StringBuilder([MarshalAs(UnmanagedType.VBByRefStr)]ref  StringBuilder builder);

    [DllImport(nameof(VBByRefStrNative), EntryPoint = "Marshal_Invalid")]
    public static extern bool Marshal_ByVal([MarshalAs(UnmanagedType.VBByRefStr)]string str);
}
