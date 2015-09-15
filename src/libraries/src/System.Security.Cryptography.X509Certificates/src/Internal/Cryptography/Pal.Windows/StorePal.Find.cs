// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using BigInteger = System.Numerics.BigInteger;
using FILETIME = Internal.Cryptography.Pal.Native.FILETIME;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal : IDisposable, IStorePal
    {
        public void FindAndCopyTo(X509FindType findType, object findValue, bool validOnly, X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            StorePal findResults = CreatedLinkedStoreWithFindResults(findType, findValue, validOnly);
            findResults.CopyTo(collection);
        }

        private StorePal CreatedLinkedStoreWithFindResults(X509FindType findType, Object findValue, bool validOnly)
        {
            unsafe
            {
                switch (findType)
                {
                    case X509FindType.FindByThumbprint:
                        {
                            byte[] thumbPrint = ConfirmedCast<String>(findValue).DecodeHexString();
                            fixed (byte* pThumbPrint = thumbPrint)
                            {
                                CRYPTOAPI_BLOB blob = new CRYPTOAPI_BLOB(thumbPrint.Length, pThumbPrint);
                                return FindCore(CertFindType.CERT_FIND_HASH, &blob, validOnly);
                            }
                        }

                    case X509FindType.FindBySubjectName:
                        {
                            String subjectName = ConfirmedCast<String>(findValue);
                            fixed (char* pSubjectName = subjectName)
                            {
                                return FindCore(CertFindType.CERT_FIND_SUBJECT_STR, pSubjectName, validOnly);
                            }
                        }

                    case X509FindType.FindBySubjectDistinguishedName:
                        {
                            String subjectDistinguishedName = ConfirmedCast<String>(findValue);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    String actual = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.None);
                                    return subjectDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                                }
                            );
                        }

                    case X509FindType.FindByIssuerName:
                        {
                            String issuerName = ConfirmedCast<String>(findValue);
                            fixed (char* pIssuerName = issuerName)
                            {
                                return FindCore(CertFindType.CERT_FIND_ISSUER_STR, pIssuerName, validOnly);
                            }
                        }

                    case X509FindType.FindByIssuerDistinguishedName:
                        {
                            String issuerDistinguishedName = ConfirmedCast<String>(findValue);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    String actual = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.CERT_NAME_ISSUER_FLAG);
                                    return issuerDistinguishedName.Equals(actual, StringComparison.OrdinalIgnoreCase);
                                }
                            );
                        }

                    case X509FindType.FindBySerialNumber:
                        {
                            String decimalOrHexString = ConfirmedCast<String>(findValue);

                            // FindBySerialNumber allows the input format to be either in hex or decimal. Since we can't know which one was intended,
                            // it compares against both interpretations and treats a match of either as a successful find.

                            byte[] hexBytes = decimalOrHexString.DecodeHexString();
                            Array.Reverse(hexBytes);   // String is big-endian, BigInteger constructor requires little-endian.
                            BigInteger expected1 = PositiveBigIntegerFromByteArray(hexBytes);

                            BigInteger ten = new BigInteger(10);
                            BigInteger expected2 = BigInteger.Zero;
                            foreach (char c in decimalOrHexString)
                            {
                                if (c >= '0' && c <= '9')
                                {
                                    expected2 = BigInteger.Multiply(expected2, ten);
                                    expected2 = BigInteger.Add(expected2, c - '0');
                                }
                            }

                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    byte[] actual = pCertContext.CertContext->pCertInfo->SerialNumber.ToByteArray();
                                    BigInteger actualAsBigInteger = PositiveBigIntegerFromByteArray(actual);   // Convert to BigInteger as the comparison must not fail due to spurious leading zeros
                                    GC.KeepAlive(pCertContext);
                                    return expected1.Equals(actualAsBigInteger) || expected2.Equals(actualAsBigInteger);
                                }
                            );
                        }

                    case X509FindType.FindByTimeValid:
                        {
                            DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                            FILETIME fileTime = FILETIME.FromDateTime(dateTime);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    int comparison = Interop.crypt32.CertVerifyTimeValidity(ref fileTime, pCertContext.CertContext->pCertInfo);
                                    GC.KeepAlive(pCertContext);
                                    return comparison == 0;
                                }
                            );
                        }

                    case X509FindType.FindByTimeNotYetValid:
                        {
                            DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                            FILETIME fileTime = FILETIME.FromDateTime(dateTime);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    int comparison = Interop.crypt32.CertVerifyTimeValidity(ref fileTime, pCertContext.CertContext->pCertInfo);
                                    GC.KeepAlive(pCertContext);
                                    return comparison == -1;
                                }
                            );
                        }

                    case X509FindType.FindByTimeExpired:
                        {
                            DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                            FILETIME fileTime = FILETIME.FromDateTime(dateTime);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    int comparison = Interop.crypt32.CertVerifyTimeValidity(ref fileTime, pCertContext.CertContext->pCertInfo);
                                    GC.KeepAlive(pCertContext);
                                    return comparison == 1;
                                }
                            );
                        }

                    case X509FindType.FindByTemplateName:
                        {
                            String expected = ConfirmedCast<String>(findValue);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    // The template name can have 2 different formats: V1 format (<= Win2K) is just a string
                                    // V2 format (XP only) can be a friendly name or an OID.
                                    // An example of Template Name can be "ClientAuth".

                                    bool foundMatch = false;
                                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                                    {
                                        CERT_EXTENSION* pV1Template = Interop.crypt32.CertFindExtension(Oids.EnrollCertTypeExtension, pCertInfo->cExtension, pCertInfo->rgExtension);
                                        if (pV1Template != null)
                                        {
                                            byte[] extensionRawData = pV1Template->Value.ToByteArray();
                                            if (!extensionRawData.DecodeObjectNoThrow(
                                                CryptDecodeObjectStructType.X509_UNICODE_ANY_STRING,
                                                delegate (void* pvDecoded)
                                                {
                                                    CERT_NAME_VALUE* pNameValue = (CERT_NAME_VALUE*)pvDecoded;
                                                    String actual = Marshal.PtrToStringUni(new IntPtr(pNameValue->Value.pbData));
                                                    if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                                        foundMatch = true;
                                                }))
                                            {
                                                return false;
                                            }
                                        }
                                    }

                                    if (!foundMatch)
                                    {
                                        CERT_EXTENSION* pV2Template = Interop.crypt32.CertFindExtension(Oids.CertificateTemplate, pCertInfo->cExtension, pCertInfo->rgExtension);
                                        if (pV2Template != null)
                                        {
                                            byte[] extensionRawData = pV2Template->Value.ToByteArray();
                                            if (!extensionRawData.DecodeObjectNoThrow(
                                                CryptDecodeObjectStructType.X509_CERTIFICATE_TEMPLATE,
                                                delegate (void* pvDecoded)
                                                {
                                                    CERT_TEMPLATE_EXT* pTemplateExt = (CERT_TEMPLATE_EXT*)pvDecoded;
                                                    String actual = Marshal.PtrToStringAnsi(pTemplateExt->pszObjId);
                                                    String expectedOidValue = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, expected, OidGroup.Template, fallBackToAllGroups: true).OID;
                                                    if (expectedOidValue == null)
                                                        expectedOidValue = expected;
                                                    if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
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

                    case X509FindType.FindByApplicationPolicy:
                        {
                            String expected = ConfirmedOidValue(findValue, OidGroup.Policy);
                            return FindCore(validOnly,
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
                                            String actual = Marshal.PtrToStringAnsi(pOids[i]);
                                            if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
                                                return true;
                                        }
                                        return false;
                                    }
                                }
                            );
                        }

                    case X509FindType.FindByCertificatePolicy:
                        {
                            String expected = ConfirmedOidValue(findValue, OidGroup.Policy);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                                    CERT_EXTENSION* pCertExtension = Interop.crypt32.CertFindExtension(Oids.CertPolicies, pCertInfo->cExtension, pCertInfo->rgExtension);
                                    if (pCertExtension == null)
                                        return false;

                                    bool foundMatch = false;
                                    byte[] extensionRawData = pCertExtension->Value.ToByteArray();
                                    if (!extensionRawData.DecodeObjectNoThrow(
                                        CryptDecodeObjectStructType.X509_CERT_POLICIES,
                                        delegate (void* pvDecoded)
                                        {
                                            CERT_POLICIES_INFO* pCertPoliciesInfo = (CERT_POLICIES_INFO*)pvDecoded;
                                            for (int i = 0; i < pCertPoliciesInfo->cPolicyInfo; i++)
                                            {
                                                CERT_POLICY_INFO* pCertPolicyInfo = &(pCertPoliciesInfo->rgPolicyInfo[i]);
                                                String actual = Marshal.PtrToStringAnsi(pCertPolicyInfo->pszPolicyIdentifier);
                                                if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
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
                                }
                            );
                        }

                    case X509FindType.FindByExtension:
                        {
                            String oidValue = ConfirmedOidValue(findValue, OidGroup.ExtensionOrAttribute);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                                    CERT_EXTENSION* pCertExtension = Interop.crypt32.CertFindExtension(oidValue, pCertInfo->cExtension, pCertInfo->rgExtension);
                                    GC.KeepAlive(pCertContext);
                                    return pCertExtension != null;
                                }
                            );
                        }

                    case X509FindType.FindByKeyUsage:
                        {
                            X509KeyUsageFlags expected = ConfirmedX509KeyUsage(findValue);
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
                                    X509KeyUsageFlags actual;
                                    if (!Interop.crypt32.CertGetIntendedKeyUsage(CertEncodingType.All, pCertInfo, out actual, sizeof(X509KeyUsageFlags)))
                                        return true;  // no key usage means it is valid for all key usages.
                                    GC.KeepAlive(pCertContext);
                                    return (actual & expected) == expected;
                                }
                            );
                        }

                    case X509FindType.FindBySubjectKeyIdentifier:
                        {
                            byte[] expected = ConfirmedCast<String>(findValue).DecodeHexString();
                            return FindCore(validOnly,
                                delegate (SafeCertContextHandle pCertContext)
                                {
                                    int cbData = 0;
                                    if (!Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, null, ref cbData))
                                        return false;

                                    byte[] actual = new byte[cbData];
                                    if (!Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, actual, ref cbData))
                                        return false;

                                    return expected.ContentsEqual(actual);
                                }
                            );
                        }

                    default:
                        throw new CryptographicException(SR.Cryptography_X509_InvalidFindType);
                }
            }
        }

        private static T ConfirmedCast<T>(Object findValue)
        {
            Debug.Assert(findValue != null);

            if (findValue.GetType() != typeof(T))
                throw new CryptographicException(SR.Cryptography_X509_InvalidFindValue);

            return (T)findValue;
        }

        private static String ConfirmedOidValue(Object findValue, OidGroup oidGroup)
        {
            String input = ConfirmedCast<String>(findValue);
            String oidValue = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, input, oidGroup, fallBackToAllGroups: true).OID;
            if (oidValue == null)
            {
                oidValue = input;
                ValidateOidValue(oidValue);
            }
            return oidValue;
        }

        private static X509KeyUsageFlags ConfirmedX509KeyUsage(Object findValue)
        {
            if (findValue is X509KeyUsageFlags)
                return (X509KeyUsageFlags)findValue;

            if (findValue is int)
                return (X509KeyUsageFlags)(int)findValue;

            if (findValue is uint)
                return (X509KeyUsageFlags)(uint)findValue;

            if (findValue is String)
            {
                String findValueString = (String)findValue;

                if (findValueString.Equals("DigitalSignature", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.DigitalSignature;

                if (findValueString.Equals("NonRepudiation", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.NonRepudiation;

                if (findValueString.Equals("KeyEncipherment", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.KeyEncipherment;

                if (findValueString.Equals("DataEncipherment", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.DataEncipherment;

                if (findValueString.Equals("KeyAgreement", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.KeyAgreement;

                if (findValueString.Equals("KeyCertSign", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.KeyCertSign;

                if (findValueString.Equals("CrlSign", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.CrlSign;

                if (findValueString.Equals("EncipherOnly", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.EncipherOnly;

                if (findValueString.Equals("DecipherOnly", StringComparison.OrdinalIgnoreCase))
                    return X509KeyUsageFlags.DecipherOnly;

                throw new CryptographicException(SR.Cryptography_X509_InvalidFindValue);
            }

            throw new CryptographicException(SR.Cryptography_X509_InvalidFindValue);
        }

        //
        // verify the passed keyValue is valid as per X.208
        //
        // The first number must be 0, 1 or 2.
        // Enforce all characters are digits and dots.
        // Enforce that no dot starts or ends the Oid, and disallow double dots.
        // Enforce there is at least one dot separator.
        //
        private static void ValidateOidValue(String keyValue)
        {
            if (keyValue == null)
                throw new ArgumentNullException("keyValue");

            int len = keyValue.Length;
            if (len < 2)
                throw new ArgumentException(SR.Argument_InvalidOidValue);

            // should not start with a dot. The first digit must be 0, 1 or 2.
            char c = keyValue[0];
            if (c != '0' && c != '1' && c != '2')
                throw new ArgumentException(SR.Argument_InvalidOidValue);
            if (keyValue[1] != '.' || keyValue[len - 1] == '.') // should not end in a dot
                throw new ArgumentException(SR.Argument_InvalidOidValue);

            bool hasAtLeastOneDot = false;
            for (int i = 1; i < len; i++)
            {
                // ensure every character is either a digit or a dot
                if (char.IsDigit(keyValue[i]))
                    continue;
                if (keyValue[i] != '.' || keyValue[i + 1] == '.') // disallow double dots
                    throw new ArgumentException(SR.Argument_InvalidOidValue);
                hasAtLeastOneDot = true;
            }
            if (hasAtLeastOneDot)
                return;
        }

        private StorePal FindCore(bool validOnly, Func<SafeCertContextHandle, bool> filter)
        {
            unsafe
            {
                return FindCore(CertFindType.CERT_FIND_ANY, null, validOnly, filter);
            }
        }

        private unsafe StorePal FindCore(CertFindType dwFindType, void* pvFindPara, bool validOnly, Func<SafeCertContextHandle, bool> filter = null)
        {
            SafeCertStoreHandle findResults = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                CertEncodingType.All,
                IntPtr.Zero,
                CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG,
                null);
            if (findResults.IsInvalid)
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

            SafeCertContextHandle pCertContext = null;
            while (Interop.crypt32.CertFindCertificateInStore(_certStore, dwFindType, pvFindPara, ref pCertContext))
            {
                if (filter != null && !filter(pCertContext))
                    continue;

                if (validOnly)
                {
                    if (!VerifyCertificateIgnoringErrors(pCertContext))
                        continue;
                }

                if (!Interop.crypt32.CertAddCertificateLinkToStore(findResults, pCertContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            return new StorePal(findResults);
        }

        private static bool VerifyCertificateIgnoringErrors(SafeCertContextHandle pCertContext)
        {
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
                if (!(verified.HasValue && verified.Value))
                    return false;
            }

            return true;
        }

        private static String GetCertNameInfo(SafeCertContextHandle pCertContext, CertNameType dwNameType, CertNameFlags dwNameFlags)
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

        private static BigInteger PositiveBigIntegerFromByteArray(byte[] bytes)
        {
            // To prevent the big integer from misinterpreted as a negative number, add a "leading 0" to the byte array.
            // Since BigInteger(bytes[]) requires a little-endian byte array, the "leading 0" actually goes at the end of the array.
            byte[] newBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, newBytes, 0, bytes.Length);
            return new BigInteger(newBytes);
        }
    }
}
