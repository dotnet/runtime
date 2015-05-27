// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CGRecurse
{
    public class RecursiveAAC
    {
        public static string ActualResult;

        public static int cntA = 0;

        public static int cntB = 0;

        public static int cntC = 0;

        public static int Main()
        {
            string ExpectedResult = "AAC";
            int retVal = 1;
            A();
            if (ExpectedResult.Equals(ActualResult))
            {
                Console.WriteLine("Test SUCCESS");
                retVal = 100;
            }
            return retVal;
        }

        public static void C()
        {
            ActualResult = (ActualResult + "C");
            return;
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
