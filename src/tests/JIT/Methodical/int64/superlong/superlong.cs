// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_superlong_cs
{
    public struct superlong
    {
        private ulong _lo,_hi;

        private void Assign(superlong v) { _hi = v._hi; _lo = v._lo; }

        private static superlong add(superlong op1, superlong op2)
        {
            checked
            {
                superlong result;
                result._hi = op1._hi + op2._hi;
                try
                {
                    result._lo = op1._lo + op2._lo;
                }
                catch (OverflowException)
                {
                    result._lo = unchecked(op1._lo + op2._lo);
                    result._hi++;
                }
                return result;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            superlong v1;
            superlong v2;
            v1._hi = 0x8000000000000000;
            v1._lo = 0x0000000000000000;
            v2._hi = 0x7fffffffffffffff;
            v2._lo = 0xffffffffffffffff;
            superlong sum = superlong.add(v1, v2);
            if (sum._hi != 0xffffffffffffffff || sum._lo != 0xffffffffffffffff)
            {
                Console.WriteLine("Failed.");
                return 1;
            }
            v1._hi = 0x8000000000000000;
            v1._lo = 0x0000000000000000;
            v2.Assign(v1);
            try
            {
                sum = superlong.add(v1, v2);
            }
            catch (OverflowException)
            {
                v1._hi = 0x1234567876543210;
                v1._lo = 0xfdcba98789abcdef;
                v2._hi = 0xedcba98789abcdee;
                v2._lo = 0x1234567876543210;
                sum = superlong.add(v1, v2);
                if (sum._hi != 0xffffffffffffffff || sum._lo != 0x0fffffffffffffff)
                {
                    Console.WriteLine("Failed (3).");
                    return 1;
                }
                Console.WriteLine("Passed!");
                return 100;
            }
            Console.WriteLine("Failed (2).");
            return 1;
        }
    }
}
