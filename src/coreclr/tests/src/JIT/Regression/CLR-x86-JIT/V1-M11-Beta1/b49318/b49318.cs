// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static void Main1()
        {
            int N = 0;
#pragma warning disable 1718
            while (checked(N >= N))
            {
#pragma warning restore 1718
                throw new Exception();
            }
            try
            {
                return;
            }
            catch (Exception) { }
        }
        static int Main()
        {
            try
            {
                Main1();
            }
            catch (Exception) { }
            return 100;
        }
    }

}
