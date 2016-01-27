// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CGRecurse
{
    public class RecursiveAAA
    {
        public static string ActualResult;

        public static int cntA = 0;

        public static int cntB = 0;

        public static int cntC = 0;

        public static int Main()
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

        public static void A()
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
