// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    // Run these tests after setting the MANUAL_TESTS environment variable.
    public class X509Certificate2UIManualTests
    {
        public static bool ManualTestsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"));

        // Take action as decsribed in the title of the dialog box window.
        [ConditionalTheory(nameof(ManualTestsEnabled))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SelectCertificateSingleSelectionTest(int count)
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                CertificateRequest request = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, X509SignatureGenerator.CreateForECDsa(ecdsa), now, now.AddMinutes(10), new byte[1]))
                {
                    X509Certificate2Collection collection = new X509Certificate2Collection() { cert, cert };
                    X509Certificate2Collection actual = X509Certificate2UI.SelectFromCollection(
                        collection, $"Choose {count} Certificate", string.Empty, count < 2 ? X509SelectionFlag.SingleSelection : X509SelectionFlag.MultiSelection, IntPtr.Zero);
                    Assert.Equal(count, actual.Count);
                }
            }
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void DisplayCertificateTest()
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                CertificateRequest request = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, X509SignatureGenerator.CreateForECDsa(ecdsa), now, now.AddMinutes(10), new byte[1]))
                {
                    X509Certificate2UI.DisplayCertificate(cert);
                }
            }
        }
    }

    internal class EccTestData
    {
        internal ECParameters KeyParameters { get; private set; }

        internal static readonly EccTestData Secp256r1Data = new EccTestData
        {
            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,

                Q = new ECPoint
                {
                    X = HexToByteArray("8101ECE47464A6EAD70CF69A6E2BD3D88691A3262D22CBA4F7635EAFF26680A8"),
                    Y = HexToByteArray("D8A12BA61D599235F67D9CB4D58F1783D3CA43E78F0A5ABAA624079936C0C3A9"),
                },

                D = HexToByteArray("70A12C2DB16845ED56FF68CFC21A472B3F04D7D6851BF6349F2D7D5B3452B38A"),
            },
        };

        private static byte[] HexToByteArray(string hexString)
        {
            return Convert.FromHexString(hexString);
        }
    }
}
