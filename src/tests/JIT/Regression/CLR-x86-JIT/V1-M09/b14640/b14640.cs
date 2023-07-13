// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class repro
    {
        [Fact]
        public static int TestEntryPoint()
        {
            char b = 'B';

            Char.IsWhiteSpace(b);

            //Console.Write( "Y"+    "Y" );  // This line causes no bug.
            Console.Write("Y" + b + "Y");  // This line causes the bug!  JIT InLiner problem.

            return 100;
        }
    }
}
