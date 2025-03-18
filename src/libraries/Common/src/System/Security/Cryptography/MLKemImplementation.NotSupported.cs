// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class MLKemImplementation
    {
        internal static bool IsSupported => false;

        internal static MLKem Generate(MLKemAlgorithm algorithm)
        {
            _ = algorithm;
            Debug.Fail("Caller should have checked platform availability.");
            throw new CryptographicException();
        }
    }
}
