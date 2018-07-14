// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc7292#section-4.2.5
    //
    // SecretBag ::= SEQUENCE {
    //   secretTypeId   BAG-TYPE.&id ({SecretTypes}),
    //   secretValue    [0] EXPLICIT BAG-TYPE.&Type ({SecretTypes} {@secretTypeId})
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecretBagAsn
    {
        [ObjectIdentifier]
        public string SecretTypeId;

        [AnyValue]
        [ExpectedTag(TagClass.ContextSpecific, 0, ExplicitTag = true)]
        public ReadOnlyMemory<byte> SecretValue;
    }
}
