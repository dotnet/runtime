// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class Pkcs9ExtensionRequest : X501Attribute
    {
        internal Pkcs9ExtensionRequest(IEnumerable<X509Extension> extensions)
            : base(Oids.Pkcs9ExtensionRequestOid, EncodeAttribute(extensions))
        {
        }

        private static byte[] EncodeAttribute(IEnumerable<X509Extension> extensions)
        {
            ArgumentNullException.ThrowIfNull(extensions);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach (X509Extension e in extensions)
                {
                    new X509ExtensionAsn(e).Encode(writer);
                }
            }

            return writer.Encode();
        }
    }
}
