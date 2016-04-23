// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class TripleDESCngTests
    {
        private const int BlockSizeBytes = 8;

        private static readonly CngAlgorithm s_cngAlgorithm = new CngAlgorithm("3DES");

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalTheory(nameof(SupportsPersistedSymmetricKeys))]
        // 3DES192-ECB-NoPadding 2 blocks.
        [InlineData(2 * BlockSizeBytes, CipherMode.ECB, PaddingMode.None)]
        // 3DES192-CBC-NoPadding at 2 blocks
        [InlineData(2 * BlockSizeBytes, CipherMode.CBC, PaddingMode.None)]
        // 3DES192-CBC-Zeros at 1.5 blocks 
        [InlineData(BlockSizeBytes + BlockSizeBytes / 2, CipherMode.CBC, PaddingMode.Zeros)]
        // 3DES192-CBC-PKCS7 at 1.5 blocks 
        [InlineData(BlockSizeBytes + BlockSizeBytes / 2, CipherMode.CBC, PaddingMode.PKCS7)]
        public static void VerifyPersistedKey(
            int plainBytesCount,
            CipherMode cipherMode,
            PaddingMode paddingMode)
        {
            SymmetricCngTestHelpers.VerifyPersistedKey(
                s_cngAlgorithm,
                192,
                plainBytesCount,
                keyName => new TripleDESCng(keyName),
                () => new TripleDESCng(),
                cipherMode,
                paddingMode);
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void GetKey_NonExportable()
        {
            SymmetricCngTestHelpers.GetKey_NonExportable(
                s_cngAlgorithm,
                keyName => new TripleDESCng(keyName));
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void SetKey_DetachesFromPersistedKey()
        {
            SymmetricCngTestHelpers.SetKey_DetachesFromPersistedKey(
                s_cngAlgorithm,
                keyName => new TripleDESCng(keyName));
        }

        [OuterLoop(/* Creates/Deletes a persisted key, limit exposure to key leaking */)]
        [ConditionalFact(nameof(SupportsPersistedSymmetricKeys))]
        public static void LoadWrongKeyType()
        {
            string keyName = Guid.NewGuid().ToString();
            CngKey cngKey = CngKey.Create(new CngAlgorithm("AES"), keyName);

            try
            {
                Assert.Throws<CryptographicException>(() => new TripleDESCng(keyName));
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
                keyName => new TripleDESCng(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.MachineKey), 
                () => new TripleDESCng());
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
