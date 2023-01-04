// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
public struct AA
{
    public static int Main()
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
