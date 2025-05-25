// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class MLKemCertTests
    {
        public static bool MLKemIsNotSupported => !MLKem.IsSupported;

        [Fact]
        public static void GetMLKemPublicKey_NotMLKem()
        {
            using X509Certificate2 cert = LoadCertificateFromPem(TestData.RsaCertificate);
            Assert.Null(cert.GetMLKemPublicKey());
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemCertificatePublicKeys))]
        public static void GetMLKemPublicKey_IetfMLKemKeys(string certificatePem, byte[] expectedSpki)
        {
            MLKem kem;

            using (X509Certificate2 cert = LoadCertificateFromPem(certificatePem))
            {
                kem = cert.GetMLKemPublicKey();
            }

            using (kem)
            {
                Assert.NotNull(kem);
                AssertExtensions.SequenceEqual(expectedSpki, kem.ExportSubjectPublicKeyInfo());
            }
        }

        [ConditionalTheory(nameof(MLKemIsNotSupported))]
        [InlineData(MLKemTestData.IetfMlKem512CertificatePem)]
        [InlineData(MLKemTestData.IetfMlKem768CertificatePem)]
        [InlineData(MLKemTestData.IetfMlKem1024CertificatePem)]
        public static void GetMLKemPublicKey_PlatformNotSupported(string certificatePem)
        {
            using (X509Certificate2 cert = LoadCertificateFromPem(certificatePem))
            {
                Assert.Throws<PlatformNotSupportedException>(() => cert.GetMLKemPublicKey());
            }
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void GetMLKemPublicKey_BadSubjectPublicKeyInfo_AlgorithmParameters()
        {
            // This is an id-alg-ml-kem-512 certificate, however the algorithm parameters are [0x05, 0x00].
            using X509Certificate2 cert = LoadCertificateFromPem("""
                -----BEGIN CERTIFICATE-----
                MIIEADCCA6WgAwIBAgIJAPS8KG3Jb0zRMAoGCCqGSM49BAMCMA8xDTALBgNVBAMT
                BFJvb3QwHhcNMjUwNDE0MjEwMjUwWhcNMjYwNDEzMjEwMjUwWjAPMQ0wCwYDVQQD
                EwRUZXN0MIIDNDANBglghkgBZQMEBAEFAAOCAyEAACZFEmcJ+HtcbfkRa6F1AgiV
                xcMDGvu/zfla8oOQlTlqIfI7sSMgkeuPmDvLyVQA1KM1xVUYf8IxG3lAKiYa6ydv
                HeTKYkWNWqdy8MJcOkgk3AlatjWEz4Y81Nypc2yq+twcoiSUdZRrS/SyCDNOxBnJ
                FhV+hMkFbecTzQVdaQCdKVwcGmoH4Agpe9GxZ9RkHtlG4EMs3ShaXpeU5tPPdCIB
                KZmsUMQsrMaOZZw3zdk6cBZhknGcwOItExfBo/AHFQBNUForYGBVYmQhYRvMZEJd
                LxRS4yuNaLJYSmYYDWSLiSVGfbMG38OYWerDg6tgsfl5MFHIriFRYhFms/hitSxE
                zXlmAsgPRdPAlXSeqdZetgJHUnYbNkiRx2aUQeOuMOQxeNE7LRw7OvyZn9S0RONZ
                EyCnZqzKbEBZuPNCiSEfgsWjM+oCkVTHFrZAqsG8fVFt+wCIHvqUcnJam8V90Xge
                egaJxeOZyxsfQaAx0zw1KNQhBHpIopyRIiB0/MU4gUJCx4Ug0gEXFhVRjTbMmLyI
                XyEnzhgroVBBsHc3qKunrMTAoiYk1JxIVGdKVPEa/7A9eua5DnQTuFShMaZJbQJ0
                Sma6fkuN+XNXfqkC59G3ykDKeqY/LAS11tqHpzUUjwA/eDlQELEaych+Gre9zYsY
                zrpyJuaJndOn8pN2htUve6IdGkOafaBrNnwkD2RAWMQ0M3iA/+ed6fKid7E7C4KQ
                fiIQTwE0Qjo/BhZ/+CVCdpIjYcA2jLxzv7jDXNHAp5B2xCCwU+YExwmMdDRPmlCU
                mDsXrdRsNJhMoxfN7SsKHswSxSEkZVOJ/bgT27qLBDECWfCp7lewOrBLGAeav6M2
                //hU2nC9gnYKOfs9m7KDffBS6SVEnIhoAkK3OjmrnTMqnANxzQZmP8amWWjKJ2Y+
                RJhLkci1GiywtjZMziCX2yhuAfuNLIcUcsaBF+pklxlvX1ajzneNSLaH2vRAu0g3
                SLfC8IifAtqwbsZPM+WeSZMaIAhMfnhWOnZrUiOQncOFxbxL2K/1G1zFL2D9GB2K
                xDU3JUq8Lyno/LhpjNSjDTALMAkGA1UdEwQCMAAwCgYIKoZIzj0EAwIDSQAwRgIh
                AJMsAxDCDI91sc6QG1Ss7boIHIKnuT37UO0+z2GKQ23mAiEApWKWPwyYVAtxvsCQ
                p41euJgZq/VcSIJ0zW7LOTVQL9U=
                -----END CERTIFICATE-----
                """);
            Assert.Throws<CryptographicException>(() => cert.GetMLKemPublicKey());
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void GetMLKemPublicKey_BadSubjectPublicKeyInfo_KeySize()
        {
            // This is an id-alg-ml-kem-512 certificate, however the public encapsulation key has been truncated by
            // 1 byte.
            using X509Certificate2 cert = LoadCertificateFromPem("""
                -----BEGIN CERTIFICATE-----
                MIID+zCCA6KgAwIBAgIJAL2oWcykqC3GMAoGCCqGSM49BAMCMA8xDTALBgNVBAMT
                BFJvb3QwHhcNMjUwNDE0MjEwNTUxWhcNMjYwNDEzMjEwNTUxWjAPMQ0wCwYDVQQD
                EwRUZXN0MIIDMTALBglghkgBZQMEBAEDggMgACZFEmcJ+HtcbfkRa6F1AgiVxcMD
                Gvu/zfla8oOQlTlqIfI7sSMgkeuPmDvLyVQA1KM1xVUYf8IxG3lAKiYa6ydvHeTK
                YkWNWqdy8MJcOkgk3AlatjWEz4Y81Nypc2yq+twcoiSUdZRrS/SyCDNOxBnJFhV+
                hMkFbecTzQVdaQCdKVwcGmoH4Agpe9GxZ9RkHtlG4EMs3ShaXpeU5tPPdCIBKZms
                UMQsrMaOZZw3zdk6cBZhknGcwOItExfBo/AHFQBNUForYGBVYmQhYRvMZEJdLxRS
                4yuNaLJYSmYYDWSLiSVGfbMG38OYWerDg6tgsfl5MFHIriFRYhFms/hitSxEzXlm
                AsgPRdPAlXSeqdZetgJHUnYbNkiRx2aUQeOuMOQxeNE7LRw7OvyZn9S0RONZEyCn
                ZqzKbEBZuPNCiSEfgsWjM+oCkVTHFrZAqsG8fVFt+wCIHvqUcnJam8V90XgeegaJ
                xeOZyxsfQaAx0zw1KNQhBHpIopyRIiB0/MU4gUJCx4Ug0gEXFhVRjTbMmLyIXyEn
                zhgroVBBsHc3qKunrMTAoiYk1JxIVGdKVPEa/7A9eua5DnQTuFShMaZJbQJ0Sma6
                fkuN+XNXfqkC59G3ykDKeqY/LAS11tqHpzUUjwA/eDlQELEaych+Gre9zYsYzrpy
                JuaJndOn8pN2htUve6IdGkOafaBrNnwkD2RAWMQ0M3iA/+ed6fKid7E7C4KQfiIQ
                TwE0Qjo/BhZ/+CVCdpIjYcA2jLxzv7jDXNHAp5B2xCCwU+YExwmMdDRPmlCUmDsX
                rdRsNJhMoxfN7SsKHswSxSEkZVOJ/bgT27qLBDECWfCp7lewOrBLGAeav6M2//hU
                2nC9gnYKOfs9m7KDffBS6SVEnIhoAkK3OjmrnTMqnANxzQZmP8amWWjKJ2Y+RJhL
                kci1GiywtjZMziCX2yhuAfuNLIcUcsaBF+pklxlvX1ajzneNSLaH2vRAu0g3SLfC
                8IifAtqwbsZPM+WeSZMaIAhMfnhWOnZrUiOQncOFxbxL2K/1G1zFL2D9GB2KxDU3
                JUq8Lyno/LhpjNSjDTALMAkGA1UdEwQCMAAwCgYIKoZIzj0EAwIDRwAwRAIgMUxz
                ljArZfe1xJWJGTwtEact/twVYY03bIsC2Tkmjc8CIA2Kz5Mi5eOWk1sx5bh4g14Y
                yvTF2ROwnfWNAVt9BbMU
                -----END CERTIFICATE-----
                """);
            Assert.Throws<CryptographicException>(() => cert.GetMLKemPublicKey());
        }

        public static IEnumerable<object[]> MLKemCertificatePublicKeys
        {
            get
            {
                yield return [MLKemTestData.IetfMlKem512CertificatePem, MLKemTestData.IetfMlKem512Spki];
                yield return [MLKemTestData.IetfMlKem768CertificatePem, MLKemTestData.IetfMlKem768Spki];
                yield return [MLKemTestData.IetfMlKem1024CertificatePem, MLKemTestData.IetfMlKem1024Spki];
            }
        }

        internal static X509Certificate2 LoadCertificateFromPem(string pem)
        {
#if NET
            return X509Certificate2.CreateFromPem(pem);
#else
            return new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(pem));
#endif
        }
    }
}
