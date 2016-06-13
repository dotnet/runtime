// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Internal.Cryptography.Pal.Native;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal
{
    internal partial class FindPal : IFindPal
    {
        private readonly StorePal _storePal;
        private readonly X509Certificate2Collection _copyTo;
        private readonly bool _validOnly;

        internal FindPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            _storePal = (StorePal)StorePal.LinkFromCertificateCollection(findFrom);
            _copyTo = copyTo;
            _validOnly = validOnly;
        }

        internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            return new FindPal(findFrom, copyTo, validOnly);
        }

        public string NormalizeOid(string maybeOid, OidGroup expectedGroup)
        {
            string oidValue = Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, maybeOid, expectedGroup, fallBackToAllGroups: true).OID;

            if (oidValue == null)
            {
                oidValue = maybeOid;
                ValidateOidValue(oidValue);
            }

            return oidValue;
        }

        public unsafe void FindByThumbprint(byte[] thumbPrint)
        {
            fixed (byte* pThumbPrint = thumbPrint)
            {
                CRYPTOAPI_BLOB blob = new CRYPTOAPI_BLOB(thumbPrint.Length, pThumbPrint);
                FindCore(CertFindType.CERT_FIND_HASH, &blob);
            }
        }

        public unsafe void FindBySubjectName(string subjectName)
        {
            fixed (char* pSubjectName = subjectName)
            {
                FindCore(CertFindType.CERT_FIND_SUBJECT_STR, pSubjectName);
            }
        }

        public void FindBySubjectDistinguishedName(string subjectDistinguishedName)
        {
            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    string actual = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.None);
                    return subjectDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                });
        }

        public unsafe void FindByIssuerName(string issuerName)
        {
            fixed (char* pIssuerName = issuerName)
            {
                FindCore(CertFindType.CERT_FIND_ISSUER_STR, pIssuerName);
            }
        }

        public void FindByIssuerDistinguishedName(string issuerDistinguishedName)
        {
            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    string actual = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.CERT_NAME_ISSUER_FLAG);
                    return issuerDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                });
        }

        public unsafe void FindBySerialNumber(BigInteger hexValue, BigInteger decimalValue)
        {
            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    byte[] actual = pCertContext.CertContext->pCertInfo->SerialNumber.ToByteArray();
                    GC.KeepAlive(pCertContext);

                    // Convert to BigInteger as the comparison must not fail due to spurious leading zeros
                    BigInteger actualAsBigInteger = PositiveBigIntegerFromByteArray(actual);

                    return hexValue.Equals(actualAsBigInteger) || decimalValue.Equals(actualAsBigInteger);
                });
        }

        public void FindByTimeValid(DateTime dateTime)
        {
            FindByTime(dateTime, 0);
        }

        public void FindByTimeNotYetValid(DateTime dateTime)
        {
            FindByTime(dateTime, -1);
        }

        public void FindByTimeExpired(DateTime dateTime)
        {
            FindByTime(dateTime, 1);
        }

        private unsafe void FindByTime(DateTime dateTime, int compareResult)
        {
            FILETIME fileTime = FILETIME.FromDateTime(dateTime);

            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    int comparison = Interop.crypt32.CertVerifyTimeValidity(ref fileTime,
                        pCertContext.CertContext->pCertInfo);
                    GC.KeepAlive(pCertContext);
                    return comparison == compareResult;
                });
        }

        public unsafe void FindByTemplateName(string templateName)
        {
            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    // The template name can have 2 different formats: V1 format (<= Win2K) is just a string
                    // V2 format (XP only) can be a friendly name or an OID.
                    // An example of Template Name can be "ClientAuth".

                    bool foundMatch = false;
                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    {
                        CERT_EXTENSION* pV1Template = Interop.crypt32.CertFindExtension(Oids.EnrollCertTypeExtension,
                            pCertInfo->cExtension, pCertInfo->rgExtension);
                        if (pV1Template != null)
                        {
                            byte[] extensionRawData = pV1Template->Value.ToByteArray();
                            if (!extensionRawData.DecodeObjectNoThrow(
                                CryptDecodeObjectStructType.X509_UNICODE_ANY_STRING,
                                delegate(void* pvDecoded)
                                {
                                    CERT_NAME_VALUE* pNameValue = (CERT_NAME_VALUE*)pvDecoded;
                                    string actual = Marshal.PtrToStringUni(new IntPtr(pNameValue->Value.pbData));
                                    if (templateName.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                        foundMatch = true;
                                }))
                            {
                                return false;
                            }
                        }
                    }

                    if (!foundMatch)
                    {
                        CERT_EXTENSION* pV2Template = Interop.crypt32.CertFindExtension(Oids.CertificateTemplate,
                            pCertInfo->cExtension, pCertInfo->rgExtension);
                        if (pV2Template != null)
                        {
                            byte[] extensionRawData = pV2Template->Value.ToByteArray();
                            if (!extensionRawData.DecodeObjectNoThrow(
                                CryptDecodeObjectStructType.X509_CERTIFICATE_TEMPLATE,
                                delegate(void* pvDecoded)
                                {
                                    CERT_TEMPLATE_EXT* pTemplateExt = (CERT_TEMPLATE_EXT*)pvDecoded;
                                    string actual = Marshal.PtrToStringAnsi(pTemplateExt->pszObjId);
                                    string expectedOidValue =
                                        Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, templateName,
                                            OidGroup.Template, fallBackToAllGroups: true).OID;
                                    if (expectedOidValue == null)
                                        expectedOidValue = templateName;
                                    if (expectedOidValue.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                        foundMatch = true;
                                }))
                            {
                                return false;
                            }
                        }
                    }

                    GC.KeepAlive(pCertContext);
                    return foundMatch;
                });
        }

        public unsafe void FindByApplicationPolicy(string oidValue)
        {
            FindCore(
                delegate (SafeCertContextHandle pCertContext)
                {
                    int numOids;
                    int cbData = 0;
                    if (!Interop.crypt32.CertGetValidUsages(1, ref pCertContext, out numOids, null, ref cbData))
                        return false;

                    // -1 means the certificate is good for all usages.
                    if (numOids == -1)
                        return true;

                    fixed (byte* pOidsPointer = new byte[cbData])
                    {
                        if (!Interop.crypt32.CertGetValidUsages(1, ref pCertContext, out numOids, pOidsPointer, ref cbData))
                            return false;

                        IntPtr* pOids = (IntPtr*)pOidsPointer;
                        for (int i = 0; i < numOids; i++)
                        {
                            string actual = Marshal.PtrToStringAnsi(pOids[i]);
                            if (oidValue.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                        return false;
                    }
                });
        }

        public unsafe void FindByCertificatePolicy(string oidValue)
        {
            FindCore(
                delegate(SafeCertContextHandle pCertContext)
                {
                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    CERT_EXTENSION* pCertExtension = Interop.crypt32.CertFindExtension(Oids.CertPolicies,
                        pCertInfo->cExtension, pCertInfo->rgExtension);
                    if (pCertExtension == null)
                        return false;

                    bool foundMatch = false;
                    byte[] extensionRawData = pCertExtension->Value.ToByteArray();
                    if (!extensionRawData.DecodeObjectNoThrow(
                        CryptDecodeObjectStructType.X509_CERT_POLICIES,
                        delegate(void* pvDecoded)
                        {
                            CERT_POLICIES_INFO* pCertPoliciesInfo = (CERT_POLICIES_INFO*)pvDecoded;
                            for (int i = 0; i < pCertPoliciesInfo->cPolicyInfo; i++)
                            {
                                CERT_POLICY_INFO* pCertPolicyInfo = &(pCertPoliciesInfo->rgPolicyInfo[i]);
                                string actual = Marshal.PtrToStringAnsi(pCertPolicyInfo->pszPolicyIdentifier);
                                if (oidValue.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                {
                                    foundMatch = true;
                                    break;
                                }
                            }
                        }
                        ))
                    {
                        return false;
                    }

                    GC.KeepAlive(pCertContext);
                    return foundMatch;
                });
        }

        public unsafe void FindByExtension(string oidValue)
        {
            FindCore(
                delegate (SafeCertContextHandle pCertContext)
                {
                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    CERT_EXTENSION* pCertExtension = Interop.crypt32.CertFindExtension(oidValue, pCertInfo->cExtension, pCertInfo->rgExtension);
                    GC.KeepAlive(pCertContext);
                    return pCertExtension != null;
                });
        }

        public unsafe void FindByKeyUsage(X509KeyUsageFlags keyUsage)
        {
            FindCore(
                delegate (SafeCertContextHandle pCertContext)
                {
                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    X509KeyUsageFlags actual;
                    if (!Interop.crypt32.CertGetIntendedKeyUsage(CertEncodingType.All, pCertInfo, out actual, sizeof(X509KeyUsageFlags)))
                        return true;  // no key usage means it is valid for all key usages.
                    GC.KeepAlive(pCertContext);
                    return (actual & keyUsage) == keyUsage;
                });
        }

        public void FindBySubjectKeyIdentifier(byte[] keyIdentifier)
        {
            FindCore(
                delegate (SafeCertContextHandle pCertContext)
                {
                    int cbData = 0;
                    if (!Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, null, ref cbData))
                        return false;

                    byte[] actual = new byte[cbData];
                    if (!Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, actual, ref cbData))
                        return false;

                    return keyIdentifier.ContentsEqual(actual);
                });
        }

        public void Dispose()
        {
            _storePal.Dispose();
        }

        private unsafe void FindCore(Func<SafeCertContextHandle, bool> filter)
        {
            FindCore(CertFindType.CERT_FIND_ANY, null, filter);
        }

        private unsafe void FindCore(CertFindType dwFindType, void* pvFindPara, Func<SafeCertContextHandle, bool> filter = null)
        {
            SafeCertStoreHandle findResults = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                CertEncodingType.All,
                IntPtr.Zero,
                CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG,
                null);
            if (findResults.IsInvalid)
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

            SafeCertContextHandle pCertContext = null;
            while (Interop.crypt32.CertFindCertificateInStore(_storePal.SafeCertStoreHandle, dwFindType, pvFindPara, ref pCertContext))
            {
                if (filter != null && !filter(pCertContext))
                    continue;

                if (_validOnly)
                {
                    if (!VerifyCertificateIgnoringErrors(pCertContext))
                        continue;
                }

                if (!Interop.crypt32.CertAddCertificateLinkToStore(findResults, pCertContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            using (StorePal resultsStore = new StorePal(findResults))
            {
                resultsStore.CopyTo(_copyTo);
            }
        }

        private static bool VerifyCertificateIgnoringErrors(SafeCertContextHandle pCertContext)
        {
            // This needs to be kept in sync with IsCertValid in the
            // Unix/OpenSSL PAL version (and potentially any other PALs that come about)
            ChainPal chainPal = ChainPal.BuildChain(
                true,
                CertificatePal.FromHandle(pCertContext.DangerousGetHandle()),
                null, //extraStore
                null, //applicationPolicy
                null, //certificatePolicy
                X509RevocationMode.NoCheck,
                X509RevocationFlag.ExcludeRoot,
                DateTime.Now,
                new TimeSpan(0, 0, 0));

            if (chainPal == null)
                return false;

            using (chainPal)
            {
                Exception verificationException;
                bool? verified = chainPal.Verify(X509VerificationFlags.NoFlag, out verificationException);
                if (!verified.GetValueOrDefault())
                    return false;
            }

            return true;
        }

        private static string GetCertNameInfo(SafeCertContextHandle pCertContext, CertNameType dwNameType, CertNameFlags dwNameFlags)
        {
            Debug.Assert(dwNameType != CertNameType.CERT_NAME_ATTR_TYPE);

            CertNameStringType stringType = CertNameStringType.CERT_X500_NAME_STR | CertNameStringType.CERT_NAME_STR_REVERSE_FLAG;

            int cch = Interop.crypt32.CertGetNameString(pCertContext, dwNameType, dwNameFlags, ref stringType, null, 0);
            if (cch == 0)
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            StringBuilder sb = new StringBuilder(cch);
            if (0 == Interop.crypt32.CertGetNameString(pCertContext, dwNameType, dwNameFlags, ref stringType, sb, cch))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return sb.ToString();
        }
    }
}
