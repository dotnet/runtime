// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class CertificateRequestLoadTests
    {
        [Theory]
        [InlineData(CertificateRequestLoadOptions.Default, false)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, false)]
        [InlineData(CertificateRequestLoadOptions.Default, true)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadBigExponentRequest_Span(CertificateRequestLoadOptions options, bool oversized)
        {
            byte[] pkcs10 = TestData.BigExponentPkcs10Bytes;

            if (oversized)
            {
                Array.Resize(ref pkcs10, pkcs10.Length + 22);
            }

            CertificateRequest req = CertificateRequest.LoadSigningRequest(
                new ReadOnlySpan<byte>(pkcs10),
                HashAlgorithmName.SHA256,
                out int bytesConsumed,
                options);

            Assert.Equal(TestData.BigExponentPkcs10Bytes.Length, bytesConsumed);
            Assert.Equal(HashAlgorithmName.SHA256, req.HashAlgorithm);
            VerifyBigExponentRequest(req, options);
        }

        [Theory]
        [InlineData(CertificateRequestLoadOptions.Default)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadBigExponentRequest_Bytes(CertificateRequestLoadOptions options)
        {
            CertificateRequest req = CertificateRequest.LoadSigningRequest(
                TestData.BigExponentPkcs10Bytes,
                HashAlgorithmName.SHA384,
                options);

            Assert.Equal(HashAlgorithmName.SHA384, req.HashAlgorithm);
            VerifyBigExponentRequest(req, options);
        }

        [Theory]
        [InlineData(CertificateRequestLoadOptions.Default)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions)]
        public static void LoadBigExponentRequest_Bytes_Oversized(CertificateRequestLoadOptions options)
        {
            byte[] pkcs10 = TestData.BigExponentPkcs10Bytes;
            Array.Resize(ref pkcs10, pkcs10.Length + 1);

            Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequest(
                    pkcs10,
                    HashAlgorithmName.SHA384,
                    options));
        }

        [Theory]
        [InlineData(CertificateRequestLoadOptions.Default, false)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, false)]
        [InlineData(CertificateRequestLoadOptions.Default, true)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadBigExponentRequest_PemString(CertificateRequestLoadOptions options, bool multiPem)
        {
            string pem = TestData.BigExponentPkcs10Pem;

            if (multiPem)
            {
                pem = $@"
-----BEGIN UNRELATED-----
abcd
-----END UNRELATED-----
-----BEGIN CERTIFICATE REQUEST-----
!!!!!INVALID!!!!!
-----END CERTIFICATE REQUEST-----
-----BEGIN MORE UNRELATED-----
efgh
-----END MORE UNRELATED-----
{pem}
-----BEGIN CERTIFICATE REQUEST-----
!!!!!INVALID!!!!!
-----END CERTIFICATE REQUEST-----";
            }

            CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                pem,
                HashAlgorithmName.SHA512,
                options);

            Assert.Equal(HashAlgorithmName.SHA512, req.HashAlgorithm);
            VerifyBigExponentRequest(req, options);
        }

        [Theory]
        [InlineData(CertificateRequestLoadOptions.Default, false)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, false)]
        [InlineData(CertificateRequestLoadOptions.Default, true)]
        [InlineData(CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadBigExponentRequest_PemSpam(CertificateRequestLoadOptions options, bool multiPem)
        {
            string pem = TestData.BigExponentPkcs10Pem;

            if (multiPem)
            {
                pem = $@"
-----BEGIN UNRELATED-----
abcd
-----END UNRELATED-----
Free Floating Text
-----BEGIN CERTIFICATE REQUEST-----
!!!!!INVALID!!!!!
-----END CERTIFICATE REQUEST-----
-----BEGIN MORE UNRELATED-----
efgh
-----END MORE UNRELATED-----
More Text.
{pem}
-----BEGIN CERTIFICATE REQUEST-----
!!!!!INVALID!!!!!
-----END CERTIFICATE REQUEST-----";
            }

            CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                pem.AsSpan(),
                HashAlgorithmName.SHA1,
                options);

            Assert.Equal(HashAlgorithmName.SHA1, req.HashAlgorithm);
            VerifyBigExponentRequest(req, options);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void HashAlgorithmLaxInLoad()
        {
            HashAlgorithmName hashAlgorithm = new HashAlgorithmName("I promise to be a hash algorithm");

            CertificateRequest req = CertificateRequest.LoadSigningRequest(
                TestData.BigExponentPkcs10Bytes,
                hashAlgorithm);

            Assert.Equal(hashAlgorithm, req.HashAlgorithm);
        }

        [Fact]
        public static void LoadPem_NoMatch()
        {
            CryptographicException ex;

            const string NoMatchPem = @"
-----BEGIN CERTIFICATE REQUEST-----
%% Not Base64 %%
-----END CERTIFICATE REQUEST-----
-----BEGIN CERTIFICATE-----
AQAB
-----END CERTIFICATE-----";

            ex = Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequestPem(NoMatchPem, HashAlgorithmName.SHA256));

            Assert.Contains("CERTIFICATE REQUEST", ex.Message);

            ex = Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequestPem(
                    NoMatchPem.AsSpan(),
                    HashAlgorithmName.SHA256));

            Assert.Contains("CERTIFICATE REQUEST", ex.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadWithAttributes()
        {
            // Generated by `openssl req -new -keyin bigexponent.pem` where bigexponent.pem
            // represents the PKCS8 of TestData.RsaBigExponentParams
            // All default values were taken, except
            // Challenge password: 1234 (vs unspecified)
            // An optional company name: Fabrikam (vs unspecified)
            const string Pkcs10Pem =
                "-----BEGIN CERTIFICATE REQUEST-----\n" +
                "MIICujCCAaICAQAwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUx\n" +
                "ITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDCCASQwDQYJKoZIhvcN\n" +
                "AQEBBQADggERADCCAQwCggEBAK+BwcvYID9iSlOe1mCBdTcjk6KDfUiQ5IoZ3tNp\n" +
                "cxFWIJaNa+DT2qOKp3e+Au4La5O3JOjcwStjK0+oC7ySW85iT0ynzGBjBrOUA+KM\n" +
                "ky0k3VRv/k72o38QdwsiFeqMu1v0J+jE2Jt56zODdRAMX4PlXem0Rm3fvu5CU5rv\n" +
                "M+8Ye3dgw7GhshA8LYFEVkoMEDmgnIXPa1l061FvyNZiPJSuOloLs7THkpV9QyOR\n" +
                "Vmzz4qUq+wwUK54GgbiXJnGvK4LdOQo5uTnPcZVoaH5JkKYwUMp3aNzWs3iELxj9\n" +
                "sfbZ/wlrr3vrmNz5MNZvz9UD9Y1Bv/RiEuJOOvxF6kK9iEcCBQIAAARBoC4wEwYJ\n" +
                "KoZIhvcNAQkHMQYMBDEyMzQwFwYJKoZIhvcNAQkCMQoMCEZhYnJpa2FtMA0GCSqG\n" +
                "SIb3DQEBCwUAA4IBAQCr1X8D+ZkJqBmuZVEYqLPvNvie+KBycxgiJ08ZaV/dyndZ\n" +
                "cudn6G9K0hiIwwGrfI5gbIb7QdPi64g3l9VdIrdH3yvQ6AcOZ644paiUUpe3u93l\n" +
                "DTY+BGN7C0reJwL7ehalIrtS7hLKAQerg/qS7JO9aLRTbIXR52BQIUs9htYeATC5\n" +
                "VHHssrOZpIHqIN4oaZbE0BwZm0ap6RVD80Oexko8pjiz9XNmtUWadeXXtezuOWTb\n" +
                "duuJlh31kITIrbWVoMawMRq6JwNTPAFyiDMB/EFIvjxpUoS5yJe14bT8Hw2XvAFK\n" +
                "Z9jOhHEPmAsasfRRSwr6CXyIKqo1HVT1ARPgHKHX\n" +
                "-----END CERTIFICATE REQUEST-----";

            CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                Pkcs10Pem,
                HashAlgorithmName.SHA384);

            Assert.Equal(2, req.OtherRequestAttributes.Count);

            AsnEncodedData attr = req.OtherRequestAttributes[0];
            Assert.Equal("1.2.840.113549.1.9.7", attr.Oid.Value);
            Assert.Equal("0C0431323334", attr.RawData.ByteArrayToHex());

            attr = req.OtherRequestAttributes[1];
            Assert.Equal("1.2.840.113549.1.9.2", attr.Oid.Value);
            Assert.Equal("0C0846616272696B616D", attr.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void LoadUnsortedAttributes()
        {
            // This is TestData.BigExponentPkcs10Bytes, except
            // * A challenge password was added after the extensions requests
            // * The signature was changed to just 1 bit, to cut down on space.
            const string Pkcs10Pem = @"
-----BEGIN CERTIFICATE REQUEST-----
MIICJTCCAg4CAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls
b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/
YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr
YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib
eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ
dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5
z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi
Tjr8RepCvYhHAgUCAAAEQaBUMD0GCSqGSIb3DQEJDjEwMC4wLAYDVR0RBCUwI4cE
fwAAAYcQAAAAAAAAAAAAAAAAAAAAAYIJbG9jYWxob3N0MBMGCSqGSIb3DQEJBzEG
DAQxMjM0MA0GCSqGSIb3DQEBCwUAAwIHgA==
-----END CERTIFICATE REQUEST-----";

            Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequestPem(
                    Pkcs10Pem,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.SkipSignatureValidation));
        }

        [Fact]
        public static void LoadDuplicateExtensionRequests()
        {
            // This is TestData.BigExponentPkcs10Bytes, except
            // * A challenge password was added before the extensions requests
            // * The extensions requests attribute was cloned (appears twice)
            // * The signature was changed to just 1 bit, to cut down on space.
            const string Pkcs10Pem = @"
-----BEGIN CERTIFICATE REQUEST-----
MIICZTCCAk4CAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls
b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/
YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr
YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib
eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ
dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5
z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi
Tjr8RepCvYhHAgUCAAAEQaCBkzATBgkqhkiG9w0BCQcxBgwEMTIzNDA9BgkqhkiG
9w0BCQ4xMDAuMCwGA1UdEQQlMCOHBH8AAAGHEAAAAAAAAAAAAAAAAAAAAAGCCWxv
Y2FsaG9zdDA9BgkqhkiG9w0BCQ4xMDAuMCwGA1UdEQQlMCOHBH8AAAGHEAAAAAAA
AAAAAAAAAAAAAAGCCWxvY2FsaG9zdDANBgkqhkiG9w0BAQsFAAMCB4A=
-----END CERTIFICATE REQUEST-----
";

            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequestPem(
                    Pkcs10Pem,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.SkipSignatureValidation));

            Assert.Contains("Extension Request", ex.Message);
        }

        [Fact]
        public static void LoadMultipleExtensionRequestsInOneAttribute()
        {
            // This is TestData.BigExponentPkcs10Bytes, except
            // * A challenge password was added before the extensions requests
            // * The extensions requests attribute value was cloned within the one attribute node
            // * The signature was changed to just 1 bit, to cut down on space.
            const string Pkcs10Pem = @"
-----BEGIN CERTIFICATE REQUEST-----
MIICVjCCAj8CAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls
b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/
YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr
YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib
eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ
dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5
z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi
Tjr8RepCvYhHAgUCAAAEQaCBhDATBgkqhkiG9w0BCQcxBgwEMTIzNDBtBgkqhkiG
9w0BCQ4xYDAuMCwGA1UdEQQlMCOHBH8AAAGHEAAAAAAAAAAAAAAAAAAAAAGCCWxv
Y2FsaG9zdDAuMCwGA1UdEQQlMCOHBH8AAAGHEAAAAAAAAAAAAAAAAAAAAAGCCWxv
Y2FsaG9zdDANBgkqhkiG9w0BAQsFAAMCB4A=
-----END CERTIFICATE REQUEST-----";

            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => CertificateRequest.LoadSigningRequestPem(
                    Pkcs10Pem,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.SkipSignatureValidation));

            Assert.Contains("Extension Request", ex.Message);
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        [InlineData("SHA1")]
        public static void VerifySignature_RSA_PKCS1(string hashAlgorithm)
        {
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);

            using (RSA key = RSA.Create())
            {
                CertificateRequest first = new CertificateRequest(
                    "CN=Test",
                    key,
                    hashAlgorithmName,
                    RSASignaturePadding.Pkcs1);

                byte[] pkcs10;

                if (hashAlgorithm == "SHA1")
                {
                    pkcs10 = first.CreateSigningRequest(new RSASha1Pkcs1SignatureGenerator(key));
                }
                else
                {
                    pkcs10 = first.CreateSigningRequest();
                }

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _);

                pkcs10[^1] ^= 0xFF;

                Assert.Throws<CryptographicException>(
                    () => CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _));

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(
                    pkcs10,
                    hashAlgorithmName,
                    out _,
                    CertificateRequestLoadOptions.SkipSignatureValidation);
            }
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        [InlineData("SHA1")]
        public static void VerifySignature_RSA_PSS(string hashAlgorithm)
        {
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);

            using (RSA key = RSA.Create())
            {
                CertificateRequest first = new CertificateRequest(
                    "CN=Test",
                    key,
                    hashAlgorithmName,
                    RSASignaturePadding.Pss);

                byte[] pkcs10;

                if (hashAlgorithm == "SHA1")
                {
                    pkcs10 = first.CreateSigningRequest(new RSASha1PssSignatureGenerator(key));
                }
                else
                {
                    pkcs10 = first.CreateSigningRequest();
                }

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _);

                pkcs10[^1] ^= 0xFF;

                Assert.Throws<CryptographicException>(
                    () => CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _));

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(
                    pkcs10,
                    hashAlgorithmName,
                    out _,
                    CertificateRequestLoadOptions.SkipSignatureValidation);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void VerifySignature_DSA()
        {
            // macOS is limited to FIPS 186-2 DSA, so SHA-1 is the only valid algorithm.
            HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA1;

            using (DSA key = DSA.Create(TestData.GetDSA1024Params()))
            {
                DSAX509SignatureGenerator generator = new DSAX509SignatureGenerator(key);

                CertificateRequest first = new CertificateRequest(
                    new X500DistinguishedName("CN=Test"),
                    generator.PublicKey,
                    hashAlgorithmName);

                byte[] pkcs10 = first.CreateSigningRequest(generator);

                // The inbox version doesn't support DSA
                Assert.Throws<NotSupportedException>(
                    () => CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _));

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(
                    pkcs10,
                    hashAlgorithmName,
                    out _,
                    CertificateRequestLoadOptions.SkipSignatureValidation);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadAndSignRequest_NoRSAPadding()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset notBefore = now.AddMonths(-1);
            DateTimeOffset notAfter = now.AddMonths(1);

            using (RSA rootKey = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    rootKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootReq.CertificateExtensions.Add(
                    X509BasicConstraintsExtension.CreateForCertificateAuthority());

                using (X509Certificate2 rootCert = rootReq.CreateSelfSigned(notBefore, notAfter))
                {
                    CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                        TestData.BigExponentPkcs10Pem,
                        HashAlgorithmName.SHA384);

                    byte[] serial = new byte[] { 0x02, 0x04, 0x06, 0x08 };

                    Exception ex = Assert.Throws<InvalidOperationException>(
                        () => req.Create(rootCert, notBefore, notAfter, serial));

                    Assert.Contains(nameof(RSASignaturePadding), ex.Message);
                    Assert.Contains(nameof(X509SignatureGenerator), ex.Message);

                    X509SignatureGenerator gen =
                        X509SignatureGenerator.CreateForRSA(rootKey, RSASignaturePadding.Pkcs1);

                    X509Certificate2 issued = req.Create(
                        rootCert.SubjectName,
                        gen,
                        notBefore,
                        notAfter,
                        serial);

                    using (issued)
                    {
                        Assert.Equal("1.2.840.113549.1.1.12", issued.SignatureAlgorithm.Value);
                    }
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadAndSignRequest_WithRSAPadding()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset notBefore = now.AddMonths(-1);
            DateTimeOffset notAfter = now.AddMonths(1);

            using (RSA rootKey = RSA.Create(TestData.RsaBigExponentParams))
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    rootKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootReq.CertificateExtensions.Add(
                    X509BasicConstraintsExtension.CreateForCertificateAuthority());

                using (X509Certificate2 rootCert = rootReq.CreateSelfSigned(notBefore, notAfter))
                {
                    CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                        TestData.BigExponentPkcs10Pem,
                        HashAlgorithmName.SHA512,
                        signerSignaturePadding: RSASignaturePadding.Pkcs1);

                    byte[] serial = new byte[] { 0x02, 0x04, 0x06, 0x08 };

                    X509Certificate2 issued = req.Create(
                        rootCert,
                        notBefore,
                        notAfter,
                        serial);

                    using (issued)
                    {
                        Assert.Equal("1.2.840.113549.1.1.13", issued.SignatureAlgorithm.Value);
                    }

                    // Using a generator overrides the decision

                    X509SignatureGenerator gen =
                        X509SignatureGenerator.CreateForRSA(rootKey, RSASignaturePadding.Pss);

                    issued = req.Create(
                        rootCert.SubjectName,
                        gen,
                        notBefore,
                        notAfter,
                        serial);

                    using (issued)
                    {
                        Assert.Equal("1.2.840.113549.1.1.10", issued.SignatureAlgorithm.Value);
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void LoadAndSignRequest_ECDsaIgnoresRSAPadding(bool specifyAnyways)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset notBefore = now.AddMonths(-1);
            DateTimeOffset notAfter = now.AddMonths(1);

            using (ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384))
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    rootKey,
                    HashAlgorithmName.SHA384);

                rootReq.CertificateExtensions.Add(
                    X509BasicConstraintsExtension.CreateForCertificateAuthority());

                using (X509Certificate2 rootCert = rootReq.CreateSelfSigned(notBefore, notAfter))
                {
                    RSASignaturePadding padding = specifyAnyways ? RSASignaturePadding.Pss : null;

                    // A PKCS10 for an ECDSA key with no attributes at all.
                    const string Pkcs10Pem = @"
-----BEGIN CERTIFICATE REQUEST-----
  MIIBCjCBkgIBADATMREwDwYDVQQDEwhOb3QgUm9vdDB2MBAGByqGSM49AgEGBSuB
  BAAiA2IABATbHVzs8lyAElJbPxYW0PJWosOg6bdkQQvem8Qq8EXMGCLk13Hibxzb
  eViS8ZTTq84sgRYpDhEQHwufix/MQ0gECe93LN1X6DZQgvvy1FVGm8XNtPTrZgGO
  GwZ3IQmaBqAAMAoGCCqGSM49BAMDA2cAMGQCMHtDz2m+GnrwjJ9H7/UE578cePe1
  1luBYpJcXKCAusDxsnvC8fAOkjXI6rwp9AVcjAIwIKoBVpkgyOzTDs+rEBJEQaKa
  WK1BHMwWl7lY6Z0WrMIQuGsdljzpbeLk8h7Kdcbm
  -----END CERTIFICATE REQUEST-----";

                    CertificateRequest req = CertificateRequest.LoadSigningRequestPem(
                        Pkcs10Pem,
                        HashAlgorithmName.SHA512,
                        signerSignaturePadding: padding);

                    byte[] serial = new byte[] { 0x02, 0x04, 0x06, 0x08 };

                    X509Certificate2 issued = req.Create(
                        rootCert,
                        notBefore,
                        notAfter,
                        serial);

                    using (issued)
                    {
                        Assert.Equal("1.2.840.10045.4.3.4", issued.SignatureAlgorithm.Value);
                    }
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72906", TestPlatforms.Android)]
        public static void LoadRequestWithDuplicateAttributes()
        {
            // The output from CertificateRequestUsageTests.CreateSigningRequestWithDuplicateAttributes
            const string Pkcs10Pem =
                "-----BEGIN CERTIFICATE REQUEST-----\n" +
                "MIIC/TCCAeUCAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u\n" +
                "MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp\n" +
                "b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls\n" +
                "b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/\n" +
                "YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr\n" +
                "YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib\n" +
                "eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ\n" +
                "dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5\n" +
                "z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi\n" +
                "Tjr8RepCvYhHAgUCAAAEQaArMBMGCSqGSIb3DQEJBzEGDAQxMjM0MBQGCSqGSIb3\n" +
                "DQEJBzEHDAUxMjM0NTANBgkqhkiG9w0BAQsFAAOCAQEAB3lwd8z6XGmX6mbOo3Xm\n" +
                "+ZyW4glQtJ51FAXA1zy83y5Uqyf85ZtTFl6UPw970x8KlSlY/9eMhyo/LORAwQql\n" +
                "J8oga5ho2clJF62IJX9/Ih6JlmcMfyi9qEQaqsY/Og4IBSvxQo39SGzGFLv9mhxa\n" +
                "R1YWoVggsbs638ph/T8Upz/GKb/0tBnGBThRZJip7HLugzzvSJGnirpp0fZhnwWM\n" +
                "l1IlddN5/AdZ86j/r5RNlDKDHlwqI3UJ5Olb1iVFt00d/vwVRM09V1ZNIpiCmPv6\n" +
                "MJG3L+NUKOpSUDXn9qtCxB0pd1MaZVit5EvJI98sKZhILRz3S5KXTxf+kBjNxC98\n" +
                "AQ==\n" +
                "-----END CERTIFICATE REQUEST-----";

            CertificateRequest req =
                CertificateRequest.LoadSigningRequestPem(Pkcs10Pem, HashAlgorithmName.SHA256);

            Assert.Equal(2, req.OtherRequestAttributes.Count);

            AsnEncodedData attr = req.OtherRequestAttributes[0];
            Assert.Equal("1.2.840.113549.1.9.7", attr.Oid.Value);
            Assert.Equal("0C0431323334", attr.RawData.ByteArrayToHex());

            attr = req.OtherRequestAttributes[1];
            Assert.Equal("1.2.840.113549.1.9.7", attr.Oid.Value);
            Assert.Equal("0C053132333435", attr.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void LoadRequestWithAttributeValues()
        {
            // The output from CertificateRequestUsageTests.CreateSigningRequestWithDuplicateAttributes,
            // but modified to be
            //
            // Attribute
            //   id: ChallengePassword
            //   values:
            //     cp1
            //     cp2
            //
            // instead of
            //
            // Attribute
            //  id: ChallengePassword
            //  values:
            //    cp1
            // Attribute
            //  id: ChallengePassword
            //  values:
            //    cp2
            //
            // And then made the signature 0 bits long rather than really compute it.
            const string Pkcs10Pem = @"
-----BEGIN CERTIFICATE REQUEST-----
MIIB7DCCAdYCAQAwgYoxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
b24xIDAeBgNVBAsTFy5ORVQgRnJhbWV3b3JrIChDb3JlRlgpMRIwEAYDVQQDEwls
b2NhbGhvc3QwggEkMA0GCSqGSIb3DQEBAQUAA4IBEQAwggEMAoIBAQCvgcHL2CA/
YkpTntZggXU3I5Oig31IkOSKGd7TaXMRViCWjWvg09qjiqd3vgLuC2uTtyTo3MEr
YytPqAu8klvOYk9Mp8xgYwazlAPijJMtJN1Ub/5O9qN/EHcLIhXqjLtb9CfoxNib
eeszg3UQDF+D5V3ptEZt377uQlOa7zPvGHt3YMOxobIQPC2BRFZKDBA5oJyFz2tZ
dOtRb8jWYjyUrjpaC7O0x5KVfUMjkVZs8+KlKvsMFCueBoG4lyZxryuC3TkKObk5
z3GVaGh+SZCmMFDKd2jc1rN4hC8Y/bH22f8Ja69765jc+TDWb8/VA/WNQb/0YhLi
Tjr8RepCvYhHAgUCAAAEQaAcMBoGCSqGSIb3DQEJBzENDAQxMjM0DAUxMjM0NTAN
BgkqhkiG9w0BAQsFAAMBAA==
-----END CERTIFICATE REQUEST-----";

            CertificateRequest req =
                CertificateRequest.LoadSigningRequestPem(
                    Pkcs10Pem,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.SkipSignatureValidation);

            Assert.Equal(2, req.OtherRequestAttributes.Count);

            AsnEncodedData attr = req.OtherRequestAttributes[0];
            Assert.Equal("1.2.840.113549.1.9.7", attr.Oid.Value);
            Assert.Equal("0C0431323334", attr.RawData.ByteArrayToHex());

            attr = req.OtherRequestAttributes[1];
            Assert.Equal("1.2.840.113549.1.9.7", attr.Oid.Value);
            Assert.Equal("0C053132333435", attr.RawData.ByteArrayToHex());
        }

        private static void VerifyBigExponentRequest(
            CertificateRequest req,
            CertificateRequestLoadOptions options)
        {
            VerifyBigExponentRequest(
                req,
                (options & CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions) != 0);
        }

        private static void VerifyBigExponentRequest(CertificateRequest req, bool loadExtensions)
        {
            Assert.Equal("1.2.840.113549.1.1.1", req.PublicKey.Oid.Value);
            Assert.Equal("0500", req.PublicKey.EncodedParameters.RawData.ByteArrayToHex());
            Assert.Null(req.PublicKey.EncodedParameters.Oid);
            Assert.Null(req.PublicKey.EncodedKeyValue.Oid);

            Assert.Equal(
                "3082010C0282010100AF81C1CBD8203F624A539ED6608175372393A2837D4890" +
                    "E48A19DED36973115620968D6BE0D3DAA38AA777BE02EE0B6B93B724E8DCC12B" +
                    "632B4FA80BBC925BCE624F4CA7CC606306B39403E28C932D24DD546FFE4EF6A3" +
                    "7F10770B2215EA8CBB5BF427E8C4D89B79EB338375100C5F83E55DE9B4466DDF" +
                    "BEEE42539AEF33EF187B7760C3B1A1B2103C2D8144564A0C1039A09C85CF6B59" +
                    "74EB516FC8D6623C94AE3A5A0BB3B4C792957D432391566CF3E2A52AFB0C142B" +
                    "9E0681B8972671AF2B82DD390A39B939CF719568687E4990A63050CA7768DCD6" +
                    "B378842F18FDB1F6D9FF096BAF7BEB98DCF930D66FCFD503F58D41BFF46212E2" +
                    "4E3AFC45EA42BD884702050200000441",
                req.PublicKey.EncodedKeyValue.RawData.ByteArrayToHex());

            Assert.Equal(
                "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                req.SubjectName.Name);

            if (loadExtensions)
            {
                Assert.Equal(1, req.CertificateExtensions.Count);

                X509SubjectAlternativeNameExtension san =
                    Assert.IsType<X509SubjectAlternativeNameExtension>(req.CertificateExtensions[0]);

                Assert.Equal(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, san.EnumerateIPAddresses());
                Assert.Equal(new[] { "localhost" }, san.EnumerateDnsNames());
            }
            else
            {
                Assert.Empty(req.CertificateExtensions);
            }

            Assert.Empty(req.OtherRequestAttributes);
        }
    }
}
