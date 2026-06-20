// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using ReproCore;

internal static class AotTypeLoadRecoveryProbe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        AotTypeLoadRecoveryHarness.Run();
    }
}
