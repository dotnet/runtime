// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
