// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class EnvironmentUserName
    {
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Uap)]
        public void UserNameIsHardCodedOnUap()
        {
            Assert.Equal("Windows User", Environment.UserName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        public void UserNameIsCorrectOnNonUap()
        {
            // Highly unlikely anyone is using user with this name
            Assert.NotEqual("Windows User", Environment.UserName);
        }

        [Fact]
        public void UserName_Valid()
        {
            string name = Environment.UserName;
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.Equal(-1, name.IndexOf('\0'));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UserName_MatchesEnvironment_Windows()
        {
            Assert.Equal(Environment.GetEnvironmentVariable("USERNAME"), Environment.UserName);
        }
    }
}
