// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12SecretBag : Pkcs12SafeBag
    {
        private Oid? _secretTypeOid;
        private readonly SecretBagAsn _decoded;

        public ReadOnlyMemory<byte> SecretValue => _decoded.SecretValue;

        private Pkcs12SecretBag(ReadOnlyMemory<byte> encodedBagValue)
            : base(Oids.Pkcs12SecretBag, encodedBagValue, skipCopy: true)
        {
        }

        internal Pkcs12SecretBag(Oid secretTypeOid, ReadOnlyMemory<byte> secretValue)
            : this(EncodeBagValue(secretTypeOid, secretValue))
        {
            _secretTypeOid = new Oid(secretTypeOid);

            _decoded = SecretBagAsn.Decode(EncodedBagValue, AsnEncodingRules.BER);
        }

        private Pkcs12SecretBag(SecretBagAsn secretBagAsn, ReadOnlyMemory<byte> encodedBagValue)
            : this(encodedBagValue)
        {
            _decoded = secretBagAsn;
        }

        public Oid GetSecretType()
        {
            _secretTypeOid ??= new Oid(_decoded.SecretTypeId);

            return _secretTypeOid.CopyOid();
        }

        private static byte[] EncodeBagValue(Oid secretTypeOid, in ReadOnlyMemory<byte> secretValue)
        {
            Debug.Assert(secretTypeOid != null && secretTypeOid.Value != null);

            SecretBagAsn secretBagAsn = new SecretBagAsn
            {
                SecretTypeId = secretTypeOid.Value,
                SecretValue = secretValue,
            };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            secretBagAsn.Encode(writer);
            return writer.Encode();
        }

        internal static Pkcs12SecretBag DecodeValue(ReadOnlyMemory<byte> bagValue)
        {
            SecretBagAsn decoded = SecretBagAsn.Decode(bagValue, AsnEncodingRules.BER);
            return new Pkcs12SecretBag(decoded, bagValue);
        }
    }
}
