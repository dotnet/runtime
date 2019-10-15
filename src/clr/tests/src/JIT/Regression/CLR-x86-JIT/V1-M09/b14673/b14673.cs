// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
