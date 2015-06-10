// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;
    class BB
    {
        public static uint m_uStatic3 = 205u;

        public static void Static1()
        {
            try
            {
                GC.Collect();
            }
            finally
            {
#pragma warning disable 1718
                while (m_uStatic3 == m_uStatic3)
#pragma warning restore
                {
                    throw new Exception();
                }
            }
        }
        static int Main()
        {
            try
            {
                Static1();
            }
            catch (Exception) { return 100; }
            return 1;
        }
    }
}
