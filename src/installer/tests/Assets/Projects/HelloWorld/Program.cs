// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace HelloWorld
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));
            Console.WriteLine(RuntimeInformation.FrameworkDescription);

            if (args.Length == 0)
                return;

            switch (args[0])
            {
                case "load_shared_library":
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("SharedLibrary"));
                    PropertyInfo property = asm.GetType("SharedLibrary.SharedType").GetProperty("Value");
                    Console.WriteLine($"SharedLibrary.SharedType.Value = {property.GetValue(null)}");
                    break;
                case "print_properties":
                    foreach (string propertyName in args[1..])
                    {
                        var propertyValue = (string)System.AppContext.GetData(propertyName);
                        if (string.IsNullOrEmpty(propertyValue))
                        {
                            Console.WriteLine($"Property '{propertyName}' was not found.");
                            continue;
                        }

                        Console.WriteLine($"AppContext.GetData({propertyName}) = {propertyValue}");
                    }
                    break;
                case "throw_exception":
                    throw new Exception("Goodbye World!");
                default:
                    break;
            }
        }
    }
}
