// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.ECDiffieHellman.Tests
{
    public class ECDiffieHellmanPublicKeyTests
    {
        private class TestDerived : ECDiffieHellmanPublicKey
        {
            public TestDerived(byte[] keyBlob) : base(keyBlob) { }
        }

        [Fact]
        public void TestInvalidConstructorArgs()
        {
            AssertExtensions.Throws<ArgumentNullException>("keyBlob", () => new TestDerived(null));
        }

        [Fact]
        public void TestToByteArray()
        {
            byte[] arg = new byte[1] { 1 };
            var pk = new TestDerived(arg);

            Assert.Equal(1, pk.ToByteArray()[0]);
        }

        [Fact]
        public void TestToXmlString()
        {
            byte[] arg = new byte[1] { 1 };
            var pk = new TestDerived(arg);

            Assert.Throws<NotImplementedException>(() => pk.ToXmlString());
        }
    }
}
