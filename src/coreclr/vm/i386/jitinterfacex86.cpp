// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: JITinterfaceX86.CPP
//
// ===========================================================================

// This contains JITinterface routines that are tailored for
// X86 platforms. Non-X86 versions of these can be found in
// JITinterfaceGen.cpp


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "eventtrace.h"

extern "C" LONG g_global_alloc_lock;

/*********************************************************************/
#ifdef FEATURE_HIJACK
extern "C" void STDCALL JIT_TailCallHelper(Thread * pThread);
void STDCALL JIT_TailCallHelper(Thread * pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    pThread->UnhijackThread();
}
#endif // FEATURE_HIJACK
