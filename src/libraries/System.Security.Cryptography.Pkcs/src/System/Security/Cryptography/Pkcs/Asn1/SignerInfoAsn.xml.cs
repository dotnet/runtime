// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SignerInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.SignerIdentifierAsn Sid;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn DigestAlgorithm;
        internal ReadOnlyMemory<byte>? SignedAttributes;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;
        internal ReadOnlyMemory<byte> SignatureValue;
        internal System.Security.Cryptography.Asn1.AttributeAsn[] UnsignedAttributes;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);
            Sid.Encode(writer);
            DigestAlgorithm.Encode(writer);

            if (SignedAttributes.HasValue)
            {
                // Validator for tag constraint for SignedAttributes
                {
                    if (!Asn1Tag.TryDecode(SignedAttributes.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        throw new CryptographicException();
                    }
                }

                writer.WriteEncodedValue(SignedAttributes.Value.Span);
            }

            SignatureAlgorithm.Encode(writer);
            writer.WriteOctetString(SignatureValue.Span);

            if (UnsignedAttributes != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < UnsignedAttributes.Length; i++)
                {
                    UnsignedAttributes[i].Encode(writer); 
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static SignerInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static SignerInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out SignerInfoAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out SignerInfoAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out SignerInfoAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader collectionReader;
            

            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.SignerIdentifierAsn.Decode(sequenceReader, out decoded.Sid);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.DigestAlgorithm);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                decoded.SignedAttributes = sequenceReader.ReadEncodedValue();
            }

            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.SignatureAlgorithm);

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpSignatureValue))
            {
                decoded.SignatureValue = tmpSignatureValue;
            }
            else
            {
                decoded.SignatureValue = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for UnsignedAttributes
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.UnsignedAttributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
