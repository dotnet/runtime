// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Cose
{
    internal enum CoseAlgorithm : int
    {
        // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
        // ECDsa w/SHA
        ES256 = -7,
        ES384 = -35,
        ES512 = -36,

        // RSASSA-PSS w/SHA
        PS256 = -37,
        PS384 = -38,
        PS512 = -39,

        // RSASSA-PKCS1-v1_5 using SHA
        RS256 = -257,
        RS384 = -258,
        RS512 = -259,

        // ML-DSA
        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        MLDsa44 = -48,
        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        MLDsa65 = -49,
        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        MLDsa87 = -50,
    }
}
