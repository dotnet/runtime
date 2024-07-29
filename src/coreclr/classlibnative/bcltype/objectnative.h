// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ObjectNative.h
//

//
// Purpose: Native methods on System.Object
//
//

#ifndef _OBJECTNATIVE_H_
#define _OBJECTNATIVE_H_

#include "fcall.h"


//
// Each function that we call through native only gets one argument,
// which is actually a pointer to it's stack of arguments.  Our structs
// for accessing these are defined below.
//

class ObjectNative
{
public:

    static FCDECL1(INT32, GetHashCode, Object* vThisRef);
    static FCDECL1(INT32, TryGetHashCode, Object* vThisRef);
    static FCDECL2(FC_BOOL_RET, ContentEquals, Object *pThisRef, Object *pCompareRef);
    static FCDECL1(Object*, GetClass, Object* pThis);
    static FCDECL1(FC_BOOL_RET, IsLockHeld, Object* pThisUNSAFE);
};

extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle);
extern "C" BOOL QCALLTYPE Monitor_Wait(QCall::ObjectHandleOnStack pThis, INT32 Timeout);
extern "C" void QCALLTYPE Monitor_Pulse(QCall::ObjectHandleOnStack pThis);
extern "C" void QCALLTYPE Monitor_PulseAll(QCall::ObjectHandleOnStack pThis);
extern "C" INT64 QCALLTYPE Monitor_GetLockContentionCount();

#endif // _OBJECTNATIVE_H_
