// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef GCENV_H_
#define GCENV_H_

//
// Extra VM headers required to compile GC-related files
//

#include "finalizerthread.h"

#include "threadsuspend.h"

#include "stubhelpers.h"

#include "eeprofinterfaces.inl"

#ifdef GC_PROFILING
#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "profilepriv.h"
#endif

#ifdef DEBUGGING_SUPPORTED
#include "dbginterface.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP



#define GCMemoryStatus MEMORYSTATUSEX

#include "util.hpp"

#include "gcenv.interlocked.h"
#include "gcenv.interlocked.inl"

#ifdef TARGET_UNIX
#include "gcenv.unix.inl"
#else
#include "gcenv.windows.inl"
#endif

namespace ETW
{
    typedef  enum _GC_ROOT_KIND {
        GC_ROOT_STACK = 0,
        GC_ROOT_FQ = 1,
        GC_ROOT_HANDLES = 2,
        GC_ROOT_OLDER = 3,
        GC_ROOT_SIZEDREF = 4,
        GC_ROOT_OVERFLOW = 5,
        GC_ROOT_DH_HANDLES = 6,
        GC_ROOT_NEW_FQ = 7,
        GC_ROOT_STEAL = 8,
        GC_ROOT_BGC = 9
    } GC_ROOT_KIND;
};

#ifdef TARGET_UNIX
#define _tcslen wcslen
#define _tcscpy wcscpy
#define _tfopen _wfopen
#endif

#endif // GCENV_H_
