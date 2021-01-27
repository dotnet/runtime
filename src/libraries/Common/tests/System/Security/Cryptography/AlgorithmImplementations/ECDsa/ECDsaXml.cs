// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnMono("Not supported on Browser", TestPlatforms.Browser)]
    public partial class ECDsaXml : ECDsaTestsBase
    {
        [Fact]
        public static void TestNotImplementedException()
        {
            using (ECDsa ec = ECDsaFactory.Create())
            {
                Assert.Throws<NotImplementedException>(() => ec.FromXmlString(null));
                Assert.Throws<NotImplementedException>(() => ec.ToXmlString(true));
            }
        }
    }
}
