// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9MessageDigest : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9MessageDigest() :
            base(Oid.FromOidValue(Oids.MessageDigest, OidGroup.ExtensionOrAttribute))
        {
        }

        internal Pkcs9MessageDigest(ReadOnlySpan<byte> signatureDigest)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.WriteOctetString(signatureDigest);
                RawData = writer.Encode();
            }
        }

        //
        // Public properties.
        //

        public byte[] MessageDigest
        {
            get
            {
                return _lazyMessageDigest ?? (_lazyMessageDigest = Decode(RawData));
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazyMessageDigest = null;
        }

        //
        // Private methods.
        //

        private static byte[] Decode(byte[] rawData)
        {
            if (rawData == null)
                return null;

            return PkcsPal.Instance.DecodeOctetString(rawData);
        }

        private volatile byte[] _lazyMessageDigest = null;
    }
}


