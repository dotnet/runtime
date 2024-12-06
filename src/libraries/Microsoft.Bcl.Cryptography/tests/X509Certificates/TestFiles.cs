// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static class TestFiles
    {
        internal const string TestDataFolder = "TestData";

        // Certs
        internal static readonly string MsCertificateDerFile = Path.Combine(TestDataFolder, "MS.cer");
        internal static readonly string MsCertificatePemFile = Path.Combine(TestDataFolder, "MS.pem");

        internal const string MicrosoftRootCertFileName = "microsoft.cer";
        internal static readonly string MicrosoftRootCertFile = Path.Combine(TestDataFolder, MicrosoftRootCertFileName);

        internal const string MyCertFileName = "My.cer";

        internal static readonly string SignedMsuFile = Path.Combine(TestDataFolder, "Windows6.1-KB3004361-x64.msu");

        internal const string TestCertFileName = "test.cer";
        internal static readonly string TestCertFile = Path.Combine(TestDataFolder, TestCertFileName);

        // PKCS#7
        internal static readonly string Pkcs7ChainDerFile = Path.Combine(TestDataFolder, "certchain.p7b");
        internal static readonly string Pkcs7ChainPemFile = Path.Combine(TestDataFolder, "certchain.p7c");
        internal static readonly string Pkcs7EmptyDerFile = Path.Combine(TestDataFolder, "empty.p7b");
        internal static readonly string Pkcs7EmptyPemFile = Path.Combine(TestDataFolder, "empty.p7c");
        internal static readonly string Pkcs7SingleDerFile = Path.Combine(TestDataFolder, "singlecert.p7b");
        internal static readonly string Pkcs7SinglePemFile = Path.Combine(TestDataFolder, "singlecert.p7c");

        // PKCS#12
        private static readonly string PfxSuffix = PlatformSupport.IsRC2Supported ? ".pfx" : ".noRC2.pfx";

        internal static readonly string ChainPfxFile = Path.Combine(TestDataFolder, "test" + PfxSuffix);
        internal static readonly string DummyTcpServerPfxFile = Path.Combine(TestDataFolder, "DummyTcpServer" + PfxSuffix);
        internal static readonly string PfxFileName = "My" + PfxSuffix;
        internal static readonly string PfxFile = Path.Combine(TestDataFolder, PfxFileName);
    }
}
