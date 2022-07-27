// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

public unsafe class Runtime_72926
{
    public static int Main()
    {
        if (CallForLongAsVector64() != Vector64<double>.Zero)
        {
            return 101;
        }
        if (CallForDoubleAsVector64() != Vector64<double>.Zero)
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForLongAsVector64()
    {
        long value = 0;
        return *(Vector64<double>*)&value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForDoubleAsVector64()
    {
        double value = 0;
        return *(Vector64<double>*)&value;
    }
}
