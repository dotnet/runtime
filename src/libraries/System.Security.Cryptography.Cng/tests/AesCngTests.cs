// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class AesCngTests
    {
        private const int BlockSizeBytes = 16;

        private static readonly CngAlgorithm s_cngAlgorithm = new CngAlgorithm("AES");

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalTheory(nameof(SupportsPersistedSymmetricKeys))]
        // AES128-ECB-NoPadding 2 blocks.
        [InlineData(128, 2 * BlockSizeBytes, CipherMode.ECB, PaddingMode.None)]
        // AES128-ECB-Zeros 2 blocks.
        [InlineData(128, 2 * BlockSizeBytes, CipherMode.ECB, PaddingMode.Zeros)]
        // AES128-ECB-Zeros 1.5 blocks.
        [InlineData(128, BlockSizeBytes + BlockSizeBytes / 2, CipherMode.ECB, PaddingMode.Zeros)]
        // AES128-CBC-NoPadding at 2 blocks
        [InlineData(128, 2 * BlockSizeBytes, CipherMode.CBC, PaddingMode.None)]
        // AES256-CBC-Zeros at 1.5 blocks
        [InlineData(256, BlockSizeBytes + BlockSizeBytes / 2, CipherMode.CBC, PaddingMode.Zeros)]
        // AES192-CBC-PKCS7 at 1.5 blocks
        [InlineData(192, BlockSizeBytes + BlockSizeBytes / 2, CipherMode.CBC, PaddingMode.PKCS7)]
        // AES128-CFB8-NoPadding at 2 blocks
        [InlineData(128, 2 * BlockSizeBytes, CipherMode.CFB, PaddingMode.None, 8)]
        public static void VerifyPersistedKey(
            int keySize,
            int plainBytesCount,
            CipherMode cipherMode,
            PaddingMode paddingMode,
            int feedbackSizeInBits = 0)
        {
            // Windows 7 does not support CFB except in CFB8 mode.
            if (cipherMode == CipherMode.CFB && feedbackSizeInBits != 8 && PlatformDetection.IsWindows7)
            {
                return;
            }

            SymmetricCngTestHelpers.VerifyPersistedKey(
                s_cngAlgorithm,
                keySize,
                plainBytesCount,
                keyName => new AesCng(keyName),
                cngKey => new AesCng(cngKey),
                () => new AesCng(),
                cipherMode,
                paddingMode,
                feedbackSizeInBits);
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void GetKey_NonExportable()
        {
            SymmetricCngTestHelpers.GetKey_NonExportable(
                s_cngAlgorithm,
                keyName => new AesCng(keyName),
                cngKey => new AesCng(cngKey));
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void SetKey_DetachesFromPersistedKey()
        {
            SymmetricCngTestHelpers.SetKey_DetachesFromPersistedKey(
                s_cngAlgorithm,
                keyName => new AesCng(keyName),
                cngKey => new AesCng(cngKey));
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void LoadWrongKeyType_ByKeyName()
        {
            string keyName = Guid.NewGuid().ToString();
            CngKey cngKey = CngKey.Create(new CngAlgorithm("3DES"), keyName);

            try
            {
                Assert.Throws<CryptographicException>(() => new AesCng(keyName));
            }
            finally
            {
                cngKey.Delete();
            }
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void LoadWrongKeyType_ByCngKey()
        {
            string keyName = Guid.NewGuid().ToString();
            CngKey cngKey = CngKey.Create(new CngAlgorithm("3DES"), keyName);

            try
            {
                Assert.Throws<CryptographicException>(() => new AesCng(cngKey));
            }
            finally
            {
                cngKey.Delete();
            }
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys), nameof(IsAdministrator))]
        public static void VerifyMachineKey()
        {
            SymmetricCngTestHelpers.VerifyMachineKey(
                s_cngAlgorithm,
                8 * BlockSizeBytes,
                keyName => new AesCng(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.MachineKey),
                cngKey => new AesCng(cngKey),
                () => new AesCng());
        }

        [OuterLoop("Creates/Deletes a persisted key, limit exposure to key leaking")]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void VerifyUnsupportedFeedbackSizeForPersistedCfb()
        {
            SymmetricCngTestHelpers.VerifyCfbPersistedUnsupportedFeedbackSize(
                s_cngAlgorithm,
                cngKey => new AesCng(cngKey.KeyName),
                notSupportedFeedbackSizeInBits: 128);

            SymmetricCngTestHelpers.VerifyCfbPersistedUnsupportedFeedbackSize(
                s_cngAlgorithm,
                cngKey => new AesCng(cngKey),
                notSupportedFeedbackSizeInBits: 128);
        }

        [OuterLoop("Creates/Deletes a persisted key, limit exposure to key leaking")]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void VerifyRequiresAesCngKey()
        {
            SymmetricCngTestHelpers.VerifyMismatchAlgorithmFails(
                s_cngAlgorithm,
                cngKey => new TripleDESCng(cngKey.KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider));
        }

        [OuterLoop("Creates/Deletes a persisted key, limit exposure to key leaking")]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void VerifyCngKeyIndependentLifetime()
        {
            string keyName = Guid.NewGuid().ToString();
            CngKeyCreationParameters creationParameters = new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
            };

            CngKey cngKey = CngKey.Create(new CngAlgorithm("AES"), keyName, creationParameters);
            CngKey openedKey = CngKey.Open(keyName, creationParameters.Provider, CngKeyOpenOptions.Silent);

            try
            {
                SymmetricCngTestHelpers.VerifyPersistedKey(
                    cngKey,
                    plainBytesCount: 64,
                    cngKey => new AesCng(cngKey),
                    () => new AesCng(),
                    CipherMode.CBC,
                    PaddingMode.PKCS7,
                    feedbackSizeInBits: 0,
                    disposeKey: true);

                // VerifyPersistedKey should have disposed of the CngKey.
                Assert.Throws<ObjectDisposedException>(() => cngKey.KeySize);
            }
            finally
            {
                // Now we need to delete the persisted key.
                openedKey.Delete();
            }
        }

        public static bool SupportsPersistedSymmetricKeys
        {
            get { return SymmetricCngTestHelpers.SupportsPersistedSymmetricKeys; }
        }

        public static bool IsAdministrator
        {
            get { return SymmetricCngTestHelpers.IsAdministrator; }
        }
    }
}
