// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Net.Tests
{
    public class TlsSystemDefault : RemoteExecutorTestBase
    {
        [Fact]
        public void ServicePointManager_SecurityProtocolDefault_Ok()
        {
            RemoteInvoke(() => Assert.Equal(SecurityProtocolType.SystemDefault, ServicePointManager.SecurityProtocol));
        }

        [Fact]
        public void ServicePointManager_CheckAllowedProtocols_SystemDefault_Allowed()
        {
            RemoteInvoke(() => ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault);
        }
    }
}
