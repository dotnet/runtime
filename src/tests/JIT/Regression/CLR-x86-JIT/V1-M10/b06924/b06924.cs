// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace AAAA
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;
    public class CtTest
    {
        private static int iTest = 5;
        [Fact]
        public static int TestEntryPoint()
        {
            iTest++;
            Console.WriteLine("iTest is " + iTest);
            return 100;
        }
    }
}
