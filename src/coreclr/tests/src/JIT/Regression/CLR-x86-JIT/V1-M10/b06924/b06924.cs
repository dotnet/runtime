// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
