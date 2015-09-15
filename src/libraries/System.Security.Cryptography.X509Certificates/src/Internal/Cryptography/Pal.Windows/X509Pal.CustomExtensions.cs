// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
        {
            unsafe
            {
                ushort keyUsagesAsShort = (ushort)keyUsages;
                CRYPT_BIT_BLOB blob = new CRYPT_BIT_BLOB()
                {
                    cbData = 2,
                    pbData = (byte*)&keyUsagesAsShort,
                    cUnusedBits = 0,
                };
                return Interop.crypt32.EncodeObject(CryptDecodeObjectStructType.X509_KEY_USAGE, &blob);
            }
        }

        public void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
        {
            unsafe
            {
                uint keyUsagesAsUint = 0;
                encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_KEY_USAGE,
                    delegate (void* pvDecoded)
                    {
                        CRYPT_BIT_BLOB* pBlob = (CRYPT_BIT_BLOB*)pvDecoded;
                        keyUsagesAsUint = 0;
                        if (pBlob->pbData != null)
                        {
                            keyUsagesAsUint = *(uint*)(pBlob->pbData);
                        }
                    }
                );
                keyUsages = (X509KeyUsageFlags)keyUsagesAsUint;
            }
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
                bool localCertificateAuthority = false;
                bool localHasPathLengthConstraint = false;
                int localPathLengthConstraint = 0;

                encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS,
                    delegate (void* pvDecoded)
                    {
                        CERT_BASIC_CONSTRAINTS_INFO* pBasicConstraints = (CERT_BASIC_CONSTRAINTS_INFO*)pvDecoded;
                        localCertificateAuthority = (pBasicConstraints->SubjectType.pbData[0] & CERT_BASIC_CONSTRAINTS_INFO.CERT_CA_SUBJECT_FLAG) != 0;
                        localHasPathLengthConstraint = pBasicConstraints->fPathLenConstraint != 0;
                        localPathLengthConstraint = pBasicConstraints->dwPathLenConstraint;
                    }
                );

                certificateAuthority = localCertificateAuthority;
                hasPathLengthConstraint = localHasPathLengthConstraint;
                pathLengthConstraint = localPathLengthConstraint;
            }
        }

        public void DecodeX509BasicConstraints2Extension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint)
        {
            unsafe
            {
                bool localCertificateAuthority = false;
                bool localHasPathLengthConstraint = false;
                int localPathLengthConstraint = 0;

                encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS2,
                    delegate (void* pvDecoded)
                    {
                        CERT_BASIC_CONSTRAINTS2_INFO* pBasicConstraints2 = (CERT_BASIC_CONSTRAINTS2_INFO*)pvDecoded;
                        localCertificateAuthority = pBasicConstraints2->fCA != 0;
                        localHasPathLengthConstraint = pBasicConstraints2->fPathLenConstraint != 0;
                        localPathLengthConstraint = pBasicConstraints2->dwPathLenConstraint;
                    }
                );

                certificateAuthority = localCertificateAuthority;
                hasPathLengthConstraint = localHasPathLengthConstraint;
                pathLengthConstraint = localPathLengthConstraint;
            }
        }

        public byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages)
        {
            int numUsages;
            using (SafeHandle usagesSafeHandle = usages.ToLpstrArray(out numUsages))
            {
                unsafe
                {
                    CERT_ENHKEY_USAGE enhKeyUsage = new CERT_ENHKEY_USAGE()
                    {
                        cUsageIdentifier = numUsages,
                        rgpszUsageIdentifier = (IntPtr*)(usagesSafeHandle.DangerousGetHandle()),
                    };

                    return Interop.crypt32.EncodeObject(Oids.EnhancedKeyUsage, &enhKeyUsage);
                }
            }
        }

        public void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
        {
            OidCollection localUsages = new OidCollection();

            unsafe
            {
                encoded.DecodeObject(
                    CryptDecodeObjectStructType.X509_ENHANCED_KEY_USAGE,
                    delegate (void* pvDecoded)
                    {
                        CERT_ENHKEY_USAGE* pEnhKeyUsage = (CERT_ENHKEY_USAGE*)pvDecoded;
                        int count = pEnhKeyUsage->cUsageIdentifier;
                        for (int i = 0; i < count; i++)
                        {
                            IntPtr oidValuePointer = pEnhKeyUsage->rgpszUsageIdentifier[i];
                            String oidValue = Marshal.PtrToStringAnsi(oidValuePointer);
                            Oid oid = new Oid(oidValue);
                            localUsages.Add(oid);
                        }
                    }
                );
            }

            usages = localUsages;
            return;
        }

        public byte[] EncodeX509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier)
        {
            unsafe
            {
                fixed (byte* pSubkectKeyIdentifier = subjectKeyIdentifier)
                {
                    CRYPTOAPI_BLOB blob = new CRYPTOAPI_BLOB(subjectKeyIdentifier.Length, pSubkectKeyIdentifier);
                    return Interop.crypt32.EncodeObject(Oids.SubjectKeyIdentifier, &blob);
                }
            }
        }

        public void DecodeX509SubjectKeyIdentifierExtension(byte[] encoded, out byte[] subjectKeyIdentifier)
        {
            unsafe
            {
                byte[] localSubjectKeyIdentifier = null;
                encoded.DecodeObject(
                    Oids.SubjectKeyIdentifier,
                    delegate (void* pvDecoded)
                    {
                        CRYPTOAPI_BLOB* pBlob = (CRYPTOAPI_BLOB*)pvDecoded;
                        localSubjectKeyIdentifier = pBlob->ToByteArray();
                    }
                );
                subjectKeyIdentifier = localSubjectKeyIdentifier;
            }
        }

        public byte[] ComputeCapiSha1OfPublicKey(PublicKey key)
        {
            unsafe
            {
                fixed (byte* pszOidValue = key.Oid.ValueAsAscii())
                {
                    byte[] encodedParameters = key.EncodedParameters.RawData;
                    fixed (byte* pEncodedParameters = encodedParameters)
                    {
                        byte[] encodedKeyValue = key.EncodedKeyValue.RawData;
                        fixed (byte* pEncodedKeyValue = encodedKeyValue)
                        {
                            CERT_PUBLIC_KEY_INFO publicKeyInfo = new CERT_PUBLIC_KEY_INFO()
                            {
                                Algorithm = new CRYPT_ALGORITHM_IDENTIFIER()
                                {
                                    pszObjId = new IntPtr(pszOidValue),
                                    Parameters = new CRYPTOAPI_BLOB(encodedParameters.Length, pEncodedParameters),
                                },

                                PublicKey = new CRYPT_BIT_BLOB()
                                {
                                    cbData = encodedKeyValue.Length,
                                    pbData = pEncodedKeyValue,
                                    cUnusedBits = 0,
                                },
                            };

                            int cb = 20;
                            byte[] buffer = new byte[cb];
                            if (!Interop.crypt32.CryptHashPublicKeyInfo(IntPtr.Zero, AlgId.CALG_SHA1, 0, CertEncodingType.All, ref publicKeyInfo, buffer, ref cb))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
                            if (cb < buffer.Length)
                            {
                                byte[] newBuffer = new byte[cb];
                                Array.Copy(buffer, newBuffer, cb);
                                buffer = newBuffer;
                            }
                            return buffer;
                        }
                    }
                }
            }
        }
    }
}

