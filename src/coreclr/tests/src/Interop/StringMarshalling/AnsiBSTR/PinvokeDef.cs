// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

#pragma warning disable CS0618

namespace NativeDefs
{
    public static class AnsiBStrTestNative
    {
        public const string NativeBinaryName = nameof(AnsiBStrTestNative);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string Marshal_InOut([In, Out][MarshalAs(UnmanagedType.AnsiBStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string Marshal_Out([Out][MarshalAs(UnmanagedType.AnsiBStr)]string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string MarshalPointer_InOut([MarshalAs(UnmanagedType.AnsiBStr)]ref string s);

        [DllImport(NativeBinaryName)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string MarshalPointer_Out([MarshalAs(UnmanagedType.AnsiBStr)]out string s);
    }
}
