// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Internal.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    //
    // Convenience wrappers for CryptEncodeObject() and CryptDecodeObject(). It's debatable whether they belong in Interop.Crypt32 but
    // there's no natural way to express these as extension methods and they'd be undiscoverable if put in any other class.
    //
    internal static partial class Crypt32
    {
        internal static SafeHandle CryptDecodeObjectToMemory(CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded)
        {
            unsafe
            {
                fixed (byte* pbEncodedPointer = pbEncoded)
                {
                    return CryptDecodeObjectToMemory(lpszStructType, (IntPtr)pbEncodedPointer, pbEncoded.Length);
                }
            }
        }

        internal static SafeHandle CryptDecodeObjectToMemory(CryptDecodeObjectStructType lpszStructType, IntPtr pbEncoded, int cbEncoded)
        {
            int cbRequired = 0;
            unsafe
            {
                if (!CryptDecodeObject(MsgEncodingType.All, (IntPtr)lpszStructType, pbEncoded, cbEncoded, 0, null, ref cbRequired))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                SafeHandle sh = SafeHeapAllocHandle.Alloc(cbRequired);
                if (!CryptDecodeObject(MsgEncodingType.All, (IntPtr)lpszStructType, pbEncoded, cbEncoded, 0, (void*)sh.DangerousGetHandle(), ref cbRequired))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                return sh;
            }
        }

        internal static unsafe byte[] CryptEncodeObjectToByteArray(CryptDecodeObjectStructType lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!CryptEncodeObject(MsgEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!CryptEncodeObject(MsgEncodingType.All, lpszStructType, decoded, encoded, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encoded.Resize(cb);
        }
    }
}

