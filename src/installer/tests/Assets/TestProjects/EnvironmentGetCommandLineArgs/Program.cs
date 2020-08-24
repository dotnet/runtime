using System;

namespace EnvironmentGetCommandLineArgs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                Console.WriteLine(arg);
            }
        }
    }
}
