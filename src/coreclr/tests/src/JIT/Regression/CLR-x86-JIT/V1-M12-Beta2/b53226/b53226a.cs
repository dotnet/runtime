// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    using System;

    internal class App
    {
        private static void Func(TypedReference tr) { }

        private static int Main()
        {
            bool b = false;
            TypedReference tr = __makeref(b);
            Func(b ? tr : __makeref(b));
            return 100;
        }
    }
}
