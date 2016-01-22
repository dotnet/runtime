// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal static partial class Interop
{
    internal static partial class mincore
    {            
        [DllImport(Interop.Libraries.SecurityBase, SetLastError = true)]
         internal static extern
        bool DuplicateTokenEx(SafeTokenHandle ExistingTokenHandle, 
            TokenAccessLevels DesiredAccess,
            IntPtr TokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            System.Security.Principal.TokenType TokenType,
            ref SafeTokenHandle DuplicateTokenHandle);       
    }
}
