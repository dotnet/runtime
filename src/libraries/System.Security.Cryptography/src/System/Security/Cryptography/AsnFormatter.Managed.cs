// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Text;

namespace System.Security.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        private static readonly AsnFormatter s_instance = new ManagedAsnFormatter();
    }

    internal sealed class ManagedAsnFormatter : AsnFormatter
    {
        protected override string? FormatNative(Oid? oid, byte[] rawData, bool multiLine)
        {
            if (oid == null || string.IsNullOrEmpty(oid.Value))
            {
                return EncodeSpaceSeparatedHexString(rawData);
            }

            switch (oid.Value)
            {
                case "2.5.29.17":
                    return FormatSubjectAlternativeName(rawData);
            }

            return null;
        }

        private static string? FormatSubjectAlternativeName(byte[] rawData)
        {
            // Because SubjectAlternativeName is a commonly parsed structure, we'll
            // specifically format this one.  And we'll match the OpenSSL format, which
            // includes not localizing any of the values (or respecting the multiLine boolean)
            //
            // The intent here is to be functionally equivalent to OpenSSL GENERAL_NAME_print.

            try
            {
                StringBuilder output = new StringBuilder();
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader collectionReader = reader.ReadSequence();

                reader.ThrowIfNotEmpty();

                while (collectionReader.HasData)
                {
                    GeneralNameAsn.Decode(ref collectionReader, rawData, out GeneralNameAsn generalName);

                    if (output.Length != 0)
                    {
                        output.Append(", ");
                    }

                    if (generalName.OtherName.HasValue)
                    {
                        output.Append("othername:<unsupported>");
                    }
                    else if (generalName.Rfc822Name != null)
                    {
                        output.Append("email:");
                        output.Append(generalName.Rfc822Name);
                    }
                    else if (generalName.DnsName != null)
                    {
                        output.Append("DNS:");
                        output.Append(generalName.DnsName);
                    }
                    else if (generalName.X400Address != null)
                    {
                        output.Append("X400Name:<unsupported>");
                    }
                    else if (generalName.DirectoryName != null)
                    {
                        // OpenSSL supports printing one of these, but the logic lives in X509Certificates,
                        // and it isn't very common.  So we'll skip this one until someone asks for it.
                        output.Append("DirName:<unsupported>");
                    }
                    else if (generalName.EdiPartyName != null)
                    {
                        output.Append("EdiPartyName:<unsupported>");
                    }
                    else if (generalName.Uri != null)
                    {
                        output.Append("URI:");
                        output.Append(generalName.Uri);
                    }
                    else if (generalName.IPAddress.HasValue)
                    {
                        ReadOnlySpan<byte> ipAddressBytes = generalName.IPAddress.Value.Span;

                        output.Append("IP Address");
                        if (ipAddressBytes.Length == 4)
                        {
                            // Add the colon and dotted-decimal representation of IPv4.
                            output.Append(
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
                                output.Append($":{ipAddressBytes[i] << 8 | ipAddressBytes[i + 1]:X}");
                            }
                        }
                        else
                        {
                            output.Append(":<invalid>");
                        }
                    }
                    else if (generalName.RegisteredId != null)
                    {
                        output.Append("Registered ID:");
                        output.Append(generalName.RegisteredId);
                    }
                    else
                    {
                        // A new extension to GeneralName could legitimately hit this,
                        // but it's correct to say that until we know what that is that
                        // the pretty-print has failed, and we should fall back to hex.
                        //
                        // But it could also simply be poorly encoded user data.
                        return null;
                    }
                }

                return output.ToString();
            }
            catch (AsnContentException)
            {
                return null;
            }
        }
    }
}
