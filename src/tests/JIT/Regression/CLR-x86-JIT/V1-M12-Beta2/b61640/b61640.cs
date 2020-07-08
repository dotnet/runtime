// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public class Class1
    {
        struct fooy
        {
            public bool b1;
            public bool b2;
        }

        public static int Main(string[] args)
        {
            foo(true, true);
            return 100;
        }

        public static bool foo(bool b1, bool b2)
        {
            try
            {
                fooy f;
                f.b1 = b1;
                f.b2 = b2;

                if (f.b1)
                {
                    if (!f.b2)
                    {
                        int iRowCount = 4;

                        if (iRowCount > 0)
                        {
                            for (int iCount = 0; iCount < iRowCount; iCount++)
                            {
                                Console.WriteLine("Wow");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            return true;
        }
    }
}
