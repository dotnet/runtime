// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace GitHub_20260
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {         
            // The jit will devirtualize the call to ToString and then undo the box.
            // Make sure that happens properly for vectors.

            Vector<double> x = new Vector<double>();
            string s = ((IFormattable)x).ToString("G", CultureInfo.InvariantCulture);
            string e = null;

            switch (Vector<double>.Count)
            {
                case 2:
                    e = "<0, 0>";
                    break;
                case 4:
                    e = "<0, 0, 0, 0>";
                    break;
                case 8:
                    e = "<0, 0, 0, 0, 0, 0, 0, 0>";
                    break;
                default:
                    e = "unexpected vector length";
                    break;
            }

            if (s != e)
            {
                Console.WriteLine($"FAIL: Expected {e}, got {s}");
                return -1;
            }

            Console.WriteLine($"PASS: {s}");
            return 100;
        }
    }
}
