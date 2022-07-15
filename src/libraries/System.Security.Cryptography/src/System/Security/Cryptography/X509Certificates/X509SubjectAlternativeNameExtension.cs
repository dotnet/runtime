// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509SubjectAlternativeNameExtension : X509Extension
    {
        private List<GeneralNameAsn>? _decoded;

        public X509SubjectAlternativeNameExtension() : base(Oids.SubjectAltName)
        {
            _decoded = new List<GeneralNameAsn>(0);
        }

        public X509SubjectAlternativeNameExtension(byte[] rawData, bool critical = false)
            : base(Oids.SubjectAltName, rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        public X509SubjectAlternativeNameExtension(ReadOnlySpan<byte> rawData, bool critical = false)
            : base(Oids.SubjectAltName, rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = null;
        }

        public IEnumerable<string> EnumerateDnsNames()
        {
            List<GeneralNameAsn> decoded = (_decoded ??= Decode(RawData));

            return EnumerateDnsNames(decoded);
        }

        private static IEnumerable<string> EnumerateDnsNames(List<GeneralNameAsn> decoded)
        {
            foreach (GeneralNameAsn item in decoded)
            {
                if (item.DnsName is not null)
                {
                    yield return item.DnsName;
                }
            }
        }

        public IEnumerable<IPAddress> EnumerateIPAddresses()
        {
            List<GeneralNameAsn> decoded = (_decoded ??= Decode(RawData));

            return EnumerateIPAddresses(decoded);
        }

        private static IEnumerable<IPAddress> EnumerateIPAddresses(List<GeneralNameAsn> decoded)
        {
            foreach (GeneralNameAsn item in decoded)
            {
                if (item.IPAddress.HasValue)
                {
                    ReadOnlySpan<byte> value = item.IPAddress.GetValueOrDefault().Span;

                    if (value.Length is 4 or 16)
                    {
                        yield return new IPAddress(value);
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_X509_SAN_UnknownIPAddressSize);
                    }
                }
            }
        }

        private static List<GeneralNameAsn> Decode(ReadOnlySpan<byte> rawData)
        {
            try
            {
                AsnValueReader outer = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader sequence = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                List<GeneralNameAsn> decoded = new List<GeneralNameAsn>();

                while (sequence.HasData)
                {
                    GeneralNameAsn.Decode(ref sequence, default, out GeneralNameAsn item);
                    decoded.Add(item);
                }

                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
