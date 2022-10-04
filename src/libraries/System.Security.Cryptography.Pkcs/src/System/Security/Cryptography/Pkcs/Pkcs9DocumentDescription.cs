// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9DocumentDescription : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9DocumentDescription()
            : base(Oids.DocumentDescriptionOid.CopyOid())
        {
        }

        public Pkcs9DocumentDescription(string documentDescription)
            : base(Oids.DocumentDescriptionOid.CopyOid(), Encode(documentDescription))
        {
            _lazyDocumentDescription = documentDescription;
        }

        public Pkcs9DocumentDescription(byte[] encodedDocumentDescription)
            : base(Oids.DocumentDescriptionOid.CopyOid(), encodedDocumentDescription)
        {
        }

        internal Pkcs9DocumentDescription(ReadOnlySpan<byte> encodedDocumentDescription)
            : base(Oids.DocumentDescriptionOid.CopyOid(), encodedDocumentDescription)
        {
        }

        //
        // Public methods.
        //

        public string DocumentDescription
        {
            get
            {
                return _lazyDocumentDescription ??= Decode(RawData);
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazyDocumentDescription = null;
        }

        //
        // Private methods.
        //

        [return: NotNullIfNotNull(nameof(rawData))]
        private static string? Decode(byte[]? rawData)
        {
            if (rawData == null)
                return null;

            byte[] octets = PkcsHelpers.DecodeOctetString(rawData);
            return octets.OctetStringToUnicode();
        }

        private static byte[] Encode(string documentDescription)
        {
            if (documentDescription is null)
            {
                throw new ArgumentNullException(nameof(documentDescription));
            }

            byte[] octets = documentDescription.UnicodeToOctetString();
            return PkcsHelpers.EncodeOctetString(octets);
        }

        private volatile string? _lazyDocumentDescription;
    }
}
