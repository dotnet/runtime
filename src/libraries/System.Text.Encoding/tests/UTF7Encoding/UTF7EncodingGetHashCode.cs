// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetHashCode
    {
        // PosTest1: Two return value equals with two ref of a instance
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7a = new UTF7Encoding();
            UTF7Encoding utf7b = utf7a;
            Assert.Equal(utf7a.GetHashCode(), utf7b.GetHashCode());
        }
    }
}
