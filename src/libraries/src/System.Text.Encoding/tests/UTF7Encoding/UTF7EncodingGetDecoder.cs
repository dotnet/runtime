// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetDecoder
    {
        // PosTest1: Verify method GetDecoder
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Decoder decoder = utf7.GetDecoder();
        }
    }
}

