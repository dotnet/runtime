// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace InterProcessCommunication.Tests
{
    /// <summary>
    /// Provides an entry point in a new process that will load a specified method and invoke it.
    /// </summary>
    static class Program
    {
        static int Main(string[] args)
        {
            // The program expects to be passed the target assembly name to load, the type
            // from that assembly to find, and the method from that assembly to invoke.
            // Any additional arguments are passed as strings to the method.
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: TestConsoleApp assemblyName typeName methodName");
                return -1;
            }
            string assemblyName = args[0];
            string typeName = args[1];
            string methodName = args[2];
            string[] additionalArgs = args.Length > 3 ? 
                args.Subarray(3, args.Length - 3) : 
                Array.Empty<string>();

            // Load the specified assembly, type, and method, then invoke the method.
            // The program's exit code is the return value of the invoked method.
            try
            {
                Assembly a = Assembly.Load(new AssemblyName(assemblyName));
                Type t = a.GetType(typeName);
                MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod(methodName);
                return (int)mi.Invoke(null, additionalArgs);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("Exception from TestConsoleApp: " + exc);
                return -2;
            }
        }

        private static T[] Subarray<T>(this T[] arr, int offset, int count)
        {
            var newArr = new T[count];
            Array.Copy(arr, offset, newArr, 0, count);
            return newArr;
        }
    }
}
