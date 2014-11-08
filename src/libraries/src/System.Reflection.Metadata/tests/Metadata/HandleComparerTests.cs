// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class HandleComparerTests
    {
        [Fact]
        public void CompareTokens()
        {
            Assert.True(TokenTypeIds.CompareTokens(0x02000001, 0x02000002) < 0);
            Assert.True(TokenTypeIds.CompareTokens(0x02000002, 0x02000001) > 0);
            Assert.True(TokenTypeIds.CompareTokens(0x02000001, 0x02000001) == 0);

            // token type is ignored
            Assert.True(TokenTypeIds.CompareTokens(0x20000001, 0x21000002) < 0);

            // virtual tokens follow non-virtual:
            Assert.True(TokenTypeIds.CompareTokens(0x82000001, 0x02000002) > 0);
            Assert.True(TokenTypeIds.CompareTokens(0x02000002, 0x82000001) < 0);
            Assert.True(TokenTypeIds.CompareTokens(0x82000001, 0x82000001) == 0);

            // make sure we won't overflow for extreme values:
            Assert.True(TokenTypeIds.CompareTokens(0xffffffff, 0) > 0);
            Assert.True(TokenTypeIds.CompareTokens(0, 0xffffffff) < 0);
            Assert.True(TokenTypeIds.CompareTokens(0xfffffffe, 0xffffffff) < 0);
            Assert.True(TokenTypeIds.CompareTokens(0xffffffff, 0xfffffffe) > 0);
            Assert.True(TokenTypeIds.CompareTokens(0xffffffff, 0xffffffff) == 0);
        }
    }
}
