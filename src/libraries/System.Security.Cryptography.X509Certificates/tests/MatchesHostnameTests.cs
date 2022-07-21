// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class MatchesHostnameTests
    {
        [Theory]
        [InlineData("fruit.example", false)]
        [InlineData("127.0.0.1", false)]
        [InlineData("microsoft.com", true)]
        [InlineData("www.microsoft.com", true)]
        [InlineData("wwwqa.microsoft.com", true)]
        [InlineData("wwwqa2.microsoft.com", false)]
        [InlineData("staticview.microsoft.com", true)]
        [InlineData("c.s-microsoft.com", true)]
        [InlineData("i.s-microsoft.com", true)]
        [InlineData("j.s-microsoft.com", false)]
        [InlineData("s-microsoft.com", false)]
        [InlineData("privacy.microsoft.com", true)]
        [InlineData("more.privacy.microsoft.com", false)]
        [InlineData("moreprivacy.microsoft.com", false)]
        public static void MicrosoftDotComSslMatchesHostname(string candidate, bool expected)
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            {
                AssertMatch(expected, cert, candidate);
            }
        }

        [Fact]
        public static void SanDnsMeansNoCommonNameFallback()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=zalzalak.fruit.example",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("yumberry.fruit.example");
                sanBuilder.AddDnsName("*.pome.fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "yumberry.fruit.example");
                    AssertMatch(true, cert, "zalzalak.pome.fruit.example");

                    // zalzalak is a pome, and our fake DNS knows that, but the certificate doesn't.
                    AssertMatch(false, cert, "zalzalak.fruit.example");
                }
            }
        }

        [Fact]
        public static void SanWithNoDnsMeansDoCommonNameFallback()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=zalzalak.fruit.example",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddIpAddress(IPAddress.Loopback);
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "yumberry.fruit.example");
                    AssertMatch(true, cert, "127.0.0.1");

                    // Since the SAN contains no dNSName values, we fall back to the CN.
                    AssertMatch(true, cert, "zalzalak.fruit.example");
                    AssertMatch(false, cert, "zalzalak.fruit.example", allowCommonName: false);
                }
            }
        }

        [Fact]
        public static void SanWithIPAddressMeansNoCommonNameFallback()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddIpAddress(IPAddress.Loopback);
                sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
                sanBuilder.AddIpAddress(IPAddress.Parse("[fe80::1]"));
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "127.0.0.1");
                    // Various text forms of IPv6Loopback
                    AssertMatch(true, cert, "[::1]");
                    AssertMatch(true, cert, "::1");
                    AssertMatch(true, cert, "[::01]");
                    AssertMatch(true, cert, "[0000::1]");
                    // Various text forms of fe80::1
                    AssertMatch(true, cert, "[fe80::1]");
                    AssertMatch(true, cert, "[FE80::1]");
                    AssertMatch(true, cert, "fe80::1");
                    AssertMatch(true, cert, "[fe80:0000::1]");
                    AssertMatch(true, cert, "[fe80:0000::01]");
                    // Various text forms of fe80::
                    AssertMatch(false, cert, "[fe80::]");
                    AssertMatch(false, cert, "fe80::");
                    AssertMatch(false, cert, "[fe80::0]");
                    AssertMatch(false, cert, "[fe80:0000::]");

                    // Since the SAN has an iPAddress value, we do not fall back to the CN.
                    AssertMatch(false, cert, "10.0.0.1");
                }
            }
        }

        [Fact]
        public static void IPv6InCommonNameIsTextMatch()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=[fe80::1]",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("huckleberry.fruit.example");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    // Various text forms of fe80::1.  These would all match under IP-ID, but
                    // only the first couple match under CN-ID (case-insensitive exact)
                    AssertMatch(true, cert, "[fe80::1]");
                    AssertMatch(true, cert, "[fE80::1]");
                    AssertMatch(true, cert, "[FE80::1]");
                    AssertMatch(true, cert, "[Fe80::1]");

                    AssertMatch(false, cert, "fe80::1");
                    AssertMatch(false, cert, "[fe80:0000::1]");
                    AssertMatch(false, cert, "[fe80:0000::01]");
                }
            }
        }

        [Fact]
        public static void SanDoesNotMatchIPAddressInDnsName()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("127.0.0.1");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    // 127.0.0.1 is an IP Address, but the SAN calls it a dNSName, so it won't match.
                    AssertMatch(false, cert, "127.0.0.1");

                    // Since the SAN contains no iPAddress values, we fall back to the CN.
                    AssertMatch(true, cert, "10.0.0.1");
                }
            }
        }

        [Fact]
        public static void CommonNameDoesNotUseWildcards()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=*.fruit.example",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "papaya.fruit.example");
                }
            }
        }

        [Fact]
        public static void NoPartialWildcards()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("*berry.fruit.example");
                sanBuilder.AddDnsName("cran*.fruit.example");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "cranberry.fruit.example");
                    AssertMatch(false, cert, "cranapple.fruit.example");
                    AssertMatch(false, cert, "strawberry.fruit.example");
                }
            }
        }

        [Fact]
        public static void WildcardsDoNotMatchThroughPeriods()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("fruit.example");
                sanBuilder.AddDnsName("*.fruit.example");
                sanBuilder.AddDnsName("rambutan.fruit.example");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "apple.fruit.example");
                    AssertMatch(true, cert, "blackberry.fruit.example");
                    AssertMatch(true, cert, "pome.fruit.example");
                    AssertMatch(true, cert, "pomme.fruit.example");
                    AssertMatch(true, cert, "rambutan.fruit.example");
                    AssertMatch(false, cert, "apple.pome.fruit.example");
                    AssertMatch(false, cert, "apple.pomme.fruit.example");

                    AssertMatch(false, cert, "apple.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "blackberry.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "pome.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "pomme.fruit.example", allowWildcards: false);
                    // This one has a redundant dNSName after the wildcard
                    AssertMatch(true, cert, "rambutan.fruit.example", allowWildcards: false);

                    AssertMatch(true, cert, "fruit.example");
                    AssertMatch(true, cert, "fruit.example", allowWildcards: false);
                }
            }
        }

        [Fact]
        public static void WildcardsDoNotMatchAfterFirstPosition()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("fruit.example");
                sanBuilder.AddDnsName("*.fruit.example");
                sanBuilder.AddDnsName("*.*.fruit.example");
                sanBuilder.AddDnsName("apple.*.fruit.example");
                sanBuilder.AddDnsName("rambutan.fruit.example");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "apple.fruit.example");
                    AssertMatch(true, cert, "blackberry.fruit.example");
                    AssertMatch(true, cert, "pome.fruit.example");
                    AssertMatch(true, cert, "pomme.fruit.example");
                    AssertMatch(true, cert, "rambutan.fruit.example");
                    AssertMatch(false, cert, "apple.pome.fruit.example");
                    AssertMatch(false, cert, "apple.pomme.fruit.example");

                    AssertMatch(false, cert, "apple.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "blackberry.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "pome.fruit.example", allowWildcards: false);
                    AssertMatch(false, cert, "pomme.fruit.example", allowWildcards: false);
                    // This one has a redundant dNSName after the wildcard
                    AssertMatch(true, cert, "rambutan.fruit.example", allowWildcards: false);

                    AssertMatch(true, cert, "fruit.example");
                    AssertMatch(true, cert, "fruit.example", allowWildcards: false);
                }
            }
        }

        [Fact]
        public static void DnsMatchNotCaseSensitive()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("fruit.EXAMPLE");
                sanBuilder.AddDnsName("*.FrUIt.eXaMpLE");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "aPPlE.fruit.example");
                    AssertMatch(true, cert, "tOmaTO.FRUIT.example");
                    AssertMatch(false, cert, "tOmaTO.vegetable.example");
                    AssertMatch(true, cert, "FRUit.example");
                    AssertMatch(false, cert, "VEGetaBlE.example");
                }
            }
        }

        [Fact]
        public static void DnsNameIgnoresTrailingPeriod()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("fruit.example.");
                sanBuilder.AddDnsName("*.FrUIt.eXaMpLE.");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "aPPlE.fruit.example");
                    AssertMatch(true, cert, "tOmaTO.FRUIT.example");
                    AssertMatch(false, cert, "tOmaTO.vegetable.example");
                    AssertMatch(true, cert, "FRUit.example");
                    AssertMatch(false, cert, "VEGetaBlE.example");
                }
            }
        }

        [Fact]
        public static void DnsNameMatchIgnoresTrailingPeriodFromParameter()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=10.0.0.1",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("fruit.EXAMPLE");
                sanBuilder.AddDnsName("*.FrUIt.eXaMpLE");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "aPPlE.fruit.example.");
                    AssertMatch(true, cert, "tOmaTO.FRUIT.example.");
                    AssertMatch(false, cert, "tOmaTO.vegetable.example.");
                    AssertMatch(true, cert, "FRUit.example.");
                    AssertMatch(false, cert, "VEGetaBlE.example.");
                }
            }
        }

        [Fact]
        public static void CommonNameMatchDoesNotIgnoreTrailingPeriodFromParameter()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=fruit.example",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "FRUit.example");
                    AssertMatch(false, cert, "FRUit.example", allowCommonName: false);
                    AssertMatch(false, cert, "FRUit.example.");
                }
            }
        }

        [Fact]
        public static void CommonNameMatchDoesNotIgnoreTrailingPeriodFromValue()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=fruit.example.",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(true, cert, "FRUit.example.");
                    AssertMatch(false, cert, "FRUit.example.", allowCommonName: false);
                    AssertMatch(false, cert, "FRUit.example");
                }
            }
        }

        [Fact]
        public static void NoMatchIfMultipleCommonNames()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=fruit.example, CN=potato.vegetable.example",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(false, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void NoMatchIfMultipleCommonNamesWithMultiRDN()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=fruit.example, CN=potato.vegetable.example+ST=Idaho",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(false, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void NoMatchIfCommonNamesInMultiRDN()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=potato.vegetable.example+ST=Idaho",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(false, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void MultiRdnWithNoCommonNameIsOK()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=potato.vegetable.example,ST=Idaho+ST=Utah",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(true, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void NoMatchAndNoCommonName()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "ST=Idaho",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(false, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void NoMatchAndEmptyCommonName()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=,ST=Idaho",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "FRUit.example");
                    AssertMatch(false, cert, "potato.vegetable.example");
                }
            }
        }

        [Fact]
        public static void NoMatchOnEmptyDnsName()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=potato.vegetable.example",
                    key,
                    HashAlgorithmName.SHA256);

                req.CertificateExtensions.Add(
                    new X509Extension("2.5.29.17", "30028200".HexToByteArray(), false));

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    X509SubjectAlternativeNameExtension sanExt =
                        (X509SubjectAlternativeNameExtension)cert.Extensions[0];

                    if (sanExt.EnumerateDnsNames().Single() != "")
                    {
                        throw new InvalidOperationException("Invalid test data");
                    }

                    AssertMatch(false, cert, "example");
                    AssertMatch(false, cert, "example.");
                }
            }
        }

        [Fact]
        public static void NoMatchOnDnsNameWithLeadingPeriod()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=potato.vegetable.example",
                    key,
                    HashAlgorithmName.SHA256);

                req.CertificateExtensions.Add(
                    new X509Extension(
                        "2.5.29.17",
                        "301682142E70656163682E66727569742E6578616D706C65".HexToByteArray(),
                        false));

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    X509SubjectAlternativeNameExtension sanExt =
                        (X509SubjectAlternativeNameExtension)cert.Extensions[0];

                    if (sanExt.EnumerateDnsNames().Single() != ".peach.fruit.example")
                    {
                        throw new InvalidOperationException("Invalid test data");
                    }

                    AssertMatch(false, cert, "peach.fruit.example");
                }
            }
        }

        [Fact]
        public static void WildcardRequiresSuffixToMatch()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=potato.vegetable.example",
                    key,
                    HashAlgorithmName.SHA256);

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("*");
                sanBuilder.AddDnsName("*.");
                sanBuilder.AddEmailAddress("it@fruit.example");

                req.CertificateExtensions.Add(sanBuilder.Build());

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(false, cert, "example");
                    AssertMatch(false, cert, "example.");
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void DnsMatchIgnoresUriAndSrv(bool includeDnsName)
        {
            // URI: https://tangerine.fruit.example
            // Other(SRV): _mail.ugli.fruit.example
            ReadOnlySpan<byte> uriAndSrv = new byte[]
            {
                0x30, 0x4A, 0x86, 0x20, 0x68, 0x74, 0x74, 0x70, 0x73, 0x3A, 0x2F, 0x2F, 0x74, 0x61, 0x6E, 0x67,
                0x65, 0x72, 0x69, 0x6E, 0x65, 0x2E, 0x66, 0x72, 0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D,
                0x70, 0x6C, 0x65, 0x2F, 0xA0, 0x26, 0x06, 0x08, 0x2B, 0x06, 0x01, 0x05, 0x05, 0x07, 0x08, 0x07,
                0xA0, 0x1A, 0x16, 0x18, 0x5F, 0x6D, 0x61, 0x69, 0x6C, 0x2E, 0x75, 0x67, 0x6C, 0x69, 0x2E, 0x66,
                0x72, 0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65,
            };

            // URI: https://tangerine.fruit.example
            // Other(SRV): _mail.ugli.fruit.example
            // DNS: strawberry.aggregate.fruit.example
            // DNS: strawberry.fruit.example
            ReadOnlySpan<byte> uriSrvAndDns = new byte[]
            {
                0x30, 0x81, 0x88, 0x86, 0x20, 0x68, 0x74, 0x74, 0x70, 0x73, 0x3A, 0x2F, 0x2F, 0x74, 0x61, 0x6E,
                0x67, 0x65, 0x72, 0x69, 0x6E, 0x65, 0x2E, 0x66, 0x72, 0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61,
                0x6D, 0x70, 0x6C, 0x65, 0x2F, 0xA0, 0x26, 0x06, 0x08, 0x2B, 0x06, 0x01, 0x05, 0x05, 0x07, 0x08,
                0x07, 0xA0, 0x1A, 0x16, 0x18, 0x5F, 0x6D, 0x61, 0x69, 0x6C, 0x2E, 0x75, 0x67, 0x6C, 0x69, 0x2E,
                0x66, 0x72, 0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x82, 0x22, 0x73,
                0x74, 0x72, 0x61, 0x77, 0x62, 0x65, 0x72, 0x72, 0x79, 0x2E, 0x61, 0x67, 0x67, 0x72, 0x65, 0x67,
                0x61, 0x74, 0x65, 0x2E, 0x66, 0x72, 0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C,
                0x65, 0x82, 0x18, 0x73, 0x74, 0x72, 0x61, 0x77, 0x62, 0x65, 0x72, 0x72, 0x79, 0x2E, 0x66, 0x72,
                0x75, 0x69, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65,
            };

            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=strawberry.fruit.example",
                    key,
                    HashAlgorithmName.SHA256);
                
                req.CertificateExtensions.Add(
                    new X509Extension(
                        "2.5.29.17",
                        includeDnsName ? uriSrvAndDns : uriAndSrv,
                        critical: false));

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    AssertMatch(includeDnsName, cert, "strawberry.aggregate.fruit.example");
                    AssertMatch(true, cert, "strawberry.fruit.example");
                    AssertMatch(false, cert, "tangerine.fruit.example");
                    AssertMatch(false, cert, "ugli.fruit.example");
                }
            }
        }

        [Fact]
        public static void TooManySanExtensionsThrows()
        {
            byte[] tooManySans = (
                "3082021430820175A00302010202083C883E44C34DA5CB300A06082A8648CE3D" +
                "04030230233121301F06035504031318706F7461746F2E766567657461626C65" +
                "2E6578616D706C65301E170D3232303630383232333530365A170D3232303630" +
                "383232333730365A30233121301F06035504031318706F7461746F2E76656765" +
                "7461626C652E6578616D706C6530819B301006072A8648CE3D020106052B8104" +
                "0023038186000400BA92930960C2C98D81F4DEAB62E75C0F768B5518A8FF58C2" +
                "1D43B453AA2D1C73FA6BB0586349DDD61D0C25DC46B444BF5806F72F0F83546C" +
                "B27583AE0007B101780007B7AE5717D4343C85D168212F2C2E4EC8F8B9F1953F" +
                "A159C5E74A191B609E6A38FAAC404E3A0C094DD39A6732673545EE8C195A2B9B" +
                "600420E9F55C145232304EA350304E30180603551D110411300F820D66727569" +
                "742E6578616D706C6530320603551D11042B302982152A2E64727570652E6672" +
                "7569742E6578616D706C65811069744066727569742E6578616D706C65300A06" +
                "082A8648CE3D04030203818C003081880242009DA8DF6009D12EC733ADEE7479" +
                "18B4611E185E478BA1D33AB7150A6A29F21FF31B48846B132868934A9F989C88" +
                "39C7B8955A70DD5D4E9E1BB7C0D78F6AD8C3C6DC024200958482B9444D1AD2D3" +
                "F67B51AD13064F2FDD4EC2F64ECB352D3F11BE8066F9021DD0CF309654351781" +
                "69E940B767111BB2D28119EB3A2461617792F1CDF131F794").HexToByteArray();

            using (X509Certificate2 cert = new X509Certificate2(tooManySans))
            {
                Assert.Throws<CryptographicException>(
                    () => cert.MatchesHostname("fruit.example"));

                Assert.Throws<CryptographicException>(
                    () => cert.MatchesHostname("fruit.example", allowWildcards: false));

                Assert.Throws<CryptographicException>(
                    () => cert.MatchesHostname("fruit.example", allowCommonName: false));

                Assert.Throws<CryptographicException>(
                    () => cert.MatchesHostname("fruit.example", false, false));

                // But argument validation comes first.
                Assert.Throws<ArgumentNullException>("hostname", () => cert.MatchesHostname(null));
                Assert.Throws<ArgumentNullException>("hostname", () => cert.MatchesHostname(null, false, true));
                Assert.Throws<ArgumentNullException>("hostname", () => cert.MatchesHostname(null, true, false));
                Assert.Throws<ArgumentNullException>("hostname", () => cert.MatchesHostname(null, false, false));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void OnlyValidHostnamesAccepted(bool addSanExt)
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=John Smith",
                    key,
                    HashAlgorithmName.SHA256);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset notBefore = now.AddMinutes(-1);
                DateTimeOffset notAfter = now.AddMinutes(1);

                if (addSanExt)
                {
                    SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                    sanBuilder.AddDnsName("fruit.example");
                    sanBuilder.AddDnsName("*.fruit.example");
                    sanBuilder.AddDnsName("*.*.fruit.example");
                    sanBuilder.AddDnsName("apple.*.fruit.example");
                    sanBuilder.AddDnsName("rambutan.fruit.example");
                    sanBuilder.AddEmailAddress("it@fruit.example");

                    req.CertificateExtensions.Add(sanBuilder.Build());
                }

                using (X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter))
                {
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("John Smith"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("*.pomme.fruit.example"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname(".pomme.fruit.example"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("*berry.fruit.example"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("cran*.fruit.example"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("cran*.fruit.example"));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname(""));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("."));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("*."));
                    Assert.Throws<ArgumentException>("hostname", () => cert.MatchesHostname("*"));
                }
            }
        }

        private static void AssertMatch(
            bool expected,
            X509Certificate2 cert,
            string hostname,
            bool allowWildcards = true,
            bool allowCommonName = true)
        {
            bool match = cert.MatchesHostname(hostname, allowWildcards, allowCommonName);

            if (match != expected)
            {
                string display = $"Matches {(hostname.Contains('*') ? "(literal) " : "")}'{hostname}'";

                if (!allowWildcards && !allowCommonName)
                {
                    display += " with no wildcards or common name fallback";
                }
                else if (!allowWildcards)
                {
                    display += " with no wildcards";
                }
                else if (!allowCommonName)
                {
                    display += " with no common name fallback";
                }

                if (expected)
                {
                    Assert.True(match, display);
                }
                else
                {
                    Assert.False(match, display);
                }
            }
        }
    }
}
