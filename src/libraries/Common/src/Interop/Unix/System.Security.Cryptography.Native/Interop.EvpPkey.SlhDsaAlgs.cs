// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static partial class EvpPKeySlhDsaAlgs
        {
            internal static string? SlhDsaSha2_128s { get; }
            internal static string? SlhDsaShake128s { get; }
            internal static string? SlhDsaSha2_128f { get; }
            internal static string? SlhDsaShake128f { get; }
            internal static string? SlhDsaSha2_192s { get; }
            internal static string? SlhDsaShake192s { get; }
            internal static string? SlhDsaSha2_192f { get; }
            internal static string? SlhDsaShake192f { get; }
            internal static string? SlhDsaSha2_256s { get; }
            internal static string? SlhDsaShake256s { get; }
            internal static string? SlhDsaSha2_256f { get; }
            internal static string? SlhDsaShake256f { get; }

            static EvpPKeySlhDsaAlgs()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                SlhDsaSha2_128s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_128s.Name);
                SlhDsaShake128s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake128s.Name);
                SlhDsaSha2_128f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_128f.Name);
                SlhDsaShake128f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake128f.Name);
                SlhDsaSha2_192s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_192s.Name);
                SlhDsaShake192s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake192s.Name);
                SlhDsaSha2_192f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_192f.Name);
                SlhDsaShake192f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake192f.Name);
                SlhDsaSha2_256s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_256s.Name);
                SlhDsaShake256s = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake256s.Name);
                SlhDsaSha2_256f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaSha2_256f.Name);
                SlhDsaShake256f = IsSignatureAlgorithmAvailable(SlhDsaAlgorithm.SlhDsaShake256f.Name);
            }
        }
    }
}
