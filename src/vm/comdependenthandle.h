// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
//     * A Primary object (think key)
//     * A Secondary Object (think value)
// 
// The reference to both the primary object is (long) weak (will not keep the object alive). However the
// reference to the secondary object is (long) weak if the primary object is dead, and strong if the primary
// object is alive. (Hence it is a 'Dependent' handle since the strength of the secondary reference depends
// on the primary).
// 
// The effect of this semantics is that it seems that while the DependentHandle exists, the system behaves as
// if there was a normal strong reference from the primary object to the secondary one.
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
    static FCDECL3(VOID,   nInitialize, Object *primary, Object *secondary, OBJECTHANDLE *outHandle);
    static FCDECL2(VOID,   nGetPrimary, OBJECTHANDLE handle, Object **outPrimary);
    static FCDECL3(VOID,   nGetPrimaryAndSecondary, OBJECTHANDLE handle, Object **outPrimary, Object **outSecondary);
    static FCDECL1(VOID,   nFree, OBJECTHANDLE handle);
};

#endif

