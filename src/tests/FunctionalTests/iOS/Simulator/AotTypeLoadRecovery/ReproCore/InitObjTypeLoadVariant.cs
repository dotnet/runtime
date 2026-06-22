// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using ReproContracts;

namespace ReproCore;

public static class InitObjTypeLoadHarness
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        if (Environment.TickCount == int.MinValue)
        {
            DirectInitObj();
            InitObjInsideExceptionClause();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DirectInitObj()
    {
        MissingInitObjValue value = default;
        Consume(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitObjInsideExceptionClause()
    {
        try
        {
            MissingInitObjValue value = default;
            Consume(value);
        }
        catch (Exception ex) when (ex.GetHashCode() == int.MinValue)
        {
            GC.KeepAlive(ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value)
    {
        if (Environment.TickCount == int.MinValue)
            GC.KeepAlive(value);
    }
}
