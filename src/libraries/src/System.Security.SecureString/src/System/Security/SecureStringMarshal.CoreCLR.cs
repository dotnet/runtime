// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public static class SecureStringMarshal
    {
        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) => s != null ? s.MarshalToString(globalAlloc: false, unicode: false) : IntPtr.Zero;

        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) => s != null ? s.MarshalToString(globalAlloc: true, unicode: false) : IntPtr.Zero;

        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s) => s != null ? s.MarshalToString(globalAlloc: false, unicode: true) : IntPtr.Zero;

        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) => s != null ? s.MarshalToString(globalAlloc: true, unicode: true) : IntPtr.Zero;
    }
}
