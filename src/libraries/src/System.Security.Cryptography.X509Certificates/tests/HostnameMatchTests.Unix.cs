// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class HostnameMatchTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public static void MatchCN_NoWildcards(bool wantsWildcard, bool mixedCase)
        {
            string targetName = "LocalHost.loCAldoMaIn";
            string subjectCN = wantsWildcard ? "*.LOcaLdomain" : targetName;

            RunTest(targetName, subjectCN, null, !mixedCase, !wantsWildcard);
        }

        [Theory]
        [InlineData("Capitalized.SomeDomain.TLD", false, true)]
        [InlineData("Capitalized.SomeDomain.TLD", true, true)]
        [InlineData("Too.Many.SomeDomain.TLD", false, false)]
        [InlineData("Too.Many.SomeDomain.TLD", true, false)]
        [InlineData("Now.Lower.SomeDomain.TLD", false, true)]
        [InlineData("Now.Lower.SomeDomain.TLD", true, true)]
        [InlineData("Score.1812-Overture.somedomain.TLD", false, true)]
        [InlineData("Score.1812-Overture.somedomain.TLD", true, true)]
        [InlineData("1-800.Lower.somedomain.TLD", false, true)]
        [InlineData("1-800.Lower.somedomain.TLD", true, true)]
        public static void MatchSubjectAltName(string targetName, bool mixedCase, bool expectedResult)
        {
            string[] sanEntries =
            {
                "Capitalized.SomeDomain.TLD",
                "*.SomeDomain.TLD",
                "*.lower.someDomain.Tld",
                "*.1812-Overture.SomeDomain.Tld",
            };

            RunTest(targetName, "SAN Certificate", sanEntries, !mixedCase, expectedResult);
        }

        [Fact]
        public static void SubjectAltName_NoFallback()
        {
            string[] sanEntries =
            {
                "reference.example.org",
                "other.example.org",
                "reference.example",
            };

            RunTest("www.example.org", "www.example.org", sanEntries, false, false);
        }

        private static void RunTest(
            string targetName,
            string subjectCN,
            IList<string> sanDnsNames,
            bool flattenCase,
            bool expectedResult)
        {
            using (RSA rsa = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest request = new CertificateRequest(
                    $"CN={FixCase(subjectCN, flattenCase)}, O=.NET Framework (CoreFX)",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature,
                        false));

                if (sanDnsNames != null)
                {
                    var builder = new SubjectAlternativeNameBuilder();

                    foreach (string sanDnsName in sanDnsNames)
                    {
                        builder.AddDnsName(sanDnsName);
                    }

                    X509Extension extension = builder.Build();

                    // The SAN builder will have done DNS case normalization via IdnMapping.
                    // We need to undo that here.
                    if (!flattenCase)
                    {
                        UTF8Encoding encoding = new UTF8Encoding();

                        byte[] extensionBytes = extension.RawData;
                        Span<byte> extensionSpan = extensionBytes;

                        foreach (string sanDnsName in sanDnsNames)
                        {
                            // If the string is longer than 127 then the quick DER encoding check
                            // is not correct.
                            Assert.InRange(sanDnsName.Length, 1, 127);

                            byte[] lowerBytes = encoding.GetBytes(sanDnsName.ToLowerInvariant());
                            byte[] mixedBytes = encoding.GetBytes(sanDnsName);

                            // Only 7-bit ASCII should be here, no byte expansion.
                            // (non-7-bit ASCII values require IdnMapping normalization)
                            Assert.Equal(sanDnsName.Length, lowerBytes.Length);
                            Assert.Equal(sanDnsName.Length, mixedBytes.Length);

                            int idx = extensionSpan.IndexOf(lowerBytes);

                            while (idx >= 0)
                            {
                                if (idx < 2 ||
                                    extensionBytes[idx - 2] != 0x82 ||
                                    extensionBytes[idx - 1] != sanDnsName.Length)
                                {
                                    int relativeIdx = extensionSpan.Slice(idx + 1).IndexOf(lowerBytes);
                                    idx = idx + 1 + relativeIdx;
                                    continue;
                                }

                                mixedBytes.AsSpan().CopyTo(extensionSpan.Slice(idx));
                                break;
                            }
                        }

                        extension.RawData = extensionBytes;
                    }

                    request.CertificateExtensions.Add(extension);
                }

                DateTimeOffset start = DateTimeOffset.UtcNow.AddYears(-1);
                DateTimeOffset end = start.AddYears(1);

                using (X509Certificate2 cert = request.CreateSelfSigned(start, end))
                {
                    bool isMatch = CheckHostname(cert, targetName);
                    string lowerTarget = targetName.ToLowerInvariant();
                    bool isLowerMatch = CheckHostname(cert, lowerTarget);

                    if (expectedResult)
                    {
                        Assert.True(isMatch, $"{targetName} matches");
                        Assert.True(isLowerMatch, $"{lowerTarget} (lowercase) matches");
                    }
                    else
                    {
                        Assert.False(isMatch, $"{targetName} matches");
                        Assert.False(isLowerMatch, $"{lowerTarget} (lowercase) matches");
                    }
                }
            }
        }

        private static string FixCase(string input, bool flatten)
        {
            return flatten ? input.ToLowerInvariant() : input;
        }

        private static bool CheckHostname(X509Certificate2 cert, string targetName)
        {
            int value = CheckX509Hostname(cert.Handle, targetName, targetName.Length);
            GC.KeepAlive(cert);
            Assert.InRange(value, 0, 1);
            return value != 0;
        }

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "CryptoNative_CheckX509Hostname")]
        private static extern int CheckX509Hostname(IntPtr x509, string hostname, int cchHostname);
    }
}
