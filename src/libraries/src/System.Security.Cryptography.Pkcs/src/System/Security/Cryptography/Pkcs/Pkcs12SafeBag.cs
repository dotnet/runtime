// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs
{
    public abstract class Pkcs12SafeBag
    {
        private readonly string _bagIdValue;
        private Oid _bagOid;
        private CryptographicAttributeObjectCollection _attributes;

        public ReadOnlyMemory<byte> EncodedBagValue { get; }

        public CryptographicAttributeObjectCollection Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new CryptographicAttributeObjectCollection();
                }

                return _attributes;
            }

            internal set
            {
                Debug.Assert(value != null);
                _attributes = value;
            }
        }

        protected Pkcs12SafeBag(string bagIdValue, ReadOnlyMemory<byte> encodedBagValue, bool skipCopy=false)
        {
            if (string.IsNullOrEmpty(bagIdValue))
                throw new ArgumentNullException(nameof(bagIdValue));

            // Read to ensure that there is precisely one legally encoded value.
            AsnReader reader = new AsnReader(encodedBagValue, AsnEncodingRules.BER);
            reader.ReadEncodedValue();
            reader.ThrowIfNotEmpty();

            _bagIdValue = bagIdValue;
            EncodedBagValue = skipCopy ? encodedBagValue : encodedBagValue.ToArray();
        }

        public byte[] Encode()
        {
            using (AsnWriter writer = EncodeToNewWriter())
            {
                return writer.Encode();
            }
        }

        public Oid GetBagId()
        {
            if (_bagOid == null)
            {
                _bagOid = new Oid(_bagIdValue);
            }

            return new Oid(_bagOid);
        }

        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            using (AsnWriter writer = EncodeToNewWriter())
            {
                ReadOnlySpan<byte> encoded = writer.EncodeAsSpan();

                if (destination.Length < encoded.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                encoded.CopyTo(destination);
                bytesWritten = encoded.Length;
                return true;
            }
        }
        
        internal void EncodeTo(AsnWriter writer)
        {
            writer.PushSequence();

            writer.WriteObjectIdentifier(_bagIdValue);

            Asn1Tag contextSpecific0 = new Asn1Tag(TagClass.ContextSpecific, 0);
            writer.PushSequence(contextSpecific0);
            writer.WriteEncodedValue(EncodedBagValue.Span);
            writer.PopSequence(contextSpecific0);

            if (_attributes?.Count > 0)
            {
                List<AttributeAsn> attrs = CmsSigner.BuildAttributes(_attributes);

                writer.PushSetOf();

                foreach (AttributeAsn attr in attrs)
                {
                    attr.Encode(writer);
                }

                writer.PopSetOf();
            }

            writer.PopSequence();
        }

        private AsnWriter EncodeToNewWriter()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

            try
            {
                EncodeTo(writer);
                return writer;
            }
            catch
            {
                writer.Dispose();
                throw;
            }
        }

        internal sealed class UnknownBag : Pkcs12SafeBag
        {
            internal UnknownBag(string oidValue, ReadOnlyMemory<byte> bagValue)
                : base(oidValue, bagValue)
            {
            }
        }
    }
}
