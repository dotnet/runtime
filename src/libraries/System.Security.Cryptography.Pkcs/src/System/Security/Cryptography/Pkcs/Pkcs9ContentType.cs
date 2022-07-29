// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9ContentType : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9ContentType()
            : base(Oids.ContentTypeOid.CopyOid())
        {
        }

        internal Pkcs9ContentType(ReadOnlySpan<byte> rawData)
            : base(Oids.ContentTypeOid.CopyOid(), rawData)
        {
        }

        //
        // Public properties.
        //

        public Oid ContentType
        {
            get
            {
                return _lazyContentType ??= Decode(RawData);
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazyContentType = null;
        }

        //
        // Private methods.
        //

        [return: NotNullIfNotNull(nameof(rawData))]
        private static Oid? Decode(byte[]? rawData)
        {
            if (rawData == null)
                return null;

            string contentTypeValue = PkcsHelpers.DecodeOid(rawData);
            return new Oid(contentTypeValue);
        }

        private volatile Oid? _lazyContentType;
    }
}
