// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    public class SymbolTokenTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var symbolToken = new SymbolToken();
            Assert.Equal(0, symbolToken.GetToken());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_Value(int value)
        {
            var symbolToken = new SymbolToken(value);
            Assert.Equal(value, symbolToken.GetToken());
            Assert.Equal(value, symbolToken.GetHashCode());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new SymbolToken(1), new SymbolToken(1), true };
            yield return new object[] { new SymbolToken(1), new SymbolToken(0), false };
            yield return new object[] { new SymbolToken(), new SymbolToken(0), true };

            yield return new object[] { new SymbolToken(), new object(), false };
            yield return new object[] { new SymbolToken(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(SymbolToken symbolToken, object other, bool expected)
        {
            Assert.Equal(expected, symbolToken.Equals(other));
            if (other is SymbolToken otherSymbolToken)
            {
                Assert.Equal(expected, symbolToken.Equals(otherSymbolToken));
                Assert.Equal(expected, symbolToken == otherSymbolToken);
                Assert.Equal(!expected, symbolToken != otherSymbolToken);
            }
        }
    }
}
