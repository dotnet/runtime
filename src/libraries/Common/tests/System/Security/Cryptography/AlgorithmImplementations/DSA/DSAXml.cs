// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public partial class DSAXml
    {
        [Fact]
        public static void TestPlatformNotSupportedException()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                Assert.Throws<PlatformNotSupportedException>(() => dsa.FromXmlString(null));
                Assert.Throws<PlatformNotSupportedException>(() => dsa.ToXmlString(true));
            }
        }
    }
}
