// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct BB
    {
        bool m_b;
        static void Static1(BB param3, ref bool param5) { }
        static int Main()
        {
            Static1(new BB(), ref new BB[] { new BB() }[0].m_b);
            return 100;
        }
    }
}
