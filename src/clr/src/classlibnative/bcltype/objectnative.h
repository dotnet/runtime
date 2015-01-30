//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

    // This method will return a Class object for the object
    //  iff the Class object has already been created.
    //  If the Class object doesn't exist then you must call the GetClass() method.
    static FCDECL1(Object*, GetObjectValue, Object* vThisRef);
    static FCDECL1(INT32, GetHashCode, Object* vThisRef);
    static FCDECL2(FC_BOOL_RET, Equals, Object *pThisRef, Object *pCompareRef);
    static FCDECL1(Object*, Clone, Object* pThis);
    static FCDECL1(Object*, GetClass, Object* pThis);
    static FCDECL3(FC_BOOL_RET, WaitTimeout, CLR_BOOL exitContext, INT32 Timeout, Object* pThisUNSAFE);
    static FCDECL1(void, Pulse, Object* pThisUNSAFE);
    static FCDECL1(void, PulseAll, Object* pThisUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsLockHeld, Object* pThisUNSAFE);
};

#endif // _OBJECTNATIVE_H_
