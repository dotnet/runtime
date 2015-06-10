// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    public class AA
    {
        public static Array Method1()
        {
            Array[] arr = new Array[1];
            try
            {
                return arr[0];
            }
            finally
            {
                throw new Exception();
            }
            return arr[0];
        }
        public static int Main()
        {
            try
            {
                Method1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
