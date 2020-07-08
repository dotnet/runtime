// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Tests
{
    public class TlsSystemDefault
    {
        [Fact]
        public void ServicePointManager_SecurityProtocolDefault_Ok()
        {
            Assert.Equal(SecurityProtocolType.SystemDefault, ServicePointManager.SecurityProtocol);
        }

        [Fact]
        public void ServicePointManager_CheckAllowedProtocols_SystemDefault_Allowed()
        {
            SecurityProtocolType orig = ServicePointManager.SecurityProtocol;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
                Assert.Equal(orig, ServicePointManager.SecurityProtocol);
            }
            finally
            {
                ServicePointManager.SecurityProtocol = orig;
            }
        }
    }
}
