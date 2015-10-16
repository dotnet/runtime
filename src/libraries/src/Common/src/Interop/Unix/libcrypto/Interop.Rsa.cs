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

                int modulusSize = Crypto.RsaSize(key);

                // RSACryptoServiceProvider expects P, DP, Q, DQ, and InverseQ to all
                // be padded up to half the modulus size.
                int halfModulus = modulusSize / 2;

                rsaParameters = new RSAParameters
                {
                    Modulus = Crypto.ExtractBignum(rsaStructure->n, modulusSize),
                    Exponent = Crypto.ExtractBignum(rsaStructure->e, 0),
                };

                if (includePrivateParameters)
                {
                    rsaParameters.D = Crypto.ExtractBignum(rsaStructure->d, modulusSize);
                    rsaParameters.P = Crypto.ExtractBignum(rsaStructure->p, halfModulus);
                    rsaParameters.DP = Crypto.ExtractBignum(rsaStructure->dmp1, halfModulus);
                    rsaParameters.Q = Crypto.ExtractBignum(rsaStructure->q, halfModulus);
                    rsaParameters.DQ = Crypto.ExtractBignum(rsaStructure->dmq1, halfModulus);
                    rsaParameters.InverseQ = Crypto.ExtractBignum(rsaStructure->iqmp, halfModulus);
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
