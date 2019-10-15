// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace AAAA
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;
    public class CtTest
    {
        private static int iTest = 5;
        public static int Main(String[] args)
        {
            iTest++;
            Console.WriteLine("iTest is " + iTest);
            return 100;
        }
    }
}
