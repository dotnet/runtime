// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9DocumentName : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9DocumentName()
            : base(Oids.DocumentNameOid.CopyOid())
        {
        }

        public Pkcs9DocumentName(string documentName)
            : base(Oids.DocumentNameOid.CopyOid(), Encode(documentName))
        {
            _lazyDocumentName = documentName;
        }

        public Pkcs9DocumentName(byte[] encodedDocumentName)
            : base(Oids.DocumentNameOid.CopyOid(), encodedDocumentName)
        {
        }

        //
        // Public methods.
        //

        public string DocumentName
        {
            get
            {
                return _lazyDocumentName ?? (_lazyDocumentName = Decode(RawData));
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazyDocumentName = null;
        }

        //
        // Private methods.
        //

        [return: NotNullIfNotNull("rawData")]
        private static string? Decode(byte[]? rawData)
        {
            if (rawData == null)
                return null;

            byte[] octets = PkcsHelpers.DecodeOctetString(rawData);
            return octets.OctetStringToUnicode();
        }

        private static byte[] Encode(string documentName)
        {
            if (documentName is null)
            {
                throw new ArgumentNullException(nameof(documentName));
            }

            byte[] octets = documentName.UnicodeToOctetString();
            return PkcsHelpers.EncodeOctetString(octets);
        }

        private volatile string? _lazyDocumentName;
    }
}
