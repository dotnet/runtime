// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;
    class App
    {
        static int Main()
        {
            bool param3 = false;
            try
            {
                //do anything here...
            }
            finally
            {
                do
                {
                    //and here...
                } while (param3);
            }
            return 100;
        }
    }
}
