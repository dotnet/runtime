// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;

    public class Bug
    {
        internal void runTest(Object var2)
        {
            int iTemp = 5;
            Object VarResult = (iTemp);

            if ((int)VarResult == 5)
                Console.WriteLine("Test paSsed");
            else
                Console.WriteLine("Test FAiLED");
        }
        [Fact]
        public static int TestEntryPoint()
        {
            Bug oCbTest = new Bug();
            oCbTest.runTest((3));
            return 100;
        }
    }
}
