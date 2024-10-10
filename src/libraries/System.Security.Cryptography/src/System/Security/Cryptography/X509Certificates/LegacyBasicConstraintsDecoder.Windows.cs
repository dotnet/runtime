// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class LegacyBasicConstraintsDecoder
    {
        internal static bool IsSupported => true;

        internal static void DecodeX509BasicConstraintsExtension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            unsafe
            {
                (certificateAuthority, hasPathLengthConstraint, pathLengthConstraint) = encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS,
                    static delegate (void* pvDecoded, int cbDecoded)
                    {
                        Debug.Assert(cbDecoded >= sizeof(CERT_BASIC_CONSTRAINTS_INFO));
                        CERT_BASIC_CONSTRAINTS_INFO* pBasicConstraints = (CERT_BASIC_CONSTRAINTS_INFO*)pvDecoded;
                        return ((Marshal.ReadByte(pBasicConstraints->SubjectType.pbData) & CERT_BASIC_CONSTRAINTS_INFO.CERT_CA_SUBJECT_FLAG) != 0,
                                pBasicConstraints->fPathLenConstraint != 0,
                                pBasicConstraints->dwPathLenConstraint);
                    });
            }
        }
    }
}
