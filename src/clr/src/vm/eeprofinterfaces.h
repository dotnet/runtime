//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 
// EEProfInterfaces.h
// 

//
// Common types used internally in the EE to support issuing profiling API callbacks
// 

// ======================================================================================

#ifndef _EEPROFINTERFACES_H_
#define _EEPROFINTERFACES_H_

#include <stddef.h>
#include "corprof.h"
#include "profilepriv.h"

#define PROF_USER_MASK 0xFFFFFFFF

class EEToProfInterfaceImpl;
class ProfToEEInterfaceImpl;
class Thread;
class Frame;
class MethodDesc;
class Object;
class Module;

// This file defines the _internal_ interface between the EE and the
// implementation of the COM profiling API.  The _external_ API is defined
// in inc/corprof.idl.
//
// Most IDs used by the _external_ API are just the pointer values
// of the corresponding CLR data structure.
//

/*
 * The following methods dispatch allocations tracking to the profiler as
 * well as the method table reordering codes (as appropriate).
 */

void __stdcall ProfilerObjectAllocatedCallback(OBJECTREF objref, ClassID classId);

void __stdcall GarbageCollectionStartedCallback(int generation, BOOL induced);

void __stdcall GarbageCollectionFinishedCallback();

void __stdcall UpdateGenerationBounds();
#include "eetoprofinterfaceimpl.h"


enum PTR_TYPE
{
    PT_MODULE,
    PT_ASSEMBLY,
};

void __stdcall ProfilerManagedToUnmanagedTransitionMD(MethodDesc * pMD,
                                                          COR_PRF_TRANSITION_REASON reason);

void __stdcall ProfilerUnmanagedToManagedTransitionMD(MethodDesc * pMD,
                                                          COR_PRF_TRANSITION_REASON reason);

#endif //_EEPROFINTERFACES_H_
