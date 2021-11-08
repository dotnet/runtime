// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "EqualDomainSid", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial int IsEqualDomainSid(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "EqualDomainSid", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int IsEqualDomainSid(
#endif
            byte[] sid1,
            byte[] sid2,
            out bool result);
    }
}
