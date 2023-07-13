// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public abstract partial class PfxIterationCountTests
    {
        private const long DefaultIterationLimit = 600_000;
        internal abstract X509Certificate Import(byte[] blob);
        internal abstract X509Certificate Import(byte[] blob, string password);
        internal abstract X509Certificate Import(byte[] blob, SecureString password);
        internal abstract X509Certificate Import(string fileName);
        internal abstract X509Certificate Import(string fileName, string password);
        internal abstract X509Certificate Import(string fileName, SecureString password);

        [ConditionalTheory]
        [MemberData(nameof(GetCertsWith_IterationCountNotExceedingDefaultLimit_AndNullOrEmptyPassword_MemberData))]
        public void Import_IterationCounLimitNotExceeded_Succeeds(string name, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2 which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            if (PfxTests.IsPkcs12IterationCountAllowed(iterationCount, PfxTests.DefaultIterations))
            {
                X509Certificate cert = Import(blob);
                Assert.True(cert.Subject == "CN=test" || cert.Subject == "CN=potato");
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(GetCertsWith_IterationCountExceedingDefaultLimit_MemberData))]
        public void Import_IterationCountLimitExceeded_Throws(string name, string password, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            _ = password;
            _ = iterationCount;

            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2 which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            CryptographicException ce = Assert.Throws<CryptographicException>(() => Import(blob));
            Assert.Contains("2233907", ce.Message);
        }

        [ConditionalTheory]
        [MemberData(nameof(GetCertsWith_IterationCountExceedingDefaultLimit_MemberData))]
        public void ImportWithPasswordOrFileName_IterationCountLimitExceeded(string name, string password, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2 which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            using (TempFileHolder tempFile = new TempFileHolder(blob))
            {
                string fileName = tempFile.FilePath;
                if (PfxTests.IsPkcs12IterationCountAllowed(iterationCount, PfxTests.DefaultIterations))
                {
                    Assert.NotNull(Import(blob, password));
                    Assert.NotNull(Import(blob, PfxTests.GetSecureString(password)));

                    Assert.NotNull(Import(fileName));
                    Assert.NotNull(Import(fileName, password));
                    Assert.NotNull(Import(fileName, PfxTests.GetSecureString(password)));
                }
                else
                {
                    if (OperatingSystem.IsWindows())
                    {
                        // Specifying password or importing from file will still give us error because cert is beyond Windows limit.
                        // But we will get the CryptoThrowHelper+WindowsCryptographicException.
                        VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(() => Import(blob, password));
                        VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(() => Import(blob, PfxTests.GetSecureString(password)));

                        // Using a file will do as above as well.
                        VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(() => Import(fileName));
                        VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(() => Import(fileName, password));
                        VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(() => Import(fileName, PfxTests.GetSecureString(password)));
                    }
                }
            }
        }

        internal static void VerifyThrowsCryptoExButDoesNotThrowPfxWithoutPassword(Action action)
        {
            CryptographicException ce = Assert.ThrowsAny<CryptographicException>(action);
            Assert.DoesNotContain("2233907", ce.Message);
        }

        [ConditionalTheory]
        [MemberData(nameof(GetCertsWith_NonNullOrEmptyPassword_MemberData))]
        public void Import_NonNullOrEmptyPasswordExpected_Throws(string name, string password, bool usesPbes2, byte[] blob, long iterationCount, bool usesRC2)
        {
            if (usesPbes2 && !PfxTests.Pkcs12PBES2Supported)
            {
                throw new SkipTestException(name + " uses PBES2 which is not supported on this version.");
            }

            if (usesRC2 && !PlatformSupport.IsRC2Supported)
            {
                throw new SkipTestException(name + " uses RC2, which is not supported on this platform.");
            }

            CryptographicException ce = Assert.ThrowsAny<CryptographicException>(() => Import(blob));

            if (PfxTests.IsPkcs12IterationCountAllowed(iterationCount, PfxTests.DefaultIterations))
            {
                Assert.NotNull(Import(blob, password));
                Assert.NotNull(Import(blob, PfxTests.GetSecureString(password)));


                using (TempFileHolder tempFile = new TempFileHolder(blob))
                {
                    string fileName = tempFile.FilePath;
                    Assert.NotNull(Import(fileName, password));
                    Assert.NotNull(Import(fileName, PfxTests.GetSecureString(password)));
                }
            }
        }

        internal static readonly List<PfxInfo> s_Certificates = GetCertificates();

        internal static List<PfxInfo> GetCertificates()
        {
            List<PfxInfo> certificates = new List<PfxInfo>();
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12NoPassword2048RoundsHex), null, 2048 * 3, true, TestData.Pkcs12NoPassword2048RoundsHex.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12OpenSslOneCertDefaultEmptyPassword), "", 2048 * 3, true, TestData.Pkcs12OpenSslOneCertDefaultEmptyPassword.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12OpenSslOneCertDefaultNoMac), null, 2048, true, TestData.Pkcs12OpenSslOneCertDefaultNoMac.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12NoPasswordRandomCounts), null, 938, true, TestData.Pkcs12NoPasswordRandomCounts));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12WindowsDotnetExportEmptyPassword), "", 6000, false, TestData.Pkcs12WindowsDotnetExportEmptyPassword.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12MacosKeychainCreated), null, 4097, false, TestData.Pkcs12MacosKeychainCreated.HexToByteArray(), usesRC2: true));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12BuilderSaltWithMacNullPassword), null, 120000, true, TestData.Pkcs12BuilderSaltWithMacNullPassword.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12Builder3DESCBCWithNullPassword), null, 30000, false, TestData.Pkcs12Builder3DESCBCWithNullPassword.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12Builder3DESCBCWithEmptyPassword), "", 30000, false, TestData.Pkcs12Builder3DESCBCWithEmptyPassword.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12WindowsWithCertPrivacyPasswordIsOne), "1", 4000, false, TestData.Pkcs12WindowsWithCertPrivacyPasswordIsOne.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12WindowsWithoutCertPrivacyPasswordIsOne), "1", 4000, false, TestData.Pkcs12WindowsWithoutCertPrivacyPasswordIsOne.HexToByteArray()));
            certificates.Add(new PfxInfo(
                nameof(TestData.Pkcs12NoPassword600KPlusOneRoundsHex), null, 600_001 * 3, true, TestData.Pkcs12NoPassword600KPlusOneRoundsHex.HexToByteArray()));

            return certificates;
        }

        public static IEnumerable<object[]> GetCertsWith_IterationCountNotExceedingDefaultLimit_AndNullOrEmptyPassword_MemberData()
        {
            foreach (PfxInfo p in s_Certificates.Where(
                c => c.IterationCount <= DefaultIterationLimit &&
                string.IsNullOrEmpty(c.Password)))
            {
                yield return new object[] { p.Name, p.UsesPbes2, p.Blob, p.IterationCount, p.UsesRC2 };
            }
        }

        public static IEnumerable<object[]> GetCertsWith_IterationCountExceedingDefaultLimit_MemberData()
        {
            foreach (PfxInfo p in s_Certificates.Where(c => c.IterationCount > DefaultIterationLimit))
            {
                yield return new object[] { p.Name, p.Password, p.UsesPbes2, p.Blob, p.IterationCount, p.UsesRC2 };
            }
        }

        public static IEnumerable<object[]> GetCertsWith_NonNullOrEmptyPassword_MemberData()
        {
            foreach(PfxInfo p in s_Certificates.Where(c => !string.IsNullOrEmpty(c.Password)))
            {
                yield return new object[] { p.Name, p.Password, p.UsesPbes2, p.Blob, p.IterationCount, p.UsesRC2 };
            }
        }
    }

    public class PfxInfo
    {
        internal string Name { get; set; }
        internal string? Password { get; set; }
        internal long IterationCount { get; set; }
        internal bool UsesPbes2 { get; set; }
        internal byte[] Blob { get; set; }
        internal bool UsesRC2 { get; set; }

        internal PfxInfo(string name, string password, long iterationCount, bool usesPbes2, byte[] blob, bool usesRC2 = false)
        {
            Name = name;
            Password = password;
            IterationCount = iterationCount;
            UsesPbes2 = usesPbes2;
            Blob = blob;
            UsesRC2 = usesRC2;
        }
    }
}
