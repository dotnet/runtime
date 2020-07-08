// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        static int Main()
        {
            int[] an = new int[2];
            bool b = false;
            try
            {
                //do anything here...
            }
            catch (Exception)
            {
                while (b)
                {
                    an[0] = 1;
                }
            }
            while (b) { }
            return 100;
        }
    }
}
