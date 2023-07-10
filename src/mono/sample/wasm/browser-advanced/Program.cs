// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");

            var start = DateTime.UtcNow;
            var timezonesCount = TimeZoneInfo.GetSystemTimeZones().Count;
            var end = DateTime.UtcNow;
            Console.WriteLine($"Found {timezonesCount} timezones in the TZ database in {end-start}");

            TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            Console.WriteLine($"{utc.DisplayName} BaseUtcOffset is {utc.BaseUtcOffset}");

            try
            {
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
                Console.WriteLine($"{tst.DisplayName} BaseUtcOffset is {tst.BaseUtcOffset}");
            }
            catch (TimeZoneNotFoundException tznfe)
            {
                Console.WriteLine($"Could not find Asia/Tokyo: {tznfe.Message}");
            }


            return 0;
        }

        [LibraryImport("fibonacci")]
        public static partial int Fibonacci(int n);

        [JSImport("Sample.Test.add", "main.js")]
        internal static partial int Add(int a, int b);

        [JSExport]
        internal static int TestMeaning()
        {
            // call to C code via [DllImport]
            var half = Fibonacci(8);
            // call back to JS via [JSImport]
            return Add(half, half);
        }

        [JSExport]
        internal static bool IsPrime(int number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            var boundary = (int)Math.Floor(Math.Sqrt(number));
                
            for (int i = 3; i <= boundary; i += 2)
                if (number % i == 0)
                    return false;
            
            return true;        
        }        
    }
}
