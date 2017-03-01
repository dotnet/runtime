using System;
using System.IO;
using System.Reflection;

namespace LightupClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Assembly asmGreet = null;

            try
            {
                asmGreet = Assembly.Load(new AssemblyName("LightupLib"));
                
                // Get reference to the method that we wish to invoke
                Type type = asmGreet.GetType("LightupLib.Greet");
                var method = System.Reflection.TypeExtensions.GetMethod(type, "Hello");

                // Invoke it
                string greeting = (string)method.Invoke(null, new object[] {"LightupClient"});
                Console.WriteLine("{0}", greeting);
            }
            catch(FileNotFoundException ex)
            {
                throw new Exception("Exception: Failed to load the lightup assembly!", ex);
            }
        }
    }
}
