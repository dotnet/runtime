// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class Program
{
    static int Main()
    {
        var matrix = new double[128 * 128];
        
        GC.Collect(GC.MaxGeneration);

        GC.KeepAlive(matrix);
        return 100;
    }
}

