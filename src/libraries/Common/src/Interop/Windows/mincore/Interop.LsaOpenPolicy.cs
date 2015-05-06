// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.SecurityLsaPolicy, EntryPoint = "LsaOpenPolicy", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint LsaOpenPolicy(string systemName, ref LSA_OBJECT_ATTRIBUTES attributes, int accessMask, out SafeLsaPolicyHandle handle);
    }
}
