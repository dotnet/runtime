// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        private static readonly AsnFormatter s_instance = new AppleAsnFormatter();

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }

    internal class AppleAsnFormatter : AsnFormatter
    {
        protected override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
        {
            if (oid == null || string.IsNullOrEmpty(oid.Value))
            {
                return EncodeHexString(rawData, true);
            }

            switch (oid.Value)
            {
                case "2.5.29.17":
                    return FormatSubjectAlternativeName(rawData);
            }

            return null;
        }

        private const string CommaSpace = ", ";

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

        private string FormatSubjectAlternativeName(byte[] rawData)
        {
            // Because SubjectAlternativeName is a commonly parsed structure, we'll
            // specifically format this one.  And we'll match the OpenSSL format, which
            // includes not localizing any of the values (or respecting the multiLine boolean)
            //
            // The intent here is to be functionally equivalent to OpenSSL GENERAL_NAME_print.

            // The end size of this string is hard to predict.
            // * dNSName values have a tag that takes four characters to represent ("DNS:")
            //   and then their payload is ASCII encoded (so one byte -> one char), so they
            //   work out to be about equal (in chars) to their DER encoded length (in bytes).
            // * iPAddress values have a tag that takes 11 characters ("IP Address:") and then
            //   grow from 4 bytes to up to 15 characters for IPv4, or 16 bytes to 47 characters
            //   for IPv6
            //
            // So use a List<string> and just Concat them all when we're done, and we reduce the
            // number of times we copy the header values (vs pointers to the header values).
            List<string> segments = new List<string>();

            try
            {
                // SubjectAltName ::= GeneralNames
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
                DerSequenceReader altNameReader = new DerSequenceReader(rawData);

                while (altNameReader.HasData)
                {
                    if (segments.Count != 0)
                    {
                        segments.Add(CommaSpace);
                    }

                    byte tag = altNameReader.PeekTag();

                    if ((tag & DerSequenceReader.ContextSpecificTagFlag) == 0)
                    {
                        // All GeneralName values need the ContextSpecific flag.
                        return null;
                    }

                    GeneralNameType nameType = (GeneralNameType)(tag & DerSequenceReader.TagNumberMask);

                    bool needsConstructedFlag = false;

                    switch (nameType)
                    {
                        case GeneralNameType.OtherName:
                        case GeneralNameType.X400Address:
                        case GeneralNameType.DirectoryName:
                        case GeneralNameType.EdiPartyName:
                            needsConstructedFlag = true;
                            break;
                    }

                    if (needsConstructedFlag &&
                        (tag & DerSequenceReader.ConstructedFlag) == 0)
                    {
                        // All of the SEQUENCE types require the constructed bit,
                        // or OpenSSL will have refused to print it.
                        return null;
                    }

                    switch (nameType)
                    {
                        case GeneralNameType.OtherName:
                            segments.Add("othername:<unsupported>");
                            altNameReader.SkipValue();
                            break;
                        case GeneralNameType.Rfc822Name:
                            segments.Add("email:");
                            segments.Add(altNameReader.ReadIA5String());
                            break;
                        case GeneralNameType.DnsName:
                            segments.Add("DNS:");
                            segments.Add(altNameReader.ReadIA5String());
                            break;
                        case GeneralNameType.X400Address:
                            segments.Add("X400Name:<unsupported>");
                            altNameReader.SkipValue();
                            break;
                        case GeneralNameType.DirectoryName:
                            // OpenSSL supports printing one of these, but the logic lives in X509Certificates,
                            // and it isn't very common.  So we'll skip this one until someone asks for it.
                            segments.Add("DirName:<unsupported>");
                            altNameReader.SkipValue();
                            break;
                        case GeneralNameType.EdiPartyName:
                            segments.Add("EdiPartyName:<unsupported>");
                            altNameReader.SkipValue();
                            break;
                        case GeneralNameType.UniformResourceIdentifier:
                            segments.Add("URI:");
                            segments.Add(altNameReader.ReadIA5String());
                            break;
                        case GeneralNameType.IPAddress:
                            segments.Add("IP Address");

                            byte[] ipAddressBytes = altNameReader.ReadOctetString();

                            if (ipAddressBytes.Length == 4)
                            {
                                // Add the colon and dotted-decimal representation of IPv4.
                                segments.Add(
                                    $":{ipAddressBytes[0]}.{ipAddressBytes[1]}.{ipAddressBytes[2]}.{ipAddressBytes[3]}");
                            }
                            else if (ipAddressBytes.Length == 16)
                            {
                                // Print the IP Address value as colon separated UInt16 hex values without leading zeroes.
                                // 20 01 0D B8 AC 10 FE 01 00 00 00 00 00 00 00 00
                                //
                                // IP Address:2001:DB8:AC10:FE01:0:0:0:0
                                for (int i = 0; i < ipAddressBytes.Length; i += 2)
                                {
                                    segments.Add($":{ipAddressBytes[i] << 8 | ipAddressBytes[i + 1]:X}");
                                }
                            }
                            else
                            {
                                segments.Add(":<invalid>");
                            }

                            break;
                        case GeneralNameType.RegisteredId:
                            segments.Add("Registered ID:");
                            segments.Add(altNameReader.ReadOidAsString());
                            break;
                        default:
                            // A new extension to GeneralName could legitimately hit this,
                            // but it's correct to say that until we know what that is that
                            // the pretty-print has failed, and we should fall back to hex.
                            //
                            // But it could also simply be poorly encoded user data.
                            return null;
                    }
                }

                return string.Concat(segments);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}
