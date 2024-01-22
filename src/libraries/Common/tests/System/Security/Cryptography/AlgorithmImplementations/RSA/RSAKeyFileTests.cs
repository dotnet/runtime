// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Encryption.RC2.Tests;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class RSAKeyFileTests
    {
        public static bool Supports384BitPrivateKeyAndRC2 { get; } = RSAFactory.Supports384PrivateKey && RC2Factory.IsSupported;
        public static bool SupportsLargeExponent { get; } = RSAFactory.SupportsLargeExponent;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void UseAfterDispose(bool importKey)
        {
            RSA rsa = importKey ? RSAFactory.Create(TestData.RSA2048Params) : RSAFactory.Create(1024);
            byte[] pkcs1Public;
            byte[] pkcs1Private;
            byte[] pkcs8Private;
            byte[] pkcs8EncryptedPrivate;
            byte[] subjectPublicKeyInfo;

            string pwStr = "Hello";
            // Because the PBE algorithm uses PBES2 the string->byte encoding is UTF-8.
            byte[] pwBytes = TestData.HelloBytes;

            PbeParameters pbeParameters = new PbeParameters(
                PbeEncryptionAlgorithm.Aes192Cbc,
                HashAlgorithmName.SHA256,
                3072);

            // Ensure the key was loaded, then dispose it.
            // Also ensures all of the inputs are valid for the disposed tests.
            using (rsa)
            {
                pkcs1Public = rsa.ExportRSAPublicKey();
                pkcs1Private = rsa.ExportRSAPrivateKey();
                pkcs8Private = rsa.ExportPkcs8PrivateKey();
                pkcs8EncryptedPrivate = rsa.ExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters);
                subjectPublicKeyInfo = rsa.ExportSubjectPublicKeyInfo();
            }

            Assert.Throws<ObjectDisposedException>(() => rsa.ImportRSAPublicKey(pkcs1Public, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportRSAPrivateKey(pkcs1Private, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportPkcs8PrivateKey(pkcs8Private, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportEncryptedPkcs8PrivateKey(pwStr, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportEncryptedPkcs8PrivateKey(pwBytes, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _));

            Assert.Throws<ObjectDisposedException>(() => rsa.ExportRSAPublicKey());
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportRSAPublicKey(pkcs1Public, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ExportRSAPrivateKey());
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportRSAPrivateKey(pkcs1Private, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportPkcs8PrivateKey(pkcs8Private, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportEncryptedPkcs8PrivateKey(pwStr, pbeParameters, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ExportEncryptedPkcs8PrivateKey(pwBytes, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportEncryptedPkcs8PrivateKey(pwBytes, pbeParameters, pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => rsa.TryExportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _));

            // Check encrypted import with the wrong password.
            // It shouldn't do enough work to realize it was wrong.
            pwBytes = Array.Empty<byte>();
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportEncryptedPkcs8PrivateKey((ReadOnlySpan<char>)"", pkcs8EncryptedPrivate, out _));
            Assert.Throws<ObjectDisposedException>(() => rsa.ImportEncryptedPkcs8PrivateKey(pwBytes, pkcs8EncryptedPrivate, out _));
        }

        [ConditionalFact(nameof(SupportsLargeExponent))]
        public static void ReadWriteBigExponentPrivatePkcs1()
        {
            ReadWriteBase64PrivatePkcs1(
                @"
MIIEpQIBAAKCAQEAr4HBy9ggP2JKU57WYIF1NyOTooN9SJDkihne02lzEVYglo1r
4NPao4qnd74C7gtrk7ck6NzBK2MrT6gLvJJbzmJPTKfMYGMGs5QD4oyTLSTdVG/+
TvajfxB3CyIV6oy7W/Qn6MTYm3nrM4N1EAxfg+Vd6bRGbd++7kJTmu8z7xh7d2DD
saGyEDwtgURWSgwQOaCchc9rWXTrUW/I1mI8lK46WguztMeSlX1DI5FWbPPipSr7
DBQrngaBuJcmca8rgt05Cjm5Oc9xlWhofkmQpjBQyndo3NazeIQvGP2x9tn/CWuv
e+uY3Pkw1m/P1QP1jUG/9GIS4k46/EXqQr2IRwIFAgAABEECggEAZK+bpSYkg9qS
tT8TQ5/Q7xMBL4eavAPLfAbxIJkE81LB8iNRncSL+u67URsNlV9hZ7UOA0/qKrxZ
C06p+/DFH5/+oW95J65oHL9zWEUryinVhwXgyqEGATsJpvX1kRSY0sT9aRVYVIjl
862Jg2yTyHda+rTRPCAUJmvo7muKpmyelC1JNGbI46Nw+OY3jOldY34DZzZwvkvK
zl/NrdI42fMso13oRXdqxL82EYgSMoxJP5HCWpvUJnLQr6/eCvfmGQeNSLSF75GT
Pdz/tUWHuPUS0iPIGJTpF4SYLzxcZYcTUfRlWrAjxK2ZtrA6lvkEbOEkpHHoKPBf
jbO8fMzy0QKBgQDkOjgmqXIErjzYZJqE20u/ByXEsI+MQ4QFV6DNBOMTr20EYN3m
nNxQitBD1yUU2npmvJGM2WJPSFZEud7qsr4OESlW1HLPD9UfgP0zhy0tzFYqBYiw
EujJDOfSVLlHksbnoCs8yqFQ5npkN3rMSUea1etVVJOyEAywQQlW99c79QKBgQDE
3S163WylB0DTlz9AxN69urUff1GBq65ybDJZaj7dCu5E2q3dipt6hkxP/a4AxMsf
EBd7oBwEZvgS1SJhD4xFQ/HD71efqeE66NoaSo2uMHhh0s6sA1YCebYbZRSYmIP+
hsXHQg0xKDj8L3C+1ZtSKWVCAYgmZM76OLSKNyPpywKBgAns8VH1zdLJ5uUmgjZP
pbTtCU9iLkAxv0a4UTWKWE3MtTKLC9m2NYkYP0kVk9KjrK0U4KrNofGBtcfZPFft
JuYsn8Jq835KBkTs6Cp7qK7Yj/HY6cVsxmOFzbJE6z1X0X5q1CCxnJ4r7hgZK4Fi
ZbdNpV+jgl+SLZ2Og1t2vzBxAoGBAImzO2lXiRdLiDaMSUY51NMmciRXKkCy/mGR
A4Qijj29Ee7ZBAzQOXfp4Nf8i/xL9Kkyg1Kf8dllkLGPTqvvAwN5Tyk+iNx2Gz4j
r+yxnyn4pNKpBYtxTPP00Qcz8T6nK78fvsjXHhBtDOIRXzrS3gIDJcOHmgkcQTzW
OX+Ds8uJAoGAfFftdMkXb7p2wjGDICUVBixmTU1J/z4DcEejCdoQ8VkM4Bt6HNGk
Mm3HWIPf+TEQqwZartFAybmBdqiBCAmt7HXoZ2SglRWX70Z/qP1QkYHNLkkeQ75B
CE5b4bVi7nbp+SyaseWurZ0pGmM35N6FveZ6DXK05Vrc8gf3paUiXhU=",
                TestData.RsaBigExponentParams);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
        public static void ReadWriteDiminishedDPPrivatePkcs1()
        {
            ReadWriteBase64PrivatePkcs1(
                @"
MIIBOwIBAAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQxm5NTLEHDwUd7idstLzPX
uah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQJBAK/m8jYvnK9exaSR+DAh
Ij12ip5pB+HOFOdhCbS/coNoIowa6WJGrd3Np1m9BBhouWloF8UB6Iu8/e/wAg+F
9ykCIQDzcnsehnYgVZTTxzoCJ01PGpgESilRyFzNEsb8V60ZewIhAMCyOujqUqn7
Q079SlHzXuvocqIdt4IM1EmIlrlU9GGvAh8Ijv3FFPUSLfANgfOIH9mX7ldpzzGk
rmaUzxQvyuVLAiEArCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKsCIQDC
yZWUxoxAdjfrBGsx+U6BHM0Myqqe7fY7hjWzj4aBCw==",
                TestData.DiminishedDPParameters);
        }

        [ConditionalFact]
        [OuterLoop("RSA 16384 takes considerable time.")]
        public static void ReadWritePublicPkcs1()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384.");
            }

            ReadWriteBase64PublicPkcs1(
                @"
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",
                TestData.RSA16384Params);
        }

        [ConditionalFact(nameof(SupportsLargeExponent))]
        public static void ReadWriteSubjectPublicKeyInfo()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MIIBJDANBgkqhkiG9w0BAQEFAAOCAREAMIIBDAKCAQEAr4HBy9ggP2JKU57WYIF1
NyOTooN9SJDkihne02lzEVYglo1r4NPao4qnd74C7gtrk7ck6NzBK2MrT6gLvJJb
zmJPTKfMYGMGs5QD4oyTLSTdVG/+TvajfxB3CyIV6oy7W/Qn6MTYm3nrM4N1EAxf
g+Vd6bRGbd++7kJTmu8z7xh7d2DDsaGyEDwtgURWSgwQOaCchc9rWXTrUW/I1mI8
lK46WguztMeSlX1DI5FWbPPipSr7DBQrngaBuJcmca8rgt05Cjm5Oc9xlWhofkmQ
pjBQyndo3NazeIQvGP2x9tn/CWuve+uY3Pkw1m/P1QP1jUG/9GIS4k46/EXqQr2I
RwIFAgAABEE=",
                TestData.RsaBigExponentParams);
        }

        [Fact]
        public static void ReadWriteSubjectPublicKeyInfo_DiminishedDPKey()
        {
            ReadWriteBase64SubjectPublicKeyInfo(
                @"
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBALc/WfXui9VeJLf/AprRaoVDyW0lPlQx
m5NTLEHDwUd7idstLzPXuah0WEjgao5oO1BEUR4byjYlJ+F89Cs4BhUCAwEAAQ==",
                TestData.DiminishedDPParameters);
        }


        [ConditionalFact]
        [OuterLoop("RSA 16384 takes considerable time.")]
        public static void ReadWriteRsa16384SubjectPublicKeyInfo()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384.");
            }

            ReadWriteBase64SubjectPublicKeyInfo(
                @"
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
5hHlolNkpw+thqvba8lMvzMCAwEAAQ==",
                TestData.RSA16384Params);
        }

        [ConditionalFact]
        [OuterLoop("RSA 16384 takes considerable time.")]
        public static void ReadWrite16384Pkcs8()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384");
            }

            ReadWriteBase64Pkcs8(
                @"
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
xBdaeIJFmTymL1LOru69mA9gwhuFFQ==",
                TestData.RSA16384Params);
        }

        [Fact]
        public static void ReadWriteDiminishedDPPkcs8()
        {
            ReadWriteBase64Pkcs8(
                @"
MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAtz9Z9e6L1V4kt/8C
mtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7UERRHhvKNiUn4Xz0
KzgGFQIDAQABAkEAr+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrp
Ykat3c2nWb0EGGi5aWgXxQHoi7z97/ACD4X3KQIhAPNyex6GdiBVlNPHOgInTU8a
mARKKVHIXM0SxvxXrRl7AiEAwLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0
Ya8CHwiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5UsCIQCsJMzx1JuilQAN
acPiMCuFTnRSFYAhozpmsqoLyTREqwIhAMLJlZTGjEB2N+sEazH5ToEczQzKqp7t
9juGNbOPhoEL",
                TestData.DiminishedDPParameters);
        }

        [Fact]
        public static void ReadEncryptedDiminishedDP()
        {
            // PBES1: PbeWithMD5AndDESCBC
            const string base64 = @"
MIIBgTAbBgkqhkiG9w0BBQMwDgQI4cFTpQ4kAU0CAggABIIBYO1zEiMx+W9RzsbC
rKGzDRko5PxWiX1xTya2pU2Nnt8FTstf2xWUQJtmPq3miMVHskgaXXgDc7O/cUkX
aM01UqvUxe1+PwrcllZPT6WeDLKWD0mVLGsY2BZz52UmoShXkraYovuHehyATHeL
CEZ1Nn0U+JBU0VlqwhAmD/sagZ7+aq0zggJGvr9wH0DZW5RWCncvG+iZXw/cPfQ9
gTlrGm8FIWTEwtt+nHv38Ep1zfG3DjLZ/o6IXX+KGykxdkWJVTIFrgCZ+TZ2KH9b
zL4zxrUq62QFTGlLiDXPmpua0ZksMlGfVAOyCdwZ/rfJQQomG4BeRF44jgVx5lkc
tirn7aQDNItvjDcSTWsgFNBx37TQAet0Xp2Lebc9tgWUyFimBca3PinZbWrr8e99
hw/nLtm9qs5I9nFM+RHdeqyhQ17FuH8hLEMXlre/3hkC2yGHEcgDQPWjTH+mSG2B
YMSYHxE=";

            ReadBase64EncryptedPkcs8(
                base64,
                "asdf",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA256,
                    3072),
                TestData.DiminishedDPParameters);
        }

        [Fact]
        public static void ReadEncryptedRsa1032()
        {
            // PBES2: PBKDF2 + aes192
            const string base64 = @"
MIICzzBJBgkqhkiG9w0BBQ0wPDAbBgkqhkiG9w0BBQwwDgQINYt7wOn+km4CAggA
MB0GCWCGSAFlAwQBFgQQcR2qtVNKVc4VgYqcPMBlwASCAoBEX5M21ImLfqUlgTEC
oWc+QBANsfVV8xiWwXBLsVD7jZmoxY3LdpstO5POibHtVb2/6WtzbTitXO4QPRAu
h8NM2tXKO4j0vnnuw5/he78Yn3gQFm2IIZG56a6rKFVF5bGL2hP3yMiJHfKztPzM
bpwvTr8hhv/rsHCb1P93r57yAsSaJLmFfdhRMjghvPBMF8SvwLwkVaZf0qHDIpZS
BwKpBH1GmROzxUtajSAvCRPRQvjEWJqCEPRPNA+yfUGBOvicpe0kCivdTPvMPoC4
FtQp3UIg+RW295vSXy2qCZrRXxh8Svx+fYFqN1OZJcMtsbfFAv9QRvULvwMNsOUL
Wk5Mp2dZrvjH9UV+wtko0MmEnkBVYmoittU5r8d+flvcbzW+UVKqZzCgM0UMEwyT
M1gPgdGPGvRQDF31qfvw7OGYd6bRiJCABvvF9KOx2EUgNfBip/gNlKZA/aauDlpO
IKTHWXSq3SM1JhoK67ll+CCgCdnkWZ4E8Jggf56lpdA/95EoaQ4R7fgM/kHZlYrT
lkwaaY40AOzSGAqjVXOzU/xiLWijRv/oVkbgI9htXVkZMU3ZJm5+0fA3DjXwtSEV
1YiN3Gl1JTPu6D5lu/A+AVb81F8wiEzFB3yiksGJOcFnaBoBTwMnov17br1r3C+C
DUBFBkmtANNbN9l7pqcYxvQ5LcFhAn1Ndg+QoSNa9WSntHf+LPWC7meuRTFPp+la
spgL5k2ltwkIUc3+lmccmAbzTTwIyHuLflDDr8riC49NjPckoewehtqQtdKabbNG
T/fDiOpNfqEJV4QBWUd5k1Qd/qIDNmC7NsYerwJfwxZlOvGywAgO6gQEgwAsqY7a
rBZc";

            ReadBase64EncryptedPkcs8(
                base64,
                "easy",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA384,
                    10583),
                TestData.RSA1032Parameters);
        }

        [ConditionalFact]
        [OuterLoop("RSA 16384 takes considerable time.")]
        public static void ReadEncryptedRsa16384()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384");
            }

            // PBES2: PBKDF2 + des (single DES, not 3DES).
            const string base64 = @"
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";

            ReadBase64EncryptedPkcs8(
                base64,
                "qwerty",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA512,
                    short.MaxValue),
                TestData.RSA16384Params);
        }

        [ConditionalFact(typeof(RC2Factory), nameof(RC2Factory.IsSupported))]
        public static void ReadPbes2Rc2EncryptedDiminishedDP()
        {
            // PBES2: PBKDF2 + RC2-128
            const string base64 = @"
MIIBrjBIBgkqhkiG9w0BBQ0wOzAeBgkqhkiG9w0BBQwwEQQIKZEFT76zCFECAggA
AgEQMBkGCCqGSIb3DQMCMA0CAToECE1Yyzk6++IPBIIBYDDvaYLkET8eudcYLQMf
hw2nQEwDWDo4/E3wSzWJxsO6v4pdhyfCqVblHbxe/xVRKolJfj7DO6nzolcaA9/a
tCM9vvqyCaqoNvnOJ3HnCh5BMPrCsKxND2k8jzMhVrW7tRP31J5oj7TaeIJla+Ps
+AlBQZuy5ws4f2evIo1llOX2DAbJHm4SKO697Bj9kwT9K7K26ZxNCg/Tv5NHCGmN
4ykOIyhACyd27ICCR9mzvC48zJFAUdh2nZPeQRngSPW/bx83w+c7p3sBcAuKQfop
/aGr6Gf7jRtOF4dUhfLUmyS0XTqhz1lRwJDsBJ4s21VzrrwqL+p+mvy12t4yQeN6
L8jQJiKJgTAZa9e9YJ9YXEhTldvMY7WoCcaRbRvl3D+WvV1ibfnG5fl0BwFyTO6x
RdMKfFP3he4C+CFyGGslffbxCaJhKebeuOil5xxlvP8aBPVNDtQfSS1HXHd1/Ikq
1eo=";

            ReadBase64EncryptedPkcs8(
                base64,
                "rc2",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA256,
                    3),
                TestData.DiminishedDPParameters);
        }

        [ConditionalFact(typeof(RC2Factory), nameof(RC2Factory.IsSupported))]
        public static void ReadPbes2Rc2EncryptedDiminishedDP_PasswordBytes()
        {
            // PBES2: PBKDF2 + RC2-128
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test key.")]
            const string base64 = @"
MIIBrjBIBgkqhkiG9w0BBQ0wOzAeBgkqhkiG9w0BBQwwEQQIKZEFT76zCFECAggA
AgEQMBkGCCqGSIb3DQMCMA0CAToECE1Yyzk6++IPBIIBYDDvaYLkET8eudcYLQMf
hw2nQEwDWDo4/E3wSzWJxsO6v4pdhyfCqVblHbxe/xVRKolJfj7DO6nzolcaA9/a
tCM9vvqyCaqoNvnOJ3HnCh5BMPrCsKxND2k8jzMhVrW7tRP31J5oj7TaeIJla+Ps
+AlBQZuy5ws4f2evIo1llOX2DAbJHm4SKO697Bj9kwT9K7K26ZxNCg/Tv5NHCGmN
4ykOIyhACyd27ICCR9mzvC48zJFAUdh2nZPeQRngSPW/bx83w+c7p3sBcAuKQfop
/aGr6Gf7jRtOF4dUhfLUmyS0XTqhz1lRwJDsBJ4s21VzrrwqL+p+mvy12t4yQeN6
L8jQJiKJgTAZa9e9YJ9YXEhTldvMY7WoCcaRbRvl3D+WvV1ibfnG5fl0BwFyTO6x
RdMKfFP3he4C+CFyGGslffbxCaJhKebeuOil5xxlvP8aBPVNDtQfSS1HXHd1/Ikq
1eo=";

            ReadBase64EncryptedPkcs8(
                base64,
                "rc2"u8.ToArray(),
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA256,
                    3),
                TestData.DiminishedDPParameters);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
        public static void ReadEncryptedDiminishedDP_EmptyPassword()
        {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test key.")]
            const string base64 = @"
MIIBgTAbBgkqhkiG9w0BBQMwDgQIJtjMez/9Gg4CAggABIIBYElq9UOOphEPU3b7
G/mV8M1uEdjigidMPih3b9IIJhrjMAEix2IjS+brFL7KRQgucpZZoaFU1utvkUHg
uKhKxjHV7uczG1NjRHmDdwpCadYx4dNaQpgGGAisFiED5hx8W8d9mAL1Kkaedhyb
qfTRO8LqB1SGl52mxIhZ4g5mCQH8uSOZc+k4jBhWBaSk0RaAHtIiKH6VmAiZ2r6C
7L3UTU1bhxlOMt0tABC+b4FMhEJVzl97na2KfpazpNVA4dr973rjqBid9QzXTRl6
Avdd74QjVeZug3ArZqS/3o8E0e2F2JKGty+A4dqFYxHIHvL/2N694HerYHiOJVan
nLHGUzIOxZNTB0TtcWJXP2xUBdHgdBP9/bL0qADYUhYN4e/7TQjuFpzqps3eQiVd
Dmw2pL/LzHORugcg9BxRkur91lenPNcLAvnke76tMGvSGkA82I9NpBDcGRK4cPie
5SeeJT4=";

            ReadBase64EncryptedPkcs8(
                base64,
                "",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA256,
                    3),
                TestData.DiminishedDPParameters);
        }

        [Fact]
        public static void ReadEncryptedDiminishedDP_EmptyPasswordBytes()
        {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test key.")]
            const string base64 = @"
MIIBgTAbBgkqhkiG9w0BBQMwDgQIJtjMez/9Gg4CAggABIIBYElq9UOOphEPU3b7
G/mV8M1uEdjigidMPih3b9IIJhrjMAEix2IjS+brFL7KRQgucpZZoaFU1utvkUHg
uKhKxjHV7uczG1NjRHmDdwpCadYx4dNaQpgGGAisFiED5hx8W8d9mAL1Kkaedhyb
qfTRO8LqB1SGl52mxIhZ4g5mCQH8uSOZc+k4jBhWBaSk0RaAHtIiKH6VmAiZ2r6C
7L3UTU1bhxlOMt0tABC+b4FMhEJVzl97na2KfpazpNVA4dr973rjqBid9QzXTRl6
Avdd74QjVeZug3ArZqS/3o8E0e2F2JKGty+A4dqFYxHIHvL/2N694HerYHiOJVan
nLHGUzIOxZNTB0TtcWJXP2xUBdHgdBP9/bL0qADYUhYN4e/7TQjuFpzqps3eQiVd
Dmw2pL/LzHORugcg9BxRkur91lenPNcLAvnke76tMGvSGkA82I9NpBDcGRK4cPie
5SeeJT4=";

            ReadBase64EncryptedPkcs8(
                base64,
                Array.Empty<byte>(),
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA256,
                    3),
                TestData.DiminishedDPParameters);
        }

        [ConditionalFact(nameof(Supports384BitPrivateKeyAndRC2))]
        public static void ReadPbes1Rc2EncryptedRsa384()
        {
            // PbeWithSha1AndRC2CBC
            const string base64 = @"
MIIBMTAbBgkqhkiG9w0BBQswDgQIboOZHKKNEM8CAggABIIBEKOc+r+d5gI+TK7V
+cbYrqNfu7bJYI5CCdiQf87vslKQTZGqnmpGqh4NETq/oWXckWwN9I9PGyK2oCNt
w3OUZQsFgcUXeEICCTf503KGifUGS1cG9TZ31iIF2f8xC34C/k2edICwqd5/wDAH
uDdoj7ZESHfEpj9AF3mKPyxWWVGCNTYKXQZbPZMDCOWgfBSeNI7xQw6Cj0seR6C8
hVbGcdL1z/MmM2dw3kIafjpwg3RcrL2qL803aKKl/Ypq6oxR8aCISTqThB46YByD
Ac4+NzbL4Z4jfBWzxzXcua6ujp0jS+/5GxC57q1w+vtgaiJPmrtdierkJWyS9O5e
pWre7nAO4O6sP1JzXvVmwrS5C/hw";

            ReadBase64EncryptedPkcs8(
                base64,
                "pbes1rc2",
                new PbeParameters(
                    PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                    HashAlgorithmName.SHA1,
                    2050),
                TestData.RSA384Parameters);
        }

        [Fact]
        public static void NoFuzzyRSAPublicKey()
        {
            using (RSA key = RSAFactory.Create())
            {
                int bytesRead = -1;
                byte[] rsaPriv = key.ExportRSAPrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPublicKey(rsaPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPublicKey(spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPublicKey(pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = rsaPriv.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPublicKey(encryptedPkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public static void NoFuzzySubjectPublicKeyInfo()
        {
            using (RSA key = RSAFactory.Create())
            {
                int bytesRead = -1;
                byte[] rsaPriv = key.ExportRSAPrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(rsaPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] rsaPub = key.ExportRSAPrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(rsaPub, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportSubjectPublicKeyInfo(pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = rsaPriv.AsSpan(0, 15);

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
        public static void NoFuzzyRSAPrivateKey()
        {
            using (RSA key = RSAFactory.Create())
            {
                int bytesRead = -1;
                byte[] rsaPub = key.ExportRSAPublicKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPrivateKey(rsaPub, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPrivateKey(spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPrivateKey(pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);

                ReadOnlySpan<byte> passwordBytes = spki.AsSpan(0, 15);

                byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA512,
                        123));

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportRSAPrivateKey(encryptedPkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public static void NoFuzzyPkcs8()
        {
            using (RSA key = RSAFactory.Create())
            {
                int bytesRead = -1;

                byte[] rsaPub = key.ExportRSAPublicKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(rsaPub, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] rsaPriv = key.ExportRSAPrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportPkcs8PrivateKey(rsaPriv, out bytesRead));

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
        public static void NoFuzzyEncryptedPkcs8()
        {
            using (RSA key = RSAFactory.Create())
            {
                int bytesRead = -1;
                byte[] empty = Array.Empty<byte>();
                byte[] rsaPub = key.ExportRSAPublicKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, rsaPub, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] spki = key.ExportSubjectPublicKeyInfo();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, spki, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] rsaPriv = key.ExportRSAPrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, rsaPriv, out bytesRead));

                Assert.Equal(-1, bytesRead);

                byte[] pkcs8 = key.ExportPkcs8PrivateKey();

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey(empty, pkcs8, out bytesRead));

                Assert.Equal(-1, bytesRead);
            }
        }

        [Fact]
        public static void NoPrivKeyFromPublicOnly()
        {
            using (RSA key = RSAFactory.Create())
            {
                RSAParameters srcParameters = TestData.RSA2048Params;
                RSAParameters rsaParameters = new RSAParameters
                {
                    Modulus = srcParameters.Modulus,
                    Exponent = srcParameters.Exponent,
                };

                key.ImportParameters(rsaParameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => key.ExportRSAPrivateKey());

                Assert.ThrowsAny<CryptographicException>(
                    () => key.TryExportRSAPrivateKey(Span<byte>.Empty, out _));

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
        public static void BadPbeParameters()
        {
            using (RSA key = RSAFactory.Create())
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
        public static void DecryptPkcs12WithBytes()
        {
            using (RSA key = RSAFactory.Create())
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
        public static void DecryptPkcs12PbeTooManyIterations()
        {
            // pbeWithSHAAnd3-KeyTripleDES-CBC with 600,001 iterations
            byte[] high3DesIterationKey = Convert.FromBase64String(@"
MIIE8zAlBgoqhkiG9w0BDAEDMBcEELqdfpwjkWlJZnr3xs2n+qACAwknwQSCBMgnZse/OHib1lMC
jBuUdjQCib5+HEBd2PwrjRw8k/i3kOsPeuDRsTMVvqumYpN6frYWLxRSbd3oe7n5D6bxpBrGuhfO
d/98Ustg1BDXPSdJzOyCXjS4nm+pr5ZETXxM8HUISvmpC62mrBJue3JwHVXbpXFepMcy9ywfSfjg
GhNzSYPQwHLFLVHVd3Dcb8mApgVf9aMJIjw9SeE9KQ6PeOk1LXQsoMFf3h/Sap/nuR4M9F/qa9u/
SEtjptJRlRDecXhQLS7su6cjQ/3Z5PQObORlqF2RASOaEYRyrjjvQjH0+ZAIkQ6zyv1PrPpbeT9I
U5Hzlu00hfNyAy3TfR50EqT2jP7D6ugFbvnHoDRx0e3STTgBrfFc1tbdLijnq/rxb4QNBIV70+Kw
QL2mc8CdM/lP4hT4f1vdtgLdRF2gmxndZolveNc5nAheUwqNa8I16OzZnny9090KztJxhGWd57H8
QCvz2mS6z7FANeyo9cTZilHwwErkLUgmoaQXnnrs46VwF8oeH9ZhLiO4W+OfNLm6QvB/e3/inUj/
FZFxnTYCvSeRBTD5YhME+SnyGPbJiHOPJuSIwWXyRpZeg6wmMWfduEeGuThNNSf4Fxm0baFrC6CZ
4+jT0CurP6L4NxLPBafWP7BcA2XCeVQGQd1GT9cX2I1NkRcU7Q2u7oxWKMJaj0OELC+lZTTv6y3Q
x3I96z0Eddm1qfKQr9HQq+i8LXMwSxqNH+LOtdOEA3kqYq4GU6MdPr8Nx1TaGsiwLcz14GqPl3UQ
5so2ZKHZZsa9NSer/fWVaOwmYc50JW3N4jJpDFRjKVFP5GSzLfdTwl9nduM4atu4D9KZthQCwNGC
nEZ1nwGJywmO/XvLWopOMj0cUx2GQYOUV2Hfu93+MriiKjTFEnMcT7B/BOFRpgz4jg4S+6DcHwJh
cpdIvViajQXM3WwpxnVzskAxYNTYGV0aI9ZjrGBD4vZXM1s9hTlAZc41EmTqZlJPNZ75OzKD9LJS
Iot/jiWjJ03+WT8VVqw/mfTSy2apCE3pGGKdde6wlcR/N6JI1+lQahC+0j9yvUCZyqhiiVpcwaOa
dsxxZMJ+DiilMuRXMJCxKvx5kBZTpjuKR/WnKzSE70wgB1ulJE0RkZTYjWSUSf54bHC2XRGZLtfU
sXDcycjA33H+b0cxe25OZNJUTOoFJqji9RqBvzHuAVMXJvZESORMKbl6q47DbWwFvivfweUJNVO+
3fzCcrCfAFLwI0j+KaoVr0Qrfedr7OhVEZ/v57aYidUSl2VIpLmpJTUKRnvZ0rlg/WBauwWH1X4M
u7AExd+aHO8iv0gj+rTg5co0EcOu9i8cbAowg1ZZ1m6zXvHD7fZgE9iamBmklVOQwQXOJO7BCecn
C0ezYU4Ii8uo0fXJOMWkIA7KLGaQxBrxygnFK3A0RKNos7OXiLdB8RGDqjUZyqLUb3z/YisQEX8f
R40NElq8tzOa/TudOpNAqC8mgS25uTm7ws472gUE0z9uvcq1/MgkqE++xgmLAxGUN/l7EHSAmYKG
i33DDR38LaRqG9ho3brf466OkNooBv4MpD5SA63yfooytxOgeuaqbuTzKP/OSRqJNab9wctA9nfJ
gms2YM+honjUS1sXk1zdm/8=");

            using (RSA key = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => key.ImportEncryptedPkcs8PrivateKey((ReadOnlySpan<char>)"test", high3DesIterationKey, out _));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
        public static void ReadWriteRsa2048EncryptedPkcs8_Pbes2HighIterations()
        {
            // pkcs5PBES2 hmacWithSHA256 aes128-CBC with 600,001 iterations
            ReadBase64EncryptedPkcs8(@"
MIIFNjBgBgkqhkiG9w0BBQ0wUzAyBgkqhkiG9w0BBQwwJQQQAx6OUUzmK+vwC7a+OwhXygIDCSfB
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBCoLaQQpG01FJnpQdIRq2ioBIIE0MPy/p2uwTnP
mScPxFbrpBiPQHz5PKRjKLCN5JLArsppSz+XSHxfItFUSfBKEE7dYJrEXARmhA8e2roCrZkQnb9o
TX551EivHZhqmd91p6WHYb3ZCDQ6la1JfiuV4aJiJOlG0bDAkzEaiM9m9LE3G0J3Z3ELX+mGEGKD
W+Bz1a9jr204IJQc2VihTFe5t3mujV3X9UFZH3kqOCEreVVmmD64Ea2uDJ6dk3VgGhQ9QLjTDjc5
OSjdfuyuA4gliK/7ibFubtmnVTDSbAm5Excy9tvJqVhejv/wRhfeYZ2rpv3tTLP58WKt25GEmyyV
j86v3LB7DFnnBy4NkLCE55oVe64v/8StyEjEkMxz7Owz/ZW65EccPdk7JHZeBXPXNUdLxk1x40vW
nSRzqhLckfggDwtTCk0kwGUNGBMqr2ezXKrSeO3Qj05dAUvFt9PvbnrVtSnOjFPwx0exiDMUxOFE
Sz63M2tNNAJr43w60DXaIvMMoZ1kF1mwwL5GT+cs3Dn5KFHNLkZ5irIoHAkmKRHplI15Oqr+Ws2n
MUtV9BjDjtYHlwp1s1oFpHr9hu8HHv7QIEvyZbyMdKfPv9PEaNJR0SRhkyave+vl9zHCBhP1H2wU
4fESU1y+cJXZ5WUiJZ5C1wDYWyN5Q2z67Fd4OQXhRwHCeTCZPz3bykpadwlsihCNHrYybfLn61oz
2CswZjBeQuhDmUKWaj87fsNvSEHaEGTDnyLGEzgNb1OuIrqGiT0DvjfRsVED4ZL6jlDKgAaJe+Uq
EMt1UzKmAPeBA/IFOrNPw5B+rgKjVpsCdToX9I9b3cCvKhjxguuggpzkEOF4cdgR2MJk1mqdo7ck
2fI2A5kJx9SuEVL6eKqxyFkPdBMmljKyhomAjdBAIKQ4xt2ojEip7Q9qhmWK3U3meOiIc3qrsM/K
pGDiwH1CHc/0FbHAghiARawXAtzGGyfMXsZkUTAVnEqDfu5uw4pOlv+rm5GLWfLU8p73yc+kVfPR
MdWZI6Ftct0FP/KEibc6VSaENoVRAk+XApadkeFaEUQzFzoU7RhN9FONmnCL8Ui76Kfi1kLAxYJo
rXFQjatiLOAxV50uJSkE1ppmrU1YWHVyC3RkLLnQfq0Ypz9/x7W4PVDfCJQdnCCtOfHL5Qm8fL0l
uE22Y+gPDzoM1BEKvSgmZWTlZrovXwL4u70/nPeaB1HAVUEzsoEMOSSWDkHuhmgiZHUgQwkvzNjA
I3AJk0rl2tGBWdgsDtiiQamkXm0VwubTedIt2vj9h9kJYtUlvkCg7KfKxmTSJUynOyKfI1Mhx3vW
fvni4lYc7KLUEkcNMqNu5AliaPEJQksNNkRiyX7wK5TvA+v9mlqm6Gg7GuJ2oa1DiFf8MUB9CgiJ
pniangzMBkuSd+zvCAo9YAJoyrJoR2ibcWwo4wRSVwYsVF3bB4nB2w1GHQ6HAnPKHSjVDJZyoEh6
TvoFEWjb86tn2BR+qMKqXNBVSrgF2pa06BIs0daHKimYKaU0bnuzE/T+pckJfI0VvWB/Z8jvE1Xy
gpX/dwXfODsj4zcOw4gyP70lDxUWLEPtxhS5Ti0FEuge1XKn3+GOp3clVjGpXKpJTNLsPA/wlqlo
8/ojymwPxe641Pcfa4DsMgfleHnUOEbNZ4je",
                "test",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA256,
                    600_001),
                TestData.RSA2048Params);
        }

        private static void ReadBase64EncryptedPkcs8(
            string base64EncPkcs8,
            string password,
            PbeParameters pbeParameters,
            in RSAParameters expected)
        {
            ReadWriteKey(
                base64EncPkcs8,
                expected,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportEncryptedPkcs8PrivateKey(password, source, out read),
                rsa => rsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out written),
                isEncrypted: true);
        }

        private static void ReadBase64EncryptedPkcs8(
            string base64EncPkcs8,
            byte[] passwordBytes,
            PbeParameters pbeParameters,
            in RSAParameters expected)
        {
            ReadWriteKey(
                base64EncPkcs8,
                expected,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out read),
                rsa => rsa.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out written),
                isEncrypted: true);
        }

        private static void ReadWriteBase64PublicPkcs1(
            string base64PublicPkcs1,
            in RSAParameters expected)
        {
            RSAParameters expectedPublic = new RSAParameters
            {
                Modulus = expected.Modulus,
                Exponent = expected.Exponent,
            };

            ReadWriteKey(
                base64PublicPkcs1,
                expectedPublic,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportRSAPublicKey(source, out read),
                rsa => rsa.ExportRSAPublicKey(),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportRSAPublicKey(destination, out written));
        }

        private static void ReadWriteBase64SubjectPublicKeyInfo(
            string base64SubjectPublicKeyInfo,
            in RSAParameters expected)
        {
            RSAParameters expectedPublic = new RSAParameters
            {
                Modulus = expected.Modulus,
                Exponent = expected.Exponent,
            };

            ReadWriteKey(
                base64SubjectPublicKeyInfo,
                expectedPublic,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportSubjectPublicKeyInfo(source, out read),
                rsa => rsa.ExportSubjectPublicKeyInfo(),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportSubjectPublicKeyInfo(destination, out written));
        }

        private static void ReadWriteBase64PrivatePkcs1(
            string base64PrivatePkcs1,
            in RSAParameters expected)
        {
            ReadWriteKey(
                base64PrivatePkcs1,
                expected,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportRSAPrivateKey(source, out read),
                rsa => rsa.ExportRSAPrivateKey(),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportRSAPrivateKey(destination, out written));
        }

        private static void ReadWriteBase64Pkcs8(string base64Pkcs8, in RSAParameters expected)
        {
            ReadWriteKey(
                base64Pkcs8,
                expected,
                (RSA rsa, ReadOnlySpan<byte> source, out int read) =>
                    rsa.ImportPkcs8PrivateKey(source, out read),
                rsa => rsa.ExportPkcs8PrivateKey(),
                (RSA rsa, Span<byte> destination, out int written) =>
                    rsa.TryExportPkcs8PrivateKey(destination, out written));
        }

        private static void ReadWriteKey(
            string base64,
            in RSAParameters expected,
            ReadKeyAction readAction,
            Func<RSA, byte[]> writeArrayFunc,
            WriteKeyToSpanFunc writeSpanFunc,
            bool isEncrypted = false)
        {
            bool isPrivateKey = expected.D != null;

            byte[] derBytes = Convert.FromBase64String(base64);
            byte[] arrayExport;
            byte[] tooBig;
            const int OverAllocate = 30;
            const int WriteShift = 6;

            using (RSA rsa = RSAFactory.Create())
            {
                readAction(rsa, derBytes, out int bytesRead);
                Assert.Equal(derBytes.Length, bytesRead);

                arrayExport = writeArrayFunc(rsa);

                RSAParameters rsaParameters = rsa.ExportParameters(isPrivateKey);
                ImportExport.AssertKeyEquals(expected, rsaParameters);
            }

            // Public key formats are stable.
            // Private key formats are not, since CNG recomputes the D value
            // and then all of the CRT parameters.
            if (!isPrivateKey)
            {
                Assert.Equal(derBytes.Length, arrayExport.Length);
                Assert.Equal(derBytes.ByteArrayToHex(), arrayExport.ByteArrayToHex());
            }

            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => readAction(rsa, arrayExport.AsSpan(1), out _));

                Assert.ThrowsAny<CryptographicException>(
                    () => readAction(rsa, arrayExport.AsSpan(0, arrayExport.Length - 1), out _));

                readAction(rsa, arrayExport, out int bytesRead);
                Assert.Equal(arrayExport.Length, bytesRead);

                RSAParameters rsaParameters = rsa.ExportParameters(isPrivateKey);
                ImportExport.AssertKeyEquals(expected, rsaParameters);

                Assert.False(
                    writeSpanFunc(rsa, Span<byte>.Empty, out int bytesWritten),
                    "Write to empty span");

                Assert.Equal(0, bytesWritten);

                Assert.False(
                    writeSpanFunc(
                        rsa,
                        derBytes.AsSpan(0, Math.Min(derBytes.Length, arrayExport.Length) - 1),
                        out bytesWritten),
                    "Write to too-small span");

                Assert.Equal(0, bytesWritten);

                tooBig = new byte[arrayExport.Length + OverAllocate];
                tooBig.AsSpan().Fill(0xC4);

                Assert.True(writeSpanFunc(rsa, tooBig.AsSpan(WriteShift), out bytesWritten));
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

            using (RSA rsa = RSAFactory.Create())
            {
                readAction(rsa, tooBig.AsSpan(WriteShift), out int bytesRead);
                Assert.Equal(arrayExport.Length, bytesRead);

                arrayExport.AsSpan().Fill(0xCA);

                Assert.True(
                    writeSpanFunc(rsa, arrayExport, out int bytesWritten),
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

        private delegate void ReadKeyAction(RSA rsa, ReadOnlySpan<byte> source, out int bytesRead);
        private delegate bool WriteKeyToSpanFunc(RSA rsa, Span<byte> destination, out int bytesWritten);
    }
}
