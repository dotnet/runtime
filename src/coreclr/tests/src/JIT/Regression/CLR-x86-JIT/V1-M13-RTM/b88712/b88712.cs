// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
