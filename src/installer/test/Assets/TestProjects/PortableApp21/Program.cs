using System;

namespace PortableApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine($"RESOLVED_FRAMEWORKS = {GetRuntimePropertiesFromAppDomain()}");
        }

        private static string GetRuntimePropertiesFromAppDomain()
        {
            return System.AppDomain.CurrentDomain.GetData("RESOLVED_FRAMEWORKS") as string;
        }
    }
}
