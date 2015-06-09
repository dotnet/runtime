// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
