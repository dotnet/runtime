// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class FindPal : IFindPal
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
            string? oidValue = Interop.Crypt32.FindOidInfo(Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, maybeOid, expectedGroup, fallBackToAllGroups: true).OID;

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
                Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pThumbPrint), (uint)thumbPrint.Length);
                FindCore<object>(Interop.Crypt32.CertFindType.CERT_FIND_HASH, &blob);
            }
        }

        public unsafe void FindBySubjectName(string subjectName)
        {
            fixed (char* pSubjectName = subjectName)
            {
                FindCore<object>(Interop.Crypt32.CertFindType.CERT_FIND_SUBJECT_STR, pSubjectName);
            }
        }

        public void FindBySubjectDistinguishedName(string subjectDistinguishedName)
        {
            FindCore(
                subjectDistinguishedName,
                static (subjectDistinguishedName, pCertContext) =>
                {
                    string actual = GetCertNameInfo(pCertContext, Interop.Crypt32.CertNameType.CERT_NAME_RDN_TYPE, Interop.Crypt32.CertNameFlags.None);
                    return subjectDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                });
        }

        public unsafe void FindByIssuerName(string issuerName)
        {
            fixed (char* pIssuerName = issuerName)
            {
                FindCore<object>(Interop.Crypt32.CertFindType.CERT_FIND_ISSUER_STR, pIssuerName);
            }
        }

        public void FindByIssuerDistinguishedName(string issuerDistinguishedName)
        {
            FindCore(
                issuerDistinguishedName,
                static (issuerDistinguishedName, pCertContext) =>
                {
                    string actual = GetCertNameInfo(pCertContext, Interop.Crypt32.CertNameType.CERT_NAME_RDN_TYPE, Interop.Crypt32.CertNameFlags.CERT_NAME_ISSUER_FLAG);
                    return issuerDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                });
        }

        public unsafe void FindBySerialNumber(BigInteger hexValue, BigInteger decimalValue)
        {
            FindCore(
                (hexValue, decimalValue),
                static (state, pCertContext) =>
                {
                    byte[] actual = pCertContext.CertContext->pCertInfo->SerialNumber.ToByteArray();
                    GC.KeepAlive(pCertContext);

                    // Convert to BigInteger as the comparison must not fail due to spurious leading zeros
                    BigInteger actualAsBigInteger = PositiveBigIntegerFromByteArray(actual);

                    return state.hexValue.Equals(actualAsBigInteger) || state.decimalValue.Equals(actualAsBigInteger);
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
            Interop.Crypt32.FILETIME fileTime = Interop.Crypt32.FILETIME.FromDateTime(dateTime);

            FindCore(
                (fileTime, compareResult),
                static (state, pCertContext) =>
                {
                    int comparison = Interop.Crypt32.CertVerifyTimeValidity(ref state.fileTime,
                        pCertContext.CertContext->pCertInfo);
                    GC.KeepAlive(pCertContext);
                    return comparison == state.compareResult;
                });
        }

        public unsafe void FindByTemplateName(string templateName)
        {
            FindCore(
                templateName,
                static (templateName, pCertContext) =>
                {
                    // The template name can have 2 different formats: V1 format (<= Win2K) is just a string
                    // V2 format (XP only) can be a friendly name or an OID.
                    // An example of Template Name can be "ClientAuth".

                    bool foundMatch = false;
                    Interop.Crypt32.CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    {
                        Interop.Crypt32.CERT_EXTENSION* pV1Template = Interop.Crypt32.CertFindExtension(Oids.EnrollCertTypeExtension,
                            pCertInfo->cExtension, pCertInfo->rgExtension);
                        if (pV1Template != null)
                        {
                            byte[] extensionRawData = pV1Template->Value.ToByteArray();
                            if (!extensionRawData.DecodeObjectNoThrow(
                                CryptDecodeObjectStructType.X509_UNICODE_ANY_STRING,
                                delegate (void* pvDecoded, int cbDecoded)
                                {
                                    Debug.Assert(cbDecoded >= sizeof(CERT_NAME_VALUE));
                                    CERT_NAME_VALUE* pNameValue = (CERT_NAME_VALUE*)pvDecoded;
                                    string? actual = Marshal.PtrToStringUni(pNameValue->Value.pbData);
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
                        Interop.Crypt32.CERT_EXTENSION* pV2Template = Interop.Crypt32.CertFindExtension(Oids.CertificateTemplate,
                            pCertInfo->cExtension, pCertInfo->rgExtension);
                        if (pV2Template != null)
                        {
                            byte[] extensionRawData = pV2Template->Value.ToByteArray();
                            if (!extensionRawData.DecodeObjectNoThrow(
                                CryptDecodeObjectStructType.X509_CERTIFICATE_TEMPLATE,
                                delegate (void* pvDecoded, int cbDecoded)
                                {
                                    Debug.Assert(cbDecoded >= sizeof(CERT_TEMPLATE_EXT));
                                    CERT_TEMPLATE_EXT* pTemplateExt = (CERT_TEMPLATE_EXT*)pvDecoded;
                                    string? actual = Marshal.PtrToStringAnsi(pTemplateExt->pszObjId);
                                    string? expectedOidValue =
                                        Interop.Crypt32.FindOidInfo(Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, templateName,
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
                oidValue,
                static (oidValue, pCertContext) =>
                {
                    int numOids;
                    int cbData = 0;
                    if (!Interop.Crypt32.CertGetValidUsages(1, ref pCertContext, out numOids, null, ref cbData))
                        return false;

                    // -1 means the certificate is good for all usages.
                    if (numOids == -1)
                        return true;

                    fixed (byte* pOidsPointer = new byte[cbData])
                    {
                        if (!Interop.Crypt32.CertGetValidUsages(1, ref pCertContext, out numOids, pOidsPointer, ref cbData))
                            return false;

                        IntPtr* pOids = (IntPtr*)pOidsPointer;
                        for (int i = 0; i < numOids; i++)
                        {
                            string actual = Marshal.PtrToStringAnsi(pOids[i])!;
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
                oidValue,
                static (oidValue, pCertContext) =>
                {
                    Interop.Crypt32.CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    Interop.Crypt32.CERT_EXTENSION* pCertExtension = Interop.Crypt32.CertFindExtension(Oids.CertPolicies,
                        pCertInfo->cExtension, pCertInfo->rgExtension);
                    if (pCertExtension == null)
                        return false;

                    bool foundMatch = false;
                    byte[] extensionRawData = pCertExtension->Value.ToByteArray();
                    if (!extensionRawData.DecodeObjectNoThrow(
                        CryptDecodeObjectStructType.X509_CERT_POLICIES,
                        delegate (void* pvDecoded, int cbDecoded)
                        {
                            Debug.Assert(cbDecoded >= sizeof(CERT_POLICIES_INFO));
                            CERT_POLICIES_INFO* pCertPoliciesInfo = (CERT_POLICIES_INFO*)pvDecoded;
                            for (int i = 0; i < pCertPoliciesInfo->cPolicyInfo; i++)
                            {
                                CERT_POLICY_INFO* pCertPolicyInfo = &(pCertPoliciesInfo->rgPolicyInfo[i]);
                                string actual = Marshal.PtrToStringAnsi(pCertPolicyInfo->pszPolicyIdentifier)!;
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
                oidValue,
                static (oidValue, pCertContext) =>
                {
                    Interop.Crypt32.CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    Interop.Crypt32.CERT_EXTENSION* pCertExtension = Interop.Crypt32.CertFindExtension(oidValue, pCertInfo->cExtension, pCertInfo->rgExtension);
                    GC.KeepAlive(pCertContext);
                    return pCertExtension != null;
                });
        }

        public unsafe void FindByKeyUsage(X509KeyUsageFlags keyUsage)
        {
            FindCore(
                keyUsage,
                static (keyUsage, pCertContext) =>
                {
                    Interop.Crypt32.CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                    X509KeyUsageFlags actual;
                    if (!Interop.crypt32.CertGetIntendedKeyUsage(Interop.Crypt32.CertEncodingType.All, pCertInfo, out actual, sizeof(X509KeyUsageFlags)))
                        return true;  // no key usage means it is valid for all key usages.
                    GC.KeepAlive(pCertContext);
                    return (actual & keyUsage) == keyUsage;
                });
        }

        public void FindBySubjectKeyIdentifier(byte[] keyIdentifier)
        {
            FindCore(
                keyIdentifier,
                static (keyIdentifier, pCertContext) =>
                {
                    int cbData = 0;
                    if (!Interop.Crypt32.CertGetCertificateContextProperty(pCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, null, ref cbData))
                        return false;

                    byte[] actual = new byte[cbData];
                    if (!Interop.Crypt32.CertGetCertificateContextProperty(pCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, actual, ref cbData))
                        return false;

                    return keyIdentifier.ContentsEqual(actual);
                });
        }

        public void Dispose()
        {
            _storePal.Dispose();
        }

        private unsafe void FindCore<TState>(TState state, Func<TState, SafeCertContextHandle, bool> filter)
        {
            FindCore(Interop.Crypt32.CertFindType.CERT_FIND_ANY, null, state, filter);
        }

        private unsafe void FindCore<TState>(Interop.Crypt32.CertFindType dwFindType, void* pvFindPara, TState state = default!, Func<TState, SafeCertContextHandle, bool>? filter = null)
        {
            SafeCertStoreHandle findResults = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                Interop.Crypt32.CertEncodingType.All,
                IntPtr.Zero,
                Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | Interop.Crypt32.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG,
                null);
            if (findResults.IsInvalid)
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

            SafeCertContextHandle? pCertContext = null;
            while (Interop.crypt32.CertFindCertificateInStore(_storePal.SafeCertStoreHandle, dwFindType, pvFindPara, ref pCertContext))
            {
                if (filter != null && !filter(state, pCertContext))
                    continue;

                if (_validOnly)
                {
                    if (!VerifyCertificateIgnoringErrors(pCertContext))
                        continue;
                }

                if (!Interop.Crypt32.CertAddCertificateLinkToStore(findResults, pCertContext, Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
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
            ChainPal? chainPal = ChainPal.BuildChain(
                false,
                CertificatePal.FromHandle(pCertContext.DangerousGetHandle()),
                extraStore: null,
                applicationPolicy: null,
                certificatePolicy: null,
                X509RevocationMode.NoCheck,
                X509RevocationFlag.ExcludeRoot,
                customTrustStore: null,
                X509ChainTrustMode.System,
                DateTime.Now,
                new TimeSpan(0, 0, 0),
                disableAia: false);

            if (chainPal == null)
                return false;

            using (chainPal)
            {
                Exception? verificationException;
                bool? verified = chainPal.Verify(X509VerificationFlags.NoFlag, out verificationException);
                if (!verified.GetValueOrDefault())
                    return false;
            }

            return true;
        }

        private static unsafe string GetCertNameInfo(SafeCertContextHandle pCertContext, Interop.Crypt32.CertNameType dwNameType, Interop.Crypt32.CertNameFlags dwNameFlags)
        {
            Debug.Assert(dwNameType != Interop.Crypt32.CertNameType.CERT_NAME_ATTR_TYPE);
            return Interop.crypt32.CertGetNameString(
                pCertContext,
                dwNameType,
                dwNameFlags,
                Interop.Crypt32.CertNameStringType.CERT_X500_NAME_STR | Interop.Crypt32.CertNameStringType.CERT_NAME_STR_REVERSE_FLAG);
        }
    }
}
