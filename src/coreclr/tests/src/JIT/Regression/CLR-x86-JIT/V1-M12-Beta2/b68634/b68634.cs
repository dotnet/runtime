// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public class AA
    {
        public static void Main1()
        {
            bool local2 = true;
            while (local2) { throw new Exception(); }
            while (local2)
            {
                bool[] local3 = (new bool[119]);
                //for (; local2; new AA[]{  }) - not a valid statement, see VS7 #244656.
                for (; local2; new AA())
                {
                }
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
