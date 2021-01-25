// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    class AppStarter
    {
        private static int n = 0;

        public static int Main(String[] args)
        {
            n = 1;
            Console.WriteLine("n = " + n);
            return 100;
        }
    };
};
