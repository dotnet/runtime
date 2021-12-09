// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

//
// void CallingConventionConverter_ReturnVoidReturnThunk()
//
LEAF_ENTRY CallingConventionConverter_ReturnVoidReturnThunk, _TEXT
        ret
LEAF_END CallingConventionConverter_ReturnVoidReturnThunk, _TEXT

//
// int CallingConventionConverter_ReturnIntegerReturnThunk(int)
//
LEAF_ENTRY CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT
        // UNIXTODO: Implement this function
        int 3
LEAF_END CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT

//
// Note: The "__jmpstub__" prefix is used to indicate to debugger
// that it must step-through this stub when it encounters it while
// stepping.
//

// __jmpstub__CallingConventionConverter_CommonCallingStub
//
//
// struct CallingConventionConverter_CommonCallingStub_PointerData
// {
//     void *ManagedCallConverterThunk;
//     void *UniversalThunk;
// }
//
// struct CommonCallingStubInputData
// {
//     ULONG_PTR CallingConventionId;
//     CallingConventionConverter_CommonCallingStub_PointerData *commonData;
// }
//
// r10 - Points at CommonCallingStubInputData
//
//
LEAF_ENTRY __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT
        // UNIXTODO: Implement this function
        int 3
LEAF_END __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT

//
// void CallingConventionConverter_GetStubs(IntPtr *returnVoidStub, IntPtr *returnIntegerStub, IntPtr *commonStub)
//
LEAF_ENTRY CallingConventionConverter_GetStubs, _TEXT
        // UNIXTODO: Implement this function
        int 3
LEAF_END CallingConventionConverter_GetStubs, _TEXT
