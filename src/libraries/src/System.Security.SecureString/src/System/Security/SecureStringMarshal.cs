// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Security
{
    public static class SecureStringMarshal
    {
        [System.Security.SecurityCritical]
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            return s.ToUniStr();
        }

        [System.Security.SecurityCritical]
        public static void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            Marshal.ZeroFreeCoTaskMemUnicode(s);
        }
    }
}


