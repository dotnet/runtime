// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct TbsCertificateAsn
    {
        private static byte[] s_defaultVersion = { 0x02, 0x01, 0x00 };
  
        internal int Version;
        internal ReadOnlyMemory<byte> SerialNumber;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;
        internal ReadOnlyMemory<byte> Issuer;
        internal System.Security.Cryptography.X509Certificates.Asn1.ValidityAsn Validity;
        internal ReadOnlyMemory<byte> Subject;
        internal System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;
        internal ReadOnlyMemory<byte>? IssuerUniqueId;
        internal ReadOnlyMemory<byte>? SubjectUniqueId;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[] Extensions;
      
#if DEBUG  
        static TbsCertificateAsn()
        {
            TbsCertificateAsn decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultVersion, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.Version))
            {
                reader.ThrowIfNotEmpty();
            }

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
            

            // DEFAULT value handler for Version.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteInteger(Version);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultVersion))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                        writer.WriteEncodedValue(encoded.ToArray());
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
                }
            }

            writer.WriteInteger(SerialNumber.Span);
            SignatureAlgorithm.Encode(writer);
            // Validator for tag constraint for Issuer
            {
                if (!Asn1Tag.TryDecode(Issuer.Span, out Asn1Tag validateTag, out _) ||
                    !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                {
                    throw new CryptographicException();
                }
            }

            writer.WriteEncodedValue(Issuer.Span);
            Validity.Encode(writer);
            // Validator for tag constraint for Subject
            {
                if (!Asn1Tag.TryDecode(Subject.Span, out Asn1Tag validateTag, out _) ||
                    !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                {
                    throw new CryptographicException();
                }
            }

            writer.WriteEncodedValue(Subject.Span);
            SubjectPublicKeyInfo.Encode(writer);

            if (IssuerUniqueId.HasValue)
            {
                writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 1), IssuerUniqueId.Value.Span);
            }


            if (SubjectUniqueId.HasValue)
            {
                writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 2), SubjectUniqueId.Value.Span);
            }


            if (Extensions != null)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));

                writer.PushSequence();
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer); 
                }
                writer.PopSequence();

                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
            }

            writer.PopSequence(tag);
        }

        internal static TbsCertificateAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static TbsCertificateAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out TbsCertificateAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out TbsCertificateAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out TbsCertificateAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader explicitReader;
            AsnReader defaultReader;
            AsnReader collectionReader;
            

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

                if (!explicitReader.TryReadInt32(out decoded.Version))
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultVersion, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.Version))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }

            decoded.SerialNumber = sequenceReader.ReadIntegerBytes();
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.SignatureAlgorithm);
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            decoded.Issuer = sequenceReader.ReadEncodedValue();
            System.Security.Cryptography.X509Certificates.Asn1.ValidityAsn.Decode(sequenceReader, out decoded.Validity);
            if (!sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                throw new CryptographicException();
            }

            decoded.Subject = sequenceReader.ReadEncodedValue();
            System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(sequenceReader, out decoded.SubjectPublicKeyInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(new Asn1Tag(TagClass.ContextSpecific, 1), out _, out ReadOnlyMemory<byte> tmpIssuerUniqueId))
                {
                    decoded.IssuerUniqueId = tmpIssuerUniqueId;
                }
                else
                {
                    decoded.IssuerUniqueId = sequenceReader.ReadBitString(new Asn1Tag(TagClass.ContextSpecific, 1), out _);
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(new Asn1Tag(TagClass.ContextSpecific, 2), out _, out ReadOnlyMemory<byte> tmpSubjectUniqueId))
                {
                    decoded.SubjectUniqueId = tmpSubjectUniqueId;
                }
                else
                {
                    decoded.SubjectUniqueId = sequenceReader.ReadBitString(new Asn1Tag(TagClass.ContextSpecific, 2), out _);
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = explicitReader.ReadSequence();
                    var tmpList = new List<System.Security.Cryptography.Asn1.X509ExtensionAsn>();
                    System.Security.Cryptography.Asn1.X509ExtensionAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.X509ExtensionAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.Extensions = tmpList.ToArray();
                }

                explicitReader.ThrowIfNotEmpty();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
