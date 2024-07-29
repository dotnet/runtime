// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class jitcast
    {
        public static UInt16 f()
        {
            int i = (Int32)UInt16.MaxValue;
            return (UInt16)i;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // Looks like what's happening is the JIT sees I'm casting an int
            // to an unsigned short then back to an int and it messes up by
            // sticking in sign extension.
            int a = (Int32)f();
            Console.WriteLine("a was: " + a);
            if (a == -1)
                Console.WriteLine("\n\tBug #1: a shouldn't be -1!  Widened from a unsigned short to an int shouldn't give a negative!\n");
            else
            {
                Console.WriteLine("pass");
                return 100;
            }
            return 1;
        }
    }
}
