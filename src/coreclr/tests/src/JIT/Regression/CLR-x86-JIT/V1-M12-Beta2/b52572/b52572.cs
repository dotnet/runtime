// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        static void Grind() { throw new Exception(); }

        static void Main1()
        {
            int A = 1;
            int B = 0;
            while (B > -1) { Grind(); }
            while (A > 0)
            {
                do
                {
                    while (B != A) Grind();
                } while (B > A);
            }
        }
        static int Main()
        {
            try
            {
                Main1();
                return 101;
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}
