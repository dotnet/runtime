// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class Runtime_66269
{
    public static int Main()
    {
        return Problem(1) == 2 ? 100 : 101;
    }

    private static ushort Problem(ushort arg1)
    {
        arg1 += arg1;

        return arg1;
    }
}

