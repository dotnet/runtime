// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
public struct AA
{
    public static int Main()
    {
        bool local3 = false;
        do
        {
            while (local3) { }
        } while (local3);
        return 100;
    }
}
