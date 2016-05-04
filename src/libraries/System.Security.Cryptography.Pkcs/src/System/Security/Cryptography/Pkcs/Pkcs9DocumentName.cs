// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9DocumentName : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9DocumentName()
            : base(new Oid(Oids.DocumentName))
        {
            // CAPI doesn't have an OID mapping for szOID_CAPICOM_documentName, so we cannot use the faster
            // FromOidValue factory
        }

        public Pkcs9DocumentName(string documentName)
            : base(Oids.DocumentName, Encode(documentName))
        {
            _lazyDocumentName = documentName;
        }

        public Pkcs9DocumentName(byte[] encodedDocumentName)
            : base(Oids.DocumentName, encodedDocumentName)
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

        private static string Decode(byte[] rawData)
        {
            if (rawData == null)
                return null;

            byte[] octets = PkcsPal.Instance.DecodeOctetString(rawData);
            return octets.OctetStringToUnicode();
        }

        private static byte[] Encode(string documentName)
        {
            if (documentName == null)
                throw new ArgumentNullException(nameof(documentName));

            byte[] octets = documentName.UnicodeToOctetString();
            return PkcsPal.Instance.EncodeOctetString(octets);
        }

        private volatile string _lazyDocumentName = null;
    }
}


