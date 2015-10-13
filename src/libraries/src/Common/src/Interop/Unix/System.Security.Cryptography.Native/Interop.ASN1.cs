// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal const int NID_undef = 0;

        internal const int NID_X9_62_prime256v1 = 415; // NIST P-256
        internal const int NID_secp224r1 = 713; // NIST P-224
        internal const int NID_secp384r1 = 715; // NIST P-384
        internal const int NID_secp521r1 = 716; // NIST P-521

        [DllImport(Libraries.CryptoNative)]
        internal static extern long Asn1IntegerGet(IntPtr a);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        internal static extern SafeAsn1ObjectHandle ObjTxt2Obj(string s);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        private static extern int ObjObj2Txt([Out] StringBuilder buf, int buf_len, IntPtr a);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetObjectDefinitionByName(string friendlyName);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        internal static extern int ObjSn2Nid(string sn);

        // Returns shared pointers, should not be tracked as a SafeHandle.
        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr ObjNid2Obj(int nid);
        
        [DllImport(Libraries.CryptoNative)]
        internal static extern void Asn1ObjectFree(IntPtr o);

        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern SafeAsn1BitStringHandle D2IAsn1BitString(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void Asn1BitStringFree(IntPtr o);

        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern SafeAsn1OctetStringHandle D2IAsn1OctetString(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeAsn1OctetStringHandle Asn1OctetStringNew();

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Asn1OctetStringSet(SafeAsn1OctetStringHandle o, byte[] d, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void Asn1OctetStringFree(IntPtr o);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void Asn1StringFree(IntPtr o);

        internal static string GetOidValue(IntPtr asn1ObjectPtr)
        {
            // OBJ_obj2txt returns the number of bytes that should have been in the answer, but it does not accept
            // a NULL buffer.  The documentation says "A buffer length of 80 should be more than enough to handle
            // any OID encountered in practice", so start with a buffer of size 80, and try again if required.
            StringBuilder buf = new StringBuilder(80);

            int bytesNeeded = ObjObj2Txt(buf, buf.Capacity, asn1ObjectPtr);

            if (bytesNeeded < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            Debug.Assert(bytesNeeded != 0, "OBJ_obj2txt reported a zero-length response");

            if (bytesNeeded >= buf.Capacity)
            {
                int initialBytesNeeded = bytesNeeded;

                // bytesNeeded does not count the \0 which will be written on the end (based on OpenSSL 1.0.1f),
                // so make sure to leave room for it.
                buf.EnsureCapacity(bytesNeeded + 1);

                bytesNeeded = ObjObj2Txt(buf, buf.Capacity, asn1ObjectPtr);

                if (bytesNeeded < 0)
                {
                    throw CreateOpenSslCryptographicException();
                }

                Debug.Assert(
                    bytesNeeded == initialBytesNeeded,
                    "OBJ_obj2txt changed the required number of bytes for the realloc call");

                if (bytesNeeded >= buf.Capacity)
                {
                    // OBJ_obj2txt is demanding yet more memory
                    throw new CryptographicException();
                }
            }

            return buf.ToString();
        }
    }
}
