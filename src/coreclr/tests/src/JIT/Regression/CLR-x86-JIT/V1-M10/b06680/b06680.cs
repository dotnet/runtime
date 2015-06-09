// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
