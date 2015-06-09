// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
struct AA
{
    static int Main()
    {
        bool f = false;
        if (f) f = false;
        else
        {
            int n = 0;
            while (f)
            {
                do { } while (f);
            }
        }
        return 100;
    }
}
