// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
