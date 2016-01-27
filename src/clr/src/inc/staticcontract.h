// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// StaticContract.h
// ---------------------------------------------------------------------------


#ifndef __STATIC_CONTRACT_H_
#define __STATIC_CONTRACT_H_

// Make sure we have the WCHAR defines available.
#include "palclr.h"

#define SCAN_WIDEN2(x) L ## x
#define SCAN_WIDEN(x) SCAN_WIDEN2(x)

//
// PDB annotations for the static contract analysis tool. These are seperated
// from Contract.h to allow their inclusion in any part of the system.
//

#if defined(_DEBUG) && defined(_TARGET_X86_)
#define METHOD_CANNOT_BE_FOLDED_DEBUG                               \
    static int _noFold = 0;                                         \
    _noFold++;
#else
#define METHOD_CANNOT_BE_FOLDED_DEBUG
#endif

#ifdef _TARGET_X86_

//
// currently, only x86 has a static contract analysis tool, so let's not
// bloat the PDBs of all the other architectures too..
//
#define ANNOTATION_TRY_BEGIN                __annotation(W("TRY_BEGIN"))
#define ANNOTATION_TRY_END                  __annotation(W("TRY_END"))
#define ANNOTATION_HANDLER_BEGIN            __annotation(W("HANDLER_BEGIN"))
#define ANNOTATION_HANDLER_END              __annotation(W("HANDLER_END"))
#define ANNOTATION_NOTHROW                  __annotation(W("NOTHROW"))
#define ANNOTATION_CANNOT_TAKE_LOCK         __annotation(W("CANNOT_TAKE_LOCK"))
#define ANNOTATION_WRAPPER                  __annotation(W("WRAPPER"))
#define ANNOTATION_FAULT                    __annotation(W("FAULT"))
#define ANNOTATION_FORBID_FAULT             __annotation(W("FORBID_FAULT"))
#define ANNOTATION_COOPERATIVE              __annotation(W("MODE_COOPERATIVE"))
#define ANNOTATION_MODE_COOPERATIVE         __annotation(W("MODE_PREEMPTIVE"))
#define ANNOTATION_MODE_ANY                 __annotation(W("MODE_ANY"))
#define ANNOTATION_GC_TRIGGERS              __annotation(W("GC_TRIGGERS"))
#define ANNOTATION_IGNORE_THROW             __annotation(W("THROWS"), W("NOTHROW"), W("CONDITIONAL_EXEMPT"))
#define ANNOTATION_IGNORE_LOCK              __annotation(W("CAN_TAKE_LOCK"), W("CANNOT_TAKE_LOCK"), W("CONDITIONAL_EXEMPT"))
#define ANNOTATION_IGNORE_FAULT             __annotation(W("FAULT"), W("FORBID_FAULT"), W("CONDITIONAL_EXEMPT"))
#define ANNOTATION_IGNORE_TRIGGER           __annotation(W("GC_TRIGGERS"), W("GC_NOTRIGGER"), W("CONDITIONAL_EXEMPT"))
#define ANNOTATION_IGNORE_SO                __annotation(W("SO_TOLERANT"), W("SO_INTOLERANT"), W("CONDITIONAL_EXEMPT"))
#define ANNOTATION_VIOLATION(violationmask) __annotation(W("VIOLATION(") L#violationmask W(")"))
#define ANNOTATION_UNCHECKED(thecheck)      __annotation(W("UNCHECKED(") L#thecheck W(")"))

#define ANNOTATION_MARK_BLOCK_ANNOTATION    __annotation(W("MARK"))
#define ANNOTATION_USE_BLOCK_ANNOTATION     __annotation(W("USE"))
#define ANNOTATION_END_USE_BLOCK_ANNOTATION __annotation(W("END_USE"))

// here is the plan:
//
//  a special holder which implements a violation 
//

#define ANNOTATION_FN_SPECIAL_HOLDER_BEGIN  __annotation(W("SPECIAL_HOLDER_BEGIN ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_SPECIAL_HOLDER_END       __annotation(W("SPECIAL_HOLDER_END"))
#define ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT __annotation(W("SPECIAL_HOLDER_DYNAMIC"))

#define ANNOTATION_SO_PROBE_BEGIN(probeAmount) __annotation(W("SO_PROBE_BEGIN(") L#probeAmount W(")"))
#define ANNOTATION_SO_PROBE_END             __annotation(W("SO_PROBE_END"))

//
// these annotations are all function-name qualified
//
#define ANNOTATION_FN_LEAF                  __annotation(W("LEAF ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_WRAPPER               __annotation(W("WRAPPER ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_THROWS                __annotation(W("THROWS ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_NOTHROW               __annotation(W("NOTHROW ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_CAN_TAKE_LOCK         __annotation(W("CAN_TAKE_LOCK ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_CANNOT_TAKE_LOCK      __annotation(W("CANNOT_TAKE_LOCK ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_FAULT                 __annotation(W("FAULT ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_FORBID_FAULT          __annotation(W("FORBID_FAULT ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_GC_TRIGGERS           __annotation(W("GC_TRIGGERS ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_GC_NOTRIGGER          __annotation(W("GC_NOTRIGGER ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_SO_TOLERANT           __annotation(W("SO_TOLERANT ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_SO_INTOLERANT         __annotation(W("SO_INTOLERANT ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_SO_NOT_MAINLINE       __annotation(W("SO_NOT_MAINLINE ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_MODE_COOPERATIVE      __annotation(W("MODE_COOPERATIVE ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_MODE_PREEMPTIVE       __annotation(W("MODE_PREEMPTIVE ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_MODE_ANY              __annotation(W("MODE_ANY ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_HOST_NOCALLS          __annotation(W("HOST_NOCALLS ") SCAN_WIDEN(__FUNCTION__))
#define ANNOTATION_FN_HOST_CALLS            __annotation(W("HOST_CALLS ") SCAN_WIDEN(__FUNCTION__))

#define ANNOTATION_ENTRY_POINT              __annotation(W("SO_EP ") SCAN_WIDEN(__FUNCTION__))  


// for DacCop
#define ANNOTATION_SUPPORTS_DAC             __annotation(W("SUPPORTS_DAC"))
#define ANNOTATION_SUPPORTS_DAC_HOST_ONLY   __annotation(W("SUPPORTS_DAC_HOST_ONLY"))

#ifdef _DEBUG
// @todo : put correct annotation in and fixup the static analysis tool
// This is used to flag debug-only functions that we want to ignore in our static analysis
#define ANNOTATION_DEBUG_ONLY               __annotation(W("DBG_ONLY"))

#endif

#else // _TARGET_X86_

#define ANNOTATION_TRY_BEGIN                { }
#define ANNOTATION_TRY_END                  { }
#define ANNOTATION_HANDLER_BEGIN            { }
#define ANNOTATION_HANDLER_END              { }
#define ANNOTATION_NOTHROW                  { }
#define ANNOTATION_CANNOT_TAKE_LOCK         { }
#define ANNOTATION_WRAPPER                  { }
#define ANNOTATION_FAULT                    { }
#define ANNOTATION_FORBID_FAULT             { }
#define ANNOTATION_COOPERATIVE              { }
#define ANNOTATION_MODE_COOPERATIVE         { }
#define ANNOTATION_MODE_ANY                 { }
#define ANNOTATION_GC_TRIGGERS              { }
#define ANNOTATION_IGNORE_THROW             { }
#define ANNOTATION_IGNORE_LOCK              { }
#define ANNOTATION_IGNORE_FAULT             { }
#define ANNOTATION_IGNORE_TRIGGER           { }
#define ANNOTATION_IGNORE_SO                { }
#define ANNOTATION_VIOLATION(violationmask) { }
#define ANNOTATION_UNCHECKED(thecheck)      { }

#define ANNOTATION_TRY_MARKER               { }
#define ANNOTATION_CATCH_MARKER             { }

#define ANNOTATION_FN_HOST_NOCALLS          { }
#define ANNOTATION_FN_HOST_CALLS            { }

#define ANNOTATION_FN_SPECIAL_HOLDER_BEGIN  { }
#define ANNOTATION_SPECIAL_HOLDER_END       { }
#define ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT { }

#define ANNOTATION_FN_LEAF                  { }
#define ANNOTATION_FN_WRAPPER               { }
#define ANNOTATION_FN_THROWS                { }
#define ANNOTATION_FN_NOTHROW               { }
#define ANNOTATION_FN_CAN_TAKE_LOCK         { }
#define ANNOTATION_FN_CANNOT_TAKE_LOCK      { }
#define ANNOTATION_FN_FAULT                 { }
#define ANNOTATION_FN_FORBID_FAULT          { }
#define ANNOTATION_FN_GC_TRIGGERS           { }
#define ANNOTATION_FN_GC_NOTRIGGER          { }
#define ANNOTATION_FN_SO_TOLERANT           { }
#define ANNOTATION_FN_SO_INTOLERANT         { }
#define ANNOTATION_FN_SO_NOT_MAINLINE       { }
#define ANNOTATION_FN_MODE_COOPERATIVE      { }
#define ANNOTATION_FN_MODE_PREEMPTIVE       { }
#define ANNOTATION_FN_MODE_ANY              { }
#define ANNOTATION_FN_HOST_NOCALLS          { }
#define ANNOTATION_FN_HOST_CALLS            { }

#define ANNOTATION_SUPPORTS_DAC             { }
#define ANNOTATION_SUPPORTS_DAC_HOST_ONLY   { }

#define ANNOTATION_SO_PROBE_BEGIN(probeAmount) { }
#define ANNOTATION_SO_PROBE_END             { }

#define ANNOTATION_SO_TOLERANT              { }
#define ANNOTATION_SO_INTOLERANT            { }
#define ANNOTATION_SO_NOT_MAINLINE          { }
#define ANNOTATION_SO_NOT_MAINLINE_BEGIN    { }
#define ANNOTATION_SO_NOT_MAINLINE_END      { }
#define ANNOTATION_ENTRY_POINT              { }
#ifdef _DEBUG
#define ANNOTATION_DEBUG_ONLY               { }
#endif

#endif // _TARGET_X86_

#define STATIC_CONTRACT_THROWS              ANNOTATION_FN_THROWS
#define STATIC_CONTRACT_NOTHROW             ANNOTATION_FN_NOTHROW
#define STATIC_CONTRACT_CAN_TAKE_LOCK       ANNOTATION_FN_CAN_TAKE_LOCK
#define STATIC_CONTRACT_CANNOT_TAKE_LOCK    ANNOTATION_FN_CANNOT_TAKE_LOCK
#define STATIC_CONTRACT_FAULT               ANNOTATION_FN_FAULT
#define STATIC_CONTRACT_FORBID_FAULT        ANNOTATION_FN_FORBID_FAULT
#define STATIC_CONTRACT_GC_TRIGGERS         ANNOTATION_FN_GC_TRIGGERS
#define STATIC_CONTRACT_GC_NOTRIGGER        ANNOTATION_FN_GC_NOTRIGGER
#define STATIC_CONTRACT_HOST_NOCALLS        ANNOTATION_FN_HOST_NOCALLS
#define STATIC_CONTRACT_HOST_CALLS          ANNOTATION_FN_HOST_CALLS 

#define STATIC_CONTRACT_SUPPORTS_DAC        ANNOTATION_SUPPORTS_DAC
#define STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY ANNOTATION_SUPPORTS_DAC_HOST_ONLY

#define STATIC_CONTRACT_MODE_COOPERATIVE    ANNOTATION_FN_MODE_COOPERATIVE
#define STATIC_CONTRACT_MODE_PREEMPTIVE     ANNOTATION_FN_MODE_PREEMPTIVE
#define STATIC_CONTRACT_MODE_ANY            ANNOTATION_FN_MODE_ANY
#define STATIC_CONTRACT_LEAF                ANNOTATION_FN_LEAF
#define STATIC_CONTRACT_LIMITED_METHOD      ANNOTATION_FN_LEAF
#define STATIC_CONTRACT_WRAPPER             ANNOTATION_FN_WRAPPER

#ifdef FEATURE_STACK_PROBE // Static SO contracts only required when SO Infrastructure code is present
#define STATIC_CONTRACT_SO_INTOLERANT       ANNOTATION_FN_SO_INTOLERANT
#define STATIC_CONTRACT_SO_TOLERANT         ANNOTATION_FN_SO_TOLERANT
#define STATIC_CONTRACT_SO_NOT_MAINLINE     ANNOTATION_FN_SO_NOT_MAINLINE

#define STATIC_CONTRACT_ENTRY_POINT         ANNOTATION_ENTRY_POINT; ANNOTATION_FN_SO_TOLERANT
#else // FEATURE_STACK_PROBE
#define STATIC_CONTRACT_SO_INTOLERANT
#define STATIC_CONTRACT_SO_TOLERANT
#define STATIC_CONTRACT_SO_NOT_MAINLINE
#define STATIC_CONTRACT_ENTRY_POINT
#endif // FEATURE_STACK_PROBE

#ifdef _DEBUG
#define STATIC_CONTRACT_DEBUG_ONLY                                  \
    ANNOTATION_DEBUG_ONLY;                                          \
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;                               \
    ANNOTATION_VIOLATION(TakesLockViolation);                       \
    ANNOTATION_FN_SO_NOT_MAINLINE;
#else
#define STATIC_CONTRACT_DEBUG_ONLY 
#endif

#define STATIC_CONTRACT_VIOLATION(mask)                             \
    ANNOTATION_VIOLATION(mask)

#define SCAN_SCOPE_BEGIN                                            \
    METHOD_CANNOT_BE_FOLDED_DEBUG;                                  \
    ANNOTATION_FN_SPECIAL_HOLDER_BEGIN;

#define SCAN_SCOPE_END                                              \
    METHOD_CANNOT_BE_FOLDED_DEBUG;                                  \
    ANNOTATION_SPECIAL_HOLDER_END;

namespace StaticContract
{
    struct ScanThrowMarkerStandard
    {
        __declspec(noinline) ScanThrowMarkerStandard()
        {
            METHOD_CANNOT_BE_FOLDED_DEBUG;
            STATIC_CONTRACT_THROWS;
            STATIC_CONTRACT_GC_NOTRIGGER;
            STATIC_CONTRACT_SO_TOLERANT;
        }

        static void used()
        {
        }
    };

    struct ScanThrowMarkerTerminal
    {
        __declspec(noinline) ScanThrowMarkerTerminal()
        {
            METHOD_CANNOT_BE_FOLDED_DEBUG;
        }

        static void used()
        {
        }
    };

    struct ScanThrowMarkerIgnore
    {
        __declspec(noinline) ScanThrowMarkerIgnore()
        {
            METHOD_CANNOT_BE_FOLDED_DEBUG;
        }

        static void used()
        {
        }
    };
}
typedef StaticContract::ScanThrowMarkerStandard ScanThrowMarker;

// This is used to annotate code as throwing a terminal exception, and should
// be used immediately before the throw so that infer that it can be inferred
// that the block in which this annotation appears throws unconditionally.
#define SCAN_THROW_MARKER do { ScanThrowMarker __throw_marker; } while (0)

#define SCAN_IGNORE_THROW_MARKER                                    \
    typedef StaticContract::ScanThrowMarkerIgnore ScanThrowMarker; if (0) ScanThrowMarker::used();

// Terminal exceptions are asynchronous and cannot be included in THROWS contract
// analysis. As such, this uses typedef to reassign the ScanThrowMarker to a
// non-annotating struct so that SCAN does not see the block as throwing.
#define STATIC_CONTRACT_THROWS_TERMINAL                             \
    typedef StaticContract::ScanThrowMarkerTerminal ScanThrowMarker; if (0) ScanThrowMarker::used();

#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && defined(FEATURE_STACK_PROBE) && !defined(_TARGET_ARM_) // @ARMTODO
extern void EnsureSOIntolerantOK(const char *szFunction, const char *szFile, int lineNum);

extern BOOL (*g_fpShouldValidateSOToleranceOnThisThread)();

// @todo  Is there any checks we can do here?
#define ENSURE_SHOULD_NOT_PROBE_FOR_SO

#define CHECK_IF_SO_INTOLERANT_OK \
    EnsureSOIntolerantOK(__FUNCTION__, __FILE__, __LINE__);

// Even if we can't have a full-blown contract, we can at least check
// if its ok to run an SO-Intolerant function.
#undef STATIC_CONTRACT_SO_INTOLERANT                                           
#define STATIC_CONTRACT_SO_INTOLERANT                                           \
    ANNOTATION_FN_SO_INTOLERANT;                                                \
    CHECK_IF_SO_INTOLERANT_OK;

#undef STATIC_CONTRACT_SO_NOT_MAINLINE
#define STATIC_CONTRACT_SO_NOT_MAINLINE                                         \
    ENSURE_SHOULD_NOT_PROBE_FOR_SO                                              \
    ANNOTATION_FN_SO_NOT_MAINLINE

#else
#define EnsureSOIntolerantOK(x,y,z)

#endif


#ifdef _MSC_VER
#define SCAN_IGNORE_THROW                   typedef StaticContract::ScanThrowMarkerIgnore ScanThrowMarker; ANNOTATION_IGNORE_THROW
#define SCAN_IGNORE_LOCK                    ANNOTATION_IGNORE_LOCK
#define SCAN_IGNORE_FAULT                   ANNOTATION_IGNORE_FAULT
#define SCAN_IGNORE_TRIGGER                 ANNOTATION_IGNORE_TRIGGER
#define SCAN_IGNORE_SO                      ANNOTATION_IGNORE_SO
#else
#define SCAN_IGNORE_THROW
#define SCAN_IGNORE_LOCK
#define SCAN_IGNORE_FAULT
#define SCAN_IGNORE_TRIGGER
#define SCAN_IGNORE_SO
#endif


// we use BlockMarker's only for SCAN
#if defined(_DEBUG) && defined(_TARGET_X86_) && !defined(DACCESS_COMPILE)

template <UINT COUNT>
class BlockMarker
{
public:
    __declspec(noinline) void MarkBlock()
    {
        ANNOTATION_MARK_BLOCK_ANNOTATION;
        METHOD_CANNOT_BE_FOLDED_DEBUG;
        return;
    }

    __declspec(noinline) void UseMarkedBlockAnnotation()
    {
        ANNOTATION_USE_BLOCK_ANNOTATION;
        METHOD_CANNOT_BE_FOLDED_DEBUG;
        return;
    }

    __declspec(noinline) void EndUseMarkedBlockAnnotation()
    {
        ANNOTATION_END_USE_BLOCK_ANNOTATION;
        METHOD_CANNOT_BE_FOLDED_DEBUG;
        return;
    }
};

#define SCAN_BLOCKMARKER()              BlockMarker<__COUNTER__> __blockMarker_onlyOneAllowedPerScope
#define SCAN_BLOCKMARKER_MARK()         __blockMarker_onlyOneAllowedPerScope.MarkBlock()
#define SCAN_BLOCKMARKER_USE()          __blockMarker_onlyOneAllowedPerScope.UseMarkedBlockAnnotation()
#define SCAN_BLOCKMARKER_END_USE()      __blockMarker_onlyOneAllowedPerScope.EndUseMarkedBlockAnnotation()

#define SCAN_BLOCKMARKER_N(num)         BlockMarker<__COUNTER__> __blockMarker_onlyOneAllowedPerScope##num
#define SCAN_BLOCKMARKER_MARK_N(num)    __blockMarker_onlyOneAllowedPerScope##num.MarkBlock()
#define SCAN_BLOCKMARKER_USE_N(num)     __blockMarker_onlyOneAllowedPerScope##num.UseMarkedBlockAnnotation()
#define SCAN_BLOCKMARKER_END_USE_N(num) __blockMarker_onlyOneAllowedPerScope##num.EndUseMarkedBlockAnnotation()

#define SCAN_EHMARKER()                 BlockMarker<__COUNTER__> __marker_onlyOneAllowedPerScope
#define SCAN_EHMARKER_TRY()             __annotation(W("SCOPE(BLOCK);SCAN_TRY_BEGIN")); __marker_onlyOneAllowedPerScope.MarkBlock()
#define SCAN_EHMARKER_END_TRY()         __annotation(W("SCOPE(BLOCK);SCAN_TRY_END"))
#define SCAN_EHMARKER_CATCH()           __marker_onlyOneAllowedPerScope.UseMarkedBlockAnnotation()
#define SCAN_EHMARKER_END_CATCH()       __marker_onlyOneAllowedPerScope.EndUseMarkedBlockAnnotation()

#else

#define SCAN_BLOCKMARKER()
#define SCAN_BLOCKMARKER_MARK()
#define SCAN_BLOCKMARKER_USE()
#define SCAN_BLOCKMARKER_END_USE()

#define SCAN_BLOCKMARKER_N(num)
#define SCAN_BLOCKMARKER_MARK_N(num)
#define SCAN_BLOCKMARKER_USE_N(num)
#define SCAN_BLOCKMARKER_END_USE_N(num)

#define SCAN_EHMARKER()
#define SCAN_EHMARKER_TRY()
#define SCAN_EHMARKER_END_TRY()
#define SCAN_EHMARKER_CATCH()
#define SCAN_EHMARKER_END_CATCH()

#endif


//
// @todo remove this... if there really are cases where a function just shouldn't have a contract, then perhaps
// we can add a more descriptive name for it...
//
#define CANNOT_HAVE_CONTRACT                __annotation(W("NO_CONTRACT"))

#endif // __STATIC_CONTRACT_H_
