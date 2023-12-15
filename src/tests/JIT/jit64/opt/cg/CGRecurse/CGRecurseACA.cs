// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace CGRecurse
{
    public class RecursiveACA
    {
        public static string ActualResult;

        public static int cntA = 0;

        public static int cntB = 0;

        public static int cntC = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            string ExpectedResult = "ACA";
            int retVal = 1;
            A();
            if (ExpectedResult.Equals(ActualResult))
            {
                Console.WriteLine("Test SUCCESS");
                retVal = 100;
            }
            return retVal;
        }

        private static void C()
        {
            ActualResult = (ActualResult + "C");
            if ((cntA == 1))
            {
                cntA = 0;
                return;
            }
            cntA = (cntA + 1);
            A();
            return;
        }

        internal static void A()
        {
            ActualResult = (ActualResult + "A");
            if ((cntC == 1))
            {
                cntC = 0;
                return;
            }
            cntC = (cntC + 1);
            C();
            return;
        }
    }
}
