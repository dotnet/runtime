// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
