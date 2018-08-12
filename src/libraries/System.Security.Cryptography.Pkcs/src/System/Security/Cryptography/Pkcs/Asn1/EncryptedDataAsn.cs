// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-8
    //
    // EncryptedData ::= SEQUENCE {
    //   version CMSVersion,
    //   encryptedContentInfo EncryptedContentInfo,
    //   unprotectedAttrs [1] IMPLICIT UnprotectedAttributes OPTIONAL }
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncryptedDataAsn
    {
        public int Version;

        public EncryptedContentInfoAsn EncryptedContentInfo;

        [ExpectedTag(1)]
        [SetOf]
        [OptionalValue]
        public AttributeAsn[] UnprotectedAttributes;
    }
}
