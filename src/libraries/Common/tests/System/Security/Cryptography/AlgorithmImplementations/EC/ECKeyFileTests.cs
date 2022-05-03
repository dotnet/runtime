// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Encryption.RC2.Tests;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract partial class ECKeyFileTests<T> where T : ECAlgorithm
    {
        protected abstract T CreateKey();
        protected abstract void Exercise(T key);
        protected virtual Func<T, byte[]> PublicKeyWriteArrayFunc { get; } = null;
        protected virtual WriteKeyToSpanFunc PublicKeyWriteSpanFunc { get; } = null;

        // This would need to be virtualized if there was ever a platform that
        // allowed explicit in ECDH or ECDSA but not the other.
        public static bool SupportsExplicitCurves { get; } = EcDiffieHellman.Tests.ECDiffieHellmanFactory.ExplicitCurvesSupported;

        public static bool CanDeriveNewPublicKey { get; } = EcDiffieHellman.Tests.ECDiffieHellmanFactory.CanDeriveNewPublicKey;

        public static bool SupportsBrainpool { get; } = IsCurveSupported(ECCurve.NamedCurves.brainpoolP160r1.Oid);
        public static bool SupportsSect163k1 { get; } = IsCurveSupported(EccTestData.Sect163k1Key1.Curve.Oid);
        public static bool SupportsSect283k1 { get; } = IsCurveSupported(EccTestData.Sect283k1Key1.Curve.Oid);
        public static bool SupportsC2pnb163v1 { get; } = IsCurveSupported(EccTestData.C2pnb163v1Key1.Curve.Oid);

        // Some platforms support explicitly specifying these curves, but do not support specifying them by name.
        public static bool ExplicitNamedSameSupport { get; } = !PlatformDetection.IsAndroid;
        public static bool SupportsSect163k1Explicit { get; } = SupportsSect163k1 || (!ExplicitNamedSameSupport && SupportsExplicitCurves);
        public static bool SupportsC2pnb163v1Explicit { get; } = SupportsC2pnb163v1 || (!ExplicitNamedSameSupport && SupportsExplicitCurves);

        private static bool IsCurveSupported(Oid oid)
        {
            return EcDiffieHellman.Tests.ECDiffieHellmanFactory.IsCurveValid(oid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UseAfterDispose(bool importKey)
        {
            T key = CreateKey();

            if (importKey)
            {
                key.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            }

            byte[] ecPrivate;
            byte[] pkcs8Private;
            byte[] pkcs8EncryptedPrivate;
            byte[] subjectPublicKeyInfo;

            string pwStr = "Hello";
            // Because the PBE algorithm uses PBES2 the string->byte encoding is UTF-8.
            byte[] pwBytes = Encoding.UTF8.GetBytes(pwStr);

            PbeParameters pbeParameters = new PbeParameters(
                PbeEncryptionAlgorithm.Aes192Cbc,
                HashAlgorithmName.SHA256,
                3072);

            // Ensure the key was loaded, then dispose it.
            // Also ensures all of the inputs are valid for the disposed tests.
            using (key)
            {
                ecPrivate = key.ExportECPrivateKey();
                pkcs8Private = key.ExportPkcs8PrivateKey();
                pkcs8EncryptedPrivate = key.ExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters);
                subjectPublicKeyInfo = key.ExportSubjectPublicKeyInfo();
            }

            Assert.Throws<ObjectDisposedException>(() => key.ImportECPrivateKey(ecPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ImportPkcs8PrivateKey(pkcs8Private, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ImportEncryptedPkcs8PrivateKey(pwStr, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ImportEncryptedPkcs8PrivateKey(pwBytes, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _));

            Assert.Throws<ObjectDisposedException>(() => key.ExportECPrivateKey());
            Assert.Throws<ObjectDisposedException>(() => key.TryExportECPrivateKey(ecPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => key.TryExportPkcs8PrivateKey(pkcs8Private, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => key.TryExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ExportEncryptedPkcs8PrivateKey(pwBytes, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => key.TryExportEncryptedPkcs8PrivateKey(pwBytes, pbeParameters, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => key.TryExportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _));

            // Check encrypted import with the wrong password.
            // It shouldn't do enough work to realize it was wrong.
            pwBytes = Array.Empty<byte>();
            Assert.Throws<ObjectDisposedException>(() => key.ImportEncryptedPkcs8PrivateKey((ReadOnlySpan<char>)"", pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => key.ImportEncryptedPkcs8PrivateKey(pwBytes, pkcs8EncryptedPrivate, out _));
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
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test key.")]
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

        [ConditionalFact(typeof(RC2Factory), nameof(RC2Factory.IsSupported))]
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
        public void ReadWriteNistP256ExplicitECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIIBaAIBAQQgcKEsLbFoRe1W/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qggfowgfcC
AQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////
MFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr
vVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE
axfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W
K84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8
YyVRAgEBoUQDQgAEgQHs5HRkpurXDPaabivT2IaRoyYtIsuk92Ner/JmgKjYoSum
HVmSNfZ9nLTVjxeD08pD548KWrqmJAeZNsDDqQ==",
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                SupportsExplicitCurves);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitPkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MIIBeQIBADCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAAB
AAAAAAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA
///////////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMV
AMSdNgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg
9KE5RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8A
AAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBBG0wawIBAQQgcKEsLbFoRe1W
/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qhRANCAASBAezkdGSm6tcM9ppuK9PYhpGj
Ji0iy6T3Y16v8maAqNihK6YdWZI19n2ctNWPF4PTykPnjwpauqYkB5k2wMOp",
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                SupportsExplicitCurves);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitEncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBoTAbBgkqhkiG9w0BBQMwDgQIQqYZ3N87K0ICAggABIIBgOHAWa6wz144p0uT
qZsQAbQcIpAFBQRC382dxiOHCV11OyZg264SmxS9iY1OEwIr/peACLu+Fk7zPKhv
Ox1hYz/OeLoKKdtBMqrp65JmH73jG8qeAMuYNj83AIERY7Cckuc2fEC2GTEJcNWs
olE+0p4H6yIvXI48NEQazj5w9zfOGvLmP6Kw6nX+SV3fzM9jHskU226LnDdokGVg
an6/hV1r+2+n2MujhfNzQd/5vW5zx7PN/1aMVMz3wUv9t8scDppeMR5CNCMkxlRA
cQ2lfx2vqFuY70EckgumDqm7AtKK2bLlA6XGTb8HuqKHA0l1zrul9AOBC1g33isD
5CJu1CCT34adV4E4G44uiRQUtf+K8m5Oeo8FI/gGBxdQyOh1k8TNsM+p32gTU8HH
89M5R+s1ayQI7jVPGHXm8Ch7lxvqo6FZAu6+vh23vTwVShUTpGYd0XguE6XKJjGx
eWDIWFuFRj58uAQ65/viFausHWt1BdywcwcyVRb2eLI5MR7DWA==",
                "explicit",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA256,
                    1234),
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                SupportsExplicitCurves);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitSubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MIIBSzCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAA
AAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA////
///////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSd
NgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5
RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA
//////////+85vqtpxeehPO5ysL8YyVRAgEBA0IABIEB7OR0ZKbq1wz2mm4r09iG
kaMmLSLLpPdjXq/yZoCo2KErph1ZkjX2fZy01Y8Xg9PKQ+ePClq6piQHmTbAw6k=",
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                SupportsExplicitCurves);
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
        public void ReadWriteSect163k1Key1ECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MFMCAQEEFQPBmVrfrowFGNwT3+YwS7AQF+akEqAHBgUrgQQAAaEuAywABAYXnjcZ
zIElQ1/mRYnV/KbcGIdVHQeI/rti/8kkjYs5iv4+C1w8ArP+Nw==",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1);
        }

        [Fact]
        public void ReadWriteSect163k1Key1Pkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MGMCAQAwEAYHKoZIzj0CAQYFK4EEAAEETDBKAgEBBBUDwZla366MBRjcE9/mMEuw
EBfmpBKhLgMsAAQGF543GcyBJUNf5kWJ1fym3BiHVR0HiP67Yv/JJI2LOYr+Pgtc
PAKz/jc=",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1);
        }

        [Fact]
        public void ReadWriteSect163k1Key1EncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGHMBsGCSqGSIb3DQEFAzAOBAjLBuCZyPt15QICCAAEaPa9V9VJoB8G+RIgZaYv
z4xl+rpvkDrDI0Xnh8oj1CLQldy2N77pdk3pOg9TwJo+r+eKfIJgBVezW2O615ww
f+ESRyxDnBgKz6H2RKeenyrwVhxF98SyJzAdP637vR3QmDNAWWAgoUhg",
                "Koblitz",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    7),
                EccTestData.Sect163k1Key1,
                SupportsSect163k1);
        }

        [Fact]
        public void ReadWriteSect163k1Key1SubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MEAwEAYHKoZIzj0CAQYFK4EEAAEDLAAEBheeNxnMgSVDX+ZFidX8ptwYh1UdB4j+
u2L/ySSNizmK/j4LXDwCs/43",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteSect163k1Key1ExplicitECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIHHAgEBBBUDwZla366MBRjcE9/mMEuwEBfmpBKgezB5AgEBMCUGByqGSM49AQIw
GgICAKMGCSqGSM49AQIDAzAJAgEDAgEGAgEHMAYEAQEEAQEEKwQC/hPAU3u8Eayq
B9eT3k5tXlyU7ugCiQcPsF04/1gyHy6ABTbVOMzao9kCFQQAAAAAAAAAAAACAQii
4MwNmfil7wIBAqEuAywABAYXnjcZzIElQ1/mRYnV/KbcGIdVHQeI/rti/8kkjYs5
iv4+C1w8ArP+Nw==",
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteSect163k1Key1ExplicitPkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MIHYAgEAMIGEBgcqhkjOPQIBMHkCAQEwJQYHKoZIzj0BAjAaAgIAowYJKoZIzj0B
AgMDMAkCAQMCAQYCAQcwBgQBAQQBAQQrBAL+E8BTe7wRrKoH15PeTm1eXJTu6AKJ
Bw+wXTj/WDIfLoAFNtU4zNqj2QIVBAAAAAAAAAAAAAIBCKLgzA2Z+KXvAgECBEww
SgIBAQQVA8GZWt+ujAUY3BPf5jBLsBAX5qQSoS4DLAAEBheeNxnMgSVDX+ZFidX8
ptwYh1UdB4j+u2L/ySSNizmK/j4LXDwCs/43",
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteSect163k1Key1ExplicitEncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBADAbBgkqhkiG9w0BBQMwDgQICAkWq2tKYZUCAggABIHgjBfngwE9DbCEaznz
+55MjSGbQH0NMgIRCJtQLbrI7888+KmTL6hWYPH6CQzTsi1unWrMAH2JKa7dkIe9
FWNXW7bmhcokVDh/OTXOV9QPZ3O4m19a9XOl0wNlbi47XQ3KUkcbzyFNYlDMSzFw
HRfW8+aIkyYAvYCoA4buRfigBe0xy1VKyE5aUkX0EFjx4gqC3Q5mjDMFOxlKNjVV
clSZg6tg9J7bTQsDAN0uYpBc1r8DiSQbKMxg+q13yBciXJzfmkQRtNVXQPsseiUm
z2NFvWcpK0Fh9fCVGuXV9sjJ5qE=",
                "Koblitz",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA256,
                    12),
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteSect163k1Key1ExplicitSubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MIG1MIGEBgcqhkjOPQIBMHkCAQEwJQYHKoZIzj0BAjAaAgIAowYJKoZIzj0BAgMD
MAkCAQMCAQYCAQcwBgQBAQQBAQQrBAL+E8BTe7wRrKoH15PeTm1eXJTu6AKJBw+w
XTj/WDIfLoAFNtU4zNqj2QIVBAAAAAAAAAAAAAIBCKLgzA2Z+KXvAgECAywABAYX
njcZzIElQ1/mRYnV/KbcGIdVHQeI/rti/8kkjYs5iv4+C1w8ArP+Nw==",
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit);
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
        public void ReadWriteC2pnb163v1ECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MFYCAQEEFQD00koUBxIvRFlnvh2TwAk6ZTZ5hqAKBggqhkjOPQMAAaEuAywABAIR
Jy8cVYJCaIjpG9aSV3SUIyJIqgQnCDD3oQCa1nCojekr1ZJIzIE7RQ==",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1);
        }

        [Fact]
        public void ReadWriteC2pnb163v1Pkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MGYCAQAwEwYHKoZIzj0CAQYIKoZIzj0DAAEETDBKAgEBBBUA9NJKFAcSL0RZZ74d
k8AJOmU2eYahLgMsAAQCEScvHFWCQmiI6RvWkld0lCMiSKoEJwgw96EAmtZwqI3p
K9WSSMyBO0U=",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1);
        }

        [Fact]
        public void ReadWriteC2pnb163v1EncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGPMBsGCSqGSIb3DQEFAzAOBAjdV9IDq+L+5gICCAAEcI1e6RA8kMcYB+PvOcCU
Jj65nXTIrMPmZ0DmFMF9WBg0J+yzxgDhBVynpT2uJntY4FuDlvdpcLRK1EGLZYKf
qYc5zJMYkRZ178bE3DtfrP3UxD34YvbRl2aeu334+wJOm7ApXv81ugt4OoCiPhdg
wiA=",
                "secret",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA512,
                    1024),
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1);
        }

        [Fact]
        public void ReadWriteC2pnb163v1SubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MEMwEwYHKoZIzj0CAQYIKoZIzj0DAAEDLAAEAhEnLxxVgkJoiOkb1pJXdJQjIkiq
BCcIMPehAJrWcKiN6SvVkkjMgTtF",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteC2pnb163v1ExplicitECPrivateKey()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIIBBwIBAQQVAPTSShQHEi9EWWe+HZPACTplNnmGoIG6MIG3AgEBMCUGByqGSM49
AQIwGgICAKMGCSqGSM49AQIDAzAJAgEBAgECAgEIMEQEFQclRrVDUjSkIuB4lnX0
MsiUNd5SQgQUyVF9BtUkDTz/OMdLILbNTW+d1NkDFQDSwPsVdghg3vHu9NaW5naH
VhUXVAQrBAevaZiVRhA9eTKfzD10iA8zu+gDywHsIyEbWWat6h0/h/fqWEiu8LfK
nwIVBAAAAAAAAAAAAAHmD8iCHMdNrq/BAgECoS4DLAAEAhEnLxxVgkJoiOkb1pJX
dJQjIkiqBCcIMPehAJrWcKiN6SvVkkjMgTtF",
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteC2pnb163v1ExplicitPkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MIIBFwIBADCBwwYHKoZIzj0CATCBtwIBATAlBgcqhkjOPQECMBoCAgCjBgkqhkjO
PQECAwMwCQIBAQIBAgIBCDBEBBUHJUa1Q1I0pCLgeJZ19DLIlDXeUkIEFMlRfQbV
JA08/zjHSyC2zU1vndTZAxUA0sD7FXYIYN7x7vTWluZ2h1YVF1QEKwQHr2mYlUYQ
PXkyn8w9dIgPM7voA8sB7CMhG1lmreodP4f36lhIrvC3yp8CFQQAAAAAAAAAAAAB
5g/IghzHTa6vwQIBAgRMMEoCAQEEFQD00koUBxIvRFlnvh2TwAk6ZTZ5hqEuAywA
BAIRJy8cVYJCaIjpG9aSV3SUIyJIqgQnCDD3oQCa1nCojekr1ZJIzIE7RQ==",
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteC2pnb163v1ExplicitEncryptedPkcs8()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBQTAbBgkqhkiG9w0BBQMwDgQI9+ZZnHaqxb0CAggABIIBIM+n6x/Q1hs5OW0F
oOKZmQ0mKNRKb23SMqwo0bJlxseIOVdYzOV2LH1hSWeJb7FMxo6OJXb2CpYSPqv1
v3lhdLC5t/ViqAOhG70KF+Dy/vZr8rWXRFqy+OdqwxOes/lBsG+Ws9+uEk8+Gm2G
xMHXJNKliSUePlT3wC7z8bCkEvLF7hkGjEAgcABry5Ohq3W2by6Dnd8YWJNgeiW/
Vu5rT1ThAus7w2TJjWrrEqBbIlQ9nm6/MMj9nYnVVfpPAOk/qX9Or7TmK+Sei88Q
staXBhfJk9ec8laiPpNbhHJSZ2Ph3Snb6SA7MYi5nIMP4RPxOM2eUet4/ueV1O3U
wxcZ+wOsnebIwy4ftKL+klh5EXv/9S5sCjC8g8J2cA6GmcZbiQ==",
                "secret",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA512,
                    1024),
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64446", typeof(PlatformSupport), nameof(PlatformSupport.IsAndroidVersionAtLeast31))]
        public void ReadWriteC2pnb163v1ExplicitSubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MIH0MIHDBgcqhkjOPQIBMIG3AgEBMCUGByqGSM49AQIwGgICAKMGCSqGSM49AQID
AzAJAgEBAgECAgEIMEQEFQclRrVDUjSkIuB4lnX0MsiUNd5SQgQUyVF9BtUkDTz/
OMdLILbNTW+d1NkDFQDSwPsVdghg3vHu9NaW5naHVhUXVAQrBAevaZiVRhA9eTKf
zD10iA8zu+gDywHsIyEbWWat6h0/h/fqWEiu8LfKnwIVBAAAAAAAAAAAAAHmD8iC
HMdNrq/BAgECAywABAIRJy8cVYJCaIjpG9aSV3SUIyJIqgQnCDD3oQCa1nCojekr
1ZJIzIE7RQ==",
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit);
        }

        [Fact]
        public void NoFuzzySubjectPublicKeyInfo()
        {
            using (T key = CreateKey())
            {
                int bytesRead = -1;
                byte[] ecPriv = key.ExportECPrivateKey();

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
                    () => key.ImportECPrivateKey(spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportECPrivateKey(pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = spki.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportECPrivateKey(encryptedPkcs8, out bytesRead));

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

                byte[] ecPriv = key.ExportECPrivateKey();

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

                byte[] ecPriv = key.ExportECPrivateKey();

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
                key.ImportParameters(parameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportECPrivateKey());

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportECPrivateKey(Span<byte>.Empty, out _));

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

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
        public void DecryptPkcs12PbeTooManyIterations()
        {
            // pbeWithSHAAnd3-KeyTripleDES-CBC with 600,001 iterations
            byte[] high3DesIterationKey = Convert.FromBase64String(@"
MIG6MCUGCiqGSIb3DQEMAQMwFwQQWOZyFrGwhyGTEd2nbKuLSQIDCSfBBIGQCgPLkx0OwmK3lJ9o
VAdJAg/2nvOhboOHciu5I6oh5dRkxeDjUJixsadd3uhiZb5v7UgiohBQsFv+PWU12rmz6sgWR9rK
V2UqV6Y5vrHJDlNJGI+CQKzOTF7LXyOT+EqaXHD+25TM2/kcZjZrOdigkgQBAFhbfn2/hV/t0TPe
Tj/54rcY3i0gXT6da/r/o+qV");

            using (T key = CreateKey())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey((ReadOnlySpan<char>)"test", high3DesIterationKey, out _));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
        public void ReadWriteEc256EncryptedPkcs8_Pbes2HighIterations()
        {
            // pkcs5PBES2 hmacWithSHA256 aes128-CBC with 600,001 iterations
            ReadWriteBase64EncryptedPkcs8(@"
MIH1MGAGCSqGSIb3DQEFDTBTMDIGCSqGSIb3DQEFDDAlBBA+rne0bUkwr614vLfQkwO4AgMJJ8Ew
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEIm3c9r5igQ9Vlv1mKTZYp0EgZC8KZfmJtfYmsl4
Z0Dc85ugFvtFHVeRbcvfYmFns23WL3gpGQ0mj4BKxttX+WuDk9duAsCslNLvXFY7m3MQRkWA6QHT
A8DiR3j0l5TGBkErbTUrjmB3ftvEmmF9mleRLj6qEYmmKdCV2Tfk1YBOZ2mpB9bpCPipUansyqWs
xoMaz20Yx+2TSN5dSm2FcD+0YFI=",
                "test",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA256,
                    600_001),
                EccTestData.GetNistP256ReferenceKey());
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
                        () => key.ImportEncryptedPkcs8PrivateKey((ReadOnlySpan<char>)"ThisBetterNotBeThePassword!", encrypted, out _));

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
                        key.ImportECPrivateKey(source, out read),
                    key => key.ExportECPrivateKey(),
                    (T key, Span<byte> destination, out int bytesWritten) =>
                        key.TryExportECPrivateKey(destination, out bytesWritten));
            }
            else
            {
                using (T key = CreateKey())
                {
                    Exception e = Assert.ThrowsAny<Exception>(
                        () => key.ImportECPrivateKey(Convert.FromBase64String(base64Pkcs8), out _));

                    Assert.True(
                        e is PlatformNotSupportedException || e is CryptographicException,
                        $"e should be PlatformNotSupportedException or CryptographicException.\n\te is {e.ToString()}");
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
                        key.TryExportSubjectPublicKeyInfo(destination, out written),
                    writePublicArrayFunc: PublicKeyWriteArrayFunc,
                    writePublicSpanFunc: PublicKeyWriteSpanFunc);
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
            bool isEncrypted = false,
            Func<T, byte[]> writePublicArrayFunc = null,
            WriteKeyToSpanFunc writePublicSpanFunc = null)
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

                if (writePublicArrayFunc is not null)
                {
                    byte[] publicArrayExport = writePublicArrayFunc(key);
                    Assert.Equal(arrayExport, publicArrayExport);

                    Assert.True(writePublicSpanFunc(key, publicArrayExport, out int publicExportWritten));
                    Assert.Equal(publicExportWritten, publicArrayExport.Length);
                    Assert.Equal(arrayExport, publicArrayExport);
                }

                ECParameters ecParameters = key.ExportParameters(isPrivateKey);
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

                ECParameters ecParameters = key.ExportParameters(isPrivateKey);
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

        protected delegate void ReadKeyAction(T key, ReadOnlySpan<byte> source, out int bytesRead);
        protected delegate bool WriteKeyToSpanFunc(T key, Span<byte> destination, out int bytesWritten);
    }
}
