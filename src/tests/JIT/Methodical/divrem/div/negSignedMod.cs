// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

internal class Test
{
    private static int Main()
    {
        Console.WriteLine(TimeSpan.FromTicks(-2567240321185713219).Seconds);
        if (TimeSpan.FromTicks(-2567240321185713219).Seconds != -38)
            return 101;
        return 100;
    }
}

