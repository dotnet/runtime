// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

