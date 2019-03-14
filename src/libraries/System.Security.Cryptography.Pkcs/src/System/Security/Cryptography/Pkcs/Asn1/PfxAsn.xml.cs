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
    internal partial struct PfxAsn
    {
        internal byte Version;
        internal System.Security.Cryptography.Pkcs.Asn1.ContentInfoAsn AuthSafe;
        internal System.Security.Cryptography.Pkcs.Asn1.MacData? MacData;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);
            AuthSafe.Encode(writer);

            if (MacData.HasValue)
            {
                MacData.Value.Encode(writer);
            }

            writer.PopSequence(tag);
        }

        internal static PfxAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static PfxAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out PfxAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out PfxAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out PfxAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (!sequenceReader.TryReadUInt8(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.ContentInfoAsn.Decode(sequenceReader, out decoded.AuthSafe);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.MacData tmpMacData;
                System.Security.Cryptography.Pkcs.Asn1.MacData.Decode(sequenceReader, out tmpMacData);
                decoded.MacData = tmpMacData;

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
