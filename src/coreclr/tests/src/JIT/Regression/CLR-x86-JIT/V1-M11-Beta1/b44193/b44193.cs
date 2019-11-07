// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
