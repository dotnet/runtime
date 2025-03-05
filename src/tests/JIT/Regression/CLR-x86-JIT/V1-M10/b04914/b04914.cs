// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System;
using Xunit;

namespace DefaultNamespace
{
    public class Bug
    {
        internal virtual void runTest()
        {
            Double d = Convert.ToDouble("1.0E19", NumberFormatInfo.InvariantInfo);
            Console.WriteLine("Expected value==" + d.ToString("E", NumberFormatInfo.InvariantInfo));
            UInt64 l = (UInt64)d;
            Console.WriteLine("Returned value==" + l.ToString("E", NumberFormatInfo.InvariantInfo));
            if (d.ToString("E", NumberFormatInfo.InvariantInfo).Equals(l.ToString("E", NumberFormatInfo.InvariantInfo)))
                Console.WriteLine("Test passed");
            else
                Console.WriteLine("Test FAiLED");
        }

        [Fact]
        public static void TestEntryPoint()
        {
            new Bug().runTest();
        }
    }
}
