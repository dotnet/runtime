// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace LightupClient
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Assembly asmGreet = null;
            int iRetVal = 0;

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
                Console.WriteLine("Exception: Failed to load the lightup assembly!");
                Console.WriteLine(ex.ToString());
                iRetVal = -1;
            }

            return iRetVal;
        }
    }
}
