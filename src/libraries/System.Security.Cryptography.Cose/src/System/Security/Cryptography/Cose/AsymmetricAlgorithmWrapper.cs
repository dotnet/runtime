// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed class AsymmetricAlgorithmWrapper : AsymmetricAlgorithm
    {
        internal MLDsa MLDsa { get; }

        public AsymmetricAlgorithmWrapper(MLDsa key)
        {
            MLDsa = key;
        }
    }
}
