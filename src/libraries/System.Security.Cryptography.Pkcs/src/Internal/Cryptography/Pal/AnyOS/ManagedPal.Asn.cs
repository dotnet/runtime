// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override Oid GetEncodedMessageType(byte[] encodedMessage)
        {
            AsnValueReader reader = new AsnValueReader(encodedMessage, AsnEncodingRules.BER);

            ContentInfoAsn.Decode(ref reader, encodedMessage, out ContentInfoAsn contentInfo);

            switch (contentInfo.ContentType)
            {
                case Oids.Pkcs7Data:
                case Oids.Pkcs7Signed:
                case Oids.Pkcs7Enveloped:
                case Oids.Pkcs7SignedEnveloped:
                case Oids.Pkcs7Hashed:
                case Oids.Pkcs7Encrypted:
                    return new Oid(contentInfo.ContentType);
            }

            throw new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
        }
    }
}
