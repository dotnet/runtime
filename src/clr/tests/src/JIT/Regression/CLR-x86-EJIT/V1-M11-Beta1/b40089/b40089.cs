// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
