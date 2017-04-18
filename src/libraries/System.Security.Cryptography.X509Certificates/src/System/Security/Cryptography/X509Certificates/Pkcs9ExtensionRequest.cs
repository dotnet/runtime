// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class Pkcs9ExtensionRequest : X501Attribute
    {
        internal Pkcs9ExtensionRequest(IEnumerable<X509Extension> extensions)
            : base(Oids.Pkcs9ExtensionRequest, EncodeAttribute(extensions))
        {
        }

        private static byte[] EncodeAttribute(IEnumerable<X509Extension> extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException(nameof(extensions));

            // extensionRequest ATTRIBUTE ::= {
            //  WITH SYNTAX ExtensionRequest
            //  SINGLE VALUE TRUE
            //  ID pkcs-9-at-extensionRequest
            // }
            //
            // ExtensionRequest ::= Extensions
            //
            // Extensions  ::=  SEQUENCE SIZE (1..MAX) OF Extension
            //
            // Extension  ::=  SEQUENCE  {
            //   extnID      OBJECT IDENTIFIER,
            //   critical    BOOLEAN DEFAULT FALSE,
            //   extnValue   OCTET STRING  }

            List<byte[][]> encodedExtensions = new List<byte[][]>();

            foreach (X509Extension extension in extensions)
            {
                if (extension == null)
                    continue;

                encodedExtensions.Add(extension.SegmentedEncodedX509Extension());
            }

            // The SEQUENCE over the encodedExtensions list is the value of
            // Extensions/ExtensionRequest.
            return DerEncoder.ConstructSequence(encodedExtensions.ToArray());
        }
    }
}
