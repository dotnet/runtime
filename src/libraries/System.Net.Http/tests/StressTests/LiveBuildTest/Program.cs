using System;

namespace LiveBuildTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------");
            if (args.Length > 0)
            {
                Console.WriteLine(args[0]);
            }
            Console.WriteLine(typeof(string).Assembly.Location);
        }
    }
}
