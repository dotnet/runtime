// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;
    public class AA
    {
        static void Method1(bool param1)
        {
            long local5 = 0;
            do
            {
            } while (param1);

            try
            {
                throw new Exception();
            }
            finally
            {
                while (param1)
                {
                    local5 -= local5;
                }
            }
        }
        static int Main()
        {
            try
            {
                Method1(false);
                return 101;
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}
