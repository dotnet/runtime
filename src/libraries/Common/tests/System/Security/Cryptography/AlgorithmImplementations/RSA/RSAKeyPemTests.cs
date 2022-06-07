// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class RSAKeyPemTests
    {
        private const string AmbiguousExceptionMarker = "multiple keys";
        private const string EncryptedExceptionMarker = "encrypted key";
        private const string NoPemExceptionMarker = "No supported key";

        [Fact]
        public static void ImportFromPem_NoPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"these aren't the PEMs you're looking for";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";

                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8UnEncrypted_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAtz9Z9e6L1V4kt/8C
mtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7UERRHhvKNiUn4Xz0
KzgGFQIDAQABAkEAr+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrp
Ykat3c2nWb0EGGi5aWgXxQHoi7z97/ACD4X3KQIhAPNyex6GdiBVlNPHOgInTU8a
mARKKVHIXM0SxvxXrRl7AiEAwLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0
Ya8CHwiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5UsCIQCsJMzx1JuilQAN
acPiMCuFTnRSFYAhozpmsqoLyTREqwIhAMLJlZTGjEB2N+sEazH5ToEczQzKqp7t
9juGNbOPhoEL
-----END PRIVATE KEY-----";

                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8UnEncrypted_UnrelatedAlgorithmIsIgnored()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----
-----BEGIN PRIVATE KEY-----
MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAtz9Z9e6L1V4kt/8C
mtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7UERRHhvKNiUn4Xz0
KzgGFQIDAQABAkEAr+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrp
Ykat3c2nWb0EGGi5aWgXxQHoi7z97/ACD4X3KQIhAPNyex6GdiBVlNPHOgInTU8a
mARKKVHIXM0SxvxXrRl7AiEAwLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0
Ya8CHwiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5UsCIQCsJMzx1JuilQAN
acPiMCuFTnRSFYAhozpmsqoLyTREqwIhAMLJlZTGjEB2N+sEazH5ToEczQzKqp7t
9juGNbOPhoEL
-----END PRIVATE KEY-----";

                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_SubjectPublicKeyInfo_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQx
m5NTLEHDwUd7idstLzPXuah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQ==
-----END PUBLIC KEY-----";
                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(false);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters.ToPublic(), rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_SubjectPublicKeyInfo_IgnoresUnrelatedAlgorithm()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----
-----BEGIN PUBLIC KEY-----
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQx
m5NTLEHDwUd7idstLzPXuah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQ==
-----END PUBLIC KEY-----";
                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(false);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters.ToPublic(), rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPublicKey_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN RSA PUBLIC KEY-----
MEgCQQC3P1n17ovVXiS3/wKa0WqFQ8ltJT5UMZuTUyxBw8FHe4nbLS8z17modFhI
4GqOaDtQRFEeG8o2JSfhfPQrOAYVAgMBAAE=
-----END RSA PUBLIC KEY-----";

                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(false);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters.ToPublic(), rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_PrecedingUnrelatedPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN CERTIFICATE-----
MIICTzCCAgmgAwIBAgIJAMQtYhFJ0+5jMA0GCSqGSIb3DQEBBQUAMIGSMQswCQYD
VQQGEwJVUzETMBEGA1UECAwKV2FzaGluZ3RvbjEQMA4GA1UEBwwHUmVkbW9uZDEY
MBYGA1UECgwPTWljcm9zb2Z0IENvcnAuMSAwHgYDVQQLDBcuTkVUIEZyYW1ld29y
ayAoQ29yZUZ4KTEgMB4GA1UEAwwXUlNBIDM4NC1iaXQgQ2VydGlmaWNhdGUwHhcN
MTYwMzAyMTY1OTA0WhcNMTYwNDAxMTY1OTA0WjCBkjELMAkGA1UEBhMCVVMxEzAR
BgNVBAgMCldhc2hpbmd0b24xEDAOBgNVBAcMB1JlZG1vbmQxGDAWBgNVBAoMD01p
Y3Jvc29mdCBDb3JwLjEgMB4GA1UECwwXLk5FVCBGcmFtZXdvcmsgKENvcmVGeCkx
IDAeBgNVBAMMF1JTQSAzODQtYml0IENlcnRpZmljYXRlMEwwDQYJKoZIhvcNAQEB
BQADOwAwOAIxANrMIthuZxV1Ay4x8gbc/BksZeLVEInlES0JbyiCr9tbeM22Vy/S
9h2zkEciMuPZ9QIDAQABo1AwTjAdBgNVHQ4EFgQU5FG2Fmi86hJOCf4KnjaxOGWV
dRUwHwYDVR0jBBgwFoAU5FG2Fmi86hJOCf4KnjaxOGWVdRUwDAYDVR0TBAUwAwEB
/zANBgkqhkiG9w0BAQUFAAMxAEzDg/u8TlApCnE8qxhcbTXk2MbX+2n5PCn+MVrW
wggvPj3b2WMXsVWiPr4S1Y/nBA==
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_PrecedingMalformedPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN CERTIFICATE-----
$$ I AM NOT A PEM
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_IgnoresOtherAlgorithms()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                rsa.ImportFromPem(pem);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_AmbiguousKey_RSAPrivateKey()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN RSA PRIVATE KEY-----
MII=
-----END RSA PRIVATE KEY-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_AmbiguousKey_SubjectPublicKeyInfo()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MII=
-----END PUBLIC KEY-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_AmbiguousKey_RSAPublicKey()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN RSA PUBLIC KEY-----
MII=
-----END RSA PUBLIC KEY-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_RSAPrivateKey_AmbiguousKey_EncryptedPkcs8()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MII=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_EncryptedPrivateKeyFails()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBsTBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIioaQaFwlfasCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECJLGzSuIgnSkBIIBYHofFpp5AsrkNc9w
s0uebkLBgMXbmhu+t6XQYXhnZXguT4KF4g49vIE3XwtZkXzEeSrNRIWZcPH1UWp2
qbv2d+ub3wBpMdFDzv5Zty6e6gACWwyMRy/oX8gZqWDfDnQwm7BV21yLANEFnRuT
K3c9EmQ9IAT2MLLRUeijyg6KUL0dZ5VmXbtQdDoovuhzU20HjSyQLXNbX8NzUhWy
VMuNHs8NhiIgOuFKMoqlN42LBA1+iOA4MGR5XDXXmGyKPLCs0USbD9Dm4/Q1h7Fs
x2yC94Mej7kgAusuNZk9GafsIQbM7jZT1PLxIKyMXAxIpS9sIYbegxK774npiy8/
LiBC1SQXJ3sJdAeUE0QPJEci937f8SteWUmF5mUqznb/0nYjvSZh/GcZ4GWEAO8j
RkMxT/C7OZVMOlb3HV3fJj7kDmOMqfc6aKEQjLdWtuYRB8CgaudldIpK4jP2+0b5
pBORBb0=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => rsa.ImportFromPem(pem));
                Assert.Contains(EncryptedExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8AlgorithmMismatch_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
The below PEM is a 1024-bit DSA key.
-----BEGIN PRIVATE KEY-----
MIIBSgIBADCCASsGByqGSM44BAEwggEeAoGBAL5KGXEaazCA+k1pMcCBc/+bodFh
0P4U2QDLyDtnmytusGPaHcFp69pVdJZWMBycwJdaFQkraQNmqQsjAmBHtpqMeJpE
VLgjzve83oMAw5aysmaQC4Wy35vnBZnshvdzgbPRHZD2dWmFvWxToqBnxh74rb/H
Nkpt8JrirFOdNuyvAhUA9+LZ6XHLZZKeFhDxYl+a9lYabdsCgYACRi+pc9joLRah
A9ushrXVItFyOsq45hOB9hT37nyTEmane/YAjmoR28XyDYdF/Ql97iSVm3cY3OYT
eDr38gQ/Hk0CgW3/RFrNWdbIpfMifs80vqCUNqDggcQixEmDVZ0gwq4+wz8EVyYG
42+vM7ajN4O2VGvCA99Vl6zv69hOpAQWAhQtFFLZyKAUOQwUQh4hNw+oBgPhFw==
-----END PRIVATE KEY-----";
                Assert.Throws<CryptographicException>(() => rsa.ImportFromPem(pem));
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8Encrypted_Char_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBsTBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIcvgI1lw9LqYCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECFDpLREQXt5pBIIBYOKuM5ljAvCViDL+
nTFq7A/fI9rqdL20TMdf0wy7s43oXmsw5gCStoNEaoVToFCQWYYBRU99mK8YNFA8
1ZJT53SDS7buJ0zX9oDltf2ByXRPI4mn2Il2HZvN2hi9ir1w8M3XoSFSurN9tC8r
IOiGkVfK9Ll54knONewNiCNefFZFctRfVMbac5SwHokCkBMHukl0oPrpVuBE8kRo
p7XtjM8ILtzLVz0iLqKXiNIf6kRdouCBmCn8VIQgIvPPIHD8vheMXWjN7g69P5n4
1YI4c/acljcofmq1BBPTwvxaETrg2NHW0XMIgAxoaVP8lIIGlNk1glWTYpuMd69L
AWvBUt33Sozc+dF0l7NGLAWL2tqkkpyDQuKn6UgYz/vxkFeQAVfSuaJVR+fUlHg0
N4lD7/hJq7b+yYPhlN3Fvvt8M9MtRg1TLAve67CA2v4TITHB06M/ELe3y42bZuLW
CA7ffFk=
-----END ENCRYPTED PRIVATE KEY-----";
                rsa.ImportFromEncryptedPem(pem, (ReadOnlySpan<char>)"test");
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8Encrypted_Byte_Simple()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBvTBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIciLWmWb33X0CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBBVEmHhJdbi+HKzPttNjXm4BIIB
YFejknurbot2VDXwc671A0mfA0cw/u7K44gsYXcZwAARC8j6f3lSzB0tN2kMEx/L
TB+kpMBbfAoIPKoEc9Y4w9m3NXkQYrLRONh9AFiAnOjULHwkstQfN2ofFlolDfbH
hAE6ga6aQJTQ8rDKTL4QkCg+s+qWlicPqs5ikSQfUz2Qiy8FKe7zZlJ0OWpT+zk7
EYRrUSKQcEAjfNS7anlMps2ZXRc1LkLJNHZSl6h2BuFPfIKEV9REpy3Y7sH7vNZZ
PWPa9/xM4CX/c/ommy6LqvZikUuUGc56/Hbz65SwG3voivIhOTmM28LiA6z0YXmY
E+nr7hyinl51raM1RSHojJB22oOW+GwV7GgWYIjUgIEMDOhN10FcGNfTeC65PCXx
5QSEe7EKVF0aHXBYB5SzMGVuxR/BqydDa26jlhVzO3LNvy9FYuqLKUslCrBCmPrt
raZNyk8KAsLs+FJq9T2tda0=
-----END ENCRYPTED PRIVATE KEY-----";
                rsa.ImportFromEncryptedPem(pem, "test"u8);
                RSAParameters rsaParameters = rsa.ExportParameters(true);

                ImportExport.AssertKeyEquals(TestData.DiminishedDPParameters, rsaParameters);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8Encrypted_AmbiguousPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBvTBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIciLWmWb33X0CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBBVEmHhJdbi+HKzPttNjXm4BIIB
YFejknurbot2VDXwc671A0mfA0cw/u7K44gsYXcZwAARC8j6f3lSzB0tN2kMEx/L
TB+kpMBbfAoIPKoEc9Y4w9m3NXkQYrLRONh9AFiAnOjULHwkstQfN2ofFlolDfbH
hAE6ga6aQJTQ8rDKTL4QkCg+s+qWlicPqs5ikSQfUz2Qiy8FKe7zZlJ0OWpT+zk7
EYRrUSKQcEAjfNS7anlMps2ZXRc1LkLJNHZSl6h2BuFPfIKEV9REpy3Y7sH7vNZZ
PWPa9/xM4CX/c/ommy6LqvZikUuUGc56/Hbz65SwG3voivIhOTmM28LiA6z0YXmY
E+nr7hyinl51raM1RSHojJB22oOW+GwV7GgWYIjUgIEMDOhN10FcGNfTeC65PCXx
5QSEe7EKVF0aHXBYB5SzMGVuxR/BqydDa26jlhVzO3LNvy9FYuqLKUslCrBCmPrt
raZNyk8KAsLs+FJq9T2tda0=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBsTBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIcvgI1lw9LqYCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECFDpLREQXt5pBIIBYOKuM5ljAvCViDL+
nTFq7A/fI9rqdL20TMdf0wy7s43oXmsw5gCStoNEaoVToFCQWYYBRU99mK8YNFA8
1ZJT53SDS7buJ0zX9oDltf2ByXRPI4mn2Il2HZvN2hi9ir1w8M3XoSFSurN9tC8r
IOiGkVfK9Ll54knONewNiCNefFZFctRfVMbac5SwHokCkBMHukl0oPrpVuBE8kRo
p7XtjM8ILtzLVz0iLqKXiNIf6kRdouCBmCn8VIQgIvPPIHD8vheMXWjN7g69P5n4
1YI4c/acljcofmq1BBPTwvxaETrg2NHW0XMIgAxoaVP8lIIGlNk1glWTYpuMd69L
AWvBUt33Sozc+dF0l7NGLAWL2tqkkpyDQuKn6UgYz/vxkFeQAVfSuaJVR+fUlHg0
N4lD7/hJq7b+yYPhlN3Fvvt8M9MtRg1TLAve67CA2v4TITHB06M/ELe3y42bZuLW
CA7ffFk=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    rsa.ImportFromEncryptedPem(pem, "test"u8));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8Encrypted_Byte_NoPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = "these aren't the PEMs we're looking for.";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    rsa.ImportFromEncryptedPem(pem, "test"u8));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_NoEncryptedPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAtz9Z9e6L1V4kt/8C
mtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7UERRHhvKNiUn4Xz0
KzgGFQIDAQABAkEAr+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrp
Ykat3c2nWb0EGGi5aWgXxQHoi7z97/ACD4X3KQIhAPNyex6GdiBVlNPHOgInTU8a
mARKKVHIXM0SxvxXrRl7AiEAwLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0
Ya8CHwiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5UsCIQCsJMzx1JuilQAN
acPiMCuFTnRSFYAhozpmsqoLyTREqwIhAMLJlZTGjEB2N+sEazH5ToEczQzKqp7t
9juGNbOPhoEL
-----END PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    rsa.ImportFromEncryptedPem(pem, "test"u8));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8Encrypted_Char_NoPem()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string pem = "go about your business";
                string password = "test";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    rsa.ImportFromEncryptedPem(pem, password));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        private static RSAParameters ToPublic(this RSAParameters rsaParams)
        {
            return new RSAParameters
            {
                Exponent = rsaParams.Exponent,
                Modulus = rsaParams.Modulus
            };
        }
    }
}
