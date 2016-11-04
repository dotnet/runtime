// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace RemoteExecutorConsoleApp
{
    /// <summary>
    /// Provides an entry point in a new process that will load a specified method and invoke it.
    /// </summary>
    internal static class Program
    {
        static int Main(string[] args)
        {
            // The program expects to be passed the target assembly name to load, the type
            // from that assembly to find, and the method from that assembly to invoke.
            // Any additional arguments are passed as strings to the method.
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: {0} assemblyName typeName methodName", typeof(Program).GetTypeInfo().Assembly.GetName().Name);
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
            Assembly a = null;
            Type t = null;
            MethodInfo mi = null;
            object instance = null;
            try
            {
                a = Assembly.Load(new AssemblyName(assemblyName));
                t = a.GetType(typeName);
                mi = t.GetTypeInfo().GetDeclaredMethod(methodName);
                if (!mi.IsStatic)
                {
                    instance = Activator.CreateInstance(t);
                }
                object result = mi.Invoke(instance, additionalArgs);
                return result is Task<int> ?
                    ((Task<int>)result).GetAwaiter().GetResult() :
                    (int)result;
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("Exception from RemoteExecutorConsoleApp({0}):", string.Join(", ", args));
                Console.Error.WriteLine("Assembly: {0}", a);
                Console.Error.WriteLine("Type: {0}", t);
                Console.Error.WriteLine("Method: {0}", mi);
                Console.Error.WriteLine("Exception: {0}", exc);
                throw exc;
            }
            finally
            {
                IDisposable d = instance as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
        }

        private static MethodInfo GetMethod(this Type type, string methodName)
        {
            Type t = type;
            while (t != null)
            {
                TypeInfo ti = t.GetTypeInfo();
                MethodInfo mi = ti.GetDeclaredMethod(methodName);
                if (mi != null)
                {
                    return mi;
                }
                t = ti.BaseType;
            }
            return null;
        }

        private static T[] Subarray<T>(this T[] arr, int offset, int count)
        {
            var newArr = new T[count];
            Array.Copy(arr, offset, newArr, 0, count);
            return newArr;
        }
    }
}
