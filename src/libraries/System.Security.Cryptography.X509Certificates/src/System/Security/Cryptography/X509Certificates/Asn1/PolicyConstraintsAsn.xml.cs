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
    internal partial struct PolicyConstraintsAsn
    {
        internal int? RequireExplicitPolicyDepth;
        internal int? InhibitMappingDepth;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            

            if (RequireExplicitPolicyDepth.HasValue)
            {
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 0), RequireExplicitPolicyDepth.Value);
            }


            if (InhibitMappingDepth.HasValue)
            {
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 1), InhibitMappingDepth.Value);
            }

            writer.PopSequence(tag);
        }

        internal static PolicyConstraintsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static PolicyConstraintsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out PolicyConstraintsAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out PolicyConstraintsAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out PolicyConstraintsAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (sequenceReader.TryReadInt32(new Asn1Tag(TagClass.ContextSpecific, 0), out int tmpRequireExplicitPolicyDepth))
                {
                    decoded.RequireExplicitPolicyDepth = tmpRequireExplicitPolicyDepth;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                if (sequenceReader.TryReadInt32(new Asn1Tag(TagClass.ContextSpecific, 1), out int tmpInhibitMappingDepth))
                {
                    decoded.InhibitMappingDepth = tmpInhibitMappingDepth;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
