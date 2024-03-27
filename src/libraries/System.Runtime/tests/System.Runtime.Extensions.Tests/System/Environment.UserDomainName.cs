// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class EnvironmentUserDomainName
    {
        [Fact]
        public void UserDomainNameIsCorrect()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                Assert.Equal("Windows Domain", Environment.UserDomainName);
            }
            else
            {
                // Highly unlikely anyone is using domain with this name
                Assert.NotEqual("Windows Domain", Environment.UserDomainName);
            }
        }

        [Fact]
        public void UserDomainName_Valid()
        {
            string name = Environment.UserDomainName;
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.Equal(-1, name.IndexOf('\0'));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UserDomainName_MatchesMachineName_Unix()
        {
            Assert.Equal(Environment.MachineName, Environment.UserDomainName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UserDomainName_MatchesEnvironment_Windows()
        {
            Assert.Equal(Environment.GetEnvironmentVariable("USERDOMAIN"), Environment.UserDomainName);
        }
    }
}
