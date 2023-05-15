// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace PortableApp
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

            if (args[0] == "load_shared_library")
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("SharedLibrary"));
                FieldInfo field = asm.GetType("SharedLibrary.SharedType").GetField("Value");
                Console.WriteLine($"SharedLibrary.SharedType.Value={field.GetValue(null)}");
            }
        }
    }
}
