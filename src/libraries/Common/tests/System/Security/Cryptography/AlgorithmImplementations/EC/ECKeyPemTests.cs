// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class ECKeyPemTests<TAlg> where TAlg : AsymmetricAlgorithm
    {
        private const string AmbiguousExceptionMarker = "multiple keys";
        private const string EncryptedExceptionMarker = "encrypted key";
        private const string NoPemExceptionMarker = "No supported key";

        protected abstract TAlg CreateKey();
        protected abstract ECParameters ExportParameters(TAlg key, bool includePrivateParameters);

        [Fact]
        public void ImportFromPem_NoPem()
        {
            using (TAlg key = CreateKey())
            {
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(""));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromPem_ECPrivateKey_Simple()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----");
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromPem_ECPrivateKey_IgnoresUnrelatedAlgorithm()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----");
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromPem_Pkcs8_Simple()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgcKEsLbFoRe1W/2jP
whpHKz8E19aFG/Y0ny19WzRSs4qhRANCAASBAezkdGSm6tcM9ppuK9PYhpGjJi0i
y6T3Y16v8maAqNihK6YdWZI19n2ctNWPF4PTykPnjwpauqYkB5k2wMOp
-----END PRIVATE KEY-----");
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromPem_Pkcs8_IgnoresUnrelatedAlgorithm()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----
-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgcKEsLbFoRe1W/2jP
whpHKz8E19aFG/Y0ny19WzRSs4qhRANCAASBAezkdGSm6tcM9ppuK9PYhpGjJi0i
y6T3Y16v8maAqNihK6YdWZI19n2ctNWPF4PTykPnjwpauqYkB5k2wMOp
-----END PRIVATE KEY-----");
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_Simple()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----");
                ECParameters ecParameters = ExportParameters(key, false);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.ComparePublicKey(expected.Q, ecParameters.Q, isEqual: true);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_PrecedingUnrelatedPemIsIgnored()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
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
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----");
                ECParameters ecParameters = ExportParameters(key, false);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.ComparePublicKey(expected.Q, ecParameters.Q, isEqual: true);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_IgnoresUnrelatedAlgorithms()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN RSA PRIVATE KEY-----
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==
-----END RSA PRIVATE KEY-----
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----");
                ECParameters ecParameters = ExportParameters(key, false);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.ComparePublicKey(expected.Q, ecParameters.Q, isEqual: true);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_PrecedingMalformedPem()
        {
            using (TAlg key = CreateKey())
            {
                key.ImportFromPem(@"
-----BEGIN CERTIFICATE-----
$$ I AM NOT A PEM
-----END CERTIFICATE-----
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----");
                ECParameters ecParameters = ExportParameters(key, false);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.ComparePublicKey(expected.Q, ecParameters.Q, isEqual: true);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_AmbiguousKey_Spki()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_AmbiguousKey_EncryptedPkcs8()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromPem_Spki_AmbiguousKey_EncryptedPkcs8_Pkcs8First()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgQHs5HRkpurXDPaabivT2IaRoyYt
Isuk92Ner/JmgKjYoSumHVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END PUBLIC KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromPem_EncryptedPrivateKeyFails()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(pem));
                Assert.Contains(EncryptedExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromPem_MultipleEncryptedPrivateKeyAreAmbiguous()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => key.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_Pkcs8_Char_Simple()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                key.ImportFromEncryptedPem(pem, "test");
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_Pkcs8_Byte_Simple()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHsMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAgf9krO2ZiPvAICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEEv4Re1ATH9lHzx+13GoZU0EgZAV
iE/+pIb/4quf+Y524bXUKTGYXzdSUE8Dp1qdZFcwDiCYCTtpL+065fGhmf1KZS2c
/OMt/tWvtMSj17+dJvShsu/NYJXF5fsfpSJbd3e50Y3AisW0Ob7mmF54KBfg6Y+4
aATwwQdUIKVzUZsQctsHPjbriQKKn7GKSyUOikBUNQ+TozojX8/g7JAsl+T9jGM=
-----END ENCRYPTED PRIVATE KEY-----";
                byte[] passwordBytes = Encoding.UTF8.GetBytes("test");
                key.ImportFromEncryptedPem(pem, passwordBytes);
                ECParameters ecParameters = ExportParameters(key, true);
                ECParameters expected = EccTestData.GetNistP256ReferenceKey();
                EccTestBase.AssertEqual(expected, ecParameters);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_AmbiguousPem_Byte()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHsMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAgf9krO2ZiPvAICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEEv4Re1ATH9lHzx+13GoZU0EgZAV
iE/+pIb/4quf+Y524bXUKTGYXzdSUE8Dp1qdZFcwDiCYCTtpL+065fGhmf1KZS2c
/OMt/tWvtMSj17+dJvShsu/NYJXF5fsfpSJbd3e50Y3AisW0Ob7mmF54KBfg6Y+4
aATwwQdUIKVzUZsQctsHPjbriQKKn7GKSyUOikBUNQ+TozojX8/g7JAsl+T9jGM=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                byte[] passwordBytes = Encoding.UTF8.GetBytes("test");

                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    key.ImportFromEncryptedPem(pem, passwordBytes));

                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_AmbiguousPem_Char()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHsMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAgf9krO2ZiPvAICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEEv4Re1ATH9lHzx+13GoZU0EgZAV
iE/+pIb/4quf+Y524bXUKTGYXzdSUE8Dp1qdZFcwDiCYCTtpL+065fGhmf1KZS2c
/OMt/tWvtMSj17+dJvShsu/NYJXF5fsfpSJbd3e50Y3AisW0Ob7mmF54KBfg6Y+4
aATwwQdUIKVzUZsQctsHPjbriQKKn7GKSyUOikBUNQ+TozojX8/g7JAsl+T9jGM=
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIHgMEsGCSqGSIb3DQEFDTA+MCkGCSqGSIb3DQEFDDAcBAjVvm4KTLb0JgICCAAw
DAYIKoZIhvcNAgkFADARBgUrDgMCBwQIuHgfok8Ytl0EgZDkDSJ9vt8UvSesdyV+
Evt9yfvEjiP/6yITq59drw1Kcgp6buOCVCY7LZ06aD6WpogiqGDYMuzfvqg5hNFp
opSAJ/pvHONL5kyAJLeNyG9c/mR2qyrP2L9gL0Z5fB9NyPejKTLi0PXMGQWdDTH8
Qh0fqdrNovgFLubbJFMQN/MwwIAfIuf0Mn0WFYYeQiBJ3kg=
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    key.ImportFromEncryptedPem(pem, ""));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_UnencryptedPem_ThrowsNoPem()
        {
            using (TAlg key = CreateKey())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgcKEsLbFoRe1W/2jP
whpHKz8E19aFG/Y0ny19WzRSs4qhRANCAASBAezkdGSm6tcM9ppuK9PYhpGjJi0i
y6T3Y16v8maAqNihK6YdWZI19n2ctNWPF4PTykPnjwpauqYkB5k2wMOp
-----END PRIVATE KEY-----";
                byte[] passwordBytes = Array.Empty<byte>();
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    key.ImportFromEncryptedPem(pem, passwordBytes));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public void ImportFromEncryptedPem_NoPem()
        {
            using(TAlg key = CreateKey())
            {
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    key.ImportFromEncryptedPem("", ""));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }
    }
}
