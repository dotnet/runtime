// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
