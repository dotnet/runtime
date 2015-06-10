// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct AA
    {
        void Method1() { }

        static void Main1()
        {
            (new AA[137])[101].Method1();
            throw new DivideByZeroException();
        }
        static int Main()
        {
            try
            {
                Main1();
                return 1;
            }
            catch (DivideByZeroException)
            {
                return 100;
            }
        }
    }
}
