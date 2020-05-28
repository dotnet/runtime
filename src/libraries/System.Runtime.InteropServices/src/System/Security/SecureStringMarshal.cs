// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security
{
    public static class SecureStringMarshal
    {
        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) => Marshal.SecureStringToCoTaskMemAnsi(s);
        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) => Marshal.SecureStringToGlobalAllocAnsi(s);
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s) => Marshal.SecureStringToCoTaskMemUnicode(s);
        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) => Marshal.SecureStringToGlobalAllocUnicode(s);
    }
}
