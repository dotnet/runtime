// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

#include "../wasm.h"

// This variable is defined here in native code because:
//  1) Unmanaged thread locals are currently much more efficient than managed ones.
//  2) Push/pop functions do not need the shadow stack argument.
//
// TODO-LLVM-Cleanup: replace with with PLATFORM_THREAD_LOCAL after merge.
__thread SparseVirtualUnwindFrame* t_pLastSparseVirtualUnwindFrame = nullptr;

FCIMPL_NO_SS(void, RhpPushSparseVirtualUnwindFrame, SparseVirtualUnwindFrame* pFrame, void* pUnwindTable, size_t unwindIndex)
{
    InlinePushSparseVirtualUnwindFrame(pFrame, pUnwindTable, unwindIndex);
}
FCIMPLEND

FCIMPL_NO_SS(void, RhpPopSparseVirtualUnwindFrame)
{
    InlinePopSparseVirtualUnwindFrame(t_pLastSparseVirtualUnwindFrame);
}
FCIMPLEND

FCIMPL0(SparseVirtualUnwindFrame**, RhpGetLastSparseVirtualUnwindFrameRef)
{
    return &t_pLastSparseVirtualUnwindFrame;
}
FCIMPLEND

FCIMPL0(void*, RhpGetLastPreciseVirtualUnwindFrame)
{
    return static_cast<uint8_t*>(pShadowStack) - sizeof(void*);
}
FCIMPLEND
