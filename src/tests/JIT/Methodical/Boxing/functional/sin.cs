// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace SinCalc_sin_functional_cs
{
    public class SinCalc
    {
        protected struct CalcCtx
        {
            private double _powX,_sumOfTerms;
            private object _term;

            public double fact;
            public double get_powX() { return _powX; }
            public void set_powX(double val) { _powX = val; }

            public double sumOfTerms
            {
                get { return _sumOfTerms; }
                set { _sumOfTerms = value; }
            }

            public double term
            {
                get { return (double)_term; }
                set { _term = value; }
            }
        }

        protected static object PI = 3.1415926535897932384626433832795d;

        protected static object mySin(object Angle)
        {
            CalcCtx ctx = new CalcCtx();
            object ct = ctx;

            ctx.fact = 1.0;
            ctx.set_powX(ctx.term = (double)Angle);
            ctx.sumOfTerms = 0.0;

            for (object i = 1; (int)i <= 200; i = (int)i + 2)
            {
                ctx.sumOfTerms = ctx.sumOfTerms + ctx.term;
                ctx.set_powX(ctx.get_powX() * (-(double)Angle * (double)Angle));
                ctx.fact = ctx.fact * ((int)i + 1) * ((int)i + 2);
                ctx.term = ctx.get_powX() / ctx.fact;
            }
            return ctx.sumOfTerms;
        }

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
                Angle = ((double)PI) * ((int)i / 10.0);
                Console.Write("Classlib Sin(");
                Console.Write(Angle);
                Console.Write(")=");
                Console.WriteLine(Result1 = Math.Sin((double)Angle));
                Console.Write("This Version(");
                Console.Write(Angle);
                Console.Write(")=");
                Console.WriteLine(Result2 = (double)mySin(Angle));
                Console.Write("Error is:");
                Console.WriteLine((double)Result1 - (double)Result2);
                Console.WriteLine();
                if (Math.Abs((double)Result1 - (double)Result2) > (double)mistake)
                {
                    Console.WriteLine("ERROR, Versions too far apart!");
                    return 1;
                }
                if (Math.Abs((double)testresults[(int)i] - (double)Result1) > (double)mistake)
                {
                    Console.WriteLine("ERROR, Classlib version isnt right!");
                    return 1;
                }
                if (Math.Abs((double)testresults[(int)i] - (double)Result2) > (double)mistake)
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
