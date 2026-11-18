// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace TernaryOperatorOptimization
{
    public class Program
    {
        private static bool caughtException = false;

        [Fact]
        public static int TestEntryPoint()
        {
            Console.WriteLine("Regression testcase for devdiv 106272 - Invalid JIT optimization");
            Console.WriteLine("with ternary/conditional operator (?:) in release builds");
            Console.WriteLine("This testcase needs to be build as retail version, /o+");

            try
            {
                TestIt();
            }
            catch (InvalidCastException)
            {
                caughtException = true;
            }

            Console.WriteLine();

            if (caughtException)
            {
                Console.WriteLine("!!!!!   TEST PASSED  !!!!!");
                return 100;
            }
            else
            {
                Console.WriteLine("!!!!!   TEST FAILED  !!!!!");
                return 101;
            }
        }

        private static void TestIt()
        {
            string o = SideEffectMethod() ? (string)null : (string)null;
            Console.WriteLine("o: " + o);

            string o2 = ((int)(object)"this should always throw!") == 5 ? (string)null : (string)null;
            Console.WriteLine("o2: Previous source line should have thrown an InvalidCastException!!!" + o2);
        }

        private static bool SideEffectMethod()
        {
            Console.WriteLine("This should be called!");
            return false;
        }
    }
}

