// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct AA
    {
        static void Func()
        {
            int a = 0;
            while (a == 1)
                throw new Exception();
        }
        static int Main()
        {
            try
            {
                Func();
            }
            catch (Exception) { return -1; }
            return 100;
        }
    }
}
