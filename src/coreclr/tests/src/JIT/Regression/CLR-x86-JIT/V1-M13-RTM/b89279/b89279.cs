// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class AA
{
    public static void Static5(int param1)
    {
        if (param1 != 0)
        {
            if (param1 == 1)
                return;
        }
#pragma warning disable 1717
        param1 = param1;
#pragma warning restore 1717
    }
    static int Main() { Static5(0); return 100; }
}
