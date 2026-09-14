// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class EnvironmentUserName
    {
        [Fact]
        public void UserNameIsCorrect()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                Assert.Equal("Windows User", Environment.UserName);
            }
            else
            {
                // Highly unlikely anyone is using user with this name
                Assert.NotEqual("Windows User", Environment.UserName);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "Not valid on iOS or tvOS")]
        public void UserName_Valid()
        {
            string name = Environment.UserName;
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.Equal(-1, name.IndexOf('\0'));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UserName_MatchesEnvironment_Windows()
        {
            Assert.Equal(Environment.GetEnvironmentVariable("USERNAME"), Environment.UserName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Browser)]
        public void UserName_MatchesEnvironment_Browser()
        {
            string name = Environment.UserName;
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.Equal("Browser", name);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS)]
        public void UserName_Unix_DoesNotThrow()
        {
            // Verify Environment.UserName completes without throwing an exception (e.g. IOException if user info is unavailable).
            string name = Environment.UserName;
            Assert.NotNull(name);
        }
    }
}
