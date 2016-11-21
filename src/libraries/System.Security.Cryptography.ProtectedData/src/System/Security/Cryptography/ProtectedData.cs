// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography;

using DATA_BLOB = Interop.Crypt32.DATA_BLOB;
using CryptProtectDataFlags = Interop.Crypt32.CryptProtectDataFlags;

namespace System.Security.Cryptography
{
    public static partial class ProtectedData
    {
        public static byte[] Protect(byte[] userData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            if (userData == null)
                throw new ArgumentNullException(nameof(userData));

            return ProtectOrUnprotect(userData, optionalEntropy, scope, protect: true);
        }

        public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));

            return ProtectOrUnprotect(encryptedData, optionalEntropy, scope, protect: false);
        }

        private static byte[] ProtectOrUnprotect(byte[] inputData, byte[] optionalEntropy, DataProtectionScope scope, bool protect)
        {
            unsafe
            {
                fixed (byte* pInputData = inputData, pOptionalEntropy = optionalEntropy)
                {
                    DATA_BLOB userDataBlob = new DATA_BLOB((IntPtr)pInputData, (uint)(inputData.Length));
                    DATA_BLOB optionalEntropyBlob = default(DATA_BLOB);
                    if (optionalEntropy != null)
                    {
                        optionalEntropyBlob = new DATA_BLOB((IntPtr)pOptionalEntropy, (uint)(optionalEntropy.Length));
                    }

                    // For desktop compat, we ignore unknown bits in the "scope" value rather than throwing.
                    CryptProtectDataFlags flags = CryptProtectDataFlags.CRYPTPROTECT_UI_FORBIDDEN;
                    if (scope == DataProtectionScope.LocalMachine)
                    {
                        flags |= CryptProtectDataFlags.CRYPTPROTECT_LOCAL_MACHINE;
                    }

                    DATA_BLOB outputBlob = default(DATA_BLOB);
                    try
                    {
                        bool success = protect ?
                            Interop.Crypt32.CryptProtectData(ref userDataBlob, null, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, out outputBlob) :
                            Interop.Crypt32.CryptUnprotectData(ref userDataBlob, IntPtr.Zero, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, out outputBlob);
                        if (!success)
                        {
                            int lastWin32Error = Marshal.GetLastWin32Error();
                            if (protect && ErrorMayBeCausedByUnloadedProfile(lastWin32Error))
                                throw new CryptographicException(SR.Cryptography_DpApi_ProfileMayNotBeLoaded);
                            else
                                throw lastWin32Error.ToCryptographicException();
                        }

                        // In some cases, the API would fail due to OOM but simply return a null pointer.
                        if (outputBlob.pbData == IntPtr.Zero)
                            throw new OutOfMemoryException();

                        int length = (int)(outputBlob.cbData);
                        byte[] outputBytes = new byte[length];
                        Marshal.Copy(outputBlob.pbData, outputBytes, 0, length);
                        return outputBytes;
                    }
                    finally
                    {
                        if (outputBlob.pbData != IntPtr.Zero)
                        {
                            int length = (int)(outputBlob.cbData);
                            byte* pOutputData = (byte*)(outputBlob.pbData);
                            for (int i = 0; i < length; i++)
                            {
                                pOutputData[i] = 0;
                            }
                            Marshal.FreeHGlobal(outputBlob.pbData);
                        }
                    }
                }
            }
        }

        // Determine if an error code may have been caused by trying to do a crypto operation while the
        // current user's profile is not yet loaded.
        private static bool ErrorMayBeCausedByUnloadedProfile(int errorCode)
        {
            // CAPI returns a file not found error if the user profile is not yet loaded
            return errorCode == Interop.Errors.E_FILENOTFOUND ||
                   errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND;
        }
    }
}
