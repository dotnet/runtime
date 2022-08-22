// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9MessageDigest : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9MessageDigest()
            : base(Oids.MessageDigestOid.CopyOid())
        {
        }

        internal Pkcs9MessageDigest(ReadOnlySpan<byte> rawData)
            : base(Oids.MessageDigestOid.CopyOid(), rawData)
        {
        }

        //
        // Public properties.
        //

        public byte[] MessageDigest
        {
            get
            {
                return _lazyMessageDigest ??= Decode(RawData);
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

        [return: NotNullIfNotNull(nameof(rawData))]
        private static byte[]? Decode(byte[]? rawData)
        {
            if (rawData == null)
                return null;

            return PkcsHelpers.DecodeOctetString(rawData);
        }

        private volatile byte[]? _lazyMessageDigest;
    }
}
