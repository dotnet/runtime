// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public abstract class CngPkcs8Tests<T> where T : AsymmetricAlgorithm
    {
        protected abstract T CreateKey(out CngKey cngKey);
        protected abstract void VerifyMatch(T exported, T imported);

        [Fact]
        public void NoPlaintextExportFailsPkcs8()
        {
            using (T key = CreateKey(out CngKey cngKey))
            {
                SetExportPolicy(cngKey, CngExportPolicies.AllowExport);

                byte[] exported = key.ExportPkcs8PrivateKey();

                using (T imported = CreateKey(out _))
                {
                    imported.ImportPkcs8PrivateKey(exported, out int importRead);
                    Assert.Equal(exported.Length, importRead);
                    VerifyMatch(key, imported);
                }

                byte[] tryExported = new byte[exported.Length];

                int written;

                while (!key.TryExportPkcs8PrivateKey(tryExported, out written))
                {
                    Array.Resize(ref tryExported, checked(tryExported.Length * 2));
                }

                using (T imported = CreateKey(out _))
                {
                    imported.ImportPkcs8PrivateKey(tryExported.AsSpan(0, written), out int tryImportRead);
                    Assert.Equal(written, tryImportRead);
                    VerifyMatch(key, imported);
                }


            }
        }

        [Theory]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc)]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)]
        public void NoPlaintextExportAllowsEncryptedPkcs8(PbeEncryptionAlgorithm algorithm)
        {
            PbeParameters pbeParameters = new PbeParameters(
                algorithm,
                HashAlgorithmName.SHA1,
                2048);

            using (T key = CreateKey(out CngKey cngKey))
            {
                SetExportPolicy(cngKey, CngExportPolicies.AllowExport);

                byte[] data = key.ExportEncryptedPkcs8PrivateKey(
                    (ReadOnlySpan<char>)nameof(NoPlaintextExportAllowsEncryptedPkcs8),
                    pbeParameters);

                Assert.False(
                    key.TryExportEncryptedPkcs8PrivateKey(
                        (ReadOnlySpan<char>)nameof(NoPlaintextExportAllowsEncryptedPkcs8),
                        pbeParameters,
                        data.AsSpan(0, data.Length - 1),
                        out int bytesWritten));

                Assert.Equal(0, bytesWritten);

                Assert.True(
                    key.TryExportEncryptedPkcs8PrivateKey(
                        (ReadOnlySpan<char>)nameof(NoPlaintextExportAllowsEncryptedPkcs8),
                        pbeParameters,
                        data.AsSpan(),
                        out bytesWritten));

                Assert.Equal(data.Length, bytesWritten);

                using (T key2 = CreateKey(out _))
                {
                    key2.ImportEncryptedPkcs8PrivateKey(
                        (ReadOnlySpan<char>)nameof(NoPlaintextExportAllowsEncryptedPkcs8),
                        data,
                        out int bytesRead);

                    Assert.Equal(data.Length, bytesRead);

                    VerifyMatch(key, key2);
                }
            }
        }

        private static void SetExportPolicy(CngKey key, CngExportPolicies policy)
        {
            key.SetProperty(
                new CngProperty(
                    "Export Policy",
                    BitConverter.GetBytes((int)policy),
                    CngPropertyOptions.Persist));
        }
    }
}
