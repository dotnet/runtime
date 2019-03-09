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
    internal partial struct CadesIssuerSerial
    {
        internal System.Security.Cryptography.Asn1.GeneralNameAsn[] Issuer;
        internal ReadOnlyMemory<byte> SerialNumber;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            

            writer.PushSequence();
            for (int i = 0; i < Issuer.Length; i++)
            {
                Issuer[i].Encode(writer); 
            }
            writer.PopSequence();

            writer.WriteInteger(SerialNumber.Span);
            writer.PopSequence(tag);
        }

        internal static CadesIssuerSerial Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static CadesIssuerSerial Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out CadesIssuerSerial decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out CadesIssuerSerial decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out CadesIssuerSerial decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader collectionReader;
            

            // Decode SEQUENCE OF for Issuer
            {
                collectionReader = sequenceReader.ReadSequence();
                var tmpList = new List<System.Security.Cryptography.Asn1.GeneralNameAsn>();
                System.Security.Cryptography.Asn1.GeneralNameAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(collectionReader, out tmpItem); 
                    tmpList.Add(tmpItem);
                }

                decoded.Issuer = tmpList.ToArray();
            }

            decoded.SerialNumber = sequenceReader.ReadIntegerBytes();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
