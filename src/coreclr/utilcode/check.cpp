// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//================================================================================
// Assertion checking infrastructure
//================================================================================

#include "stdafx.h"
#include <check.h>
#include <sstring.h>
#include <ex.h>
#include <contract.h>

#ifdef _DEBUG
size_t CHECK::s_cLeakedBytes = 0;
size_t CHECK::s_cNumFailures = 0;

thread_local LONG CHECK::t_count;
#endif

BOOL CHECK::s_neverEnforceAsserts = 0;


// Currently used for scan SPECIAL_HOLDER_* trickery
DEBUG_NOINLINE BOOL CHECK::EnforceAssert_StaticCheckOnly()
{
    return s_neverEnforceAsserts;
}

#ifdef ENABLE_CONTRACTS_IMPL
// Need a place to stick this, there is no contract.cpp...
BOOL BaseContract::s_alwaysEnforceContracts = 1;

#define SPECIALIZE_CONTRACT_VIOLATION_HOLDER(mask)                              \
template<> void ContractViolationHolder<mask>::Enter()                          \
{                                                                               \
    SCAN_SCOPE_BEGIN;                                                           \
    ANNOTATION_VIOLATION(mask);                                                 \
    EnterInternal(mask);                                                        \
};

#define SPECIALIZE_AUTO_CLEANUP_CONTRACT_VIOLATION_HOLDER(mask)                                                 \
template<> AutoCleanupContractViolationHolder<mask>::AutoCleanupContractViolationHolder(BOOL fEnterViolation)   \
{                                                                                                               \
    SCAN_SCOPE_BEGIN;                                                                                           \
    ANNOTATION_VIOLATION(mask);                                                                                 \
    EnterInternal(fEnterViolation ? mask : 0);                                                                  \
};

#define SPECIALIZED_VIOLATION(mask)                                             \
    SPECIALIZE_CONTRACT_VIOLATION_HOLDER(mask);                                 \
    SPECIALIZE_AUTO_CLEANUP_CONTRACT_VIOLATION_HOLDER(mask)

// There is a special case that requires 0... Why??? Who knows, let's fix that case.

SPECIALIZED_VIOLATION(0);

// Basic Specializations

SPECIALIZED_VIOLATION(AllViolation);
SPECIALIZED_VIOLATION(ThrowsViolation);
SPECIALIZED_VIOLATION(GCViolation);
SPECIALIZED_VIOLATION(ModeViolation);
SPECIALIZED_VIOLATION(FaultViolation);
SPECIALIZED_VIOLATION(FaultNotFatal);
SPECIALIZED_VIOLATION(HostViolation);
SPECIALIZED_VIOLATION(TakesLockViolation);
SPECIALIZED_VIOLATION(LoadsTypeViolation);

// Other Specializations used by the RUNTIME, if you get a compile time error you need
// to add the specific specialization that you are using here.

SPECIALIZED_VIOLATION(ThrowsViolation|GCViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|GCViolation|TakesLockViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|ModeViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultNotFatal);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|TakesLockViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|TakesLockViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|GCViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|TakesLockViolation|LoadsTypeViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|ModeViolation);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|ModeViolation|FaultNotFatal);
SPECIALIZED_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|ModeViolation|FaultNotFatal|TakesLockViolation);
SPECIALIZED_VIOLATION(GCViolation|FaultViolation);
SPECIALIZED_VIOLATION(GCViolation|FaultNotFatal|ModeViolation);
SPECIALIZED_VIOLATION(GCViolation|FaultNotFatal|TakesLockViolation);
SPECIALIZED_VIOLATION(GCViolation|FaultNotFatal|TakesLockViolation|ModeViolation);
SPECIALIZED_VIOLATION(GCViolation|ModeViolation);
SPECIALIZED_VIOLATION(FaultViolation|FaultNotFatal);
SPECIALIZED_VIOLATION(FaultNotFatal|TakesLockViolation);



#undef SPECIALIZED_VIOLATION
#undef SPECIALIZE_AUTO_CLEANUP_CONTRACT_VIOLATION_HOLDER
#undef SPECIALIZE_CONTRACT_VIOLATION_HOLDER

#endif

// Trigger triggers the actual check failure.  The trigger may provide a reason
// to include in the failure message.

void CHECK::Trigger(LPCSTR reason)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    const char *messageString = NULL;
    NewHolder<StackSString> pMessage(NULL);

    EX_TRY
    {
        FAULT_NOT_FATAL();
        pMessage = new StackSString();

        pMessage->AppendASCII(reason);
        pMessage->AppendASCII(": ");
        if (m_message != NULL)
            pMessage->AppendASCII((m_message != (LPCSTR)1) ? m_message : "<runtime check failure>");

#if _DEBUG
        pMessage->AppendASCII("FAILED: ");
        pMessage->AppendASCII(m_condition);
#endif

        messageString = pMessage->GetUTF8();
    }
    EX_CATCH
    {
        messageString = "<exception occurred while building failure description>";
    }
    EX_END_CATCH(SwallowAllExceptions);

#if _DEBUG
    DbgAssertDialog((char*)m_file, m_line, (char *)messageString);
#else
    OutputDebugStringUtf8(messageString);
    DebugBreak();
#endif

}

#ifdef _DEBUG
// Setup records context info after a failure.

void CHECK::Setup(LPCSTR message, LPCSTR condition, LPCSTR file, INT line)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    //
    // It might be nice to collect all of the message here.  But for now, we will just
    // retain the innermost one.
    //

    if (m_message == NULL)
    {
        m_message = message;
        m_condition = condition;
        m_file = file;
        m_line = line;
    }

#ifdef _DEBUG
    else if (IsInAssert())
    {
        EX_TRY
        {
            FAULT_NOT_FATAL();
            // Try to build a stack of condition failures

            StackSString context;
            context.Printf("%s\n\t%s%s FAILED: %s\n\t\t%s, line: %d",
                           m_condition,
                           message && *message ? message : "",
                           message && *message ? ": " : "",
                           condition,
                           file, line);

            m_condition = AllocateDynamicMessage(context);
        }
        EX_CATCH
        {
            // If anything goes wrong, we don't push extra context
        }
        EX_END_CATCH(SwallowAllExceptions)
    }
#endif

#if defined(_DEBUG_IMPL)
    if (IsInAssert() && IsDebuggerPresent())
    {
        DebugBreak();
    }
#endif
}

LPCSTR CHECK::FormatMessage(LPCSTR messageFormat, ...)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    LPCSTR result = NULL;

    // We never delete this allocated string. A dtor would conflict with places
    // we use this around SEH stuff. We could have some fancy stack-based allocator,
    // but that's too much work for now. In fact we believe that leaking is a reasonable
    // policy, since allocations will only happen on a failed assert, and a failed assert
    // will generally be fatal to the process.

    // The most common place for format strings will be in an assert; in
    // which case we don't care about leaking.
    // But to be safe, if we're not-inside an assert, then we'll just use
    // the format string literal to avoid allocated (and leaking) any memory.

    CHECK chk;
    if (!chk.IsInAssert())
        result = messageFormat;
    else
    {
        // This path is only run in debug.  TakesLockViolation suppresses
        // problems with SString below.
        CONTRACT_VIOLATION(FaultNotFatal|TakesLockViolation);

        EX_TRY
        {
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

            // Format it.
            va_list args;
            va_start( args, messageFormat);

            SString s;
            s.VPrintf(messageFormat, args);

            va_end(args);

            result = AllocateDynamicMessage(s);

        }
        EX_CATCH
        {
            // If anything goes wrong, just use the format string.
            result = messageFormat;
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    return result;
}

LPCSTR CHECK::AllocateDynamicMessage(const SString &s)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // Make a copy of it.
    const char * pMsg = s.GetUTF8();

    // Must copy that into our own field.
    size_t len = strlen(pMsg) + 1;
    char * p = new char[len];
    strcpy(p, pMsg);

    // But we'll keep counters of how much we're leaking for diagnostic purposes.
    s_cLeakedBytes += len;
    s_cNumFailures ++;

    // This should never fire.
    // Note use an ASSERTE (not a check) to avoid a recursive deadlock.
    _ASSERTE(s_cLeakedBytes < 10000 || !"Warning - check misuse - leaked over 10,000B due to unexpected usage pattern");
    return p;
}

#endif
