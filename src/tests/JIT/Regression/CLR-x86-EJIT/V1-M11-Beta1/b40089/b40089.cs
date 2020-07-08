// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct BB
    {
        public BB Method1(float param2)
        {
            return new BB();
        }

        static int Main()
        {
            new BB().Method1(0.0f);
            return 100;
        }
    }
}
