// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    // See osslplugins/README.md for instructions on how to build and install the test engine and setup for TPM tests.
    public class OpenSslNamedKeysTests
    {
        private const string EnvVarPrefix = "DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_";
        private const string TestEngineEnabledEnvVarName = EnvVarPrefix + "ENABLE";
        private const string TestEngineEnsureFailingEnvVarName = EnvVarPrefix + "ENSURE_FAILING";
        private const string TpmTssEngineEcDsaKeyHandleEnvVarName = EnvVarPrefix + "TPM_ECDSA_KEY_HANDLE";

        private const string NonExistingEngineName = "dntestnonexisting";
        private const string NonExistingEngineKeyName = "nonexisting";

        private const string TestEngineName = "dntest";
        private const string TestEngineKeyId = "first";
        private const string TpmTssEngineName = "tpm2tss";

        public static string TpmTssEngineEcDsaKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmTssEngineEcDsaKeyHandleEnvVarName);
        public static bool ShouldRunEngineTests { get;  } = PlatformDetection.OpenSslPresentOnSystem && StringToBool(Environment.GetEnvironmentVariable(TestEngineEnabledEnvVarName));
        public static bool ShouldFailTests { get; } = StringToBool(Environment.GetEnvironmentVariable(TestEngineEnsureFailingEnvVarName));
        public static bool ShouldRunTpmTssTests => PlatformDetection.OpenSslPresentOnSystem &&  !string.IsNullOrEmpty(TpmTssEngineEcDsaKeyHandle);

        private static bool StringToBool(string? value)
            => "true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "1";

        // PKCS#1 format
        private static readonly byte[] s_rsaPrivateKey = (
            "3082025C02010002818100BF67168485215A6AB89BCAB9331F6F5F360F4300BE5CF282F77042957E" +
            "A202908B2279F34A426D62F59D6C1056E36DC9F6EEA9AEB1B31F8122F583EE9CAE2A86A47144905D" +
            "F05441B0A5F29E03C5AC1888D93744D89638D83AC37774B339E4AFB349C714B12238B0F81A71380F" +
            "051C585CB27434FA544BDAC679E1E16581D0E902030100010281810084ED8862F2BEAE37CE0C4CA7" +
            "808CC5615F7F0BEE99469E1A3CD4973991DFDC5E1C730E34DC0EF43F350B668096878EB92428AE69" +
            "A7FA19D82ABA4E2D4A5D5F243D4B7346734D705C4C494FE2B36E2E35C39EE08BFB1172F5AB084AF4" +
            "4BD4D03702D04E6469F026EF3749CBED3ECB310746CF49DA3C2785CC17D54215EF18F3ED024100D0" +
            "63F89E01EB681CEACB781FE807F87C702B522A76B7D0E06DA44BB7D6202D5E9F3E7BE5BCCC3B32B9" +
            "B293AB62F50A8417C2FA9D6A76E465AA962AB61A8A9A13024100EB218F00B7317CC625DF2DFB7181" +
            "1DC5DA91D9A2AD859282DCA6BA3B4C674897E9D03D9E5FD2A9FD4CE7D9A3E5B79E948429C21561E7" +
            "141D90BCA75733D2489302400D07D349FE10BC47E29EAA7A44460B51ACA9E8CF62F1078CA10E7EF5" +
            "95DC193A2B76FAC458D3E477BD88DF16FE6F18233E6120CEAB1398208B542C838A91542502407882" +
            "619D9746A8D191957A26B5FCDBFA8CD455BBF7BD4EE2FD1E02B2E3ACC7DAFC3DFB66D16BD22DFD9D" +
            "92C15ABA2A6FA9F111050E8175A0D58EAB219970BC3B02404DBF36E5DCBF027AD4ED572E6F5F8383" +
            "C08CD5838C0CAE16FA58EE5C5A388B287F9C58647D58609B03912A10D0C772A3259D39651CD1EEB3" +
            "A20C5F9AE58E18C0").HexToByteArray();

        // PKCS#1 format
        private static readonly byte[] s_rsaPubKey = (
            "30818902818100BF67168485215A6AB89BCAB9331F6F5F360F4300BE5CF282F77042957EA202908B" +
            "2279F34A426D62F59D6C1056E36DC9F6EEA9AEB1B31F8122F583EE9CAE2A86A47144905DF05441B0" +
            "A5F29E03C5AC1888D93744D89638D83AC37774B339E4AFB349C714B12238B0F81A71380F051C585C" +
            "B27434FA544BDAC679E1E16581D0E90203010001").HexToByteArray();

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslNotPresentOnSystem))]
        public static void NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, TestEngineKeyId));
            Assert.Throws<PlatformNotSupportedException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, TestEngineKeyId));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void NullArguments()
        {
            Assert.Throws<ArgumentNullException>("engineName", () => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(null, TestEngineKeyId));
            Assert.Throws<ArgumentNullException>("keyId", () => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, null));

            Assert.Throws<ArgumentNullException>("engineName", () => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(null, TestEngineKeyId));
            Assert.Throws<ArgumentNullException>("keyId", () => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void NonExistingEngine()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(NonExistingEngineName, TestEngineKeyId));
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(NonExistingEngineName, TestEngineKeyId));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void NonExistingKey()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, NonExistingEngineKeyName));
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, NonExistingEngineKeyName));
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_SanityTest()
        {
            Assert.False(ShouldFailTests, "This test is supposed to fail");
        }

        [ConditionalFact(nameof(ShouldRunTpmTssTests))]
        public static void Tpm_SanityTest()
        {
            Assert.False(ShouldFailTests, "This test is supposed to fail");
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_OpenExistingPrivateKey()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, TestEngineKeyId);
            using RSA priKey = new RSAOpenSsl(priKeyHandle);
            RSAParameters rsaParams = priKey.ExportParameters(includePrivateParameters: true);
            Assert.NotNull(rsaParams.D);
            Assert.Equal(s_rsaPubKey, priKey.ExportRSAPublicKey());
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_OpenExistingPublicKey()
        {
            using SafeEvpPKeyHandle pubKeyHandle = SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, TestEngineKeyId);
            using RSA pubKey = new RSAOpenSsl(pubKeyHandle);
            Assert.ThrowsAny<CryptographicException>(() => pubKey.ExportParameters(includePrivateParameters: true));
            RSAParameters rsaParams = pubKey.ExportParameters(includePrivateParameters: false);
            Assert.Null(rsaParams.D);
            Assert.Equal(s_rsaPubKey, pubKey.ExportRSAPublicKey());
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_UsePrivateKey()
        {
            using (SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, TestEngineKeyId))
            using (RSA rsaPri = new RSAOpenSsl(priKeyHandle))
            using (RSA rsaPub = RSA.Create())
            {
                rsaPub.ImportRSAPublicKey(s_rsaPubKey, out int bytesRead);
                Assert.Equal(s_rsaPubKey.Length, bytesRead);

                byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
                byte[] signature = rsaPri.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.True(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
                signature[0] ^= 1;
                Assert.False(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
                signature[0] ^= 1;

                byte[] encrypted = rsaPub.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                Assert.NotEqual(encrypted, data);

                byte[] decrypted = rsaPri.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
                Assert.Equal(data, decrypted);
            }
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_UsePublicKey()
        {
            using (SafeEvpPKeyHandle pubKeyHandle = SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, TestEngineKeyId))
            using (RSA rsaPub = new RSAOpenSsl(pubKeyHandle))
            using (RSA rsaPri = RSA.Create())
            {
                rsaPri.ImportRSAPrivateKey(s_rsaPrivateKey, out int bytesRead);
                Assert.Equal(s_rsaPrivateKey.Length, bytesRead);

                byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
                byte[] signature = rsaPri.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

                Assert.True(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
                signature[0] ^= 1;
                Assert.False(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
                signature[0] ^= 1;

                byte[] encrypted = rsaPub.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                Assert.NotEqual(encrypted, data);

                byte[] decrypted = rsaPri.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
                Assert.Equal(data, decrypted);
            }
        }

        [ConditionalFact(nameof(ShouldRunTpmTssTests))]
        public static void Engine_OpenExistingTPMPrivateKey()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TpmTssEngineName, TpmTssEngineEcDsaKeyHandle);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);
            using ECDsa ecdsaBad = ECDsa.Create();
            ecdsaBad.KeySize = ecdsaPri.KeySize;

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
            byte[] badSignature = ecdsaBad.SignData(data, HashAlgorithmName.SHA256);
            Assert.NotEqual(data, signature);
            Assert.True(ecdsaPri.VerifyData(data, signature, HashAlgorithmName.SHA256));
            Assert.False(ecdsaPri.VerifyData(data, badSignature, HashAlgorithmName.SHA256));
        }
    }
}
