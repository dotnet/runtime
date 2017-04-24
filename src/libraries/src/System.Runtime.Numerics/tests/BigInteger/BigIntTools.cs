// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private static TypeInfo InternalCalculator
        {
            get
            {
                if (s_lazyInternalCalculator == null)
                {
                    Type t = typeof(BigInteger).Assembly.GetType("System.Numerics.BigIntegerCalculator");
                    if (t != null)
                    {
                        s_lazyInternalCalculator = t.GetTypeInfo();
                    }
                }
                return s_lazyInternalCalculator;
            }
        }

        private static volatile TypeInfo s_lazyInternalCalculator;

        public static void RunWithFakeThreshold(string name, int value, Action action)
        {
            TypeInfo internalCalculator = InternalCalculator;
            if (internalCalculator == null)
                return; // Internal frame types are not reflectable on AoT platforms. Skip the test.

            FieldInfo field = internalCalculator.GetDeclaredField(name);
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

