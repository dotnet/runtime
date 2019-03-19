// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EssCertIdV2
    {
        private static byte[] s_defaultHashAlgorithm = { 0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01 };
  
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashAlgorithm;
        internal ReadOnlyMemory<byte> Hash;
        internal System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial? IssuerSerial;
      
#if DEBUG  
        static EssCertIdV2()
        {
            EssCertIdV2 decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultHashAlgorithm, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out decoded.HashAlgorithm);
            reader.ThrowIfNotEmpty();
        }
#endif
 
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
        
            // DEFAULT value handler for HashAlgorithm.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    HashAlgorithm.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultHashAlgorithm))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }

            writer.WriteOctetString(Hash.Span);

            if (IssuerSerial.HasValue)
            {
                IssuerSerial.Value.Encode(writer);
            }

            writer.PopSequence(tag);
        }

        internal static EssCertIdV2 Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static EssCertIdV2 Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out EssCertIdV2 decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out EssCertIdV2 decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out EssCertIdV2 decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader defaultReader;
            

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.HashAlgorithm);
            }
            else
            {
                defaultReader = new AsnReader(s_defaultHashAlgorithm, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(defaultReader, out decoded.HashAlgorithm);
            }


            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpHash))
            {
                decoded.Hash = tmpHash;
            }
            else
            {
                decoded.Hash = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial tmpIssuerSerial;
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial.Decode(sequenceReader, out tmpIssuerSerial);
                decoded.IssuerSerial = tmpIssuerSerial;

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
