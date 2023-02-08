// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class AppStarter
    {
        private static int n = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            n = 1;
            Console.WriteLine("n = " + n);
            return 100;
        }
    };
};
