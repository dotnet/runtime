// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SinCalcLib
{
    using System;

    public struct PiVal
    {
        public double Value;
        public PiVal(double v) { Value = v; }
    }

    public class SinCalcLib
    {
        public static object PI = new PiVal(3.1415926535897932384626433832795d);

        public static object mySin(object Angle)
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
    }
}
