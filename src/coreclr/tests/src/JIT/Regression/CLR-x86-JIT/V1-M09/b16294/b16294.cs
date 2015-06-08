// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
