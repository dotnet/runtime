// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MemorySsaTests
{
    private static int _intStatic;

    [Fact]
    public static int TestEntryPoint()
    {
        return ProblemWithHandlerPhis(new int[0]) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithHandlerPhis(int[] x)
    {
        _intStatic = 2;

        try
        {
            _intStatic = 1;
            _ = x[0];
            _intStatic = 2;
        }        
        catch (Exception)
        {
            // Memory PHIs in handlers must consider all intermediate states
            // that might arise from stores between throwing expressions.
            if (_intStatic == 2)
            {
                return true;
            }
        }

        return false;
    }
}
