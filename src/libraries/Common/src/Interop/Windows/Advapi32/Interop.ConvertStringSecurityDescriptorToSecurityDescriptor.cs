// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
                string StringSecurityDescriptor,
                int StringSDRevision,
                out SafeLocalAllocHandle pSecurityDescriptor,
                IntPtr SecurityDescriptorSize);
    }
}
