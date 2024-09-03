using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Melanzana.CodeSign
{
    public static class AppleCertificateExtensions
    {
        private const string OrganizationalUnit = "2.5.4.11";

        private static string ReadAnyAsnString(this AsnReader tavReader)
        {
            Asn1Tag tag = tavReader.PeekTag();

            if (tag.TagClass != TagClass.Universal)
            {
                throw new CryptographicException("Invalid DER encoding");
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
                    throw new CryptographicException("Invalid DER encoding");
            }
        }
        
        public static bool IsAppleDeveloperCertificate(this X509Certificate2 certificate)
        {
            // FIXME: We should check the certificate anchor and only allow the following OIDs in extensions:
            // 1.2.840.113635.100.6.1.2 (WWDR)
            // 1.2.840.113635.100.6.1.12 (MACWWDR)
            // 1.2.840.113635.100.6.1.13 (Developer ID)
            // 1.2.840.113635.100.6.1.7 (Distribution)
            // 1.2.840.113635.100.6.1.4 (iPhone Distribution)
            return certificate.Extensions.Any(e => e.Oid?.Value?.StartsWith("1.2.840.113635.100.6.1.") ?? false);
        }

        public static string GetTeamId(this X509Certificate2 certificate)
        {
            if (certificate.IsAppleDeveloperCertificate())
            {
                AsnReader x500NameReader = new AsnReader(certificate.SubjectName.RawData, AsnEncodingRules.DER);
                AsnReader x500NameSequenceReader = x500NameReader.ReadSequence();
                while (x500NameSequenceReader.HasData)
                {
                    var rdnReader = x500NameSequenceReader.ReadSetOf(skipSortOrderValidation: true);
                    while (rdnReader.HasData)
                    {
                        AsnReader tavReader = rdnReader.ReadSequence();
                        string oid = tavReader.ReadObjectIdentifier();
                        string attributeValue = tavReader.ReadAnyAsnString();
                        if (oid == OrganizationalUnit)
                        {
                            return attributeValue;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}
