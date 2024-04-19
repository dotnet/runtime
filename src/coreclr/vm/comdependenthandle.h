// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: COMDependentHandle.h
//

//
// FCall's for the DependentHandle class
//


#ifndef __COMDEPENDENTHANDLE_H__
#define __COMDEPENDENTHANDLE_H__

#include "fcall.h"

// A dependent handle is conceputally a tuple containing two object reference
//
//     * A Target object (think key)
//     * A Dependent Object (think value)
//
// The reference to both the target object is (long) weak (will not keep the object alive). However the
// reference to the dependent object is (long) weak if the target object is dead, and strong if the target
// object is alive. (Hence it is a 'Dependent' handle since the strength of the dependent reference depends
// on the target).
//
// The effect of this semantics is that it seems that while the DependentHandle exists, the system behaves as
// if there was a normal strong reference from the target object to the dependent one.
//
// The usefulness of a DependentHandle is to allow other objects to be 'attached' to a given object. By
// having a hash table where the entries are dependent handles you can attach arbitrary objects to another
// object.
//
// If you attmpted to do this with an ordinary table if the value would have to be a strong reference, which
// if it points back to the key, will form a loop that the GC will not be able to break DependentHandle is
// effecively a way of informing the GC about the dedendent relationsship between the key and the value so
// such cycles can be broken.
//
// Almost all the interesting for DependentHandle is in code:Ref_ScanDependentHandles
class DependentHandle
{
public:
    static FCDECL2(OBJECTHANDLE, InternalAlloc, Object *target, Object *dependent);
    static FCDECL1(Object *, InternalGetTarget, OBJECTHANDLE handle);
    static FCDECL1(Object *, InternalGetDependent, OBJECTHANDLE handle);
    static FCDECL2(Object *, InternalGetTargetAndDependent, OBJECTHANDLE handle, Object **outDependent);
    static FCDECL1(VOID, InternalSetTargetToNull, OBJECTHANDLE handle);
    static FCDECL2(VOID, InternalSetDependent, OBJECTHANDLE handle, Object *dependent);
    static FCDECL1(FC_BOOL_RET, InternalFree, OBJECTHANDLE handle);
};

extern "C" OBJECTHANDLE QCALLTYPE DependentHandle_InternalAllocWithGCTransition(QCall::ObjectHandleOnStack target, QCall::ObjectHandleOnStack dependent);
extern "C" void QCALLTYPE DependentHandle_InternalFreeWithGCTransition(OBJECTHANDLE handle);

#endif
