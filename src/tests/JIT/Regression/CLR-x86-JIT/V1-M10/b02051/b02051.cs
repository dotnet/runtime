// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;
    public class JITcrash
    {
        [Fact]
        public static int TestEntryPoint()
        {
            UInt32 x = (0xFFFFFFFF);
            Int64 y = x;

            //	just added few cases of WriteLine
            Console.WriteLine("Running test");
            Console.WriteLine("x = " + x);
            Console.WriteLine("x = " + x + ".");
            Console.WriteLine("x = " + x + " y = " + y + ".");
            Console.WriteLine("Test passed.");
            return 100;
        }
    }
}
