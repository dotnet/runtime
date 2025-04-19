// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: WeakReferenceNative.h
**
**
===========================================================*/

#ifndef _WEAKREFERENCENATIVE_H
#define _WEAKREFERENCENATIVE_H

#include "weakreference.h"

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

class ComAwareWeakReferenceNative
{
public:
    static FCDECL1(FC_BOOL_RET, HasInteropInfo, Object* pObject);
};

#endif // defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

#ifdef FEATURE_COMINTEROP

extern "C" void QCALLTYPE ComWeakRefToObject(IWeakReference * pComWeakReference, QCall::ObjectHandleOnStack retRcw);
extern "C" IWeakReference * QCALLTYPE ObjectToComWeakRef(QCall::ObjectHandleOnStack obj);

#endif // FEATURE_COMINTEROP

#endif // _WEAKREFERENCENATIVE_H
