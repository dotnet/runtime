// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using Internal.Cryptography.Pal;

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
        public static SafeHandle ToLpstrArray(this OidCollection oids, out int numOids)
        {
            if (oids == null || oids.Count == 0)
            {
                numOids = 0;
                return SafeLocalAllocHandle.InvalidHandle;
            }

            // Copy the oid strings to a local list to prevent a security race condition where
            // the OidCollection or individual oids can be modified by another thread and
            // potentially cause a buffer overflow
            List<byte[]> oidStrings = new List<byte[]>();
            foreach (Oid oid in oids)
            {
                byte[] oidString = oid.ValueAsAscii();
                oidStrings.Add(oidString);
            }

            numOids = oidStrings.Count;
            unsafe
            {
                int allocationSize = checked(numOids * sizeof(void*));
                foreach (byte[] oidString in oidStrings)
                {
                    checked
                    {
                        allocationSize += oidString.Length + 1;
                    }
                }

                SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.Create(allocationSize);
                byte** pOidPointers = (byte**)(safeLocalAllocHandle.DangerousGetHandle());
                byte* pOidContents = (byte*)(pOidPointers + numOids);

                for (int i = 0; i < numOids; i++)
                {
                    pOidPointers[i] = pOidContents;
                    byte[] oidString = oidStrings[i];
                    Marshal.Copy(oidString, 0, new IntPtr(pOidContents), oidString.Length);
                    pOidContents[oidString.Length] = 0;
                    pOidContents += oidString.Length + 1;
                }

                return safeLocalAllocHandle;
            }
        }

        public static byte[] ValueAsAscii(this Oid oid)
        {
            return Encoding.ASCII.GetBytes(oid.Value);
        }

        public unsafe delegate void DecodedObjectReceiver(void* pvDecodedObject);

        public static void DecodeObject(this byte[] encoded, CryptDecodeObjectStructType lpszStructType, DecodedObjectReceiver receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref cb))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                byte* decoded = stackalloc byte[cb];
                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, (byte*)decoded, ref cb))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                receiver(decoded);
            }
        }

        public static void DecodeObject(this byte[] encoded, string lpszStructType, DecodedObjectReceiver receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref cb))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                byte* decoded = stackalloc byte[cb];
                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, (byte*)decoded, ref cb))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                receiver(decoded);
            }
        }

        public static bool DecodeObjectNoThrow(this byte[] encoded, CryptDecodeObjectStructType lpszStructType, DecodedObjectReceiver receiver)
        {
            unsafe
            {
                int cb = 0;

                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref cb))
                    return false;

                byte* decoded = stackalloc byte[cb];
                if (!Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, (byte*)decoded, ref cb))
                    return false;

                receiver(decoded);
            }
            return true;
        }
    }
}


