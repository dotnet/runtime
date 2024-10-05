// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public bool SupportsLegacyBasicConstraintsExtension
        {
            get { return true; }
        }

        public byte[] EncodeX509BasicConstraints2Extension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint)
        {
            unsafe
            {
                CERT_BASIC_CONSTRAINTS2_INFO constraintsInfo = new CERT_BASIC_CONSTRAINTS2_INFO()
                {
                    fCA = certificateAuthority ? 1 : 0,
                    fPathLenConstraint = hasPathLengthConstraint ? 1 : 0,
                    dwPathLenConstraint = pathLengthConstraint,
                };

                return Interop.crypt32.EncodeObject(Oids.BasicConstraints2, &constraintsInfo);
            }
        }

        public void DecodeX509BasicConstraintsExtension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint)
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

        public void DecodeX509BasicConstraints2Extension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint)
        {
            unsafe
            {
                (certificateAuthority, hasPathLengthConstraint, pathLengthConstraint) = encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS2,
                    static delegate (void* pvDecoded, int cbDecoded)
                    {
                        Debug.Assert(cbDecoded >= sizeof(CERT_BASIC_CONSTRAINTS2_INFO));
                        CERT_BASIC_CONSTRAINTS2_INFO* pBasicConstraints2 = (CERT_BASIC_CONSTRAINTS2_INFO*)pvDecoded;
                        return (pBasicConstraints2->fCA != 0,
                                pBasicConstraints2->fPathLenConstraint != 0,
                                pBasicConstraints2->dwPathLenConstraint);
                    });
            }
        }
    }
}
