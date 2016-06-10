using System;
using Xunit;

namespace StandaloneTestApp
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Dummy Entrypoint");
        }

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
