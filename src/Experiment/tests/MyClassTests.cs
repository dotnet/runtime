using System;
using Xunit;

namespace Experiment.Tests
{
    public class MyClassTests
    {
        [Fact]
        public void Test1()
        {
            Assert.True(MyClass.ReturnTrue);
        }
    }
}
