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
        internal static partial class EvpPKeyMLDsaAlgs
        {
            internal static string? MLDsa44 { get; }
            internal static string? MLDsa65 { get; }
            internal static string? MLDsa87 { get; }

            static EvpPKeyMLDsaAlgs()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                MLDsa44 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa44.Name);
                MLDsa65 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa65.Name);
                MLDsa87 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa87.Name);
            }
        }
    }
}
