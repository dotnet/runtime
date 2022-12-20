// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The JIT generates incorrect code for occasional uses of a struct parameter that contains a single field that is a reference type.
 * This can cause GC holes, GC overreporting, crashes, corrupt data.
 * Fix is to not undo a register allocation as worthless, but rather to just force it to spill (and not spill when the spill would be redundant).
 */

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_singleRefField_cs
{
public struct MB8
{
    public object foo;
}

public class Repro
{
    private int _state = 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int Use(MB8 mb8, string s)
    {
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int Use(MB8 mb8, int i, string s)
    {
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Use(MB8 mb8)
    {
    }

    private Repro[] _preExecutionDelegates = new Repro[0];

    private int Bug(MB8 mb8, string V_2)
    {
        if (V_2 == null)
        {
            throw new ArgumentNullException("V_2");
        }
        _state = 2;
        int loc0 = 0;
        foreach (Repro loc1 in _preExecutionDelegates)
        {
            if (loc1 != null)
            {
                loc0 = loc1.Use(mb8, V_2);
            }
            if (loc0 != 0)
                break;
        }
        if (loc0 == 1)
        {
            Use(mb8); // No retval
        }
        else if (loc0 == 2)
        {
            Use(mb8, 0, V_2); // Pop
        }
        return loc0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        new Repro().Bug(new MB8(), "Test");
        return 100;
    }
}
}
