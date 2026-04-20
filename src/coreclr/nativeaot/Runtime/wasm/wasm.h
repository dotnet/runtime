// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Define for a small subset of performance-critical FCalls that do not
// have a shadow stack argument. Has no real functional importance and
// serves as simply a marker for such FCalls.
//
#define FCIMPL_NO_SS(_rettype, _name, ...) extern "C" _rettype _name(__VA_ARGS__) {

struct SparseVirtualUnwindFrame
{
    SparseVirtualUnwindFrame* Prev;
    void* UnwindTable;
    size_t UnwindIndex;
};

// TODO-LLVM-Cleanup: replace with with PLATFORM_THREAD_LOCAL after merge.
extern __thread SparseVirtualUnwindFrame* t_pLastSparseVirtualUnwindFrame;

FORCEINLINE void InlinePushSparseVirtualUnwindFrame(SparseVirtualUnwindFrame* pFrame, void* pUnwindTable, size_t unwindIndex)
{
    ASSERT(t_pLastSparseVirtualUnwindFrame < pFrame);
    pFrame->Prev = t_pLastSparseVirtualUnwindFrame;
    pFrame->UnwindTable = pUnwindTable;
    pFrame->UnwindIndex = unwindIndex;

    t_pLastSparseVirtualUnwindFrame = pFrame;
}

FORCEINLINE void InlinePopSparseVirtualUnwindFrame(SparseVirtualUnwindFrame* pFrame)
{
    ASSERT(t_pLastSparseVirtualUnwindFrame != nullptr);
    t_pLastSparseVirtualUnwindFrame = pFrame->Prev;
}
