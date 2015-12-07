// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    using System;

    internal class App
    {
        private static int Main1()
        {
            bool b = false;
            TypedReference tr = __makeref(b);
            byte bb = __refvalue((b ? __makeref(b) : tr), byte);
            return 0;
        }
        private static int Main()
        {
            try
            {
                return Main1();
            }
            catch (InvalidCastException)
            {
                return 100;
            }
        }
    }
}
