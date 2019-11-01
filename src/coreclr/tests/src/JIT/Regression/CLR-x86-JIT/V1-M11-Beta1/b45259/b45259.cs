// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        static int Main()
        {
            try
            {
                // do what you like here
            }
            catch (Exception)
            {
                float[] af = new float[7];
                af[0] = af[1];
            }
            return 100;
        }
    }

}
