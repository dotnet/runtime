using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
        }
    }
}
