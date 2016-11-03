// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.SecureString))]

namespace System.Security
{
    [System.CLSCompliant(false)]
    public static class SecureStringMarshal {
        public static IntPtr SecureStringToCoTaskMemAnsi(System.Security.SecureString s) { throw null; }
        public static IntPtr SecureStringToCoTaskMemUnicode(System.Security.SecureString s) { throw null; }
        public static IntPtr SecureStringToGlobalAllocAnsi(System.Security.SecureString s) { throw null; }
        public static IntPtr SecureStringToGlobalAllocUnicode(System.Security.SecureString s) { throw null; }
    }
}
