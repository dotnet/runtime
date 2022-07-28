// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public unsafe class Runtime_58259
{
    public static int Main()
    {
        M(out _);
        return 100;
    }

    static delegate* unmanaged<out int, void> _f;

    public static void M(out int index)
    {
        if (_f != null)
        {
            _f(out index);
            _f(out index);
        }
        else
        {
            index = 0;
        }
    }
}

