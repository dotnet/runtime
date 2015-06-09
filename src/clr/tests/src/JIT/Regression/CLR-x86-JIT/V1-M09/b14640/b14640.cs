// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    internal class repro
    {
        public static int Main(String[] args)
        {
            char b = 'B';

            Char.IsWhiteSpace(b);

            //Console.Write( "Y"+    "Y" );  // This line causes no bug.
            Console.Write("Y" + b + "Y");  // This line causes the bug!  JIT InLiner problem.

            return 100;
        }
    }
}
