// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class CustomBagTypeTests
    {
        [Fact]
        public static void WriteCustomType()
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSafeBag(new CustomBagType(2));

            Pkcs12Builder builder = new Pkcs12Builder();
            builder.AddSafeContentsUnencrypted(contents);
            builder.SealWithoutIntegrity();

            byte[] encoded = builder.Encode();

            const string expectedHex =
                "3033020103302E06092A864886F70D010701A021041F301D301B06092A864886" +
                "F70D010701A00E040C300A3008060100A003040102";

            Assert.Equal(
                expectedHex,
                encoded.ByteArrayToHex());
        }

        [Fact]
        public static void ReadCustomType()
        {
            byte[] input = (
                "3033020103302E06092A864886F70D010701A021041F301D301B06092A864886" +
                "F70D010701A00E040C300A3008060100A003040102").HexToByteArray();

            Pkcs12Info info = Pkcs12Info.Decode(input, out _, skipCopy: true);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);

            ReadOnlyCollection<Pkcs12SafeContents> allContents = info.AuthenticatedSafe;
            Assert.Equal(1, allContents.Count);

            Pkcs12SafeContents contents = allContents[0];
            Assert.Equal(Pkcs12ConfidentialityMode.None, contents.ConfidentialityMode);

            List<Pkcs12SafeBag> bags = contents.GetBags().ToList();
            Assert.Equal(1, bags.Count);

            Pkcs12SafeBag bag = bags[0];
            Assert.IsNotType<Pkcs12CertBag>(bag);
            Assert.IsNotType<Pkcs12KeyBag>(bag);
            Assert.IsNotType<Pkcs12SecretBag>(bag);
            Assert.IsNotType<Pkcs12ShroudedKeyBag>(bag);

            CustomBagType customBag = new CustomBagType(bag.EncodedBagValue);
            Assert.Equal(2, customBag.Value);
        }

        [Fact]
        public static void CopyCustomType()
        {
            const string startHex =
                "3033020103302E06092A864886F70D010701A021041F301D301B06092A864886" +
                "F70D010701A00E040C300A3008060100A003040102";

            Pkcs12Info info = Pkcs12Info.Decode(startHex.HexToByteArray(), out _, skipCopy: true);
            // This next line implicitly asserts no encryption, and a couple of Single
            Pkcs12SafeBag bag = info.AuthenticatedSafe.Single().GetBags().Single();

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSafeBag(bag);

            Pkcs12Builder builder = new Pkcs12Builder();
            builder.AddSafeContentsUnencrypted(contents);
            builder.SealWithoutIntegrity();
            byte[] encoded = builder.Encode();

            Assert.Equal(startHex, encoded.ByteArrayToHex());
        }

        private class CustomBagType : Pkcs12SafeBag
        {
            public CustomBagType(byte value)
                : this(new byte[] { 4, 1, value })
            {
            }

            public CustomBagType(ReadOnlyMemory<byte> encoded, bool skipCopy=false)
                : base("0.0", encoded, skipCopy)
            {
            }

            public byte Value
            {
                get
                {
                    if (EncodedBagValue.Length == 3)
                    {
                        ReadOnlySpan<byte> bagValue = EncodedBagValue.Span;

                        if (bagValue[0] == 0x04 && bagValue[1] == 0x01)
                        {
                            return bagValue[2];
                        }
                    }

                    throw new InvalidOperationException();
                }
            }
        }
    }
}
