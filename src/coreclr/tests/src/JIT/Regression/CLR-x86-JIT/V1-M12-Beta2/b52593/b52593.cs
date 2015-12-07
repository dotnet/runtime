// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    using System;
    internal class App
    {
        private static void Method1(TypedReference param1, object obj) { }
        private static int Main()
        {
            int[] an = { 0 };
            Method1(__makeref(an[0]), 1);
            return 100;
        }
    }
}
