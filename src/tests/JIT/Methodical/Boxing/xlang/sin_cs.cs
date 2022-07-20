// Licensed to the .NET Foundation under one or more agreements.
using Xunit;
// The .NET Foundation licenses this file to you under the MIT license.

namespace SinCalc_against_sinlib_cs
{
    using System;
    using SinCalcLib;

    public class SinCalc
    {
        [Fact]
        public static int TestEntryPoint()
        {
            object i;
            object Angle;
            object Result1, Result2;
            object[] testresults = new object[10];
            testresults[0] = 0.000000000d;
            testresults[1] = 0.309016994d;
            testresults[2] = 0.587785252d;
            testresults[3] = 0.809016994d;
            testresults[4] = 0.951056516d;
            testresults[5] = 1.000000000d;
            testresults[6] = 0.951056516d;
            testresults[7] = 0.809016994d;
            testresults[8] = 0.587785252d;
            testresults[9] = 0.309016994d;

            object mistake = 1e-9d;
            for (i = 0; (int)i < 10; i = (int)i + 1)
            {
                Angle = ((PiVal)SinCalcLib.PI).Value * ((int)i / 10.0);
                Console.Write("Classlib Sin(");
                Console.Write(Angle);
                Console.Write(")=");
                Console.WriteLine(Result1 = Math.Sin((double)Angle));
                Console.Write("This Version(");
                Console.Write(Angle);
                Console.Write(")=");
                Console.WriteLine(Result2 = (double)SinCalcLib.mySin(Angle));
                Console.Write("Error is:");
                Console.WriteLine((double)Result1 - (double)Result2);
                Console.WriteLine();
                if (Math.Abs((double)Result1 - (double)Result2) > (double)mistake) // reasonable considering double
                {
                    Console.WriteLine("ERROR, Versions too far apart!");
                    return 1;
                }
                if (Math.Abs((double)testresults[(int)i] - (double)Result1) > (double)mistake) // reasonable considering double
                {
                    Console.WriteLine("ERROR, Classlib version isnt right!");
                    return 1;
                }
                if (Math.Abs((double)testresults[(int)i] - (double)Result2) > (double)mistake) // reasonable considering double
                {
                    Console.WriteLine("ERROR, our version isnt right!");
                    return 1;
                }

            }
            Console.WriteLine("Yippie, all correct");
            return 100;
        }
    }
}
