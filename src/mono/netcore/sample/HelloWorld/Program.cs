// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
namespace HelloWorld
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            /*bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine($"Hello World {(isMono ? "from Mono!" : "from CoreCLR!")}");
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);*/

            //Vector128<UInt32> _fld1 = Vector128<UInt32>.AllBitsSet;
            //Vector128<UInt32> _fld2 = Vector128<UInt32>.Zero;

            Vector128<UInt32> _fld1 = Vector128<UInt32>.AllBitsSet;
            Vector128<UInt32> _fld2 = Vector128<UInt32>.AllBitsSet;

            var result = Sha256.ScheduleUpdate0(_fld1, _fld2);

        }
    }
}
