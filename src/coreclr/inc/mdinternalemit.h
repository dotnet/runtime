// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MDINTERNALEMIT_H_
#define _MDINTERNALEMIT_H_

#include "cor.h"

EXTERN_GUID(IID_IMDInternalEmit, 0xf102c526, 0x38cb, 0x49ed, 0x9b, 0x5f, 0x49, 0x88, 0x16, 0xae, 0x36, 0xe0);

#undef INTERFACE
#define INTERFACE IMDInternalEmit
DECLARE_INTERFACE_(IMDInternalEmit, IUnknown)
{
    STDMETHOD(ChangeMvid)(REFGUID newMvid) PURE;
    STDMETHOD(SetMDUpdateMode)(ULONG updateMode, ULONG* pPreviousUpdateMode) PURE;
};

#endif // _MDINTERNALEMIT_H_
