

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    public class AA
    {
        public static void Main1()
        {
            try
            {
                bool local5 = true;
                if (local5)
                    throw new Exception();
                do
                {
                    while (local5)
                    {
                        return;
                    }
                } while (local5);
            }
            finally { }
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
