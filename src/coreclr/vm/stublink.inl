// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// StubLink.inl
//
// Defines inline functions for StubLinker
//


#ifndef __STUBLINK_INL__
#define __STUBLINK_INL__

#include "stublink.h"
#include "eeconfig.h"
#include "safemath.h"


#ifdef STUBLINKER_GENERATES_UNWIND_INFO

inline //static
SIZE_T StubUnwindInfoHeader::ComputeAlignedSize(UINT nUnwindInfoSize)
{
    LIMITED_METHOD_CONTRACT;

    return ALIGN_UP(  FIELD_OFFSET(StubUnwindInfoHeader, FunctionEntry)
                    + nUnwindInfoSize
                    + sizeof(StubUnwindInfoHeaderSuffix)
                    , sizeof(void*));
}


#ifndef DACCESS_COMPILE

inline
void StubUnwindInfoHeader::Init ()
{
    LIMITED_METHOD_CONTRACT;

    pNext = (StubUnwindInfoHeader*)(SIZE_T)1;
}


inline
bool StubUnwindInfoHeader::IsRegistered ()
{
    LIMITED_METHOD_CONTRACT;

    return pNext != (StubUnwindInfoHeader*)(SIZE_T)1;
}

#endif // #ifndef DACCESS_COMPILE

#endif // STUBLINKER_GENERATES_UNWIND_INFO


inline
void StubLinker::Push(UINT size)
{
    LIMITED_METHOD_CONTRACT;

    ClrSafeInt<SHORT> stackSize(m_stackSize);
    _ASSERTE(FitsIn<SHORT>(size));
    SHORT sSize = static_cast<SHORT>(size);
    stackSize += sSize;
    _ASSERTE(!stackSize.IsOverflow());
    m_stackSize = stackSize.Value();
    UnwindAllocStack(sSize);
}


inline
void StubLinker::Pop(UINT size)
{
    LIMITED_METHOD_CONTRACT;

    ClrSafeInt<SHORT> stackSize(m_stackSize);
    _ASSERTE(FitsIn<SHORT>(size));
    stackSize = stackSize - ClrSafeInt<SHORT>(size);
    _ASSERTE(!stackSize.IsOverflow());
    m_stackSize = stackSize.Value();
}


inline
VOID StubLinker::EmitUnwindInfoCheck()
{
#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    if (g_pConfig->IsStubLinkerUnwindInfoVerificationOn())
    {
        if (!m_pUnwindInfoCheckLabel)
            m_pUnwindInfoCheckLabel = NewCodeLabel();
        EmitUnwindInfoCheckWorker(m_pUnwindInfoCheckLabel);
    }
#endif
}


#ifndef STUBLINKER_GENERATES_UNWIND_INFO

inline VOID StubLinker::UnwindSavedReg (UCHAR reg, ULONG SPRelativeOffset) {LIMITED_METHOD_CONTRACT;}
inline VOID StubLinker::UnwindAllocStack (SHORT FrameSizeIncrement) {LIMITED_METHOD_CONTRACT;}
inline VOID StubLinker::UnwindSetFramePointer (UCHAR reg) {LIMITED_METHOD_CONTRACT;}

inline VOID StubLinker::UnwindPushedReg (UCHAR reg)
{
    LIMITED_METHOD_CONTRACT;

    m_stackSize += sizeof(void*);
}

#endif // !STUBLINKER_GENERATES_UNWIND_INFO


#endif // !__STUBLINK_INL__

