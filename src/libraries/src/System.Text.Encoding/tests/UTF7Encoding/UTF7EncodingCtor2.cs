// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingCtor2
    {
        // PosTest1: created a new instance using new UTF7Encoding(true).
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.NotNull(utf7);
        }

        // PosTest2: created a new instance using new UTF7Encoding(false).
        [Fact]
        public void PosTest2()
        {
            UTF7Encoding utf7 = new UTF7Encoding(false);
            Assert.NotNull(utf7);
        }
    }
}