// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    public partial class Test
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            
            var rand = new Random();
            Console.WriteLine("Today's lucky number is " + rand.Next(100) + " and " + Guid.NewGuid());

            var start = DateTime.UtcNow;
            var timezonesCount = TimeZoneInfo.GetSystemTimeZones().Count;
            await Delay(100);
            var end = DateTime.UtcNow;
            Console.WriteLine($"Found {timezonesCount} timezones in the TZ database in {end - start}");

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

        [JSImport("Sample.Test.delay", "main.js")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task Delay([JSMarshalAs<JSType.Number>] int ms);

        [JSExport]
        internal static async Task PrintMeaning(Task<int> meaningPromise)
        {
            Console.WriteLine("Meaning of life is " + await meaningPromise);
        }
        [JSExport]
        internal static int SimpleTestFunctionInt()
        {
            return 666;
        }

        [JSExport]
        internal static int SimpleTestFunctionIntSize()
        {
            return sizeof(int);
        }

        [JSExport]
        internal static string SimpleTestFunctionString()
        {
            return "Your lucky number today is:";
        }

        [JSExport]
        internal static void SimpleTestFunctionPrintEmptyString()
        {
            // write an empty string to the console
            Console.WriteLine("");
        }
        [JSExport]
        internal static bool SimpleTestConsole()
        {
            // see if we can access anything from the console
            System.IO.TextWriter output = Console.Out;
            return output != null;
        }
        [JSExport]
        internal static void SimpleTestArray()
        {
            
            //var arrayPtr = System.MHTestClass.RunTestArray();
            var arrayPtr = System.MHTestClass.RunTestArrayRaw();
            //var h = new Mono.SafeGPtrArrayHandle(arrayPtr);

            var a = 1;// h[0];
            var b = 2;// h[1];
            var c = 3;// h[2];
            
            //return a + b + c;
            //using (var h = new Mono.SafeGPtrArrayHandle(TestArray_native());
        }
        
        [JSExport]
        internal static void SimpleTestFunctionPrintString()
        {
            // write an empty string to the console
            Console.WriteLine("Test");
        }
        [JSExport]
        internal static int TestMeaning()
        {
            // int testSize = 123;
            // Console.WriteLine("Size of an int is " + sizeof(int) + " bytes, and test size is " + testSize + " bytes.");
            // call to C code via [DllImport]
            var half = Fibonacci(8);
            // call back to JS via [JSImport]
            return Add(half, half);
        }

        [JSExport]
        internal static void SillyLoop()
        {
            Console.WriteLine("Entering SillyLoop()");
            // this silly method will generate few sample points for the profiler
            for (int i = 1; i <= 60; i ++)
            {
                try
                {
                    for (int s = 0; s <= 60; s ++)
                    {
                        try
                        {
                            if (DateTime.UtcNow.Millisecond == s)
                            {
                                Console.WriteLine("Time is " + s);
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
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
