// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System;

namespace DefaultNamespace
{
    public class Bug
    {
        public virtual void runTest()
        {
            CultureInfo en = new CultureInfo("en-US");
            Double d = Convert.ToDouble("1.0E19", en.NumberFormat);
            Console.WriteLine("Expected value==" + d.ToString("E", en.NumberFormat));
            UInt64 l = (UInt64)d;
            Console.WriteLine("Returned value==" + l.ToString("E", en.NumberFormat));
            if (d.ToString("E", en.NumberFormat).Equals(l.ToString("E", en.NumberFormat)))
                Console.WriteLine("Test passed");
            else
                Console.WriteLine("Test FAiLED");
        }

        public static int Main(String[] args)
        {
            new Bug().runTest();
            return 100;
        }
    }
}
