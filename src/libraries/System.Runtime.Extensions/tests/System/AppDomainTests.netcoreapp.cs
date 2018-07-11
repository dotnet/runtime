// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public partial class AppDomainTests
    {
        [Fact]
        public void GetSetupInformation()
        {
            RemoteInvoke(() => {
                Assert.Equal(AppContext.BaseDirectory, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                Assert.Equal(AppContext.TargetFrameworkName, AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
                return SuccessExitCode;
            }).Dispose();
        }        
    }
}

