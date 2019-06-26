using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine("Hello World " + (isMono ? "from Mono!" : "from CoreCLR!"));
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        }
    }
}
