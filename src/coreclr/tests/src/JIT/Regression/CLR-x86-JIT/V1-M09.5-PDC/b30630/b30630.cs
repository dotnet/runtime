// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
