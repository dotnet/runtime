// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        public virtual void runTest()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            Object o = ((UInt64)rand.Next((int)UInt64.MinValue, Int32.MaxValue));
        }

        public static int Main(String[] args)
        {
            new Bug().runTest();
            return 100;
        }
    }
}
