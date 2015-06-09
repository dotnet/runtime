// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
