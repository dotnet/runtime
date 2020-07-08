// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
