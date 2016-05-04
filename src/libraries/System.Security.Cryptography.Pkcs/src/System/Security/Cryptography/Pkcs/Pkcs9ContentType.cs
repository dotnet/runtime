// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9ContentType : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9ContentType()
            : base(Oid.FromOidValue(Oids.ContentType, OidGroup.ExtensionOrAttribute))
        {
        }

        //
        // Public properties.
        //

        public Oid ContentType
        {
            get
            {
                return _lazyContentType ?? (_lazyContentType = Decode(RawData));
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

        private static Oid Decode(byte[] rawData)
        {
            if (rawData == null)
                return null;

            string contentTypeValue = PkcsPal.Instance.DecodeOid(rawData);
            return new Oid(contentTypeValue);
        }

        private volatile Oid _lazyContentType = null;
    }
}


