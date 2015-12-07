// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QQ
{
    using System;

    internal class AA
    {
        private static void Test(TypedReference arg, String result) { }
        private static int Main()
        {
            DateTime[] t = new DateTime[200];
            t[1] = new DateTime(100, 10, 1);
            Test(__makeref(t[1]), t[1].ToString());
            return 100;
        }
    }
}
