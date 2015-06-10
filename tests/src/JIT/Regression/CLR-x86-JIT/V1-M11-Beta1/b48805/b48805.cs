// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct AA
    {
        static int Main()
        {
            bool[] ab = new bool[2];
            try
            {
                do
                {
                    continue;
                } while (ab[3]);
            }
            catch (IndexOutOfRangeException) { }
            catch (Exception) { }
            return 100;
        }
    }
}
