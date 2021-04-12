// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class DSAKeyPemTests
    {
        private const string AmbiguousExceptionMarker = "multiple keys";
        private const string EncryptedExceptionMarker = "encrypted key";
        private const string NoPemExceptionMarker = "No supported key";

        [Fact]
        public static void ImportFromPem_NoPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = "pem? what pem? there is no pem here.";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => dsa.ImportFromPem(pem));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8UnEncrypted_Simple()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                dsa.ImportFromPem(pem);
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8UnEncrypted_IgnoresUnrelatedAlgorithm()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==
-----END EC PRIVATE KEY-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                dsa.ImportFromPem(pem);
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8_UnrelatedPrecedingPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN CERTIFICATE-----
MII=
-----END CERTIFICATE-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                dsa.ImportFromPem(pem);
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8_PrecedingMalformedPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN CERTIFICATE-----
$$$ BAD PEM
-----END CERTIFICATE-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                dsa.ImportFromPem(pem);
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_SubjectPublicKeyInfo_Simple()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MIHxMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRn
QNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV1eFk
MKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6fve7
7OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorA0QAAkEAwwDg5n2HfmztOf7q
qsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8
PKRWBQ==
-----END PUBLIC KEY-----";
                dsa.ImportFromPem(pem);
                DSAParameters dsaParameters = dsa.ExportParameters(false);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters.ToPublic(), dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8_AmbiguousKey_Pkcs8()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => dsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8_AmbiguousKey_Spki()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN PUBLIC KEY-----
MIHxMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRn
QNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV1eFk
MKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6fve7
7OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorA0QAAkEAwwDg5n2HfmztOf7q
qsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8
PKRWBQ==
-----END PUBLIC KEY-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => dsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_Pkcs8_AmbiguousKey_EncryptedPkcs8()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBIDBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIkM/kCKe6rYsCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECBOccveL65bDBIHQiCcCqwxJs93g1+16
7Gx1D5lL4/nZ94fRa+Hl4nGEX4gmjuxH6pOHKyywwflAyXNTfVhOCP9zBedwENx9
MGHbpaaShD6iJfoGMRX0frr0mMCtuOOZkkjBF9pSpkhaH0TDSq1PrVLxcM0/S4Vs
+//2uPrP8U+CTW9W7CXCZw698BAuevZRuD0koT2Bn9ErhTiuVZZMcOjtLmN2oXHG
dVYwfovccu8ktEAwk5XAOo0r+5CCw2lDDw/hbDeO87BToC5Cc5nu3F5LxAUj8Flc
v8pi3w==
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => dsa.ImportFromPem(pem));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_EncryptedPrivateKeyFails()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBIDBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIkM/kCKe6rYsCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECBOccveL65bDBIHQiCcCqwxJs93g1+16
7Gx1D5lL4/nZ94fRa+Hl4nGEX4gmjuxH6pOHKyywwflAyXNTfVhOCP9zBedwENx9
MGHbpaaShD6iJfoGMRX0frr0mMCtuOOZkkjBF9pSpkhaH0TDSq1PrVLxcM0/S4Vs
+//2uPrP8U+CTW9W7CXCZw698BAuevZRuD0koT2Bn9ErhTiuVZZMcOjtLmN2oXHG
dVYwfovccu8ktEAwk5XAOo0r+5CCw2lDDw/hbDeO87BToC5Cc5nu3F5LxAUj8Flc
v8pi3w==
-----END ENCRYPTED PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () => dsa.ImportFromPem(pem));
                Assert.Contains(EncryptedExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromPem_SpkiAlgorithmMismatch_Throws()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
The below key is for an RSA SPKI
-----BEGIN PUBLIC KEY-----
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQx
m5NTLEHDwUd7idstLzPXuah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQ==
-----END PUBLIC KEY-----";
                Assert.Throws<CryptographicException>(() => dsa.ImportFromPem(pem));
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_Encrypted_Char_Simple()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBIDBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIkM/kCKe6rYsCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECBOccveL65bDBIHQiCcCqwxJs93g1+16
7Gx1D5lL4/nZ94fRa+Hl4nGEX4gmjuxH6pOHKyywwflAyXNTfVhOCP9zBedwENx9
MGHbpaaShD6iJfoGMRX0frr0mMCtuOOZkkjBF9pSpkhaH0TDSq1PrVLxcM0/S4Vs
+//2uPrP8U+CTW9W7CXCZw698BAuevZRuD0koT2Bn9ErhTiuVZZMcOjtLmN2oXHG
dVYwfovccu8ktEAwk5XAOo0r+5CCw2lDDw/hbDeO87BToC5Cc5nu3F5LxAUj8Flc
v8pi3w==
-----END ENCRYPTED PRIVATE KEY-----";
                dsa.ImportFromEncryptedPem(pem, "test");
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_Encrypted_Byte_Simple()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBLDBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIfcoipdEY/C4CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBC9heEphj00fB89aP6chSOjBIHQ
HF2RLrIw6654q2hjUdCG4PhhYNXlck0zD0mOuaVQHmnKIKArk/1DSpgSrYnKw6aE
2eujwNdySLLEwUj5l+X/IXwhOnPIZDJqUN7oMagUYJX28gnQmXyDvrt3r16utbpd
ho0YNYGUDSgOs6RxBpw1rJUCnAlHNU09peCjEP+aZSrhsxlejN/GpVS4e0JTmMeo
xTL6VO9mx52x6h5WDAQAisMVeMkBoxQUWLANXiw1zSfVbsmB7mDknsRcvD3tcgMs
7YLD7LQMiPAIjDlOP8XP/w==
-----END ENCRYPTED PRIVATE KEY-----";
                byte[] passwordBytes = Encoding.UTF8.GetBytes("test");
                dsa.ImportFromEncryptedPem(pem, passwordBytes);
                DSAParameters dsaParameters = dsa.ExportParameters(true);

                DSAImportExport.AssertKeyEquals(DSATestData.Dsa512Parameters, dsaParameters);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_Encrypted_AmbiguousPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBLDBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIfcoipdEY/C4CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBC9heEphj00fB89aP6chSOjBIHQ
HF2RLrIw6654q2hjUdCG4PhhYNXlck0zD0mOuaVQHmnKIKArk/1DSpgSrYnKw6aE
2eujwNdySLLEwUj5l+X/IXwhOnPIZDJqUN7oMagUYJX28gnQmXyDvrt3r16utbpd
ho0YNYGUDSgOs6RxBpw1rJUCnAlHNU09peCjEP+aZSrhsxlejN/GpVS4e0JTmMeo
xTL6VO9mx52x6h5WDAQAisMVeMkBoxQUWLANXiw1zSfVbsmB7mDknsRcvD3tcgMs
7YLD7LQMiPAIjDlOP8XP/w==
-----END ENCRYPTED PRIVATE KEY-----
-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIBIDBLBgkqhkiG9w0BBQ0wPjApBgkqhkiG9w0BBQwwHAQIkM/kCKe6rYsCAggA
MAwGCCqGSIb3DQIJBQAwEQYFKw4DAgcECBOccveL65bDBIHQiCcCqwxJs93g1+16
7Gx1D5lL4/nZ94fRa+Hl4nGEX4gmjuxH6pOHKyywwflAyXNTfVhOCP9zBedwENx9
MGHbpaaShD6iJfoGMRX0frr0mMCtuOOZkkjBF9pSpkhaH0TDSq1PrVLxcM0/S4Vs
+//2uPrP8U+CTW9W7CXCZw698BAuevZRuD0koT2Bn9ErhTiuVZZMcOjtLmN2oXHG
dVYwfovccu8ktEAwk5XAOo0r+5CCw2lDDw/hbDeO87BToC5Cc5nu3F5LxAUj8Flc
v8pi3w==
-----END ENCRYPTED PRIVATE KEY-----";
                byte[] passwordBytes = Encoding.UTF8.GetBytes("test");
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    dsa.ImportFromEncryptedPem(pem, passwordBytes));
                Assert.Contains(AmbiguousExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_Byte_NoPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = "";
                byte[] passwordBytes = Encoding.UTF8.GetBytes("test");
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    dsa.ImportFromEncryptedPem(pem, passwordBytes));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_Char_NoPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = "";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    dsa.ImportFromEncryptedPem(pem, ""));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_Pkcs8_NoEncryptedPem()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string pem = @"
-----BEGIN PRIVATE KEY-----
MIHGAgEAMIGoBgcqhkjOOAQBMIGcAkEA1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWN
IHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQIVAPq19iXV
1eFkMKHvYw6+M4l8wiT5AkAIRMSQ5S71jgWQLGNtZNHV6yxggqDU87/RzgeOh7Q6
fve77OGaTv4qbZwinTYAg86p9yHzmwW6+XBS3vxnpYorBBYCFC49eoTIW2Z4Xh9v
55aYKyKwy5i8
-----END PRIVATE KEY-----";
                ArgumentException ae = AssertExtensions.Throws<ArgumentException>("input", () =>
                    dsa.ImportFromEncryptedPem(pem, ""));
                Assert.Contains(NoPemExceptionMarker, ae.Message);
            }
        }

        private static DSAParameters ToPublic(this DSAParameters dsaParams)
        {
            dsaParams.X = null;
            return dsaParams;
        }
    }
}
