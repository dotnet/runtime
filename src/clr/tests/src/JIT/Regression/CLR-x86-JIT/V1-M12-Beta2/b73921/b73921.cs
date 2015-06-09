// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public class BB
    {
        public static bool TRUE() { throw new Exception(); }
        public static int Main1()
        {
            while (TRUE()) { }
            try
            {
                return 109;
            }
            catch (Exception) { }
            return 103;
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
