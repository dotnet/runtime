// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public partial class RSAXml
    {
        [Fact]
        public static void TestPlatformNotSupportedException()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.Throws<PlatformNotSupportedException>(() => rsa.FromXmlString(null));
                Assert.Throws<PlatformNotSupportedException>(() => rsa.ToXmlString(true));
            }
        }
    }
}
