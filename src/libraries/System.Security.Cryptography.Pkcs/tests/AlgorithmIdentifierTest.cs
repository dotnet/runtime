// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class AlgorithmIdentifierTest
    {
        [Fact]
        public static void ParameterlessConstructor()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier();
            Assert.Equal(0, ai.KeyLength);
            Assert.Equal(Oids.TripleDesCbc, ai.Oid.Value);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void ConstructorTakesOid()
        {
            Oid o = new Oid(Oids.Rsa);
            AlgorithmIdentifier ai = new AlgorithmIdentifier(o);
            Assert.Equal(0, ai.KeyLength);
            Assert.Equal(Oids.Rsa, ai.Oid.Value);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void ConstructorTakesNullOid()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier(null);
            Assert.Null(ai.Oid);
            Assert.Equal(0, ai.KeyLength);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void ConstructorTakesOidAndKeyLength()
        {
            Oid o = new Oid(Oids.Rsa);
            AlgorithmIdentifier ai = new AlgorithmIdentifier(o, 128);
            Assert.Equal(128, ai.KeyLength);
            Assert.Equal(Oids.Rsa, ai.Oid.Value);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void ConstructorTakesNullOidAndKeyLength()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier(null, 128);
            Assert.Null(ai.Oid);
            Assert.Equal(128, ai.KeyLength);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void ConstructorTakesOidAndNegativeKeyLength()
        {
            Oid o = new Oid(Oids.Rsa);
            AlgorithmIdentifier ai = new AlgorithmIdentifier(o, -1);
            Assert.Equal(-1, ai.KeyLength);
            Assert.Equal(Oids.Rsa, ai.Oid.Value);
            Assert.NotNull(ai.Parameters);
            Assert.Equal(0, ai.Parameters.Length);
        }

        [Fact]
        public static void KeyLength()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier
            {
                KeyLength = int.MaxValue
            };
            Assert.Equal(int.MaxValue, ai.KeyLength);
            ai.KeyLength = 0;
            Assert.Equal(0, ai.KeyLength);
            ai.KeyLength = int.MinValue;
            Assert.Equal(int.MinValue, ai.KeyLength);
        }

        [Fact]
        public static void Oid()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier
            {
                Oid = new Oid(Oids.Rsa)
            };
            Assert.Equal(Oids.Rsa, ai.Oid.Value);
            ai.Oid = null;
            Assert.Null(ai.Oid);
        }

        [Fact]
        public static void Parameters()
        {
            AlgorithmIdentifier ai = new AlgorithmIdentifier
            {
                Parameters = new byte[2] { 0x05, 0x00 } // ASN.1 NULL
            };
            Assert.Equal("0500", ai.Parameters.ByteArrayToHex());
            ai.Parameters = null;
            Assert.Null(ai.Parameters);
        }
    }
}
