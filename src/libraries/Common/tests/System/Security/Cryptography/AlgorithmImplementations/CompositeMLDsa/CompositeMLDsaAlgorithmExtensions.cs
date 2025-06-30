// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.Tests;

// This is just for CompositeMLDsaMessageEncoder, so it is not in the test namespace.
namespace Internal.Cryptography
{
    internal static class CompositeMLDsaAlgorithmExtensions
    {
        extension (CompositeMLDsaAlgorithm compositeMLDsaAlgorithm)
        {
            internal byte[] DomainSeparator => CompositeMLDsaTestHelpers.DomainSeparators[compositeMLDsaAlgorithm];

            internal HashAlgorithmName HashAlgorithmName => CompositeMLDsaTestHelpers.HashAlgorithms[compositeMLDsaAlgorithm];
        }
    }
}
