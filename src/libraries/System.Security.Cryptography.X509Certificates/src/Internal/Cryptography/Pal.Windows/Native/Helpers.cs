// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography.Pal.Native
{
    internal static class Helpers
    {
        /// <summary>
        /// Convert each Oid's value to an ASCII string, then create an unmanaged array of "numOids" LPSTR pointers, one for each Oid.
        /// "numOids" is the number of LPSTR pointers. This is normally the same as oids.Count, except in the case where a malicious caller
        /// appends to the OidCollection while this method is in progress. In such a case, this method guarantees only that this won't create
        /// an unmanaged buffer overflow condition.
        /// </summary>
        public static SafeHandle ToLpstrArray(this OidCollection? oids, out int numOids)
        {
            if (oids == null || oids.Count == 0)
            {
                numOids = 0;
                return SafeLocalAllocHandle.InvalidHandle;
            }

            // Copy the oid strings to a local array to prevent a security race condition where
            // the OidCollection or individual oids can be modified by another thread and
            // potentially cause a buffer overflow
            var oidStrings = new string[oids.Count];
            for (int i = 0; i < oidStrings.Length; i++)
            {
                oidStrings[i] = oids[i].Value!;
            }

            unsafe
            {
                int allocationSize = checked(oidStrings.Length * sizeof(void*));
                foreach (string oidString in oidStrings)
                {
                    checked
                    {
                        allocationSize += oidString.Length + 1; // Encoding.ASCII doesn't have a fallback, so it's fine to use String.Length
                    }
                }

                SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.Create(allocationSize);
                byte** pOidPointers = (byte**)(safeLocalAllocHandle.DangerousGetHandle());
                byte* pOidContents = (byte*)(pOidPointers + oidStrings.Length);

                for (int i = 0; i < oidStrings.Length; i++)
                {
                    string oidString = oidStrings[i];

                    pOidPointers[i] = pOidContents;

                    int bytesWritten = Encoding.ASCII.GetBytes(oidString, new Span<byte>(pOidContents, oidString.Length));
                    Debug.Assert(bytesWritten == oidString.Length);

                    pOidContents[oidString.Length] = 0;
                    pOidContents += oidString.Length + 1;
                }

                numOids = oidStrings.Length;
                return safeLocalAllocHandle;
            }
        }

        public static byte[] ValueAsAscii(this Oid oid)
        {
            return Encoding.ASCII.GetBytes(oid.Value!);
        }

        public unsafe delegate void DecodedObjectReceiver(void* pvDecodedObject, int cbDecodedObject);
        public unsafe delegate TResult DecodedObjectReceiver<TResult>(void* pvDecodedObject, int cbDecodedObject);

        public static TResult DecodeObject<TResult>(
            this byte[] encoded,
            CryptDecodeObjectStructType lpszStructType,
            DecodedObjectReceiver<TResult> receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    null,
                    ref cb))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                byte* decoded = stackalloc byte[cb];

                if (!Interop.crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    decoded,
                    ref cb))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                return receiver(decoded, cb);
            }
        }

        public static TResult DecodeObject<TResult>(
            this byte[] encoded,
            string lpszStructType,
            DecodedObjectReceiver<TResult> receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.Crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    null,
                    ref cb))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                byte* decoded = stackalloc byte[cb];

                if (!Interop.Crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    decoded,
                    ref cb))
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                return receiver(decoded, cb);
            }
        }

        public static bool DecodeObjectNoThrow(
            this byte[] encoded,
            CryptDecodeObjectStructType lpszStructType,
            DecodedObjectReceiver receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    null,
                    ref cb))
                {
                    return false;
                }

                byte* decoded = stackalloc byte[cb];

                if (!Interop.crypt32.CryptDecodeObjectPointer(
                    Interop.Crypt32.CertEncodingType.All,
                    lpszStructType,
                    encoded,
                    encoded.Length,
                    Interop.Crypt32.CryptDecodeObjectFlags.None,
                    decoded,
                    ref cb))
                {
                    return false;
                }

                receiver(decoded, cb);
            }

            return true;
        }
    }
}
