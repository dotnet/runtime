// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;

    public class Bug
    {
        public void runTest(Object var2)
        {
            int iTemp = 5;
            Object VarResult = (iTemp);

            if ((int)VarResult == 5)
                Console.WriteLine("Test paSsed");
            else
                Console.WriteLine("Test FAiLED");
        }
        public static int Main(String[] args)
        {
            Bug oCbTest = new Bug();
            oCbTest.runTest((3));
            return 100;
        }
    }
}
