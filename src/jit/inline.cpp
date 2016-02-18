// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// Lookup table for inline description strings

static const char* InlineDescriptions[] =
{
#define INLINE_OBSERVATION(name, type, description, impact, target) description,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline targets

static const InlineTarget InlineTargets[] =
{
#define INLINE_OBSERVATION(name, type, description, impact, target) InlineTarget::target,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline impacts

static const InlineImpact InlineImpacts[] =
{
#define INLINE_OBSERVATION(name, type, description, impact, target) InlineImpact::impact,
#include "inline.def"
#undef INLINE_OBSERVATION
};

#ifdef DEBUG

//------------------------------------------------------------------------
// inlIsValidObservation: run a validity check on an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    true if the observation is valid

bool inlIsValidObservation(InlineObservation obs)
{
    return((obs > InlineObservation::CALLEE_UNUSED_INITIAL) &&
           (obs < InlineObservation::CALLEE_UNUSED_FINAL));
}

#endif // DEBUG

//------------------------------------------------------------------------
// inlGetDescriptionString: get a string describing this inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the observation

const char* inlGetDescriptionString(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineDescriptions[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetTarget: get the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum describing the target

InlineTarget inlGetTarget(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineTargets[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetTargetString: get a string describing the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the target

const char* inlGetTargetstring(InlineObservation obs)
{
    InlineTarget t = inlGetTarget(obs);
    switch (t)
    {
    case InlineTarget::CALLER:
        return "caller";
    case InlineTarget::CALLEE:
        return "callee";
    case InlineTarget::CALLSITE:
        return "call site";
    default:
        return "unexpected target";
    }
}

//------------------------------------------------------------------------
// inlGetImpact: get the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum value describing the impact

InlineImpact inlGetImpact(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineImpacts[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetImpactString: get a string describing the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the impact

const char* inlGetImpactString(InlineObservation obs)
{
    InlineImpact i = inlGetImpact(obs);
    switch (i)
    {
    case InlineImpact::FATAL:
        return "correctness -- fatal";
    case InlineImpact::FUNDAMENTAL:
        return "correctness -- fundamental limitation";
    case InlineImpact::LIMITATION:
        return "correctness -- jit limitation";
    case InlineImpact::PERFORMANCE:
        return "performance";
    case InlineImpact::INFORMATION:
        return "information";
    default:
        return "unexpected impact";
    }
}

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular call
// for inlining.
//
// Arguments
//   compiler - the compiler instance examining an call for ininling
//   call     - the call in question
//   context  - descrptive string to describe the context of the decision

InlineResult::InlineResult(Compiler*    compiler,
                           GenTreeCall* call,
                           const char*  context)
    : inlCompiler(compiler)
    , inlDecision(InlineDecision::UNDECIDED)
    , inlObservation(InlineObservation::CALLEE_UNUSED_INITIAL)
    , inlCall(call)
    , inlCaller(nullptr)
    , inlCallee(nullptr)
    , inlContext(context)
    , inlReported(false)
{
    // Get method handle for caller
    inlCaller = inlCompiler->info.compMethodHnd;

    // Get method handle for callee, if known
    if (inlCall->gtCall.gtCallType == CT_USER_FUNC)
    {
        inlCallee = call->gtCall.gtCallMethHnd;
    }
}

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular 
// method as a possible inline candidate.
//
// Notes:
//   Used only during prejitting to try and pre-identify methods
//   that cannot be inlined, to help subsequent jit throughput.
//
//   We use the inlCallee member to track the method since logically
//   it is the callee here.
//
// Arguments
//   compiler - the compiler instance doing the prejitting
//   method   - the method in question
//   context  - descrptive string to describe the context of the decision

InlineResult::InlineResult(Compiler*              compiler,
                           CORINFO_METHOD_HANDLE  method,
                           const char*            context)
    : inlCompiler(compiler)
    , inlDecision(InlineDecision::UNDECIDED)
    , inlObservation(InlineObservation::CALLEE_UNUSED_INITIAL)
    , inlCall(nullptr)
    , inlCaller(nullptr)
    , inlCallee(method)
    , inlContext(context)
    , inlReported(false)
{
    // Empty
}

//------------------------------------------------------------------------
// report: Dump, log, and report information about an inline decision.
//
// Notes:
//
//    Called (automatically via the InlineResult dtor) when the inliner
//    is done evaluating a candidate.
//
//    Dumps state of the inline candidate, and if a decision was reached
//    sends it to the log and reports the decision back to the EE.
//
//    All this can be suppressed if desired by calling setReported() before
//    the InlineResult goes out of scope.

void InlineResult::report()
{
    // User may have suppressed reporting via setReported(). If so, do nothing.
    if (inlReported)
    {
        return;
    }

    inlReported = true;

#ifdef DEBUG

    // Optionally dump the result
    if (VERBOSE)
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s' for '%s' calling '%s'\n";
        const char* caller = (inlCaller == nullptr) ? "n/a" : inlCompiler->eeGetMethodFullName(inlCaller);
        const char* callee = (inlCallee == nullptr) ? "n/a" : inlCompiler->eeGetMethodFullName(inlCallee);

        JITDUMP(format, inlContext, resultString(), reasonString(), caller, callee);
    }

    // If the inline failed, leave information on the call so we can
    // later recover what observation lead to the failure.
    if (isFailure() && (inlCall != nullptr))
    {
        // compiler should have revoked candidacy on the call by now
        assert((inlCall->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);

        inlCall->gtInlineObservation = static_cast<unsigned>(inlObservation);
    }

#endif // DEBUG

    if (isDecided())
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s'\n";
        JITLOG_THIS(inlCompiler, (LL_INFO100000, format, inlContext, resultString(), reasonString()));
        COMP_HANDLE comp = inlCompiler->info.compCompHnd;
        comp->reportInliningDecision(inlCaller, inlCallee, result(), reasonString());
    }
}
