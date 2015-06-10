// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public class BB
    {

        public static String Method3()
        {
            try
            {
                throw new NullReferenceException();
            }
            catch (DivideByZeroException)
            {
                sbyte local2 = (new sbyte[33])[10];
            }
            return null;
        }

        static int Main()
        {
            try
            {
                Method3();
                return 1;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
