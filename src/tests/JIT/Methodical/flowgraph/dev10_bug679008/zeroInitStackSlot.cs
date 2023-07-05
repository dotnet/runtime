// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The JIT was removing a zero-init, but then emitting an untracked lifetime. 
 * Please run under GCSTRESS = 0x4
 */

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_zeroInitStackSlot_cs
{
internal struct SqlBinary
{
    private byte[] _value;
}

internal class WarehouseResultDatabase : IDisposable
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public WarehouseResultDatabase()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void IDisposable.Dispose()
    {
    }
}

internal delegate bool WarehouseRowVersionQueryDelegate(WarehouseResultDatabase database, SqlBinary waterMark);

public class Repro
{
    [Fact]
    public static int TestEntryPoint()
    {
        new Repro().ProcessResults(Query);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GetProcessingParameters(out SqlBinary binary)
    {
        binary = new SqlBinary();
    }

    private static bool Query(WarehouseResultDatabase database, SqlBinary waterMark)
    {
        return false;
    }

    private void ProcessResults(WarehouseRowVersionQueryDelegate query)
    {
        SqlBinary binary;
        bool moreDataAvailable = true;
        this.GetProcessingParameters(out binary);
        SqlBinary waterMark = binary;
        while (moreDataAvailable)
        {
            bool result = false;
            using (WarehouseResultDatabase database = new WarehouseResultDatabase())
            {
                result = query(database, waterMark);
            }
            moreDataAvailable = result;
        }
    }
}
}
