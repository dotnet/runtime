// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace HelloWorld
{
    internal class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int X()
        {
            Vector128<int> a = Vector128.Create(2,2,3,4);
            Vector128<int> b = Vector128.Create(1,2,3,4);
            return Vector128.GreaterThanAny(a,b) ? 1 : 0;
        }

        private static void Main(string[] args)
        {
            bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine($"Hello World {(isMono ? "from Mono!" : "from CoreCLR!")}");
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

            Console.WriteLine(X().ToString());
        }
    }
}
