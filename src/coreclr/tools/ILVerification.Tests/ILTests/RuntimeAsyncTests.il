// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly RuntimeAsyncTests
{
}

.class public auto ansi beforefieldinit RuntimeAsyncTestsType
        extends [System.Runtime]System.Object
{
    // Valid async method returning Task<int> with int on stack
    .method public hidebysig instance class [System.Runtime]System.Threading.Tasks.Task`1<int32> AsyncTaskInt_Valid() cil managed async
    {
        ldc.i4.0
        ret
    }

    // Valid async method returning ValueTask<int> with int on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncValueTaskInt_Valid() cil managed async
    {
        ldc.i4.0
        ret
    }

    // Valid async method returning ValueTask<bool> with bool (i4) on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> AsyncValueTaskBool_Valid() cil managed async
    {
        ldc.i4.1
        ret
    }

    // Valid async method returning ValueTask<object> with object on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<object> AsyncValueTaskObject_Valid() cil managed async
    {
        ldnull
        ret
    }

    // Invalid: async method with wrong stack state - empty stack for ValueTask<int>
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncEmptyStack_Invalid_ReturnMissing() cil managed async
    {
        ret
    }

    // Invalid: async method with wrong type on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncWrongType_Invalid_StackUnexpected() cil managed async
    {
        ldnull
        ret
    }

    // Invalid: async method with extra items on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncExtraStack_Invalid_ReturnEmpty() cil managed async
    {
        ldc.i4.0
        ldc.i4.1
        ret
    }

    // Invalid: async method with ValueTask return should have empty stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask AsyncReturnsInt_Invalid_ReturnVoid() cil managed async
    {
        ldc.i4.0
        ret
    }

    // Invalid: method with async flag but returning integer type
    .method public hidebysig instance int32 NonAsyncReturnType_Invalid_StackUnexpected() cil managed async
    {
        ldc.i4.0
        ret
    }

    // Valid: async method returning Task with nothing on stack
    .method public hidebysig instance class [System.Runtime]System.Threading.Tasks.Task AsyncTask_Valid() cil managed async
    {
        ret
    }
    
    // Valid: async method returning ValueTask with nothing on stack
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask AsyncValueTask_Valid() cil managed async
    {
        ret
    }

    // Invalid: async method returning from try block (not allowed)
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncReturnFromTry_Invalid_ReturnFromTry() cil managed async
    {
        .try
        {
            ldc.i4.0
            ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s lbl_ret
        }

    lbl_ret:
        ldc.i4.1
        ret
    }

    // Invalid: async method returning from catch block (not allowed)
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncReturnFromCatch_Invalid_ReturnFromHandler() cil managed async
    {
        .try
        {
            leave.s lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            ldc.i4.0
            ret
        }

    lbl_ret:
        ldc.i4.1
        ret
    }

    // Invalid: async method returning from filter (still not allowed)
    .method public hidebysig instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<int32> AsyncReturnFromFilter_Invalid_ReturnFromFilter() cil managed async
    {
        .try
        {
            leave.s lbl_ret
        }
        filter
        {
            pop
            ldc.i4.0
            ret

            endfilter
        }
        {
            pop
            leave.s lbl_ret
        }

    lbl_ret:
        ldc.i4.1
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
