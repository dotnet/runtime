// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern LocalAllocTestsFriend
{
}

.assembly LocalAllocTests
{
}

.class public auto ansi beforefieldinit LocAllocTests
        extends [System.Runtime]System.Object
{
    .method private hidebysig instance void Load.LocAllocShouldFailWhenStackNotEmpty_Invalid_Unverifiable.LocallocStackNotEmpty_Validation_LocallocStackNotEmpty() cil managed
    {
        ldnull // pretend there's meaningfull value on stack
        ldc.i4.2 // size parameter for localloc
        localloc
        pop // pop localloc pointer
        pop // pop ldnull
        ret
    }

    .method private hidebysig instance void Load.LocAllocShouldFailWhenSizeIsAbsent_Invalid_Unverifiable.StackUnderflow_Validation_StackUnderflow() cil managed
    {
        localloc
        pop // pop localloc pointer
        ret
    }

    .method private hidebysig instance void Load.LocAllocShouldSuccesWhenSizeIsPresent_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        ldc.i4.2 // size parameter for localloc
        localloc
        pop // pop localloc pointer
        ret
    }

    .method private hidebysig instance void Load.LocAllocInsideTry_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        .try
        {
            ldc.i4.2
            localloc
            pop
            leave.s END
        }
        finally
        {
            endfinally
        }
    END:
        ret
    }

    .method private hidebysig instance void Load.LocAllocInsideFinally_Invalid_Unverifiable.LocallocInHandler_Validation_LocallocInHandler() cil managed
    {
        .try
        {
            leave.s END
        }
        finally
        {
            ldc.i4.2
            localloc
            pop
            endfinally
        }
    END:
        ret
    }

    .method private hidebysig instance void Load.LocAllocInsideFilter_Invalid_Unverifiable.LocallocInHandler_Validation_LocallocInHandler() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            ldc.i4.2
            localloc
            pop
            ldc.i4.0
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method private hidebysig instance void Load.LocAllocWithInt64Size_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        ldc.i8 2
        localloc
        pop
        ret
    }
}
