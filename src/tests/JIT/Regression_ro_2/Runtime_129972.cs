// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_129972;

public static class Runtime_129972
{
    public static bool s_6;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int M()
    {
        sbyte var0  = default(sbyte);
        ulong[] var6  = default(ulong[]);
        sbyte[] var18 = default(sbyte[]);
        bool var5  = true;
        try
        {
            if (var5)
            {
                return 100;
            }

            try
            {
                var0 = var0;
            }
            catch (System.Exception)
            {
                try
                {
                    try
                    {
                        var6[0] = var6[0];
                    }
                    finally
                    {
                        var18[0] = var0;
                    }
                }
                catch (System.Exception) when (s_6)
                {
                }
            }
        }
        catch (System.Exception) when (var5)
        {
        }

        return -1;
    }

    [Fact]
    public static int TestEntryPoint() => M();
}



