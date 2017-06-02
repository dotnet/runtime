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
    }
}
