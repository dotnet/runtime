// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_129971;

public struct S0
{
    public bool F0;
}

public static class Runtime_129971
{
    public static bool s_9;
    public static bool s_flag;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint M4() => 0u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int M(int[] arr)
    {
        S0 var4 = default(S0);
        try
        {
            if (var4.F0)
            {
                try
                {
                    M4();
                }
                finally
                {
                    try
                    {
                        M4();
                        arr[0] = 1;
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }
        catch (System.Exception) when (s_flag)
        {
        }

        return 100;
    }

    [Fact]
    public static int TestEntryPoint() => M(new int[1]);
}



