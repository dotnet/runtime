using System;

namespace JitBench
{
    public class ConsoleTestOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
