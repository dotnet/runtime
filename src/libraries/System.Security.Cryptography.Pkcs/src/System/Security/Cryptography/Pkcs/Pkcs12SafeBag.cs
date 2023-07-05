// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public abstract class Pkcs12SafeBag
    {
        private readonly string _bagIdValue;
        private Oid? _bagOid;
        private CryptographicAttributeObjectCollection? _attributes;

        public ReadOnlyMemory<byte> EncodedBagValue { get; }

        public CryptographicAttributeObjectCollection Attributes
        {
            get
            {
                _attributes ??= new CryptographicAttributeObjectCollection();

                return _attributes;
            }

            internal set
            {
                Debug.Assert(value != null);
                _attributes = value;
            }
        }

        protected Pkcs12SafeBag(string bagIdValue, ReadOnlyMemory<byte> encodedBagValue, bool skipCopy = false)
        {
            if (string.IsNullOrEmpty(bagIdValue))
                throw new ArgumentNullException(nameof(bagIdValue));

            // Read to ensure that there is precisely one legally encoded value.
            PkcsHelpers.EnsureSingleBerValue(encodedBagValue.Span);

            _bagIdValue = bagIdValue;
            EncodedBagValue = skipCopy ? encodedBagValue : encodedBagValue.ToArray();
        }

        public byte[] Encode()
        {
            AsnWriter writer = EncodeToNewWriter();
            return writer.Encode();
        }

        public Oid GetBagId()
        {
            _bagOid ??= new Oid(_bagIdValue);

            return _bagOid.CopyOid();
        }

        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter writer = EncodeToNewWriter();
            return writer.TryEncode(destination, out bytesWritten);
        }

        internal void EncodeTo(AsnWriter writer)
        {
            writer.PushSequence();

            writer.WriteObjectIdentifierForCrypto(_bagIdValue);

            Asn1Tag contextSpecific0 = new Asn1Tag(TagClass.ContextSpecific, 0);
            writer.PushSequence(contextSpecific0);
            writer.WriteEncodedValueForCrypto(EncodedBagValue.Span);
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
            EncodeTo(writer);
            return writer;
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
