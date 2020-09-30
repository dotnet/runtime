// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        [Fact]
        public static void TestNotImplementedException()
        {
            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
            {
                Assert.Throws<NotImplementedException>(() => ec.FromXmlString(null));
                Assert.Throws<NotImplementedException>(() => ec.ToXmlString(true));
            }
        }
    }
}
