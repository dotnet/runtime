// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * The JIT was removing a zero-init, but then emitting an untracked lifetime. 
 * Please run under GCSTRESS = 0x4
 */

using System;
using System.Runtime.CompilerServices;

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

internal class Repro
{
    private static int Main()
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
