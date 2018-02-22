// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc3161#section-2.4.1
    // 
    // TimeStampReq ::= SEQUENCE  {
    //    version                      INTEGER  { v1(1) },
    //    messageImprint               MessageImprint,
    //      --a hash algorithm OID and the hash value of the data to be
    //      --time-stamped
    //    reqPolicy             TSAPolicyId              OPTIONAL,
    //    nonce                 INTEGER                  OPTIONAL,
    //    certReq               BOOLEAN                  DEFAULT FALSE,
    //    extensions            [0] IMPLICIT Extensions  OPTIONAL  }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rfc3161TimeStampReq
    {
        public int Version;

        public MessageImprint MessageImprint;

        [OptionalValue]
        public Oid ReqPolicy;

        [OptionalValue]
        [Integer]
        public ReadOnlyMemory<byte>? Nonce;

#pragma warning disable CS3016
        [DefaultValue(0x01, 0x01, 0x00)]
#pragma warning restore CS3016
        public bool CertReq;

        [ExpectedTag(0)]
        [OptionalValue]
        internal X509ExtensionAsn[] Extensions;
    }
}
