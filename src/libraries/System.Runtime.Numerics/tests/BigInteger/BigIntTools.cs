// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BigIntTools
{
    public class Utils
    {
        public static string BuildRandomNumber(int maxdigits, int seed)
        {
            Random random = new Random(seed);

            // Ensure that we have at least 1 digit
            int numDigits = random.Next() % maxdigits + 1;

            StringBuilder randNum = new StringBuilder();

            // We'll make some numbers negative
            while (randNum.Length < numDigits)
            {
                randNum.Append(random.Next().ToString());
            }
            if (random.Next() % 2 == 0)
            {
                return "-" + randNum.ToString().Substring(0, numDigits);
            }
            else
            {
                return randNum.ToString().Substring(0, numDigits);
            }
        }

#if DEBUG
        public static void RunWithFakeThreshold(ref int field, int value, Action action)
        {
            int lastValue = field;
            try
            {
                field = value;
                action();
            }
            finally
            {
                field = lastValue;
            }
        }
#endif
    }
}
