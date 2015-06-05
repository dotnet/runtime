// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private static readonly TypeInfo s_internalCalculator =
            typeof(BigInteger).GetTypeInfo()
                              .Assembly
                              .GetType("System.Numerics.BigIntegerCalculator")
                              .GetTypeInfo();

        public static void RunWithFakeThreshold(string name, int value, Action action)
        {
            FieldInfo field = s_internalCalculator.GetDeclaredField(name);
            int lastValue = (int)field.GetValue(null);
            field.SetValue(null, value);
            try
            {
                action();
            }
            finally
            {
                field.SetValue(null, lastValue);
            }
        }
    }
}

