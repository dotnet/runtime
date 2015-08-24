// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

// ASN1_INTEGER_get returns a long.
// On Linux/MacOS x64 that means sizeof(void*)
using NativeLong = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        internal const int NID_undef = 0;

        internal const int NID_secp224r1 = 713; // NIST-224
        internal const int NID_secp384r1 = 715; // NIST-384
        internal const int NID_secp521r1 = 716; // NIST-521

        [DllImport(Libraries.LibCrypto)]
        internal static extern NativeLong ASN1_INTEGER_get(IntPtr a);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern SafeAsn1ObjectHandle OBJ_txt2obj(string s, [MarshalAs(UnmanagedType.Bool)] bool no_name);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        private static extern int OBJ_obj2txt(StringBuilder buf, int buf_len, IntPtr a, [MarshalAs(UnmanagedType.Bool)] bool no_name);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern int OBJ_txt2nid(string s);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern int OBJ_ln2nid(string ln);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern int OBJ_sn2nid(string sn);

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        internal static extern string OBJ_nid2ln(int n);

        // Returns shared pointers, should not be tracked as a SafeHandle.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr OBJ_nid2obj(int nid);
        
        [DllImport(Libraries.LibCrypto)]
        internal static extern void ASN1_OBJECT_free(IntPtr o);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeAsn1BitStringHandle d2i_ASN1_BIT_STRING(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void ASN1_BIT_STRING_free(IntPtr o);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeAsn1OctetStringHandle d2i_ASN1_OCTET_STRING(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeAsn1OctetStringHandle ASN1_OCTET_STRING_new();

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ASN1_OCTET_STRING_set(SafeAsn1OctetStringHandle o, byte[] d, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void ASN1_OCTET_STRING_free(IntPtr o);

        internal static string OBJ_obj2txt_helper(IntPtr asn1ObjectPtr)
        {
            // OBJ_obj2txt returns the number of bytes that should have been in the answer, but it does not accept
            // a NULL buffer.  The documentation says "A buffer length of 80 should be more than enough to handle
            // any OID encountered in practice", so start with a buffer of size 80, and try again if required.
            // Therefore, 512 should be quite sufficient.
            StringBuilder buf = new StringBuilder(80);

            int bytesNeeded = OBJ_obj2txt(buf, buf.Capacity, asn1ObjectPtr, true);

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

                bytesNeeded = OBJ_obj2txt(buf, buf.Capacity, asn1ObjectPtr, true);

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
