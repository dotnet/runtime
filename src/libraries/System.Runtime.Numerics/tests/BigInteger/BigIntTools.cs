// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
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

        public static void RunWithFakeThreshold(in int field, int value, Action action)
        {
            int lastValue = field;

            // This is tricky hack. If only DEBUG build is targeted,
            // `RunWithFakeThreshold(ref int field, int value, Action action action)` would be more appropriate.
            // However, in RELEASE build, the code should be like this.
            // This is because const fields cannot be passed as ref arguments.
            // When a const field is passed to the in argument, a local
            // variable reference is implicitly passed to the in argument
            // so that the original const field value is never rewritten.
            ref int reference = ref Unsafe.AsRef(in field);

            try
            {
                reference = value;
                if (field != value)
                    return; // In release build, the test will be skipped.
                action();
            }
            finally
            {
                reference = lastValue;
            }
        }
    }
}
