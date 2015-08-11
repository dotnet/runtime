// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    //
    // These tests currently do not work against CNG and thus are split off so we can exclude them.
    //
    public partial class ImportExport
    {
        [Fact]
        public static void PaddedExport()
        {
            // OpenSSL's numeric type for the storage of RSA key parts disregards zero-valued
            // prefix bytes.
            //
            // The .NET 4.5 RSACryptoServiceProvider type verifies that all of the D breakdown
            // values (P, DP, Q, DQ, InverseQ) are exactly half the size of D (which is itself
            // the same size as Modulus).
            //
            // These two things, in combination, suggest that we ensure that all .NET
            // implementations of RSA export their keys to the fixed array size suggested by their
            // KeySize property.
            RSAParameters diminishedDPParamaters = TestData.DiminishedDPParamaters;
            RSAParameters exported;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(diminishedDPParamaters);
                exported = rsa.ExportParameters(true);
            }

            // DP is the most likely to fail, the rest just otherwise ensure that Export
            // isn't losing data.
            AssertKeyEquals(ref diminishedDPParamaters, ref exported);
        }

        [Fact]
        public static void UnusualExponentImportExport()
        {
            // Most choices for the Exponent value in an RSA key use a Fermat prime.
            // Since a Fermat prime is 2^(2^m) + 1, it always only has two bits set, and
            // frequently has the form { 0x01, [some number of 0x00s], 0x01 }, which has the same
            // representation in both big- and little-endian.
            //
            // The only real requirement for an Exponent value is that it be coprime to (p-1)(q-1).
            // So here we'll use the (non-Fermat) prime value 433 (0x01B1) to ensure big-endian export.
            RSAParameters unusualExponentParameters = TestData.UnusualExponentParameters;
            RSAParameters exported;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(unusualExponentParameters);
                exported = rsa.ExportParameters(true);
            }

            // Exponent is the most likely to fail, the rest just otherwise ensure that Export
            // isn't losing data.
            AssertKeyEquals(ref unusualExponentParameters, ref exported);
        }
    }
}
