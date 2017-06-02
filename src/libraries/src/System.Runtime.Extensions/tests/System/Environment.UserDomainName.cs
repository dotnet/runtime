using Xunit;

namespace System.Tests
{
    public class EnvironmentUserDomainName
    {
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Uap)]
        public void UserDomainNameIsHardCodedOnUap()
        {
            Assert.Equal("Windows Domain", Environment.UserName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        public void UserDomainNameIsCorrectOnNonUap()
        {
            // Highly unlikely anyone is using domain with this name
            Assert.NotEqual("Windows Domain", Environment.UserName);
        }
    }
}
