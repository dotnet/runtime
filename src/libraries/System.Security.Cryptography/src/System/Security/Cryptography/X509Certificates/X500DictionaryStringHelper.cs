// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class X500DictionaryStringHelper
    {
        internal static string ReadAnyAsnString(this AsnReader tavReader)
        {
            Asn1Tag tag = tavReader.PeekTag();

            if (tag.TagClass != TagClass.Universal)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            switch ((UniversalTagNumber)tag.TagValue)
            {
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.NumericString:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.T61String:
                    // .NET's string comparisons start by checking the length, so a trailing
                    // NULL character which was literally embedded in the DER would cause a
                    // failure in .NET whereas it wouldn't have with strcmp.
                    return tavReader.ReadCharacterString((UniversalTagNumber)tag.TagValue).TrimEnd('\0');

                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        internal static string ReadAnyAsnString(ref this AsnValueReader tavReader)
        {
            Asn1Tag tag = tavReader.PeekTag();

            if (tag.TagClass != TagClass.Universal)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            switch ((UniversalTagNumber)tag.TagValue)
            {
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.NumericString:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.T61String:
                    // .NET's string comparisons start by checking the length, so a trailing
                    // NULL character which was literally embedded in the DER would cause a
                    // failure in .NET whereas it wouldn't have with strcmp.
                    return tavReader.ReadCharacterString((UniversalTagNumber)tag.TagValue).TrimEnd('\0');

                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }
    }
}
