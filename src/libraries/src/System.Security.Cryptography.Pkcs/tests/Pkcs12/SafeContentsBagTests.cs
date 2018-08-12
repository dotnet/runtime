// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class SafeContentsBagTests
    {
        private static readonly Oid s_zeroOid = new Oid("0.0", "0.0");
        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

        private static readonly PbeParameters s_pbkdf2Parameters = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA384,
            0x1001);

        [Fact]
        public static void CreatingBagSerializesInput()
        {
            Pkcs12SafeContents container = new Pkcs12SafeContents();

            Pkcs12SafeContents builtContents = new Pkcs12SafeContents();
            builtContents.AddSecret(s_zeroOid, s_derNull);

            builtContents.AddSecret(s_zeroOid, new byte[] { 4, 1, 2 }).Attributes.Add(
                new Pkcs9LocalKeyId(s_derNull.Span));

            builtContents.AddSecret(s_zeroOid, new byte[] { 4, 1, 3 });

            Pkcs12SafeContentsBag safeContentsBag = container.AddNestedContents(builtContents);

            builtContents.AddSecret(s_zeroOid, new byte[] { 4, 1, 4 });

            Pkcs12SafeContents readContents = safeContentsBag.SafeContents;
            Assert.NotSame(readContents, builtContents);

            Assert.Equal(
                Pkcs12ConfidentialityMode.None,
                readContents.ConfidentialityMode);

            Assert.True(readContents.IsReadOnly);
            
            List<Pkcs12SafeBag> bags1 = builtContents.GetBags().ToList();
            List<Pkcs12SafeBag> bags2 = readContents.GetBags().ToList();

            Assert.Equal(4, bags1.Count);
            Assert.Equal(3, bags2.Count);

            for (int i = 0; i < bags2.Count; i++)
            {
                byte[] encoded1 = bags1[i].Encode();
                byte[] encoded2 = bags1[i].Encode();

                Assert.True(encoded1.AsSpan().SequenceEqual(encoded2), $"Bag {i} encodes the same");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void ReadSerializedData(bool encryptSafe)
        {
            Pkcs12SafeContents container = new Pkcs12SafeContents();

            Pkcs12SafeContents builtContents = new Pkcs12SafeContents();
            builtContents.AddSecret(s_zeroOid, s_derNull);

            builtContents.AddSecret(s_zeroOid, new byte[] { 4, 1, 2 }).Attributes.Add(
                new Pkcs9LocalKeyId(s_derNull.Span));

            builtContents.AddSecret(s_zeroOid, new byte[] { 4, 1, 3 });
            container.AddNestedContents(builtContents);

            Pkcs12Builder builder = new Pkcs12Builder();

            if (encryptSafe)
            {
                builder.AddSafeContentsEncrypted(container, s_derNull.Span, s_pbkdf2Parameters);
            }
            else
            {
                builder.AddSafeContentsUnencrypted(container);
            }

            builder.SealWithoutIntegrity();
            byte[] encoded = builder.Encode();

            Pkcs12Info info = Pkcs12Info.Decode(encoded, out _, skipCopy: true);
            Pkcs12SafeContents onlySafe = info.AuthenticatedSafe.Single();

            if (encryptSafe)
            {
                onlySafe.Decrypt(s_derNull.Span);
            }

            Pkcs12SafeBag onlyBag = onlySafe.GetBags().Single();
            Pkcs12SafeContentsBag safeContentsBag = Assert.IsType<Pkcs12SafeContentsBag>(onlyBag);
            Pkcs12SafeContents readContents = safeContentsBag.SafeContents;

            Assert.Equal(
                Pkcs12ConfidentialityMode.None,
                readContents.ConfidentialityMode);

            Assert.True(readContents.IsReadOnly);

            List<Pkcs12SafeBag> bags1 = builtContents.GetBags().ToList();
            List<Pkcs12SafeBag> bags2 = readContents.GetBags().ToList();

            Assert.Equal(bags1.Count, bags2.Count);

            for (int i = 0; i < bags2.Count; i++)
            {
                byte[] encoded1 = bags1[i].Encode();
                byte[] encoded2 = bags1[i].Encode();

                Assert.True(encoded1.AsSpan().SequenceEqual(encoded2), $"Bag {i} encodes the same");
            }
        }
    }
}
