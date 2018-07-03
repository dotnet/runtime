// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Pkcs;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class ECKeyFileTests<T> where T : AsymmetricAlgorithm
    {
        protected abstract T CreateKey();
        protected abstract byte[] ExportECPrivateKey(T key);
        protected abstract bool TryExportECPrivateKey(T key, Span<byte> destination, out int bytesWritten);
        protected abstract void ImportECPrivateKey(T key, ReadOnlySpan<byte> source, out int bytesRead);
        protected abstract void ImportParameters(T key, ECParameters ecParameters);
        protected abstract ECParameters ExportParameters(T key, bool includePrivate);

        public static bool SupportsBrainpool { get; } = IsCurveSupported(ECCurve.NamedCurves.brainpoolP160r1.Oid);

        public static bool SupportsSect283k1 { get; } = IsCurveSupported(EccTestData.Sect283k1Key1.Curve.Oid);

        private static bool IsCurveSupported(Oid oid)
        {
            return EcDiffieHellman.Tests.ECDiffieHellmanFactory.IsCurveValid(oid);
        }

        [Fact]
        public void ReadWriteNistP521Pkcs8()
        {
            const string base64 = @"
MIHuAgEAMBAGByqGSM49AgEGBSuBBAAjBIHWMIHTAgEBBEIBpV+HhaVzC67h1rPT
AQaff9ZNiwTM6lfv1ZYeaPM/q0NUUWbKZVPNOP9xPRKJxpi9fQhrVeAbW9XtJ+Nj
A3axFmahgYkDgYYABAB1HyYyTHPO9dReuzKTfjBg41GWCldZStA+scoMXqdHEhM2
a6mR0kQGcX+G/e/eCG4JuVSlfcD16UWXVtYMKq5t4AGo3bs/AsjCNSRyn1SLfiMy
UjPvZ90wdSuSTyl0WePC4Sro2PT+RFTjhHwYslXKzvWXN7kY4d5A+V6f/k9Xt5FT
oA==";

            ReadWriteBase64Pkcs8(base64, EccTestData.GetNistP521Key2());
        }

        [Fact]
        public void ReadWriteNistP521Pkcs8_ECDH()
        {
            const string base64 = @"
MIHsAgEAMA4GBSuBBAEMBgUrgQQAIwSB1jCB0wIBAQRCAaVfh4Wlcwuu4daz0wEG
n3/WTYsEzOpX79WWHmjzP6tDVFFmymVTzTj/cT0SicaYvX0Ia1XgG1vV7SfjYwN2
sRZmoYGJA4GGAAQAdR8mMkxzzvXUXrsyk34wYONRlgpXWUrQPrHKDF6nRxITNmup
kdJEBnF/hv3v3ghuCblUpX3A9elFl1bWDCqubeABqN27PwLIwjUkcp9Ui34jMlIz
72fdMHUrkk8pdFnjwuEq6Nj0/kRU44R8GLJVys71lze5GOHeQPlen/5PV7eRU6A=";

            ReadWriteBase64Pkcs8(
                base64,
                EccTestData.GetNistP521Key2(),
                isSupported: false);
        }

        [Fact]
        public void ReadWriteNistP521SubjectPublicKeyInfo()
        {
            const string base64 = @"
MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQAdR8mMkxzzvXUXrsyk34wYONRlgpX
WUrQPrHKDF6nRxITNmupkdJEBnF/hv3v3ghuCblUpX3A9elFl1bWDCqubeABqN27
PwLIwjUkcp9Ui34jMlIz72fdMHUrkk8pdFnjwuEq6Nj0/kRU44R8GLJVys71lze5
GOHeQPlen/5PV7eRU6A=";

            ReadWriteBase64SubjectPublicKeyInfo(base64, EccTestData.GetNistP521Key2());
        }

        [Fact]
        public void ReadWriteNistP521SubjectPublicKeyInfo_ECDH()
        {
            const string base64 = @"
MIGZMA4GBSuBBAEMBgUrgQQAIwOBhgAEAHUfJjJMc8711F67MpN+MGDjUZYKV1lK
0D6xygxep0cSEzZrqZHSRAZxf4b9794Ibgm5VKV9wPXpRZdW1gwqrm3gAajduz8C
yMI1JHKfVIt+IzJSM+9n3TB1K5JPKXRZ48LhKujY9P5EVOOEfBiyVcrO9Zc3uRjh
3kD5Xp/+T1e3kVOg";

            ReadWriteBase64SubjectPublicKeyInfo(
                base64,
                EccTestData.GetNistP521Key2(),
                isSupported: false);
        }

        [Fact]
        public void ReadNistP521EncryptedPkcs8_Pbes2_Aes128_Sha384()
        {
            // PBES2, PBKDF2 (SHA384), AES128
            const string base64 = @"
MIIBXTBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQI/JyXWyp/t3kCAggA
MAwGCCqGSIb3DQIKBQAwHQYJYIZIAWUDBAECBBA3H8mbFK5afB5GzIemCCQkBIIB
AKAz1z09ATUA8UfoDMwTyXiHUS8Mb/zkUCH+I7rav4orhAnSyYAyLKcHeGne+kUa
8ewQ5S7oMMLXE0HHQ8CpORlSgxTssqTAHigXEqdRb8nQ8hJJa2dFtNXyUeFtxZ7p
x+aSLD6Y3J+mgzeVp1ICgROtuRjA9RYjUdd/3cy2BAlW+Atfs/300Jhkke3H0Gqc
F71o65UNB+verEgN49rQK7FAFtoVI2oRjHLO1cGjxZkbWe2KLtgJWsgmexRq3/a+
Pfuapj3LAHALZtDNMZ+QCFN2ZXUSFNWiBSwnwCAtfFCn/EchPo3MFR3K0q/qXTua
qtlbnispri1a/EghiaPQ0po=";

            ReadWriteBase64EncryptedPkcs8(
                base64,
                "qwerty",
                new PbeParameters(
                    PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                    HashAlgorithmName.SHA1,
                    12321),
                EccTestData.GetNistP521Key2());
        }

        [Fact]
        public void ReadNistP521EncryptedPkcs8_Pbes2_Aes128_Sha384_PasswordBytes()
        {
            // PBES2, PBKDF2 (SHA384), AES128
            const string base64 = @"
MIIBXTBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQI/JyXWyp/t3kCAggA
MAwGCCqGSIb3DQIKBQAwHQYJYIZIAWUDBAECBBA3H8mbFK5afB5GzIemCCQkBIIB
AKAz1z09ATUA8UfoDMwTyXiHUS8Mb/zkUCH+I7rav4orhAnSyYAyLKcHeGne+kUa
8ewQ5S7oMMLXE0HHQ8CpORlSgxTssqTAHigXEqdRb8nQ8hJJa2dFtNXyUeFtxZ7p
x+aSLD6Y3J+mgzeVp1ICgROtuRjA9RYjUdd/3cy2BAlW+Atfs/300Jhkke3H0Gqc
F71o65UNB+verEgN49rQK7FAFtoVI2oRjHLO1cGjxZkbWe2KLtgJWsgmexRq3/a+
Pfuapj3LAHALZtDNMZ+QCFN2ZXUSFNWiBSwnwCAtfFCn/EchPo3MFR3K0q/qXTua
qtlbnispri1a/EghiaPQ0po=";

            ReadWriteBase64EncryptedPkcs8(
                base64,
                Encoding.UTF8.GetBytes("qwerty"),
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA1,
                    12321),
                EccTestData.GetNistP521Key2());
        }

        [Fact]
        public void ReadNistP256EncryptedPkcs8_Pbes1_RC2_MD5()
        {
            const string base64 = @"
MIGwMBsGCSqGSIb3DQEFBjAOBAiVk8SDhLdiNwICCAAEgZB2rI9tf7jjGdEwJNrS
8F/xNIo/0OSUSkQyg5n/ovRK1IodzPpWqipqM8TGfZk4sxn7h7RBmX2FlMkTLO4i
mVannH3jd9cmCAz0aewDO0/LgmvDnzWiJ/CoDamzwC8bzDocq1Y/PsVYsYzSrJ7n
m8STNpW+zSpHWlpHpWHgXGq4wrUKJifxOv6Rm5KTYcvUT38=";

            ReadWriteBase64EncryptedPkcs8(
                base64,
                "secp256r1",
                new PbeParameters(
                    PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                    HashAlgorithmName.SHA1,
                    1024),
                EccTestData.GetNistP256ReferenceKey());
        }

        [Fact]
        public void ReadWriteNistP256ECPrivateKey()
        {
            const string base64 = @"
MHcCAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEHoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSumHVmS
NfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==";

            ReadWriteBase64ECPrivateKey(
                base64,
                EccTestData.GetNistP256ReferenceKey());
        }

        [Fact]
        public void ReadNistP256Explicit()
        {
            byte[] explicitSPKI = Convert.FromBase64String(@"
MIIBSzCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAA
AAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA////
///////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSd
NgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5
RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA
//////////+85vqtpxeehPO5ysL8YyVRAgEBA0IABIEB7OR0ZKbq1wz2mm4r09iG
kaMmLSLLpPdjXq/yZoCo2KErph1ZkjX2fZy01Y8Xg9PKQ+ePClq6piQHmTbAw6k=");

            byte[] explicitECPrivateKey = Convert.FromBase64String(@"
MIIBaAIBAQQgcKEsLbFoRe1W/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qggfowgfcC
AQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////
MFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr
vVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE
axfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W
K84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8
YyVRAgEBoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSum
HVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==");

            byte[] explicitPkcs8 = Convert.FromBase64String(@"
MIIBeQIBADCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAAB
AAAAAAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA
///////////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMV
AMSdNgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg
9KE5RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8A
AAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBBG0wawIBAQQgcKEsLbFoRe1W
/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qhRANCAASBAezkdGSm6tcM9ppuK9PYhpGj
Ji0iy6T3Y16v8maAqNihK6YdWZI19n2ctNWPF4PTykPnjwpauqYkB5k2wMOp");

            using (T key = CreateKey())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(explicitSPKI, out _));

                Assert.ThrowsAny<CryptographicException>(
                    () => ImportECPrivateKey(key, explicitECPrivateKey, out _));

                // Win10 supports explicit curve PKCS8 on the CNG types. 
                if (!PlatformDetection.IsWindows10Version1607OrGreater)
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportPkcs8PrivateKey(explicitPkcs8, out _));

                    Pkcs8PrivateKeyInfo builder = Pkcs8PrivateKeyInfo.Decode(explicitPkcs8, out _, skipCopy: true);
                    byte[] explicitEncryptedPkcs8 = builder.Encrypt(
                        "asdf",
                        new PbeParameters(
                            PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                            HashAlgorithmName.SHA1,
                            2048));

                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportEncryptedPkcs8PrivateKey("asdf", explicitEncryptedPkcs8, out _));
                }
            }
        }

        [Fact]
        public void ReadWriteBrainpoolKey1ECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MFQCAQEEFMXZRFR94RXbJYjcb966O0c+nE2WoAsGCSskAwMCCAEBAaEsAyoABI5i
jwk5x2KSdsrb/pnAHDZQk1TictLI7vH2zDIF0AV+ud5sqeMQUJY=",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1Pkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MGQCAQAwFAYHKoZIzj0CAQYJKyQDAwIIAQEBBEkwRwIBAQQUxdlEVH3hFdsliNxv
3ro7Rz6cTZahLAMqAASOYo8JOcdiknbK2/6ZwBw2UJNU4nLSyO7x9swyBdAFfrne
bKnjEFCW",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1EncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGHMBsGCSqGSIb3DQEFAzAOBAhSgCZvbsatLQICCAAEaKGDyoSVej1yNPCn7K6q
ooI857+joe6NZjR+w1xuH4JfrQZGvelWZ2AWtQezuz4UzPLnL3Nyf6jjPPuKarpk
HiDaMtpw7yT5+32Vkxv5C2jvqNPpicmEFpf2wJ8yVLQtMOKAF2sOwxN/",
                "12345",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA384,
                    4096),
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1SubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MEIwFAYHKoZIzj0CAQYJKyQDAwIIAQEBAyoABI5ijwk5x2KSdsrb/pnAHDZQk1Ti
ctLI7vH2zDIF0AV+ud5sqeMQUJY=",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool);
        }

        [Fact]
        public void ReadWriteSect283k1Key1ECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIGAAgEBBCQAtPGuHn/c1LDoIFPAipCIUrJiMebAFnD8xsPqLF0/7UDt8DegBwYF
K4EEABChTANKAAQHUncL0z5qbuIJbLaxIOdJe0e2wHehR8tX2vaTkJ2EBxbup6oE
fbmZXDVgPF5rL4zf8Otx03rjQxughJ66sTpMkAPHlp9VzZA=",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1);
        }

        [Fact]
        public void ReadWriteSect283k1Key1Pkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MIGQAgEAMBAGByqGSM49AgEGBSuBBAAQBHkwdwIBAQQkALTxrh5/3NSw6CBTwIqQ
iFKyYjHmwBZw/MbD6ixdP+1A7fA3oUwDSgAEB1J3C9M+am7iCWy2sSDnSXtHtsB3
oUfLV9r2k5CdhAcW7qeqBH25mVw1YDxeay+M3/DrcdN640MboISeurE6TJADx5af
Vc2Q",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1);
        }

        [Fact]
        public void ReadWriteSect283k1Key1EncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIG4MBsGCSqGSIb3DQEFAzAOBAhf/Ix8WHVvxQICCAAEgZheT2iB2sBmNjV2qIgI
DsNyPY+0rwbWR8MHZcRN0zAL9Q3kawaZyWeKe4j3m3Y39YWURVymYeLAm70syrEw
057W6kNVXxR/hEq4MlHJZxZdS+R6LGpEvWFEWiuN0wBtmhO24+KmqPMH8XhGszBv
nTvuaAMG/xvXzKoigakX+1D60cmftPsC7t23SF+xMdzfZNlJGrxXFYX1Gg==",
                "12345",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA384,
                    4096),
                EccTestData.Sect283k1Key1,
                SupportsSect283k1);
        }

        [Fact]
        public void ReadWriteSect283k1Key1SubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MF4wEAYHKoZIzj0CAQYFK4EEABADSgAEB1J3C9M+am7iCWy2sSDnSXtHtsB3oUfL
V9r2k5CdhAcW7qeqBH25mVw1YDxeay+M3/DrcdN640MboISeurE6TJADx5afVc2Q",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1);
        }

        [Fact]
        public void NoFuzzySubjectPublicKeyInfo()
        {
            using (T key = CreateKey())
            {
                int bytesRead = -1;
                byte[] ecPriv = ExportECPrivateKey(key);

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(ecPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = ecPriv.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(encryptedPkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public void NoFuzzyECPrivateKey()
        {
            using (T key = CreateKey())
            {
                int bytesRead = -1;
                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => ImportECPrivateKey(key, spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => ImportECPrivateKey(key, pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = spki.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => ImportECPrivateKey(key, encryptedPkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public void NoFuzzyPkcs8()
        {
            using (T key = CreateKey())
            {
                int bytesRead = -1;
                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] ecPriv = ExportECPrivateKey(key);

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(ecPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = spki.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(encryptedPkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public void NoFuzzyEncryptedPkcs8()
        {
            using (T key = CreateKey())
            {
                int bytesRead = -1;
                byte[] spki = key.ExportSubjectPublicKeyInfo();
                byte[] empty = Array.Empty<byte>();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] ecPriv = ExportECPrivateKey(key);

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, ecPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public void NoPrivKeyFromPublicOnly()
        {
            using (T key = CreateKey())
            {
                ECParameters parameters = EccTestData.GetNistP521Key2();
                parameters.D = null;
                ImportParameters(key, parameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => ExportECPrivateKey(key));

                Assert.ThrowsAny<CryptographicException>(
                    () => TryExportECPrivateKey(key, Span<byte>.Empty, out _));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportPkcs8PrivateKey());

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportPkcs8PrivateKey(Span<byte>.Empty, out _));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA256, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA256, 72),
                        Span<byte>.Empty,
                        out _));
            }
        }

        [Fact]
        public void BadPbeParameters()
        {
            using (T key = CreateKey())
            {
                Assert.ThrowsAny<ArgumentNullException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        null));

                Assert.ThrowsAny<ArgumentNullException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<char>.Empty,
                        null));

                Assert.ThrowsAny<ArgumentNullException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        null,
                        Span<byte>.Empty,
                        out _));

                Assert.ThrowsAny<ArgumentNullException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<char>.Empty,
                        null,
                        Span<byte>.Empty,
                        out _));

                // PKCS12 requires SHA-1
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 72),
                        Span<byte>.Empty,
                        out _));

                // PKCS12 requires SHA-1
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.MD5, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        ReadOnlySpan<byte>.Empty,
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.MD5, 72),
                        Span<byte>.Empty,
                        out _));

                // PKCS12 requires a char-based password
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 72),
                        Span<byte>.Empty,
                        out _));

                // Unknown encryption algorithm
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(0, HashAlgorithmName.SHA1, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(0, HashAlgorithmName.SHA1, 72),
                        Span<byte>.Empty,
                        out _));

                // Unknown encryption algorithm (negative enum value)
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters((PbeEncryptionAlgorithm)(-5), HashAlgorithmName.SHA1, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters((PbeEncryptionAlgorithm)(-5), HashAlgorithmName.SHA1, 72),
                        Span<byte>.Empty,
                        out _));

                // Unknown encryption algorithm (overly-large enum value)
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters((PbeEncryptionAlgorithm)15, HashAlgorithmName.SHA1, 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters((PbeEncryptionAlgorithm)15, HashAlgorithmName.SHA1, 72),
                        Span<byte>.Empty,
                        out _));

                // Unknown hash algorithm
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, new HashAlgorithmName("Potato"), 72)));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportEncryptedPkcs8PrivateKey(
                        new byte[3],
                        new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, new HashAlgorithmName("Potato"), 72),
                        Span<byte>.Empty,
                        out _));
            }
        }

        [Fact]
        public void DecryptPkcs12WithBytes()
        {
            using (T key = CreateKey())
            {
                string charBased = "hello";
                byte[] byteBased = Encoding.UTF8.GetBytes(charBased);

                byte[] encrypted = key.ExportEncryptedPkcs8PrivateKey(
                    charBased,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                        HashAlgorithmName.SHA1,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(byteBased, encrypted, out _));
            }
        }

        private void ReadWriteBase64EncryptedPkcs8(
            string base64EncryptedPkcs8,
            string password,
            PbeParameters pbe,
            in ECParameters expected,
            bool isSupported=true)
        {
            if (isSupported)
            {
                ReadWriteKey(
                    base64EncryptedPkcs8,
                    expected,
                    (T key, ReadOnlySpan<byte> source, out int read) =>
                        key.ImportEncryptedPkcs8PrivateKey(password, source, out read),
                    key => key.ExportEncryptedPkcs8PrivateKey(password, pbe),
                    (T key, Span<byte> destination, out int bytesWritten) =>
                        key.TryExportEncryptedPkcs8PrivateKey(password, pbe, destination, out bytesWritten),
                    isEncrypted: true);
            }
            else
            {
                byte[] encrypted = Convert.FromBase64String(base64EncryptedPkcs8);

                using (T key = CreateKey())
                {
                    // Wrong password
                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportEncryptedPkcs8PrivateKey(encrypted.AsSpan(1, 14), encrypted, out _));

                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportEncryptedPkcs8PrivateKey(password + password, encrypted, out _));

                    int bytesRead = -1;

                    Exception e = Assert.ThrowsAny<Exception>(
                        () => key.ImportEncryptedPkcs8PrivateKey(password, encrypted, out bytesRead));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        "e is PlatformNotSupportedException || e is CryptographicException");

                    Assert.Equal(-1, bytesRead);
                }
            }
        }

        private void ReadWriteBase64EncryptedPkcs8(
            string base64EncryptedPkcs8,
            byte[] passwordBytes,
            PbeParameters pbe,
            in ECParameters expected,
            bool isSupported=true)
        {
            if (isSupported)
            {
                ReadWriteKey(
                    base64EncryptedPkcs8,
                    expected,
                    (T key, ReadOnlySpan<byte> source, out int read) =>
                        key.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out read),
                    key => key.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbe),
                    (T key, Span<byte> destination, out int bytesWritten) =>
                        key.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbe, destination, out bytesWritten),
                    isEncrypted: true);
            }
            else
            {
                byte[] encrypted = Convert.FromBase64String(base64EncryptedPkcs8);
                byte[] wrongPassword = new byte[passwordBytes.Length + 2];
                RandomNumberGenerator.Fill(wrongPassword);

                using (T key = CreateKey())
                {
                    // Wrong password
                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportEncryptedPkcs8PrivateKey(wrongPassword, encrypted, out _));

                    Assert.ThrowsAny<CryptographicException>(
                        () => key.ImportEncryptedPkcs8PrivateKey("ThisBetterNotBeThePassword!", encrypted, out _));

                    int bytesRead = -1;

                    Exception e = Assert.ThrowsAny<Exception>(
                        () => key.ImportEncryptedPkcs8PrivateKey(passwordBytes, encrypted, out bytesRead));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        "e is PlatformNotSupportedException || e is CryptographicException");

                    Assert.Equal(-1, bytesRead);
                }
            }
        }

        private void ReadWriteBase64ECPrivateKey(string base64Pkcs8, in ECParameters expected, bool isSupported=true)
        {
            if (isSupported)
            {
                ReadWriteKey(
                    base64Pkcs8,
                    expected,
                    (T key, ReadOnlySpan<byte> source, out int read) =>
                        ImportECPrivateKey(key, source, out read),
                    key => ExportECPrivateKey(key),
                    (T key, Span<byte> destination, out int bytesWritten) =>
                        TryExportECPrivateKey(key, destination, out bytesWritten));
            }
            else
            {
                using (T key = CreateKey())
                {
                    Exception e = Assert.ThrowsAny<Exception>(
                        () => ImportECPrivateKey(key, Convert.FromBase64String(base64Pkcs8), out _));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        "e is PlatformNotSupportedException || e is CryptographicException");
                }
            }
        }

        private void ReadWriteBase64Pkcs8(string base64Pkcs8, in ECParameters expected, bool isSupported=true)
        {
            if (isSupported)
            {
                ReadWriteKey(
                    base64Pkcs8,
                    expected,
                    (T key, ReadOnlySpan<byte> source, out int read) =>
                        key.ImportPkcs8PrivateKey(source, out read),
                    key => key.ExportPkcs8PrivateKey(),
                    (T key, Span<byte> destination, out int bytesWritten) =>
                        key.TryExportPkcs8PrivateKey(destination, out bytesWritten));
            }
            else
            {
                using (T key = CreateKey())
                {
                    Exception e = Assert.ThrowsAny<Exception>(
                        () => key.ImportPkcs8PrivateKey(Convert.FromBase64String(base64Pkcs8), out _));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        "e is PlatformNotSupportedException || e is CryptographicException");
                }
            }
        }

        private void ReadWriteBase64SubjectPublicKeyInfo(
            string base64SubjectPublicKeyInfo,
            in ECParameters expected,
            bool isSupported = true)
        {
            if (isSupported)
            {
                ECParameters expectedPublic = expected;
                expectedPublic.D = null;

                ReadWriteKey(
                    base64SubjectPublicKeyInfo,
                    expectedPublic,
                    (T key, ReadOnlySpan<byte> source, out int read) =>
                        key.ImportSubjectPublicKeyInfo(source, out read),
                    key => key.ExportSubjectPublicKeyInfo(),
                    (T key, Span<byte> destination, out int written) =>
                        key.TryExportSubjectPublicKeyInfo(destination, out written));
            }
            else
            {
                using (T key = CreateKey())
                {
                    Exception e = Assert.ThrowsAny<Exception>(
                        () => key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(base64SubjectPublicKeyInfo), out _));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        "e is PlatformNotSupportedException || e is CryptographicException");
                }
            }
        }

        private void ReadWriteKey(
            string base64,
            in ECParameters expected,
            ReadKeyAction readAction,
            Func<T, byte[]> writeArrayFunc,
            WriteKeyToSpanFunc writeSpanFunc,
            bool isEncrypted = false)
        {
            bool isPrivateKey = expected.D != null;

            byte[] derBytes = Convert.FromBase64String(base64);
            byte[] arrayExport;
            byte[] tooBig;
            const int OverAllocate = 30;
            const int WriteShift = 6;

            using (T key = CreateKey())
            {
                readAction(key, derBytes, out int bytesRead);
                Assert.Equal(derBytes.Length, bytesRead);

                arrayExport = writeArrayFunc(key);

                ECParameters ecParameters = ExportParameters(key, isPrivateKey);
                EccTestBase.AssertEqual(expected, ecParameters);
            }

            // It's not reasonable to assume that arrayExport and derBytes have the same
            // contents, because the SubjectPublicKeyInfo and PrivateKeyInfo formats both
            // have the curve identifier in the AlgorithmIdentifier.Parameters field, and
            // either the input or the output may have chosen to then not emit it in the
            // optional domainParameters field of the ECPrivateKey blob.
            //
            // Once we have exported the data to normalize it, though, we should see
            // consistency in the answer format.

            using (T key = CreateKey())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => readAction(key, arrayExport.AsSpan(1), out _));

                Assert.ThrowsAny<CryptographicException>(
                    () => readAction(key, arrayExport.AsSpan(0, arrayExport.Length - 1), out _));

                readAction(key, arrayExport, out int bytesRead);
                Assert.Equal(arrayExport.Length, bytesRead);

                ECParameters ecParameters = ExportParameters(key, isPrivateKey);
                EccTestBase.AssertEqual(expected, ecParameters);

                Assert.False(
                    writeSpanFunc(key, Span<byte>.Empty, out int bytesWritten),
                    "Write to empty span");

                Assert.Equal(0, bytesWritten);

                Assert.False(
                    writeSpanFunc(
                        key,
                        derBytes.AsSpan(0, Math.Min(derBytes.Length, arrayExport.Length) - 1),
                        out bytesWritten),
                    "Write to too-small span");

                Assert.Equal(0, bytesWritten);

                tooBig = new byte[arrayExport.Length + OverAllocate];
                tooBig.AsSpan().Fill(0xC4);

                Assert.True(writeSpanFunc(key, tooBig.AsSpan(WriteShift), out bytesWritten));
                Assert.Equal(arrayExport.Length, bytesWritten);

                Assert.Equal(0xC4, tooBig[WriteShift - 1]);
                Assert.Equal(0xC4, tooBig[WriteShift + bytesWritten + 1]);

                // If encrypted, the data should have had a random salt applied, so unstable.
                // Otherwise, we've normalized the data (even for private keys) so the output
                // should match what it output previously.
                if (isEncrypted)
                {
                    Assert.NotEqual(
                        arrayExport.ByteArrayToHex(),
                        tooBig.AsSpan(WriteShift, bytesWritten).ByteArrayToHex());
                }
                else
                {
                    Assert.Equal(
                        arrayExport.ByteArrayToHex(),
                        tooBig.AsSpan(WriteShift, bytesWritten).ByteArrayToHex());
                }
            }

            using (T key = CreateKey())
            {
                readAction(key, tooBig.AsSpan(WriteShift), out int bytesRead);
                Assert.Equal(arrayExport.Length, bytesRead);

                arrayExport.AsSpan().Fill(0xCA);

                Assert.True(
                    writeSpanFunc(key, arrayExport, out int bytesWritten),
                    "Write to precisely allocated Span");

                if (isEncrypted)
                {
                    Assert.NotEqual(
                        tooBig.AsSpan(WriteShift, bytesWritten).ByteArrayToHex(),
                        arrayExport.ByteArrayToHex());
                }
                else
                {
                    Assert.Equal(
                        tooBig.AsSpan(WriteShift, bytesWritten).ByteArrayToHex(),
                        arrayExport.ByteArrayToHex());
                }
            }
        }

        private delegate void ReadKeyAction(T key, ReadOnlySpan<byte> source, out int bytesRead);
        private delegate bool WriteKeyToSpanFunc(T key, Span<byte> destination, out int bytesWritten);
    }
}
