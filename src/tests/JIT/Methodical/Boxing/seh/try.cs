// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace SinCalc_try_seh_cs
{
    internal class MistakeException : Exception
    {
        public object mistake;
        public MistakeException(double m) : base("Mistake!") { mistake = m; }
        override public String ToString() { return "Mistake is " + mistake.ToString(); }
    }

    public class SinCalc
    {
        protected static object PI = 3.1415926535897932384626433832795d;

        protected static object mySin(object Angle)
        {
            object powX, sumOfTerms, term, fact = 1.0;

            powX = term = Angle;
            sumOfTerms = 0.0;

            for (object i = 1; (int)i <= 200; i = (int)i + 2)
            {
                sumOfTerms = (double)sumOfTerms + (double)term;
                powX = (double)powX * (-(double)Angle * (double)Angle);
                fact = (double)fact * ((int)i + 1) * ((int)i + 2);
                term = (double)powX / (double)fact;
            }
            return sumOfTerms;
        }

        protected static object CalcAndCheck(object Angle, object Expected)
        {
            object mistake = 1e-9d;
            object Result1 = Math.Sin((double)Angle);
            object Result2 = (double)mySin(Angle);
            if (Math.Abs((double)Result1 - (double)Result2) > (double)mistake)
                throw new MistakeException(Math.Abs((double)Result1 - (double)Result2));
            if (Math.Abs((double)Result1 - (double)Expected) > (double)mistake)
                throw new MistakeException(Math.Abs((double)Result1 - (double)Expected));
            if (Math.Abs((double)Result2 - (double)Expected) > (double)mistake)
                throw new MistakeException(Math.Abs((double)Result2 - (double)Expected));
            return Result1;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            object i;
            object Angle;
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

            object total1 = 0.0d, total2 = 0.0d, total3 = 0.0d;
            for (i = 0; (int)i < 10; i = (int)i + 1)
            {
                try
                {
                    try
                    {
                        Angle = ((double)PI) * ((int)i / 10.0);
                        total2 = (double)total2 + (double)Angle;
                        total3 = (double)total3 +
                            (double)CalcAndCheck(Angle, (double)testresults[(int)i] + 0.0000000004 * (int)i);
                        Console.WriteLine("OK");
                    }
                    finally
                    {
                        Console.WriteLine("Current totals " + total1.ToString() + " and " +
                            ((double)total2).ToString() + " and " + total3.ToString());
                    }
                }
                catch (MistakeException ex)
                {
                    Console.WriteLine("Mistake is " + ex.mistake.ToString());
                    total1 = (double)total1 + (double)ex.mistake;
                }
            }
            Console.WriteLine("**** PASSED ****");
            return 100;
        }
    }
}
