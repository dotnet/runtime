// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

if (Environment.GetEnvironmentVariable("Never") == "Ever")
{
    Console.WriteLine(IncrementalFixture.GetValue(1));
}

return 100;

static class IncrementalFixture
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetValue(int value) => value + 0x61234567;
}
