// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Internal.Cryptography.Pal.AnyOS;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9LocalKeyId : Pkcs9AttributeObject
    {
        private byte[] _lazyKeyId;

        public Pkcs9LocalKeyId() :
            base(new Oid(Oids.LocalKeyId))
        {
        }

        public Pkcs9LocalKeyId(byte[] keyId)
            // The ReadOnlySpan constructor permits null
            : this(new ReadOnlySpan<byte>(keyId))
        {
        }

        public Pkcs9LocalKeyId(ReadOnlySpan<byte> keyId)
            : this()
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.WriteOctetString(keyId);
                RawData = writer.Encode();
            }
        }

        public ReadOnlyMemory<byte> KeyId =>
            _lazyKeyId ?? (_lazyKeyId = Decode(RawData));

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazyKeyId = null;
        }

        private static byte[] Decode(byte[] rawData)
        {
            if (rawData == null)
            {
                return null;
            }

            return ManagedPkcsPal.Instance.DecodeOctetString(rawData);
        }
    }
}
