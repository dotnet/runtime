// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum SECURITY_IMPERSONATION_LEVEL : uint
        {
            SecurityAnonymous = 0x0u,
            SecurityIdentification = 0x1u,
            SecurityImpersonation = 0x2u,
            SecurityDelegation = 0x3u,
        }
    }
}
