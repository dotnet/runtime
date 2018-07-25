// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
            internal string AlgorithmId;
            internal byte[] Parameters;
        }

        internal byte[] RawData;
        internal byte[] SubjectPublicKeyInfo;

        internal int Version;
        internal byte[] SerialNumber;
        internal AlgorithmIdentifier TbsSignature;
        internal X500DistinguishedName Issuer;
        internal DateTime NotBefore;
        internal DateTime NotAfter;
        internal X500DistinguishedName Subject;
        internal AlgorithmIdentifier PublicKeyAlgorithm;
        internal byte[] PublicKey;
        internal byte[] IssuerUniqueId;
        internal byte[] SubjectUniqueId;
        internal List<X509Extension> Extensions;
        internal AlgorithmIdentifier SignatureAlgorithm;
        internal byte[] SignatureValue;

        internal CertificateData(byte[] rawData)
        {
#if DEBUG
        try
        {
#endif
            DerSequenceReader reader = new DerSequenceReader(rawData);

            DerSequenceReader tbsCertificate = reader.ReadSequence();

            if (tbsCertificate.PeekTag() == DerSequenceReader.ContextSpecificConstructedTag0)
            {
                DerSequenceReader version = tbsCertificate.ReadSequence();
                Version = version.ReadInteger();
            }
            else if (tbsCertificate.PeekTag() != (byte)DerSequenceReader.DerTag.Integer)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
            else
            {
                Version = 0;
            }

            if (Version < 0 || Version > 2)
                throw new CryptographicException();

            SerialNumber = tbsCertificate.ReadIntegerBytes();

            DerSequenceReader tbsSignature = tbsCertificate.ReadSequence();
            TbsSignature.AlgorithmId = tbsSignature.ReadOidAsString();
            TbsSignature.Parameters = tbsSignature.HasData ? tbsSignature.ReadNextEncodedValue() : Array.Empty<byte>();

            if (tbsSignature.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            Issuer = new X500DistinguishedName(tbsCertificate.ReadNextEncodedValue());

            DerSequenceReader validity = tbsCertificate.ReadSequence();
            NotBefore = validity.ReadX509Date();
            NotAfter = validity.ReadX509Date();

            if (validity.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            Subject = new X500DistinguishedName(tbsCertificate.ReadNextEncodedValue());

            SubjectPublicKeyInfo = tbsCertificate.ReadNextEncodedValue();
            DerSequenceReader subjectPublicKeyInfo = new DerSequenceReader(SubjectPublicKeyInfo);
            DerSequenceReader subjectKeyAlgorithm = subjectPublicKeyInfo.ReadSequence();
            PublicKeyAlgorithm.AlgorithmId = subjectKeyAlgorithm.ReadOidAsString();
            PublicKeyAlgorithm.Parameters = subjectKeyAlgorithm.HasData ? subjectKeyAlgorithm.ReadNextEncodedValue() : Array.Empty<byte>();

            if (subjectKeyAlgorithm.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            PublicKey = subjectPublicKeyInfo.ReadBitString();

            if (subjectPublicKeyInfo.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            if (Version > 0 &&
                tbsCertificate.HasData &&
                tbsCertificate.PeekTag() == DerSequenceReader.ContextSpecificConstructedTag1)
            {
                IssuerUniqueId = tbsCertificate.ReadBitString();
            }
            else
            {
                IssuerUniqueId = null;
            }

            if (Version > 0 &&
                tbsCertificate.HasData &&
                tbsCertificate.PeekTag() == DerSequenceReader.ContextSpecificConstructedTag2)
            {
                SubjectUniqueId = tbsCertificate.ReadBitString();
            }
            else
            {
                SubjectUniqueId = null;
            }

            Extensions = new List<X509Extension>();

            if (Version > 1 &&
                tbsCertificate.HasData &&
                tbsCertificate.PeekTag() == DerSequenceReader.ContextSpecificConstructedTag3)
            {
                DerSequenceReader extensions = tbsCertificate.ReadSequence();
                extensions = extensions.ReadSequence();

                while (extensions.HasData)
                {
                    DerSequenceReader extensionReader = extensions.ReadSequence();
                    string oid = extensionReader.ReadOidAsString();
                    bool critical = false;

                    if (extensionReader.PeekTag() == (byte)DerSequenceReader.DerTag.Boolean)
                    {
                        critical = extensionReader.ReadBoolean();
                    }

                    byte[] extensionData = extensionReader.ReadOctetString();

                    Extensions.Add(new X509Extension(oid, extensionData, critical));

                    if (extensionReader.HasData)
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            if (tbsCertificate.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            DerSequenceReader signatureAlgorithm = reader.ReadSequence();
            SignatureAlgorithm.AlgorithmId = signatureAlgorithm.ReadOidAsString();
            SignatureAlgorithm.Parameters = signatureAlgorithm.HasData ? signatureAlgorithm.ReadNextEncodedValue() : Array.Empty<byte>();

            if (signatureAlgorithm.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            SignatureValue = reader.ReadBitString();

            if (reader.HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            RawData = rawData;
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

            // SubjectAltName ::= GeneralNames
            //
            // IssuerAltName ::= GeneralNames
            //
            // GeneralNames ::= SEQUENCE SIZE (1..MAX) OF GeneralName
            //
            // GeneralName ::= CHOICE {
            //   otherName                       [0]     OtherName,
            //   rfc822Name                      [1]     IA5String,
            //   dNSName                         [2]     IA5String,
            //   x400Address                     [3]     ORAddress,
            //   directoryName                   [4]     Name,
            //   ediPartyName                    [5]     EDIPartyName,
            //   uniformResourceIdentifier       [6]     IA5String,
            //   iPAddress                       [7]     OCTET STRING,
            //   registeredID                    [8]     OBJECT IDENTIFIER }
            //
            // OtherName::= SEQUENCE {
            //   type - id    OBJECT IDENTIFIER,
            //   value[0] EXPLICIT ANY DEFINED BY type - id }

            byte expectedTag = (byte)(DerSequenceReader.ContextSpecificTagFlag | (byte)matchType);

            if (matchType == GeneralNameType.OtherName)
            {
                expectedTag |= DerSequenceReader.ConstructedFlag;
            }

            DerSequenceReader altNameReader = new DerSequenceReader(extensionBytes);

            while (altNameReader.HasData)
            {
                if (altNameReader.PeekTag() != expectedTag)
                {
                    altNameReader.SkipValue();
                    continue;
                }

                switch (matchType)
                {
                    case GeneralNameType.OtherName:
                    {
                        DerSequenceReader otherNameReader = altNameReader.ReadSequence();
                        string oid = otherNameReader.ReadOidAsString();

                        if (oid == otherOid)
                        {
                            // Payload is value[0] EXPLICIT, meaning
                            // a) it'll be tagged as ContextSpecific0
                            // b) that's interpretable as a Sequence (EXPLICIT)
                            // c) the payload will then be retagged as the correct type (EXPLICIT)
                            if (otherNameReader.PeekTag() != DerSequenceReader.ContextSpecificConstructedTag0)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }

                            otherNameReader = otherNameReader.ReadSequence();

                            // Currently only UPN is supported, which is a UTF8 string per
                            // https://msdn.microsoft.com/en-us/library/ff842518.aspx
                            return otherNameReader.ReadUtf8String();
                        }

                        // If the OtherName OID didn't match, move to the next entry.
                        continue;
                    }
                    case GeneralNameType.Rfc822Name:
                    case GeneralNameType.DnsName:
                    case GeneralNameType.UniformResourceIdentifier:
                        return altNameReader.ReadIA5String();
                    default:
                        altNameReader.SkipValue();
                        continue;
                }
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadReverseRdns(X500DistinguishedName name)
        {
            DerSequenceReader x500NameReader = new DerSequenceReader(name.RawData);
            var rdnReaders = new Stack<DerSequenceReader>();

            while (x500NameReader.HasData)
            {
                rdnReaders.Push(x500NameReader.ReadSet());
            }


            while (rdnReaders.Count > 0)
            {
                DerSequenceReader rdnReader = rdnReaders.Pop();

                while (rdnReader.HasData)
                {
                    DerSequenceReader tavReader = rdnReader.ReadSequence();
                    string oid = tavReader.ReadOidAsString();

                    var tag = (DerSequenceReader.DerTag)tavReader.PeekTag();
                    string value = null;

                    switch (tag)
                    {
                        case DerSequenceReader.DerTag.BMPString:
                            value = tavReader.ReadBMPString();
                            break;
                        case DerSequenceReader.DerTag.IA5String:
                            value = tavReader.ReadIA5String();
                            break;
                        case DerSequenceReader.DerTag.PrintableString:
                            value = tavReader.ReadPrintableString();
                            break;
                        case DerSequenceReader.DerTag.UTF8String:
                            value = tavReader.ReadUtf8String();
                            break;
                        case DerSequenceReader.DerTag.T61String:
                            value = tavReader.ReadT61String();
                            break;

                        // Ignore anything we don't know how to read.
                    }

                    if (value != null)
                    {
                        yield return new KeyValuePair<string, string>(oid, value);
                    }
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
