// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct GeneralNameAsn
    {
        internal System.Security.Cryptography.Asn1.OtherNameAsn? OtherName;
        internal string? Rfc822Name;
        internal string? DnsName;
        internal ReadOnlyMemory<byte>? X400Address;
        internal ReadOnlyMemory<byte>? DirectoryName;
        internal System.Security.Cryptography.Asn1.EdiPartyNameAsn? EdiPartyName;
        internal string? Uri;
        internal ReadOnlyMemory<byte>? IPAddress;
        internal string? RegisteredId;

#if DEBUG
        static GeneralNameAsn()
        {
            var usedTags = new System.Collections.Generic.Dictionary<Asn1Tag, string>();
            Action<Asn1Tag, string> ensureUniqueTag = (tag, fieldName) =>
            {
                if (usedTags.TryGetValue(tag, out string? existing))
                {
                    throw new InvalidOperationException($"Tag '{tag}' is in use by both '{existing}' and '{fieldName}'");
                }

                usedTags.Add(tag, fieldName);
            };

            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "OtherName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "Rfc822Name");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 2), "DnsName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 3), "X400Address");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 4), "DirectoryName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 5), "EdiPartyName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 6), "Uri");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 7), "IPAddress");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 8), "RegisteredId");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (OtherName.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                OtherName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 0));
                wroteValue = true;
            }

            if (Rfc822Name != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteCharacterString(UniversalTagNumber.IA5String, Rfc822Name, new Asn1Tag(TagClass.ContextSpecific, 1));
                wroteValue = true;
            }

            if (DnsName != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteCharacterString(UniversalTagNumber.IA5String, DnsName, new Asn1Tag(TagClass.ContextSpecific, 2));
                wroteValue = true;
            }

            if (X400Address.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for X400Address
                {
                    if (!Asn1Tag.TryDecode(X400Address.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(X400Address.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (DirectoryName.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
                try
                {
                    writer.WriteEncodedValue(DirectoryName.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
                wroteValue = true;
            }

            if (EdiPartyName.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                EdiPartyName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 5));
                wroteValue = true;
            }

            if (Uri != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteCharacterString(UniversalTagNumber.IA5String, Uri, new Asn1Tag(TagClass.ContextSpecific, 6));
                wroteValue = true;
            }

            if (IPAddress.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(IPAddress.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 7));
                wroteValue = true;
            }

            if (RegisteredId != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                try
                {
                    writer.WriteObjectIdentifier(RegisteredId, new Asn1Tag(TagClass.ContextSpecific, 8));
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static GeneralNameAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out GeneralNameAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, rebind, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ValueAsnReader explicitReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                System.Security.Cryptography.Asn1.OtherNameAsn tmpOtherName;
                System.Security.Cryptography.Asn1.OtherNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 0), rebind, out tmpOtherName);
                decoded.OtherName = tmpOtherName;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.Rfc822Name = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                decoded.DnsName = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.X400Address = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 4)))
            {
                explicitReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
                tmpSpan = explicitReader.ReadEncodedValue();
                decoded.DirectoryName = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                explicitReader.ThrowIfNotEmpty();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 5)))
            {
                System.Security.Cryptography.Asn1.EdiPartyNameAsn tmpEdiPartyName;
                System.Security.Cryptography.Asn1.EdiPartyNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 5), rebind, out tmpEdiPartyName);
                decoded.EdiPartyName = tmpEdiPartyName;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
            {
                decoded.Uri = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 7)))
                {
                    decoded.IPAddress = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.IPAddress = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
                }

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 8)))
            {
                decoded.RegisteredId = reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueGeneralNameAsn
    {
        internal ReadOnlySpan<byte> OtherName;
        internal bool HasOtherName;
        internal string? Rfc822Name;
        internal string? DnsName;
        internal ReadOnlySpan<byte> X400Address;
        internal bool HasX400Address;
        internal ReadOnlySpan<byte> DirectoryName;
        internal bool HasDirectoryName;
        internal ReadOnlySpan<byte> EdiPartyName;
        internal bool HasEdiPartyName;
        internal string? Uri;
        internal ReadOnlySpan<byte> IPAddress;
        internal bool HasIPAddress;
        internal string? RegisteredId;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueGeneralNameAsn decoded)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                DecodeCore(ref reader, out decoded);
                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueGeneralNameAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(scoped ref ValueAsnReader reader, out ValueGeneralNameAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ValueAsnReader explicitReader;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                decoded.OtherName = reader.ReadEncodedValue();
                decoded.HasOtherName = true;
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.Rfc822Name = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                decoded.DnsName = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                if (!reader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
                {
                    throw new CryptographicException();
                }

                decoded.X400Address = reader.ReadEncodedValue();
                decoded.HasX400Address = true;
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 4)))
            {
                explicitReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
                decoded.DirectoryName = explicitReader.ReadEncodedValue();
                decoded.HasDirectoryName = true;
                explicitReader.ThrowIfNotEmpty();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 5)))
            {
                decoded.EdiPartyName = reader.ReadEncodedValue();
                decoded.HasEdiPartyName = true;
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
            {
                decoded.Uri = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 7)))
                {
                    decoded.IPAddress = tmpSpan;
                }
                else
                {
                    decoded.IPAddress = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
                }

                decoded.HasIPAddress = true;
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 8)))
            {
                decoded.RegisteredId = reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
