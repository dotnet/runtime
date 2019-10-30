// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        public virtual void runTest()
        {
            int iVal1 = 2;
            int iVal2 = 3;
            Console.WriteLine(Math.Min(iVal1, iVal2));
        }
        public static int Main(String[] args)
        {
            Bug oCbTest = new Bug();
            oCbTest.runTest();
            return 100;
        }
    }
    ///// EOF}
}
