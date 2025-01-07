// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class HeadMergeException
{
    [Fact]
    public static int TestEntryPoint()
    {
        int local = 100;
        try
        {
            // Ensure head merging does not move the write to 'local' out here.
            if (Throws())
            {
                local = local + 1;
                local *= 2;
            }
            else
            {
                local = local + 1;
                local *= 3;
            }
        }
        catch (Exception)
        {
        }

        return local;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Throws() => throw new Exception();
}
