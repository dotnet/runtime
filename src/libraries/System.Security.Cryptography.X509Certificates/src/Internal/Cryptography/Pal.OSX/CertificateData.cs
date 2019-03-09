// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;

namespace Internal.Cryptography.Pal
{
    internal enum GeneralNameType
    {
        OtherName = 0,
        Rfc822Name = 1,
        // RFC 822: Standard for the format of ARPA Internet Text Messages.
        // That means "email", and an RFC 822 Name: "Email address"
        Email = Rfc822Name,
        DnsName = 2,
        X400Address = 3,
        DirectoryName = 4,
        EdiPartyName = 5,
        UniformResourceIdentifier = 6,
        IPAddress = 7,
        RegisteredId = 8,
    }

    internal struct CertificateData
    {
        internal struct AlgorithmIdentifier
        {
            public AlgorithmIdentifier(AlgorithmIdentifierAsn algorithmIdentifier)
            {
                AlgorithmId = algorithmIdentifier.Algorithm.Value;
                Parameters = algorithmIdentifier.Parameters?.ToArray() ?? Array.Empty<byte>();
            }

            internal string AlgorithmId;
            internal byte[] Parameters;
        }

        private CertificateAsn certificate;
        internal byte[] RawData;
        internal byte[] SubjectPublicKeyInfo;
        internal X500DistinguishedName Issuer;
        internal X500DistinguishedName Subject;
        internal List<X509Extension> Extensions;

        internal int Version => certificate.TbsCertificate.Version;

        internal byte[] SerialNumber => certificate.TbsCertificate.SerialNumber.ToArray();

        internal DateTime NotBefore => certificate.TbsCertificate.Validity.NotBefore.GetValue().UtcDateTime;

        internal DateTime NotAfter => certificate.TbsCertificate.Validity.NotAfter.GetValue().UtcDateTime;

        internal AlgorithmIdentifier PublicKeyAlgorithm => new AlgorithmIdentifier(certificate.TbsCertificate.SubjectPublicKeyInfo.Algorithm);

        internal byte[] PublicKey => certificate.TbsCertificate.SubjectPublicKeyInfo.SubjectPublicKey.ToArray();

        internal byte[] IssuerUniqueId => certificate.TbsCertificate.IssuerUniqueId?.ToArray();

        internal byte[] SubjectUniqueId => certificate.TbsCertificate.SubjectUniqueId?.ToArray();

        internal AlgorithmIdentifier SignatureAlgorithm => new AlgorithmIdentifier(certificate.SignatureAlgorithm);

        internal byte[] SignatureValue => certificate.SignatureValue.ToArray();

        internal CertificateData(byte[] rawData)
        {
#if DEBUG
        try
        {
#endif
            RawData = rawData;
            certificate = CertificateAsn.Decode(rawData, AsnEncodingRules.DER);
            certificate.TbsCertificate.ValidateVersion();
            Issuer = new X500DistinguishedName(certificate.TbsCertificate.Issuer.ToArray());
            Subject = new X500DistinguishedName(certificate.TbsCertificate.Subject.ToArray());

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                certificate.TbsCertificate.SubjectPublicKeyInfo.Encode(writer);
                SubjectPublicKeyInfo = writer.Encode();
            }

            Extensions = new List<X509Extension>();
            if (certificate.TbsCertificate.Extensions != null)
            {
                foreach (X509ExtensionAsn rawExtension in certificate.TbsCertificate.Extensions)
                {
                    X509Extension extension = new X509Extension(
                        rawExtension.ExtnId,
                        rawExtension.ExtnValue.ToArray(),
                        rawExtension.Critical);

                    Extensions.Add(extension);
                }
            }
#if DEBUG
        }
        catch (Exception e)
        {
            throw new CryptographicException(
                $"Error in reading certificate:{Environment.NewLine}{PemPrintCert(rawData)}",
                e);
        }
#endif
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            // Algorithm behaviors (pseudocode).  When forIssuer is true, replace "Subject" with "Issuer" and
            // SAN (Subject Alternative Names) with IAN (Issuer Alternative Names).
            //
            // SimpleName: Subject[CN] ?? Subject[OU] ?? Subject[O] ?? Subject[E] ?? Subject.Rdns.FirstOrDefault() ??
            // SAN.Entries.FirstOrDefault(type == GEN_EMAIL);
            // EmailName: SAN.Entries.FirstOrDefault(type == GEN_EMAIL) ?? Subject[E];
            // UpnName: SAN.Entries.FirsOrDefaultt(type == GEN_OTHER && entry.AsOther().OID == szOidUpn).AsOther().Value;
            // DnsName: SAN.Entries.FirstOrDefault(type == GEN_DNS) ?? Subject[CN];
            // DnsFromAlternativeName: SAN.Entries.FirstOrDefault(type == GEN_DNS);
            // UrlName: SAN.Entries.FirstOrDefault(type == GEN_URI);

            if (nameType == X509NameType.SimpleName)
            {
                X500DistinguishedName name = forIssuer ? Issuer : Subject;
                string candidate = GetSimpleNameInfo(name);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            // Check the Subject Alternative Name (or Issuer Alternative Name) for the right value;
            {
                string extensionId = forIssuer ? Oids.IssuerAltName : Oids.SubjectAltName;
                GeneralNameType? matchType = null;
                string otherOid = null;

                // Currently all X509NameType types have a path where they look at the SAN/IAN,
                // but we need to figure out which kind they want.
                switch (nameType)
                {
                    case X509NameType.DnsName:
                    case X509NameType.DnsFromAlternativeName:
                        matchType = GeneralNameType.DnsName;
                        break;
                    case X509NameType.SimpleName:
                    case X509NameType.EmailName:
                        matchType = GeneralNameType.Email;
                        break;
                    case X509NameType.UpnName:
                        matchType = GeneralNameType.OtherName;
                        otherOid = Oids.UserPrincipalName;
                        break;
                    case X509NameType.UrlName:
                        matchType = GeneralNameType.UniformResourceIdentifier;
                        break;
                }

                if (matchType.HasValue)
                {
                    foreach (X509Extension extension in Extensions)
                    {
                        if (extension.Oid.Value == extensionId)
                        {
                            string candidate = FindAltNameMatch(extension.RawData, matchType.Value, otherOid);

                            if (candidate != null)
                            {
                                return candidate;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Fail($"Unresolved matchType for X509NameType.{nameType}");
                }
            }

            // Subject-based fallback
            {
                string expectedKey = null;

                switch (nameType)
                {
                    case X509NameType.EmailName:
                        expectedKey = Oids.EmailAddress;
                        break;
                    case X509NameType.DnsName:
                        // Note: This does not include DnsFromAlternativeName, since
                        // the subject (or issuer) is not the Alternative Name.
                        expectedKey = Oids.CommonName;
                        break;
                }

                if (expectedKey != null)
                {
                    X500DistinguishedName name = forIssuer ? Issuer : Subject;

                    foreach (var kvp in ReadReverseRdns(name))
                    {
                        if (kvp.Key == expectedKey)
                        {
                            return kvp.Value;
                        }
                    }
                }
            }

            return "";
        }

        private static string GetSimpleNameInfo(X500DistinguishedName name)
        {
            string ou = null;
            string o = null;
            string e = null;
            string firstRdn = null;

            foreach (var kvp in ReadReverseRdns(name))
            {
                string oid = kvp.Key;
                string value = kvp.Value;

                // TODO: Check this (and the OpenSSL-using version) if OU/etc are specified more than once.
                // (Compare against Windows)
                switch (oid)
                {
                    case Oids.CommonName:
                        return value;
                    case Oids.OrganizationalUnit:
                        ou = value;
                        break;
                    case Oids.Organization:
                        o = value;
                        break;
                    case Oids.EmailAddress:
                        e = value;
                        break;
                    default:
                        if (firstRdn == null)
                        {
                            firstRdn = value;
                        }

                        break;
                }
            }

            return ou ?? o ?? e ?? firstRdn;
        }

        private static string FindAltNameMatch(byte[] extensionBytes, GeneralNameType matchType, string otherOid)
        {
            // If Other, have OID, else, no OID.
            Debug.Assert(
                (otherOid == null) == (matchType != GeneralNameType.OtherName),
                $"otherOid has incorrect nullarity for matchType {matchType}");

            Debug.Assert(
                matchType == GeneralNameType.UniformResourceIdentifier ||
                matchType == GeneralNameType.DnsName ||
                matchType == GeneralNameType.Email ||
                matchType == GeneralNameType.OtherName,
                $"matchType ({matchType}) is not currently supported");

            Debug.Assert(
                otherOid == null || otherOid == Oids.UserPrincipalName,
                $"otherOid ({otherOid}) is not supported");

            AsnReader reader = new AsnReader(extensionBytes, AsnEncodingRules.DER);
            AsnReader sequenceReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            while (sequenceReader.HasData)
            {
                GeneralNameAsn.Decode(sequenceReader, out GeneralNameAsn generalName);

                switch (matchType)
                {
                    case GeneralNameType.OtherName:
                        // If the OtherName OID didn't match, move to the next entry.
                        if (generalName.OtherName.HasValue && generalName.OtherName.Value.TypeId == otherOid)
                        {
                            // Currently only UPN is supported, which is a UTF8 string per
                            // https://msdn.microsoft.com/en-us/library/ff842518.aspx
                            AsnReader nameReader = new AsnReader(generalName.OtherName.Value.Value, AsnEncodingRules.DER);
                            string udnName = nameReader.ReadCharacterString(UniversalTagNumber.UTF8String);
                            nameReader.ThrowIfNotEmpty();
                            return udnName;
                        }
                        break;

                    case GeneralNameType.Rfc822Name:
                        if (generalName.Rfc822Name != null)
                        {
                            return generalName.Rfc822Name;
                        }
                        break;

                    case GeneralNameType.DnsName:
                        if (generalName.DnsName != null)
                        {
                            return generalName.DnsName;
                        }
                        break;

                    case GeneralNameType.UniformResourceIdentifier:
                        if (generalName.Uri != null)
                        {
                            return generalName.Uri;
                        }
                        break;
                }
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadReverseRdns(X500DistinguishedName name)
        {
            AsnReader x500NameReader = new AsnReader(name.RawData, AsnEncodingRules.DER);
            AsnReader sequenceReader = x500NameReader.ReadSequence();
            var rdnReaders = new Stack<AsnReader>();
            x500NameReader.ThrowIfNotEmpty();

            while (sequenceReader.HasData)
            {
                rdnReaders.Push(sequenceReader.ReadSetOf());
            }

            while (rdnReaders.Count > 0)
            {
                AsnReader rdnReader = rdnReaders.Pop();
                while (rdnReader.HasData)
                {
                    AsnReader tavReader = rdnReader.ReadSequence();
                    string oid = tavReader.ReadObjectIdentifierAsString();
                    string value = tavReader.ReadDirectoryOrIA5String();
                    tavReader.ThrowIfNotEmpty();
                    yield return new KeyValuePair<string, string>(oid, value);
                }
            }
        }

#if DEBUG
        private static string PemPrintCert(byte[] rawData)
        {
            const string PemHeader = "-----BEGIN CERTIFICATE-----";
            const string PemFooter = "-----END CERTIFICATE-----";

            StringBuilder builder = new StringBuilder(PemHeader.Length + PemFooter.Length + rawData.Length * 2);
            builder.Append(PemHeader);
            builder.AppendLine();
            
            builder.Append(Convert.ToBase64String(rawData, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine();
        
            builder.Append(PemFooter);
            builder.AppendLine();

            return builder.ToString();
        }
#endif
    }
}
