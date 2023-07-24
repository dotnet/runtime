// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class unsignedNegative
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Int16 testUI = (-1);

            Console.WriteLine(testUI);
            Console.WriteLine((-1).ToString("d"));

            testUI = Int16.Parse("32535");
            Console.WriteLine(testUI);
            if (testUI < 0) Console.WriteLine("Fail (testUI < 0)");
            if (testUI < (Int16)0) Console.WriteLine("Fail (testUI < (ushort)0)");
            else Console.WriteLine("cast to unsigned short worked");

            testUI = Int16.Parse((-1).ToString("d"));
            Console.WriteLine(testUI);
            return testUI + 101;

        }
    }
}
