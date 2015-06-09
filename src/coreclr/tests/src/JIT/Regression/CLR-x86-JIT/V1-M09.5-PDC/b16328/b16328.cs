// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;

    class DD
    {
        public static int zero = 0;
        public static int Main()
        {
            try
            {
                int x = 100 / DD.zero;
            }
            catch (DivideByZeroException)
            {
                return 100;
            }
            return 1;
        }
    }
}
