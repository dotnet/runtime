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

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

class ComAwareWeakReferenceNative
{
public:
    static FCDECL1(FC_BOOL_RET, HasInteropInfo, Object* pObject);
};

extern "C" void QCALLTYPE ComWeakRefToObject(IWeakReference * pComWeakReference, INT64 wrapperId, QCall::ObjectHandleOnStack retRcw);
extern "C" IWeakReference * QCALLTYPE ObjectToComWeakRef(QCall::ObjectHandleOnStack obj, INT64* wrapperId);

#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

#endif // _WEAKREFERENCENATIVE_H
