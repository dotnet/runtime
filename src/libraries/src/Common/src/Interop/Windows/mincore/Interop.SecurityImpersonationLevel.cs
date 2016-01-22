// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
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