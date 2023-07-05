// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace CGRecurse
{
    public class RecursiveAAA
    {
        public static string ActualResult;

        public static int cntA = 0;

        public static int cntB = 0;

        public static int cntC = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            string ExpectedResult = "AAA";
            int retVal = 1;
            A();
            if (ExpectedResult.Equals(ActualResult))
            {
                Console.WriteLine("Test SUCCESS");
                retVal = 100;
            }
            return retVal;
        }

        internal static void A()
        {
            ActualResult = (ActualResult + "A");
            if ((cntA == 1))
            {
                cntA = 0;
                return;
            }
            cntA = (cntA + 1);
            A();
            if ((cntA == 1))
            {
                cntA = 0;
                return;
            }
            cntA = (cntA + 1);
            A();
            return;
        }
    }
}
