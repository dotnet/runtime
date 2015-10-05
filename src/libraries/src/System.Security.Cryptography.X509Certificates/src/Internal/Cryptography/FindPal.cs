// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal partial class FindPal
    {
        public static X509Certificate2Collection FindFromCollection(
            X509Certificate2Collection coll,
            X509FindType findType,
            object findValue,
            bool validOnly)
        {
            X509Certificate2Collection results = new X509Certificate2Collection();

            using (IFindPal findPal = OpenPal(coll, results, validOnly))
            {
                switch (findType)
                {
                    case X509FindType.FindByThumbprint:
                    {
                        byte[] thumbPrint = ConfirmedCast<string>(findValue).DecodeHexString();
                        findPal.FindByThumbprint(thumbPrint);
                        break;
                    }
                    case X509FindType.FindBySubjectName:
                    {
                        string subjectName = ConfirmedCast<string>(findValue);
                        findPal.FindBySubjectName(subjectName);
                        break;
                    }
                    case X509FindType.FindBySubjectDistinguishedName:
                    {
                        string subjectDistinguishedName = ConfirmedCast<string>(findValue);
                        findPal.FindBySubjectDistinguishedName(subjectDistinguishedName);
                        break;
                    }
                    case X509FindType.FindByIssuerName:
                    {
                        string issuerName = ConfirmedCast<string>(findValue);
                        findPal.FindByIssuerName(issuerName);
                        break;
                    }
                    case X509FindType.FindByIssuerDistinguishedName:
                    {
                        string issuerDistinguishedName = ConfirmedCast<string>(findValue);
                        findPal.FindByIssuerDistinguishedName(issuerDistinguishedName);
                        break;
                    }
                    case X509FindType.FindBySerialNumber:
                    {
                        string decimalOrHexString = ConfirmedCast<string>(findValue);

                        // FindBySerialNumber allows the input format to be either in
                        // hex or decimal. Since we can't know which one was intended,
                        // it compares against both interpretations and treats a match
                        // of either as a successful find.

                        // String is big-endian, BigInteger constructor requires little-endian.
                        byte[] hexBytes = decimalOrHexString.DecodeHexString();
                        Array.Reverse(hexBytes);

                        BigInteger hexValue = PositiveBigIntegerFromByteArray(hexBytes);
                        BigInteger decimalValue = LaxParseDecimalBigInteger(decimalOrHexString);
                        findPal.FindBySerialNumber(hexValue, decimalValue);
                        break;
                    }
                    case X509FindType.FindByTimeValid:
                    {
                        DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                        findPal.FindByTimeValid(dateTime);
                        break;
                    }
                    case X509FindType.FindByTimeNotYetValid:
                    {
                        DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                        findPal.FindByTimeNotYetValid(dateTime);
                        break;
                    }
                    case X509FindType.FindByTimeExpired:
                    {
                        DateTime dateTime = ConfirmedCast<DateTime>(findValue);
                        findPal.FindByTimeExpired(dateTime);
                        break;
                    }
                    case X509FindType.FindByTemplateName:
                    {
                        string expected = ConfirmedCast<string>(findValue);
                        findPal.FindByTemplateName(expected);
                        break;
                    }
                    case X509FindType.FindByApplicationPolicy:
                    {
                        string oidValue = ConfirmedOidValue(findPal, findValue, OidGroup.Policy);
                        findPal.FindByApplicationPolicy(oidValue);
                        break;
                    }
                    case X509FindType.FindByCertificatePolicy:
                    {
                        string oidValue = ConfirmedOidValue(findPal, findValue, OidGroup.Policy);
                        findPal.FindByCertificatePolicy(oidValue);
                        break;
                    }
                    case X509FindType.FindByExtension:
                    {
                        string oidValue = ConfirmedOidValue(findPal, findValue, OidGroup.ExtensionOrAttribute);
                        findPal.FindByExtension(oidValue);
                        break;
                    }
                    case X509FindType.FindByKeyUsage:
                    {
                        X509KeyUsageFlags keyUsage = ConfirmedX509KeyUsage(findValue);
                        findPal.FindByKeyUsage(keyUsage);
                        break;
                    }
                    case X509FindType.FindBySubjectKeyIdentifier:
                    {
                        byte[] keyIdentifier = ConfirmedCast<string>(findValue).DecodeHexString();
                        findPal.FindBySubjectKeyIdentifier(keyIdentifier);
                        break;
                    }
                    default:
                        throw new CryptographicException(SR.Cryptography_X509_InvalidFindType);
                }
            }
           
            return results;
        }

        private static T ConfirmedCast<T>(object findValue)
        {
            Debug.Assert(findValue != null);

            if (findValue.GetType() != typeof(T))
                throw new CryptographicException(SR.Cryptography_X509_InvalidFindValue);

            return (T)findValue;
        }

        private static string ConfirmedOidValue(IFindPal findPal, object findValue, OidGroup oidGroup)
        {
            string maybeOid = ConfirmedCast<string>(findValue);

            if (maybeOid.Length == 0)
            {
                throw new ArgumentException(SR.Argument_InvalidOidValue);
            }

            return findPal.NormalizeOid(maybeOid, oidGroup);
        }

        private static X509KeyUsageFlags ConfirmedX509KeyUsage(object findValue)
        {
            if (findValue is X509KeyUsageFlags)
                return (X509KeyUsageFlags)findValue;

            if (findValue is int)
                return (X509KeyUsageFlags)(int)findValue;

            if (findValue is uint)
                return (X509KeyUsageFlags)(uint)findValue;

            string findValueString = findValue as string;

            if (findValueString != null)
            {
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
        internal static void ValidateOidValue(string keyValue)
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

            // While characters 0 and 1 were both validated, start at character 1 to validate
            // that there aren't two dots in a row.
            for (int i = 1; i < len; i++)
            {
                // ensure every character is either a digit or a dot
                if (char.IsDigit(keyValue[i]))
                    continue;
                if (keyValue[i] != '.' || keyValue[i + 1] == '.') // disallow double dots
                    throw new ArgumentException(SR.Argument_InvalidOidValue);
            }
        }

        internal static BigInteger PositiveBigIntegerFromByteArray(byte[] bytes)
        {
            // To prevent the big integer from misinterpreted as a negative number,
            // add a "leading 0" to the byte array.
            // Since BigInteger(bytes[]) requires a little-endian byte array,
            // the "leading 0" actually goes at the end of the array.
            byte[] newBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, newBytes, 0, bytes.Length);
            return new BigInteger(newBytes);
        }

        private static BigInteger LaxParseDecimalBigInteger(string decimalString)
        {
            BigInteger ten = new BigInteger(10);
            BigInteger accum = BigInteger.Zero;

            foreach (char c in decimalString)
            {
                if (c >= '0' && c <= '9')
                {
                    accum = BigInteger.Multiply(accum, ten);
                    accum = BigInteger.Add(accum, c - '0');
                }
            }

            return accum;
        }
    }
}
