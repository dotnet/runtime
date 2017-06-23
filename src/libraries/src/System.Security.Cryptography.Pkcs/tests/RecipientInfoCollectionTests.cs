// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class RecipientInfoCollectionTests
    {
        [Fact]
        public static void TestCount()
        {
            RecipientInfoCollection col = CreateTestCollection();
            Assert.Equal(3, col.Count);
        }

        [Fact]
        public static void TestGetEnumerator()
        {
            RecipientInfoCollection col = CreateTestCollection();
            ValidateMembers(col.GetEnumerator());
        }

        [Fact]
        public static void TestExplicitGetEnumerator()
        {
            IEnumerable col = CreateTestCollection();
            ValidateMembers(col.GetEnumerator());
        }

        [Fact]
        public static void TestIndex()
        {
            RecipientInfoCollection col = CreateTestCollection();
            RecipientInfo[] recipients = { col[0], col[1], col[2] };
            ValidateMembers(recipients);
        }

        [Fact]
        public static void TestIndexExceptions()
        {
            RecipientInfoCollection col = CreateTestCollection();
            RecipientInfo ignore = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => ignore = col[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => ignore = col[3]);
        }

        [Fact]
        public static void TestCopyTo()
        {
            RecipientInfoCollection col = CreateTestCollection();

            RecipientInfo[] recipients = new RecipientInfo[3];
            col.CopyTo(recipients, 0);
            ValidateMembers(recipients);
        }

        [Fact]
        public static void TestExplicitCopyTo()
        {
            ICollection col = CreateTestCollection();
            RecipientInfo[] recipients = new RecipientInfo[3];
            col.CopyTo(recipients, 0);
            ValidateMembers(recipients);
        }

        [Fact]
        public static void TestCopyToOffset()
        {
            RecipientInfoCollection col = CreateTestCollection();

            RecipientInfo[] recipients = new RecipientInfo[6];
            col.CopyTo(recipients, 2);
            Assert.Null(recipients[0]);
            Assert.Null(recipients[1]);
            Assert.Null(recipients[5]);
            ValidateMembers(recipients.Skip(2).Take(3));
        }

        [Fact]
        public static void TestExplicitCopyToOffset()
        {
            ICollection col = CreateTestCollection();

            RecipientInfo[] recipients = new RecipientInfo[6];
            col.CopyTo(recipients, 2);
            Assert.Null(recipients[0]);
            Assert.Null(recipients[1]);
            Assert.Null(recipients[5]);
            ValidateMembers(recipients.Skip(2).Take(3));
        }

        [Fact]
        public static void TestCopyToExceptions()
        {
            RecipientInfoCollection col = CreateTestCollection();

            Assert.Throws<ArgumentNullException>(() => col.CopyTo(null, 0));

            RecipientInfo[] recipients = new RecipientInfo[6];

            col.CopyTo(recipients, 3);
            AssertExtensions.Throws<ArgumentException>("destinationArray", null, () => col.CopyTo(recipients, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => col.CopyTo(recipients, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => col.CopyTo(recipients, 6));

            ICollection ic = col;
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(recipients, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(recipients, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(recipients, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => ic.CopyTo(recipients, 6));
            AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(new RecipientInfo[2, 2], 0));
        }

        [Fact]
        public static void TestExplicitCopyToExceptions()
        {
            ICollection col = CreateTestCollection();

            Assert.Throws<ArgumentNullException>(() => col.CopyTo(null, 0));

            RecipientInfo[] recipients = new RecipientInfo[6];

            col.CopyTo(recipients, 3);
            AssertExtensions.Throws<ArgumentException>(null, () => col.CopyTo(recipients, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => col.CopyTo(recipients, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => col.CopyTo(recipients, 6));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has non-zero lower bound
                Array array = Array.CreateInstance(typeof(object), new int[] { 10 }, new int[] { 10 });
                Assert.Throws<IndexOutOfRangeException>(() => col.CopyTo(array, 0));
            }
        }


        private static void ValidateMembers(IEnumerable e)
        {
            ValidateMembers(e.GetEnumerator());
        }

        private static void ValidateMembers(IEnumerator e)
        {
            RecipientInfo[] recipients = new RecipientInfo[3];

            Assert.True(e.MoveNext());
            recipients[0] = (RecipientInfo)(e.Current);
            Assert.True(e.MoveNext());
            recipients[1] = (RecipientInfo)(e.Current);
            Assert.True(e.MoveNext());
            recipients[2] = (RecipientInfo)(e.Current);
            Assert.False(e.MoveNext());

            X509IssuerSerial[] si = recipients.Select(r => (X509IssuerSerial)(r.RecipientIdentifier.Value)).OrderBy(x => x.IssuerName).ToArray();
            Assert.Equal("CN=RSAKeyTransfer1", si[0].IssuerName);
            Assert.Equal("CN=RSAKeyTransfer2", si[1].IssuerName);
            Assert.Equal("CN=RSAKeyTransfer3", si[2].IssuerName);
        }

        private static RecipientInfoCollection CreateTestCollection()
        {
            // Creates a RecipientInfoCollection with three items.
            //     
            // Because RecipientInfoCollection can only be created by the framework, we have to do this the hard way...
            //
            byte[] encodedMessage =
                 ("3082029f06092a864886f70d010703a08202903082028c020100318202583081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004"
                + "818061b4161da3daff2c6c304b8decb021b09ee2523f5162124a6893b077b22a71327c8ab12a82f80472845e274643bfee33"
                + "d34caca6b59fffc66f7fdb2279726f58615258bc3787b479fdfeb4856279e85106d5c271b2f5cadcc8b5622f69cb7e7efd90"
                + "38727c1cb717cb867d2f3e87c3f653cb77837706abb01d40bb22136dac753081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e736665723202102bce9f9ece39f98044f0cd2faa9a14e7300d06092a864886f70d010101050004"
                + "818077293ebfd59a4cef9161ef60f082eca1fd7b2e52804992ea5421527bbea35d7abf810d4316e07dfe766f90b221ae34aa"
                + "192e200c26105aba5511c5e168e4cb0bb2996dce730648d5bc8a0005fbb112a80f9a525e266654d4f3de8318abb8f769c387"
                + "e402889354965f05814dcc4a787de1d5442107ab1bf8dcdbeb432d4d70a73081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e736665723302104497d870785a23aa4432ed0106ef72a6300d06092a864886f70d010101050004"
                + "81807f2d1e0c34b9c4d8e07cf50107114e10f8c3759eca4bb6385451cf7d3619548b217670e4d9eea0c7a09c513c0e4fc1b1"
                + "978ee2b2aab4c7b04183031d2685bf5ea32b8b48d8eef34743bdf14ba71cde56c97618d48692e59f529cd5a7922caff4ac02"
                + "e5a856a5b28db681b0b508b9761b6fa05a5634742c3542986e4073e7932a302b06092a864886f70d010701301406082a8648"
                + "86f70d030704081ddc958302db22518008d0f4f5bb03b69819").HexToByteArray();
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            return ecms.RecipientInfos;
        }
    }
}


