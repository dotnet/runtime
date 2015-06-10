// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static int Main()
        {
            bool b = true;
            do
            {
                try
                {
                    b = true;
                    do
                    {
                        while (b)
                            return 100;
                    } while (b);
                }
                catch (Exception) { }
                do
                {
                    long local4 = 32L;
                    do
                    {
                    } while (checked(38L >= local4));
                } while (b);
            } while (b);
            return -1;
        }
    }
}
