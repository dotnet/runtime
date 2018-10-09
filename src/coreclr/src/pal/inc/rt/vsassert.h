// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

// This is a simple implementation of what's in vsassert.h in vscommon, since
// we don't want to pull in the entire vsassert library.

#ifndef __VSASSERT_H__
#define __VSASSERT_H__

#define VSASSERT(e, szMsg)                                      \
do {                                                            \
    if (!(e)) {                                                 \
        PAL_fprintf (stderr,                                        \
                 "ASSERT FAILED:\n"                             \
                 "\tExpression: %s\n"                           \
                 "\tLocation:   line %d in %s\n"                \
                 "\tFunction:   %s\n"                           \
                 "\tMessage:    %s\n",                          \
                 #e, __LINE__, __FILE__, __FUNCTION__, szMsg);  \
            DebugBreak();                                       \
    }                                                           \
} while (0)

#define VSFAIL(szMsg) VSASSERT(0, szMsg)
#define VSIMPLIES(fHypothesis, fConclusion, szMsg) VSASSERT(!(fHypothesis) || (fConclusion), szMsg)
#define VSVERIFY(fTest, szMsg) VSASSERT((fTest), (szMsg))

#undef VSAlloc
#undef VSAllocZero
#undef VSRealloc
#undef VSReallocZero
#undef VSFree
#undef VSSize
#undef VsDebAlloc
#undef VsDebRealloc
#undef VsDebSafeRealloc
#undef VsDebFree
#undef VsDebHeapSize

#undef VsDebHeapCreate
#undef VsDebHeapDestroy

#undef VsDebugInitialize
#undef VsDebugTerminate

// NOTE: These have changed to use the HeapAlloc family (as opposed to
// LocalAlloc family) because of HeapReAlloc's behavior (a block may move to
// satisfy a realloc request, as opposed to LocalReAlloc's behavior of simply
// failing).

#define VSAlloc(cb)          HeapAlloc(GetProcessHeap(), 0, cb)
#define VSAllocZero(cb)      HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, cb)
#define VSRealloc(pv, cb)    HeapReAlloc(GetProcessHeap(), 0, pv, cb)
#define VSReallocZero(pv,cb) CoreCLR_pal_doesnt_have_vsrealloczero
#define VSFree(pv)           HeapFree(GetProcessHeap(), 0, pv)
#define VSSize(pv)           CoreCLR_pal_doesnt_have_heapsize

#define VsDebAlloc(flags,cb)          VSAlloc(cb)
#define VsDebRealloc(pv,flags,cb)     VSRealloc(pv,cb)
#define VsDebSafeRealloc(pv,flags,cb) CoreCLR_pal_doenst_have_saferealloc
#define VsDebFree(pv)                 VSFree(pv)
#define VsDebHeapSize(heap, pv)       VSSize(pv)

#define VsDebHeapCreate(flags, name)         CoreCLR_doesnt_have_heapcreate
#define VsDebHeapDestroy(heap, fLeakCheck)   CoreCLR_doesnt_have_heapdestroy

#define VsDebugAllocInternal(hheap,dwFlags,cb,pszFile,uLine,dwInst,pszExtra) \
    HeapAlloc(GetProcessHeap(), dwFlags, cb)

#define DEFAULT_HEAP 0
#define INSTANCE_GLOBAL 0

#define VsDebugInitialize() do {} while (0)
#define VsDebugTerminate() do {} while (0)


// Debug switches
//
#define DEFINE_SWITCH(NAME, PACKAGE, DESC) VSDEFINE_SWITCH(NAME, PACKAGE, DESC)
#define EXTERN_SWITCH(NAME)                VSEXTERN_SWITCH(NAME)
#define FSWITCH(NAME)                      VSFSWITCH(NAME)

#define VSDEFINE_SWITCH(NAME, PACKAGE, DESC)
#define VSEXTERN_SWITCH(NAME)
#define VSFSWITCH(NAME) FALSE

#define VsIgnoreAllocs(f)

#endif // __VSASSERT_H__
