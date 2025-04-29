// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaTestsBase
    {
        /// <summary>
        /// Gets whether the platform has native support for SLH-DSA. Note that SLH-DSA may still be
        /// used on the platform if it doesn't rely on native support (e.g. KeyVault keys).
        /// </summary>
        public static bool SupportedOnPlatform => false; // TODO: Once SLH-DSA is supported on a platform, set this to check for that platform.

        /// <summary>
        /// Gets whether the platform lacks native support for SLH-DSA. Note that SLH-DSA may still be
        /// used on the platform if it doesn't rely on native support (e.g. KeyVault keys).
        /// </summary>
        public static bool NotSupportedOnPlatform => !SupportedOnPlatform;

        public static IEnumerable<object[]> AlgorithmsData => AlgorithmsRaw.Select(a => new[] { a });

        public static SlhDsaAlgorithm[] AlgorithmsRaw =
        [
            SlhDsaAlgorithm.SlhDsaSha2_128s,
            SlhDsaAlgorithm.SlhDsaShake128s,
            SlhDsaAlgorithm.SlhDsaSha2_128f,
            SlhDsaAlgorithm.SlhDsaShake128f,
            SlhDsaAlgorithm.SlhDsaSha2_192s,
            SlhDsaAlgorithm.SlhDsaShake192s,
            SlhDsaAlgorithm.SlhDsaSha2_192f,
            SlhDsaAlgorithm.SlhDsaShake192f,
            SlhDsaAlgorithm.SlhDsaSha2_256s,
            SlhDsaAlgorithm.SlhDsaShake256s,
            SlhDsaAlgorithm.SlhDsaSha2_256f,
            SlhDsaAlgorithm.SlhDsaShake256f,
        ];
    }
}
