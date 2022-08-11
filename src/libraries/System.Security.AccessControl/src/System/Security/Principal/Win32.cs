// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Versioning;

namespace System.Security.Principal
{
    internal static class Win32
    {
        internal static int OpenThreadToken(TokenAccessLevels dwDesiredAccess, WinSecurityContext dwOpenAs, out SafeTokenHandle? phThreadToken)
        {
            int hr = 0;
            bool openAsSelf = true;
            if (dwOpenAs == WinSecurityContext.Thread)
                openAsSelf = false;

            if (!Interop.Advapi32.OpenThreadToken((IntPtr)(-2), dwDesiredAccess, openAsSelf, out phThreadToken))
            {
                if (dwOpenAs == WinSecurityContext.Both)
                {
                    openAsSelf = false;
                    hr = 0;
                    phThreadToken.Dispose();
                    if (!Interop.Advapi32.OpenThreadToken((IntPtr)(-2), dwDesiredAccess, openAsSelf, out phThreadToken))
                        hr = Marshal.GetHRForLastWin32Error();
                }
                else
                {
                    hr = Marshal.GetHRForLastWin32Error();
                }
            }
            if (hr != 0)
            {
                phThreadToken.Dispose();
                phThreadToken = null;
            }

            return hr;
        }

        internal static int SetThreadToken(SafeTokenHandle? hToken)
        {
            int hr = 0;
            if (!Interop.Advapi32.SetThreadToken(IntPtr.Zero, hToken))
            {
                hr = Marshal.GetHRForLastWin32Error();
            }
            return hr;
        }
    }

    // The following two enums ported from WindowsIdentity.cs since it is needed for the impersonation APIs here.

    // Keep in sync with vm\comprincipal.h
    internal enum WinSecurityContext
    {
        Thread = 1, // OpenAsSelf = false
        Process = 2, // OpenAsSelf = true
        Both = 3 // OpenAsSelf = true, then OpenAsSelf = false
    }

    internal enum TokenType : int
    {
        TokenPrimary = 1,
        TokenImpersonation
    }
}
