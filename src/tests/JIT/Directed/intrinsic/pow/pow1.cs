// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//Testing the special values

using System;
using System.Runtime.InteropServices;
using Xunit;

public class pow1
{
    [Fact]
    public static int TestEntryPoint()
    {
        double x, y, z;
        bool pass = true;

        //Check if the test is being executed on ARMARCH
        bool isProcessorArmArch = RuntimeInformation.ProcessArchitecture == Architecture.Arm
                                || RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

        x = 0;
        y = 0;
        z = Math.Pow(x, y);
        if (z != 1)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = Double.MinValue;
        y = 1.0;
        z = Math.Pow(x, y);
        if (z != Double.MinValue)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        if (isProcessorArmArch)
        {
            //Skip this Test due to the way how Double.Epsilon is defined on ARMARCH
            Console.WriteLine("Skipping Pow(Double.Epsilon,1) test on ARMARCH");
        }
        else
        {
            x = Double.Epsilon;
            y = 1.0;
            z = Math.Pow(x, y);
            if (z != Double.Epsilon)
            {
                Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
                pass = false;
            }
        }

        x = Double.MaxValue;
        y = 1.0;
        z = Math.Pow(x, y);
        if (z != Double.MaxValue)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = Double.NegativeInfinity;
        y = 1;
        z = Math.Pow(x, y);
        if (z != Double.NegativeInfinity)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = Double.NaN;
        y = 1;
        z = Math.Pow(x, y);
        if (!Double.IsNaN(z))
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = Double.PositiveInfinity;
        y = 1;
        z = Math.Pow(x, y);
        if (z != Double.PositiveInfinity)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = 1;
        y = Double.MinValue;
        z = Math.Pow(x, y);
        if (z != 1)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = 1;
        y = Double.MaxValue;
        z = Math.Pow(x, y);
        if (z != 1)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = 1.0;
        y = Double.Epsilon;
        z = Math.Pow(x, y);
        if (z != 1)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        x = 1;
        y = Double.NaN;
        z = Math.Pow(x, y);
        if (z != 1)
        {
            Console.WriteLine("x: {0}, y: {1}, Pow(x,y): {2}", x, y, z);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
