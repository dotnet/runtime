// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;
using TempFileHolder = System.Security.Cryptography.X509Certificates.Tests.TempFileHolder;

namespace System.Security.Cryptography.Tests
{
    // See osslplugins/README.md for instructions on how to build and install the test engine and setup for TPM tests.
    public class OpenSslNamedKeysTests
    {
        private const string EnvVarPrefix = "DOTNET_CRYPTOGRAPHY_TESTS_";

        private const string EngineEnvVarPrefix = EnvVarPrefix + "ENGINE_";
        private const string TestEngineEnabledEnvVarName = EngineEnvVarPrefix + "ENABLE";

        private const string TpmEnvVarPrefix = EnvVarPrefix + "TPM_";
        private const string TpmEcDsaKeyHandleEnvVarName = TpmEnvVarPrefix + "ECDSA_KEY_HANDLE";
        private const string TpmEcDhKeyHandleEnvVarName = TpmEnvVarPrefix + "ECDH_KEY_HANDLE";
        private const string TpmRsaSignKeyHandleEnvVarName = TpmEnvVarPrefix + "RSA_SIGN_KEY_HANDLE";
        private const string TpmRsaDecryptKeyHandleEnvVarName = TpmEnvVarPrefix + "RSA_DECRYPT_KEY_HANDLE";

        private const string NonExistingEngineName = "dntestnonexisting";
        private const string NonExistingEngineOrProviderKeyName = "nonexisting";

        private const string TestEngineName = "dntest";
        private const string TestEngineKeyId = "first";
        private const string TpmTssEngineName = "tpm2tss";

        private const string Tpm2ProviderName = "tpm2";

        private static string TpmEcDsaKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmEcDsaKeyHandleEnvVarName);
        private static string TpmEcDsaKeyHandleUri { get; } = GetHandleKeyUri(TpmEcDsaKeyHandle);

        private static string TpmEcDhKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmEcDhKeyHandleEnvVarName);
        private static string TpmEcDhKeyHandleUri { get; } = GetHandleKeyUri(TpmEcDhKeyHandle);

        private static string TpmRsaSignKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmRsaSignKeyHandleEnvVarName);
        private static string TpmRsaSignKeyHandleUri { get; } = GetHandleKeyUri(TpmRsaSignKeyHandle);

        private static string TpmRsaDecryptKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmRsaDecryptKeyHandleEnvVarName);
        private static string TpmRsaDecryptKeyHandleUri { get; } = GetHandleKeyUri(TpmRsaDecryptKeyHandle);

        public static bool ShouldRunEngineTests { get; } = PlatformDetection.OpenSslPresentOnSystem && StringToBool(Environment.GetEnvironmentVariable(TestEngineEnabledEnvVarName));

        public static bool ProvidersSupported { get; } = PlatformDetection.IsOpenSsl3;
        public static bool ProvidersNotSupported => !ProvidersSupported;
        public static bool ShouldRunProviderEcDsaTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmEcDsaKeyHandleUri);
        public static bool ShouldRunProviderEcDhTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmEcDhKeyHandleUri);
        public static bool ShouldRunProviderRsaSignTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmRsaSignKeyHandleUri);
        public static bool ShouldRunProviderRsaDecryptTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmRsaDecryptKeyHandleUri);
        public static bool ShouldRunAnyProviderTests => ShouldRunProviderEcDsaTests || ShouldRunProviderEcDhTests || ShouldRunProviderRsaSignTests || ShouldRunProviderRsaDecryptTests;

        public static bool ShouldRunTpmTssTests => ShouldRunEngineTests && !string.IsNullOrEmpty(TpmEcDsaKeyHandle);

        private static readonly string AnyProviderKeyUri = TpmEcDsaKeyHandleUri ?? TpmEcDhKeyHandleUri ?? TpmRsaSignKeyHandleUri ?? TpmRsaDecryptKeyHandleUri ?? "test";

        private static bool StringToBool(string? value)
            => "true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "1";

        private static string GetHandleKeyUri(string handle)
            => handle != null ? $"handle:{handle}" : null;

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
        public static void EngineNotSupported_ThrowsPlatformNotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, TestEngineKeyId));
            Assert.Throws<PlatformNotSupportedException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, TestEngineKeyId));
        }

        [ConditionalFact(nameof(ProvidersNotSupported))]
        public static void ProvidersNotSupported_ThrowsPlatformNotSupported()
        {
            try
            {
                using SafeEvpPKeyHandle key = SafeEvpPKeyHandle.OpenKeyFromProvider("default", NonExistingEngineOrProviderKeyName);
                Assert.Fail("We expected an exception to be thrown");
            }
            catch (PlatformNotSupportedException)
            {
                // Expected
            }
            catch (CryptographicException) when (PlatformDetection.IsApplePlatform)
            {
                // Our tests detect providers using PlatformDetection.IsOpenSsl3 which is always false for Apple platforms.
                // Product on the other hand does feature detection and that might end up working
                // in which case we should still throw any CryptographicException because the keyUri does not exist.
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void NullArguments()
        {
            Assert.Throws<ArgumentNullException>("engineName", () => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(null, TestEngineKeyId));
            Assert.Throws<ArgumentNullException>("keyId", () => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, null));

            Assert.Throws<ArgumentNullException>("engineName", () => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(null, TestEngineKeyId));
            Assert.Throws<ArgumentNullException>("keyId", () => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, null));

            Assert.Throws<ArgumentNullException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider(null, AnyProviderKeyUri));
            Assert.Throws<ArgumentNullException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void EmptyNameThroughNullCharacter()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine("\0", "foo"));
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine("\0", "foo"));

            if (ProvidersSupported)
            {
                Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider("\0", "foo"));
            }
        }

        [ConditionalFact(nameof(ProvidersSupported))]
        public static void EmptyUriThroughNullCharacter()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider("default", "\0"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.OpenSslPresentOnSystem))]
        public static void Engine_NonExisting()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(NonExistingEngineOrProviderKeyName, TestEngineKeyId));
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(NonExistingEngineOrProviderKeyName, TestEngineKeyId));
        }

        [ConditionalFact(nameof(ProvidersSupported))]
        public static void Provider_NonExisting()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider(NonExistingEngineOrProviderKeyName, AnyProviderKeyUri));
        }

        [ConditionalFact(nameof(ShouldRunEngineTests))]
        public static void Engine_NonExistingKey()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TestEngineName, NonExistingEngineOrProviderKeyName));
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenPublicKeyFromEngine(TestEngineName, NonExistingEngineOrProviderKeyName));
        }

        [ConditionalFact(nameof(ShouldRunAnyProviderTests))]
        public static void Provider_NonExistingKey()
        {
            Assert.ThrowsAny<CryptographicException>(() => SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, NonExistingEngineOrProviderKeyName));
        }

        [ConditionalFact(nameof(ProvidersSupported))]
        public static void Provider_Default_RSASignAndDecrypt()
        {
            using RSA originalKey = RSA.Create();
            string pem = originalKey.ExportRSAPrivateKeyPem();

            using TempFileHolder pemFile = new TempFileHolder(Encoding.UTF8.GetBytes(pem));
            Uri fileUri = new Uri(pemFile.FilePath);
            string keyUri = fileUri.AbsoluteUri;
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider("default", keyUri);
            using RSA rsaPri = new RSAOpenSsl(priKeyHandle);
            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = rsaPri.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            Assert.True(originalKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "signature does not verify with the right key");

            byte[] encrypted = originalKey.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            byte[] decrypted = rsaPri.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
            Assert.Equal(data, decrypted);
        }

        [ConditionalFact(nameof(ProvidersSupported))]
        public static void Provider_Default_ECDsaSignAndVerify()
        {
            using ECDsa originalKey = ECDsa.Create();
            string pem = originalKey.ExportECPrivateKeyPem();

            using TempFileHolder pemFile = new TempFileHolder(Encoding.UTF8.GetBytes(pem));
            Uri fileUri = new Uri(pemFile.FilePath);
            string keyUri = fileUri.AbsoluteUri;
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider("default", keyUri);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);
            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
            Assert.True(originalKey.VerifyData(data, signature, HashAlgorithmName.SHA256), "signature does not verify with the right key");
        }

        [ConditionalFact(nameof(ProvidersSupported))]
        public static void Provider_Default_ECDHKeyExchange()
        {
            using ECDiffieHellman originalAliceKey = ECDiffieHellman.Create();
            string pem = originalAliceKey.ExportECPrivateKeyPem();

            using TempFileHolder pemFile = new TempFileHolder(Encoding.UTF8.GetBytes(pem));
            Uri fileUri = new Uri(pemFile.FilePath);
            string keyUri = fileUri.AbsoluteUri;
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider("default", keyUri);
            using ECDiffieHellman alicePri = new ECDiffieHellmanOpenSsl(priKeyHandle);
            using ECDiffieHellman bobPri = ECDiffieHellman.Create(alicePri.ExportParameters(false).Curve);

            byte[] sharedSecret1 = originalAliceKey.DeriveRawSecretAgreement(bobPri.PublicKey);
            byte[] sharedSecret2 = alicePri.DeriveRawSecretAgreement(bobPri.PublicKey);
            byte[] sharedSecret3 = bobPri.DeriveRawSecretAgreement(alicePri.PublicKey);

            Assert.Equal(sharedSecret1, sharedSecret2);
            Assert.Equal(sharedSecret1, sharedSecret3);
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
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenPrivateKeyFromEngine(TpmTssEngineName, TpmEcDsaKeyHandle);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);
            using ECDsa ecdsaBad = ECDsa.Create();
            ecdsaBad.KeySize = ecdsaPri.KeySize;

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
            byte[] badSignature = ecdsaBad.SignData(data, HashAlgorithmName.SHA256);
            Assert.Equal(signature.Length, badSignature.Length);
            Assert.NotEqual(data, signature);
            Assert.True(ecdsaPri.VerifyData(data, signature, HashAlgorithmName.SHA256));
            Assert.False(ecdsaPri.VerifyData(data, badSignature, HashAlgorithmName.SHA256));
        }

        [ConditionalFact(nameof(ShouldRunProviderEcDsaTests))]
        public static void Provider_TPM2ECDSA()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmEcDsaKeyHandleUri);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };

            byte[] ecdsaPubBytes = ecdsaPri.ExportSubjectPublicKeyInfo();
            ECDsa ecdsaPub = ECDsa.Create();
            ecdsaPub.ImportSubjectPublicKeyInfo(ecdsaPubBytes, out int bytesRead);
            Assert.Equal(ecdsaPubBytes.Length, bytesRead);

            using ECDsa ecdsaBad = ECDsa.Create();
            ecdsaBad.KeySize = ecdsaPri.KeySize;

            // Verify can sign/verify multiple times
            for (int i = 0; i < 10; i++)
            {
                data[0] = (byte)i;
                byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
                byte[] badSignature = ecdsaBad.SignData(data, HashAlgorithmName.SHA256);
                Assert.NotEqual(data, signature);
                Assert.NotEqual(data, badSignature);
                Assert.NotEqual(badSignature, signature);
                Assert.True(ecdsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256));
                Assert.False(ecdsaPub.VerifyData(data, badSignature, HashAlgorithmName.SHA256));
                Assert.False(ecdsaBad.VerifyData(data, signature, HashAlgorithmName.SHA256));

                // TPM key is intended for sign/decrypt only, we could theoretically make verify work without needing to export/import by forcing 'default' provider
                // for this operation but it's most likely misusage on user part and tpm2 provider intentionally didn't allow it so we will follow this logic.
                Assert.ThrowsAny<CryptographicException>(() => ecdsaPri.VerifyData(data, signature, HashAlgorithmName.SHA256));
            }

            // It's TPM so it should not be possible to export parameters
            Assert.ThrowsAny<CryptographicException>(() => ecdsaPri.ExportParameters(includePrivateParameters: true));
        }

        [ConditionalFact(nameof(ShouldRunProviderEcDsaTests))]
        public static void Provider_TPM2ECDSA_ExportParameters()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmEcDsaKeyHandleUri);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);

            ECDsa ecdsaPub = ECDsa.Create();
            ecdsaPub.ImportParameters(ecdsaPri.ExportParameters(false));
            Assert.ThrowsAny<CryptographicException>(() => ecdsaPri.ExportParameters(true));

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
            Assert.True(ecdsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256));
        }

        [ConditionalFact(nameof(ShouldRunProviderEcDsaTests))]
        public static void Provider_TPM2ECDSA_ExportExplicitParameters()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmEcDsaKeyHandleUri);
            using ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle);

            ECDsa ecdsaPub = ECDsa.Create();
            ecdsaPub.ImportParameters(ecdsaPri.ExportExplicitParameters(false));
            Assert.ThrowsAny<CryptographicException>(() => ecdsaPri.ExportExplicitParameters(true));

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
            Assert.True(ecdsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256));
        }

        [ConditionalFact(nameof(ShouldRunProviderEcDhTests))]
        public static void Provider_TPM2ECDH()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmEcDhKeyHandleUri);
            using ECDiffieHellman alicePri = new ECDiffieHellmanOpenSsl(priKeyHandle);
            using ECDiffieHellman alicePub = ECDiffieHellman.Create();

            ECParameters aliceECParams = alicePri.ExportParameters(includePrivateParameters: false);
            alicePub.ImportParameters(aliceECParams);

            using ECDiffieHellman bobPri = ECDiffieHellman.Create(aliceECParams.Curve);

            byte[] sharedKeyFromAlice;
            using (ECDiffieHellmanPublicKey bobPublic = bobPri.PublicKey)
            {
                sharedKeyFromAlice = alicePri.DeriveRawSecretAgreement(bobPublic);

                Assert.NotEmpty(sharedKeyFromAlice);

                byte firstByte = sharedKeyFromAlice[0];
                bool allSame = sharedKeyFromAlice.All((x) => x == firstByte);
                Assert.False(allSame, "all bytes of shared key are the same");
            }

            using (ECDiffieHellmanPublicKey alicePublic = alicePub.PublicKey)
            {
                byte[] sharedKeyFromBob = bobPri.DeriveRawSecretAgreement(alicePublic);
                Assert.Equal(sharedKeyFromAlice, sharedKeyFromBob);
            }

            // Now we derive it again but using directly PublicKey on the instance directly wrapping our TPM handle
            using (ECDiffieHellmanPublicKey alicePublic = alicePri.PublicKey)
            {
                Assert.Equal(sharedKeyFromAlice, bobPri.DeriveRawSecretAgreement(alicePublic));
            }
        }

        [ConditionalFact(nameof(ShouldRunProviderRsaSignTests))]
        [ActiveIssue("https://github.com/tpm2-software/tpm2-openssl/issues/115")]
        // or a workaround API proposal
        [ActiveIssue("https://github.com/dotnet/runtime/issues/104080")]
        public static void Provider_TPM2SignRsa()
        {
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmRsaSignKeyHandleUri);
            using RSA rsaPri = new RSAOpenSsl(priKeyHandle);
            byte[] rsaPubBytes = rsaPri.ExportSubjectPublicKeyInfo();
            RSA rsaPub = RSA.Create();
            rsaPub.ImportSubjectPublicKeyInfo(rsaPubBytes, out int bytesRead);
            Assert.Equal(rsaPubBytes.Length, bytesRead);

            using RSA rsaBad = RSA.Create();
            rsaBad.KeySize = rsaPri.KeySize;

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] badSignature = rsaBad.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            // can use same key more than once
            for (int i = 0; i < 10; i++)
            {
                data[0] = (byte)i;
                byte[] signature = rsaPri.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                Assert.True(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "signature does not verify with the right key");
                Assert.False(rsaPub.VerifyData(data, badSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "signature should not verify with the wrong key");

                signature[12] ^= 1;
                Assert.False(rsaPub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "tampered signature should not verify");

                // TPM key is intended for sign only, we could theoretically make verify work without needing to export/import by forcing 'default' provider
                // for this operation it's most likely misusage on user part and tpm2 provider intentionally didn't allow it so we will follow this logic.
                Assert.ThrowsAny<CryptographicException>(() => rsaPri.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }
        }

        [ConditionalTheory(nameof(ShouldRunProviderRsaDecryptTests))]
        [InlineData(RSAEncryptionPaddingMode.Pkcs1)]
        [InlineData(RSAEncryptionPaddingMode.Oaep)]
        public static void Provider_TPM2DecryptRsa(RSAEncryptionPaddingMode mode)
        {
            RSAEncryptionPadding padding;

            switch (mode)
            {
                case RSAEncryptionPaddingMode.Pkcs1:
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;
                case RSAEncryptionPaddingMode.Oaep:
                    padding = RSAEncryptionPadding.OaepSHA256;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmRsaDecryptKeyHandleUri);
            using RSA rsaPri = new RSAOpenSsl(priKeyHandle);
            byte[] rsaPubBytes = rsaPri.ExportSubjectPublicKeyInfo();
            RSA rsaPub = RSA.Create();
            rsaPub.ImportSubjectPublicKeyInfo(rsaPubBytes, out int bytesRead);
            Assert.Equal(rsaPubBytes.Length, bytesRead);

            using RSA rsaBad = RSA.Create();
            rsaBad.KeySize = rsaPri.KeySize;

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] encryptedWithDifferentKey = rsaBad.Encrypt(data, padding);
            Assert.ThrowsAny<CryptographicException>(() => rsaPri.Decrypt(encryptedWithDifferentKey, padding));

            // TPM private key is intended only for decrypt
            Assert.ThrowsAny<CryptographicException>(() => rsaPri.Encrypt(data, padding));

            // can use same key more than once
            for (int i = 0; i < 10; i++)
            {
                data[0] = (byte)i;
                byte[] encrypted = rsaPub.Encrypt(data, padding);
                Assert.NotEqual(encrypted, data);

                try
                {
                    Assert.Equal(data, rsaPri.Decrypt(encrypted, padding));
                }
                catch (CryptographicException) when (mode == RSAEncryptionPaddingMode.Oaep)
                {
                    // TPM2 OAEP support was added in the second half of 2023 therefore we allow for OAEP to throw for the time being
                    // See: https://github.com/tpm2-software/tpm2-openssl/issues/89
                }
            }
        }

        [ConditionalFact(nameof(ShouldRunProviderRsaDecryptTests))]
        public static void Provider_TPM2DecryptRsa_ExportParameters()
        {
            // TPM2 OAEP support was added in the second half of 2023 therefore we only test Pkcs1 padding
            // See: https://github.com/tpm2-software/tpm2-openssl/issues/89
            RSAEncryptionPadding padding = RSAEncryptionPadding.Pkcs1;
            using SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider(Tpm2ProviderName, TpmRsaDecryptKeyHandleUri);
            using RSA rsaPri = new RSAOpenSsl(priKeyHandle);

            RSA rsaPub = RSA.Create();
            rsaPub.ImportParameters(rsaPri.ExportParameters(false));

            Assert.ThrowsAny<CryptographicException>(() => rsaPri.ExportParameters(true));

            byte[] data = new byte[] { 1, 2, 3, 1, 1, 2, 3 };
            byte[] encrypted = rsaPub.Encrypt(data, padding);
            Assert.NotEqual(encrypted, data);
            Assert.Equal(data, rsaPri.Decrypt(encrypted, padding));
        }
    }
}
