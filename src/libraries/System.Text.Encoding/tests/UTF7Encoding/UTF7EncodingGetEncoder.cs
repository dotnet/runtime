// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

public class UTF7EncodingGetEncoder
{
    // PosTest1: Verify method GetEncoder
    [Fact]
    public void PosTest1()
    {
        UTF7Encoding utf7 = new UTF7Encoding();
        Encoder encoder = utf7.GetEncoder();
    }
}
