// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class foo
{

    public static int Main()
    {
        int i = 3;

        switch (i)
        {
            case 0:
                return 101;
            case 1:
                return 102;
            case 2:
                return 103;
            case 3:
                return 100;
            case 4:
                return 104;
        }

        return 100;
    }

}