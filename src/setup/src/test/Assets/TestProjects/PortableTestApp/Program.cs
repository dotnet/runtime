using System;
using Xunit;

namespace PortableTestApp
{
    public class Program
    {
        [Fact]
        public void PassingTest()
        {
            Console.WriteLine("Pass!");
            return;
        }

        [Fact]
        public void FailingTest()
        {
            throw new Exception("Fail!");
        }
    }
}
