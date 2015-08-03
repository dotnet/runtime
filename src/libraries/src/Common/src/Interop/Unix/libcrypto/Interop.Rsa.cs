// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

// On Linux and MacOS sizeof(long)==sizeof(void*) across x86/x64
using NativeLong=System.IntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeRsaHandle RSA_new();

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeRsaHandle d2i_RSAPublicKey(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RSA_up_ref(IntPtr rsa);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void RSA_free(IntPtr rsa);

        [DllImport(Libraries.LibCrypto)]
        internal extern static int RSA_public_encrypt(int flen, byte[] from, byte[] to, SafeRsaHandle rsa, OpenSslRsaPadding padding);

        [DllImport(Libraries.LibCrypto)]
        internal extern static int RSA_private_decrypt(int flen, byte[] from, byte[] to, SafeRsaHandle rsa, OpenSslRsaPadding padding);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int RSA_size(SafeRsaHandle rsa);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int RSA_generate_key_ex(SafeRsaHandle rsa, int bits, SafeBignumHandle e, IntPtr zero);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RSA_sign(int type, byte[] m, int m_len, byte[] sigret, out int siglen, SafeRsaHandle rsa);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RSA_verify(int type, byte[] m, int m_len, byte[] sigbuf, int siglen, SafeRsaHandle rsa);

        internal static unsafe RSAParameters ExportRsaParameters(SafeRsaHandle key, bool includePrivateParameters)
        {
            Debug.Assert(
                key != null && !key.IsInvalid,
                "Callers should check the key is invalid and throw an exception with a message");

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException();
            }

            RSAParameters rsaParameters;
            bool addedRef = false;

            try
            {
                key.DangerousAddRef(ref addedRef);
                RSA_ST* rsaStructure = (RSA_ST*)key.DangerousGetHandle();

                int modulusSize = RSA_size(key);

                // RSACryptoServiceProvider expects P, DP, Q, DQ, and InverseQ to all
                // be padded up to half the modulus size.
                int halfModulus = modulusSize / 2;

                rsaParameters = new RSAParameters
                {
                    Modulus = ExtractBignum(rsaStructure->n, modulusSize),
                    Exponent = ExtractBignum(rsaStructure->e, 0),
                };

                if (includePrivateParameters)
                {
                    rsaParameters.D = ExtractBignum(rsaStructure->d, modulusSize);
                    rsaParameters.P = ExtractBignum(rsaStructure->p, halfModulus);
                    rsaParameters.DP = ExtractBignum(rsaStructure->dmp1, halfModulus);
                    rsaParameters.Q = ExtractBignum(rsaStructure->q, halfModulus);
                    rsaParameters.DQ = ExtractBignum(rsaStructure->dmq1, halfModulus);
                    rsaParameters.InverseQ = ExtractBignum(rsaStructure->iqmp, halfModulus);
                }
            }
            finally
            {
                if (addedRef)
                {
                    key.DangerousRelease();
                }
            }

            return rsaParameters;
        }

        internal enum OpenSslRsaPadding
        {
            Invalid,
            RSA_PKCS1_PADDING = 1,
            RSA_SSLV23_PADDING = 2,
            RSA_NO_PADDING = 3,
            RSA_PKCS1_OAEP_PADDING = 4,
            RSA_X931_PADDING = 5,
            RSA_PKCS1_PSS_PADDING = 6,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RSA_ST
        {
            private int pad;
            private NativeLong version;
            private IntPtr meth;
            private IntPtr engine;
            public IntPtr n;
            public IntPtr e;
            public IntPtr d;
            public IntPtr p;
            public IntPtr q;
            public IntPtr dmp1;
            public IntPtr dmq1;
            public IntPtr iqmp;
        }
    }
}
