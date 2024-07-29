// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/*
 * JIT64 bug The bug is triggered by a pattern of loops over arrays of medium sized structs (between 4
 * and ~32 bytes), but those are not the only cases that might hit it, just the easiest to describe 
 * (and maybe most likely?).  In this case the last part of the trigger was that on array was offset 
 * from the other:
 *                       batch[i] = keys[batchIndex + i];
 * OsrApplyReductions and OsrGroupIVsByStride had a very similar loop, but with one notable difference.
 * The former had a call to OsrRemoveGCCandidates, but the later did not.  If OsrRemvoeGCCandidates 
 * walked across a multiply, it would change the stride, which would make a given candidate no longer 
 * fit in the stride group it was placed in.  This bug has existed since 2003 when ltaylors first wrote
 * these routines.  I believe the fix is to add the call to OsrGroupIVsByStride so the loops match. 
 */

using System;
using Xunit;

struct VT
{
    public double vt1;
    public double vt2;
    public double vt3;

    public VT(double d1, double d2, double d3)
    {
        vt1 = d1; vt2 = d2; vt3 = d3;
    }

}


public class DblArray3
{

    // instance field of valuetype
    static void f4(VT[] keys, uint m_ReadMultipleMaxBatchSize)
    {

        // Create first batch.
        // Should have incoming m_ReadMultipleMaxBatchSize less than keys.length
        VT[] batch = keys;
        if (keys.Length > m_ReadMultipleMaxBatchSize)
        {
            batch = new VT[m_ReadMultipleMaxBatchSize];
        }

        int batchIndex = 0;
        do
        {
            if (batch != keys)
            {
                // Multiple batches case.

                // If new batch should be smaller, create a new array.
                // Otherwise, reuse the old one.
                if (keys.Length < batchIndex + m_ReadMultipleMaxBatchSize)
                {
                    batch = new VT[keys.Length - batchIndex];
                }

                // Copy keys to the batch array.
                for (int i = 0; i < batch.Length; i++)
                {
                    batch[i] = keys[batchIndex + i];
                }
            }

            // Process the current batch and move to the next one.
            batchIndex += batch.Length;
        }
        while (batchIndex < keys.Length);
    }



    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            VT[] keys = new VT[10];
            for (uint ii = 0; ii < keys.Length; ii++)
                keys[ii] = new VT(0xf, 0x4, 0xe);
            f4(keys, 5);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("FAILED");
            Console.WriteLine();
            Console.WriteLine(@"// 
// The bug is triggered by a pattern of loops over arrays of medium sized structs (between 4 and ~32 bytes), but those are not the only cases that might hit it, just the easiest to describe (and maybe most likely?).  In this case the last part of the trigger was that on array was offset from the other:
//                        batch[i] = keys[batchIndex + i];"
                            );
            return -1;
        }
        Console.WriteLine("PASSED");
        return 100;
    }

}
