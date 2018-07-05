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
            // The behaviour is different from full framework due to the https://github.com/dotnet/corefx/issues/23063.
            // We can modify this test later to check if the TargetFrameworkName starts with .NETCore or .NETFramework.
            Assert.Equal(AppContext.BaseDirectory, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            Assert.Equal(AppContext.TargetFrameworkName, AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
        }        
    }
}

