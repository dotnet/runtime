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


#ifdef TARGET_X86
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
#endif // TARGET_X86

#endif // !__STUBLINK_INL__

