// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests that cpblk/initblk importation sastifies ordering constraints.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly 'dynblk_order' {}

.typedef [System.Runtime]System.OverflowException as OverflowException
.typedef [System.Runtime]System.IndexOutOfRangeException as IndexOutOfRangeException

.class public DynBlkOrder extends [System.Runtime]System.Object
{
    .method public static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .locals (void* dst, void*[] srcArr, int32 srcIdx, int32 size)

        ldloca dst
        ldloca srcArr
        ldloca srcIdx
        ldloca size
        call void DynBlkOrder::InitValues(void*&, void*[]&, int32&, int32&)

        .try
        {
            ldloc dst
            ldloc srcArr
            ldloc srcIdx
            ldelem.i
            ldloc size
            conv.ovf.u
            cpblk

            leave FAIL
        }
        catch IndexOutOfRangeException
        {
            leave INITBLK
        }
        catch OverflowException
        {
            leave FAIL1
        }

    INITBLK:
        .try
        {
            ldloc dst
            ldloc srcArr
            ldloc srcIdx
            ldelem.i
            conv.i4
            ldloc size
            conv.ovf.u
            initblk
            
            leave FAIL
        }
        catch IndexOutOfRangeException
        {
            leave SUCCESS
        }
        catch OverflowException
        {
            leave FAIL2
        }

    SUCCESS:
        ldc.i4 100
        ret

    FAIL:
        ldc.i4 99
        ret

    FAIL1:
        ldc.i4 101
        ret
        
    FAIL2:
        ldc.i4 102
        ret
    }

    .method static void InitValues(void*& pSrc, void*[]& pSrcArr, int32& pSrcIdx, int32& pSize) noinlining
    {
        ldarg pSrc
        ldc.i4 0
        conv.u
        stind.i

        ldarg pSrcArr
        ldc.i4 0
        newarr int32
        stind.ref

        ldarg pSrcIdx
        ldc.i4 1
        stind.i4

        ldarg pSize
        ldc.i4 -1
        stind.i4
        
        ret
    }
}
