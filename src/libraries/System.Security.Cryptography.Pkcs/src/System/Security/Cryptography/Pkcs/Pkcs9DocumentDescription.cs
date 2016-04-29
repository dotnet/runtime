// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9DocumentDescription : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9DocumentDescription()
            : base(new Oid(Oids.DocumentDescription))
        {
            // CAPI doesn't have an OID mapping for szOID_CAPICOM_documentDescription, so we cannot use the faster
            // FromOidValue factory
        }

        public Pkcs9DocumentDescription(string documentDescription)
            : base(Oids.DocumentDescription, Encode(documentDescription))
        {
            _lazyDocumentDescription = documentDescription;
        }

        public Pkcs9DocumentDescription(byte[] encodedDocumentDescription)
            : base(Oids.DocumentDescription, encodedDocumentDescription)
        {
        }

        //
        // Public methods.
        //

        public string DocumentDescription
        {
            get
            {
                return _lazyDocumentDescription ?? (_lazyDocumentDescription = Decode(RawData));
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

        private static string Decode(byte[] rawData)
        {
            if (rawData == null)
                return null;

            byte[] octets = PkcsPal.Instance.DecodeOctetString(rawData);
            return octets.OctetStringToUnicode();
        }

        private static byte[] Encode(string documentDescription)
        {
            if (documentDescription == null)
                throw new ArgumentNullException(nameof(documentDescription));

            byte[] octets = documentDescription.UnicodeToOctetString();
            return PkcsPal.Instance.EncodeOctetString(octets);
        }

        private volatile string _lazyDocumentDescription = null;
    }
}


