// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    [System.CLSCompliant(false)]
    public static class SecureStringMarshal
    {
        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) => IntPtr.Zero;
        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) => IntPtr.Zero;
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s) => IntPtr.Zero;
        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) => IntPtr.Zero;
    }
}
