// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
            return 0;
        }

        [JSImport("Sample.Test.add", "main.js")]
        internal static partial int Add(int a, int b);

        [JSImport("Sample.Test.sub", "main.js")]
        internal static partial int Sub(int a, int b);

        [JSExport]
        internal static int TestMeaning()
        {
            // call back to JS via imports
            return Add(Sub(80, 40), 2);
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
