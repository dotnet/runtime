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

            Vector128<UInt32> _fld1 = Vector128<UInt32>.AllBitsSet;
            Vector128<UInt32> _fld2 = Vector128<UInt32>.AllBitsSet;
            Vector128<UInt32> _fld3 = Vector128<UInt32>.AllBitsSet;

            var result1 = Sha256.ScheduleUpdate0(_fld1, _fld2);
            var result2 = Sha256.ScheduleUpdate1(_fld1, _fld2, _fld3);
            var result3 = Sha256.HashUpdate1(_fld1, _fld2, _fld3);
            var result4 = Sha256.HashUpdate2(_fld1, _fld2, _fld3);
            var result0 = Sha256.IsSupported;
            var result00 = Sha256.Arm64.IsSupported;

            Console.WriteLine(result0);
            Console.WriteLine(result00);
        }
    }
}
