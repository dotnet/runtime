// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public delegate int Callback(DayOfWeek? value);

public static class DelegateCaller
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(Callback callback)
    {
        return callback(null);
    }
}
