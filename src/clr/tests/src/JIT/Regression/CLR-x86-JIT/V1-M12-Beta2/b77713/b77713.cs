// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public class BB
    {
        public static void Main1()
        {
            bool local2 = false;
            try
            {
                if (local2)
                    return;
            }
            finally
            {
                throw new Exception();
            }
        }
        public static int Main()
        {
            try
            {
                Main1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
