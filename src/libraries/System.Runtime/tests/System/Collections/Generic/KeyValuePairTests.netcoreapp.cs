// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Generic.Tests
{
    public partial class KeyValuePairTests
    {
        [Fact]
        public void Create_ReturnsExpected()
        {
            KeyValuePair<int, string> keyValuePair = KeyValuePair.Create(1, "2");
            Assert.Equal(1, keyValuePair.Key);
            Assert.Equal("2", keyValuePair.Value);
        }
    }
}
