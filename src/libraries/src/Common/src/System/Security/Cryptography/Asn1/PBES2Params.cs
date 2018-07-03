// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc2898#appendix-A.4
    //
    // PBES2-params ::= SEQUENCE {
    //   keyDerivationFunc AlgorithmIdentifier {{PBES2-KDFs}},
    //   encryptionScheme AlgorithmIdentifier {{PBES2-Encs}} }
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct PBES2Params
    {
        public AlgorithmIdentifierAsn KeyDerivationFunc;
        public AlgorithmIdentifierAsn EncryptionScheme;
    }
}
