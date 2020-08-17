// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport("Interop.Advapi32", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaQueryInformationPolicy", CharSet = CharSet.Unicode)]
        public static extern int LsaQueryInformationPolicy(
                                        IntPtr policyHandle,
                                        int policyInformationClass,
                                        ref IntPtr buffer
                                        );
    }
}
