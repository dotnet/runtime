using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.ServiceModel.Syndication.Tests
{
    public static class SampleTest
    {
        [Fact]
        public static void PassingTest()
        {
        }

        [Fact]
        public static void FailingTest()
        {
            Assert.True(false, "This test is expected to fail");
        }

    }
}
