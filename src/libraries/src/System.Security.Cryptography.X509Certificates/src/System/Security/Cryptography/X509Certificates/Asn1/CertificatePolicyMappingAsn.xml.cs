// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificatePolicyMappingAsn
    {
        internal string IssuerDomainPolicy;
        internal string SubjectDomainPolicy;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteObjectIdentifier(IssuerDomainPolicy);
            writer.WriteObjectIdentifier(SubjectDomainPolicy);
            writer.PopSequence(tag);
        }

        internal static CertificatePolicyMappingAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static CertificatePolicyMappingAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out CertificatePolicyMappingAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out CertificatePolicyMappingAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out CertificatePolicyMappingAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            
            decoded.IssuerDomainPolicy = sequenceReader.ReadObjectIdentifierAsString();
            decoded.SubjectDomainPolicy = sequenceReader.ReadObjectIdentifierAsString();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
