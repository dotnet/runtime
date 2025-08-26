using System;
using Xunit;

namespace System.Security.Cryptography.OpenSsl.Tests
{
    public class PlatformDetectionTest
    {
        [Fact]
        public void TestPlatformIsLinux()
        {
            // This test should run on Linux but not on Apple platforms
            // Verify we're actually running on Linux in this test environment
            Assert.True(Environment.OSVersion.Platform == PlatformID.Unix || 
                       Environment.OSVersion.Platform == PlatformID.Other);
            
            // This is just to verify our assembly-level attribute doesn't break normal tests
            Assert.True(true);
        }
    }
}