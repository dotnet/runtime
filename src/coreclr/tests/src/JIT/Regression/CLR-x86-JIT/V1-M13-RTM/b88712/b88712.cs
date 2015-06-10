// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public struct AA
{
    public static void Static5()
    {
        float a = 125.0f;
        a += (a *= 60.0f);
    }
    static int Main()
    {
        Static5();
        return 100;
    }
}
