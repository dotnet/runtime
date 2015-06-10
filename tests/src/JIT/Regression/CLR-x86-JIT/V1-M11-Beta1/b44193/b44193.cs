// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class App
    {
        static int Main()
        {
            bool b;
            int i = 0;
            do
            {
                b = false;
                do
                {
                    b = false;
                    do
                    {
                        b = false;
                        do
                        {
                            b = false;
                            do
                            {
                                b = false;
                                do
                                {
                                    b = false;
                                    do
                                    {
                                        b = false;
                                    } while (i == 1);
                                } while (b);
                            } while (b);
                        } while (b);
                    } while (b);
                } while (b);
            } while (b);
            return 100;
        }
    }
}
