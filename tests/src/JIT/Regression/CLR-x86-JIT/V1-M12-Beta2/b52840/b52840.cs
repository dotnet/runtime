// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    using System;

    internal class App
    {
        private static byte s_b;
        private static void Func(ref String s) { }
        private static void Main1()
        {
            Func(ref __refvalue(__makeref(s_b), String[])[0]);
        }
        private static int Main()
        {
            try
            {
                Main1();
                return 1;
            }
            catch (InvalidCastException)
            {
                return 100;
            }
        }
    }
}
