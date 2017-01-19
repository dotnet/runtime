// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ChainTests2
    {
        [Fact]
        public static void Create()
        {
            using (var chain = X509Chain.Create())
                Assert.NotNull(chain);
        }
    }
}
