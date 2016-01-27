// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingCtor1
    {
        // PosTest1: created a new instance.
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.NotNull(utf7);
        }
    }
}
