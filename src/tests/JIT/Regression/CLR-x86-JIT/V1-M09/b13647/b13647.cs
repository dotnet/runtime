// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        [Fact]
        public static int TestEntryPoint()
        {
            //Console.WriteLine (DateTime.GetNow().ToString());
            Console.WriteLine(DateTime.Now.ToString());
            return 100;
        }
    }

}
