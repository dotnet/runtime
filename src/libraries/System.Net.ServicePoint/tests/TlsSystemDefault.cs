// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
        }
    }
}
