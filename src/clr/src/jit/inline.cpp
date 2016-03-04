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
// inlGetObservationString: get a string describing this inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the observation

const char* inlGetObservationString(InlineObservation obs)
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

const char* inlGetTargetString(InlineObservation obs)
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
// inlGetCorInfoInlineDecision: translate decision into a CorInfoInline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    CorInfoInline value representing the decision

CorInfoInline inlGetCorInfoInlineDecision(InlineDecision d)
{
    switch (d) {
    case InlineDecision::SUCCESS:
        return INLINE_PASS;
    case InlineDecision::FAILURE:
        return INLINE_FAIL;
    case InlineDecision::NEVER:
        return INLINE_NEVER;
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// inlGetDecisionString: get a string representing this decision
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    string representing the decision

const char* inlGetDecisionString(InlineDecision d)
{
    switch (d) {
    case InlineDecision::SUCCESS:
        return "success";
    case InlineDecision::FAILURE:
        return "failed this call site";
    case InlineDecision::NEVER:
        return "failed this callee";
    case InlineDecision::CANDIDATE:
        return "candidate";
    case InlineDecision::UNDECIDED:
        return "undecided";
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// inlDecisionIsFailure: check if this decision describes a failing inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is definitely a failure

bool inlDecisionIsFailure(InlineDecision d)
{
    switch (d) {
    case InlineDecision::SUCCESS:
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        return false;
    case InlineDecision::FAILURE:
    case InlineDecision::NEVER:
        return true;
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// inlDecisionIsSuccess: check if this decision describes a sucessful inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is definitely a success

bool inlDecisionIsSuccess(InlineDecision d)
{
    switch (d) {
    case InlineDecision::SUCCESS:
        return true;
    case InlineDecision::FAILURE:
    case InlineDecision::NEVER:
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        return false;
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// inlDecisionIsNever: check if this decision describes a never inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is a never inline case

bool inlDecisionIsNever(InlineDecision d)
{
    switch (d) {
    case InlineDecision::NEVER:
        return true;
    case InlineDecision::FAILURE:
    case InlineDecision::SUCCESS:
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        return false;
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// inlDecisionIsCandidate: check if this decision describes a viable candidate
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if this inline still might happen

bool inlDecisionIsCandidate(InlineDecision d)
{
    return !inlDecisionIsFailure(d);
}

//------------------------------------------------------------------------
// inlDecisionIsDecided: check if this decision has been made
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if this inline has been decided one way or another

bool inlDecisionIsDecided(InlineDecision d)
{
    switch (d) {
    case InlineDecision::NEVER:
    case InlineDecision::FAILURE:
    case InlineDecision::SUCCESS:
        return true;
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        return false;
    default:
        assert(!"Unexpected InlineDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// InlineContext: default constructor

InlineContext::InlineContext()
    : inlParent(nullptr)
    , inlChild(nullptr)
    , inlSibling(nullptr)
    , inlOffset(BAD_IL_OFFSET)
    , inlCode(nullptr)
    , inlObservation(InlineObservation::CALLEE_UNUSED_INITIAL)
#ifdef DEBUG
    , inlCallee(nullptr)
    , inlTreeID(0)
    , inlSuccess(true)
#endif
{
    // Empty
}

//------------------------------------------------------------------------
// newRoot: construct an InlineContext for the root method
//
// Arguments:
//   compiler - compiler doing the inlining
//
// Return Value:
//    InlineContext for use as the root context
//
// Notes:
//    We leave inlCode as nullptr here (rather than the IL buffer
//    address of the root method) to preserve existing behavior, which
//    is to allow one recursive inline.

InlineContext* InlineContext::newRoot(Compiler* compiler)
{
    InlineContext* rootContext = new (compiler, CMK_Inlining) InlineContext;

#if defined(DEBUG)
    rootContext->inlCallee = compiler->info.compMethodHnd;
#endif

    return rootContext;
}

//------------------------------------------------------------------------
// newSuccess: construct an InlineContext for a successful inline
// and link it into the context tree
//
// Arguments:
//    compiler   - compiler doing the inlining
//    stmt       - statement containing call being inlined
//    inlineInfo - information about this inline
//
// Return Value:
//    A new InlineContext for statements brought into the method by
//    this inline.

InlineContext* InlineContext::newSuccess(Compiler*   compiler,
                                         InlineInfo* inlineInfo)
{
    InlineContext* calleeContext = new (compiler, CMK_Inlining) InlineContext;

    GenTree*       stmt          = inlineInfo->iciStmt;
    BYTE*          calleeIL      = inlineInfo->inlineCandidateInfo->methInfo.ILCode;
    InlineContext* parentContext = stmt->gtStmt.gtInlineContext;

    noway_assert(parentContext != nullptr);

    calleeContext->inlCode = calleeIL;
    calleeContext->inlParent = parentContext;
    // Push on front here will put siblings in reverse lexical
    // order which we undo in the dumper
    calleeContext->inlSibling = parentContext->inlChild;
    parentContext->inlChild = calleeContext;
    calleeContext->inlChild = nullptr;
    calleeContext->inlOffset = stmt->AsStmt()->gtStmtILoffsx;
    calleeContext->inlObservation = inlineInfo->inlineResult->getObservation();
#ifdef DEBUG
    calleeContext->inlCallee = inlineInfo->fncHandle;
    calleeContext->inlTreeID = inlineInfo->inlineResult->getCall()->gtTreeID;
#endif

    return calleeContext;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// newFailure: construct an InlineContext for a failing inline
// and link it into the context tree
//
// Arguments:
//    compiler     - compiler doing the inlining
//    stmt         - statement containing the attempted inline
//    inlineResult - inlineResult for the attempt
//
// Return Value:
//
//    A new InlineContext for diagnostic purposes, or nullptr if
//    the desired context could not be created.

InlineContext* InlineContext::newFailure(Compiler*     compiler,
                                         GenTree*      stmt,
                                         InlineResult* inlineResult)
{
    // Check for a parent context first. We may insert new statements
    // between the caller and callee that do not pick up either's
    // context, and these statements may have calls that we later
    // examine and fail to inline.
    //
    // See fgInlinePrependStatements for examples.

    InlineContext* parentContext = stmt->gtStmt.gtInlineContext;

    if (parentContext == nullptr)
    {
        // Assume for now this is a failure to inline a call in a
        // statement inserted between caller and callee. Just ignore
        // it for the time being.

        return nullptr;
    }

    InlineContext* failedContext = new (compiler, CMK_Inlining) InlineContext;

    failedContext->inlParent = parentContext;
    // Push on front here will put siblings in reverse lexical
    // order which we undo in the dumper
    failedContext->inlSibling = parentContext->inlChild;
    parentContext->inlChild = failedContext;
    failedContext->inlChild = nullptr;
    failedContext->inlOffset = stmt->AsStmt()->gtStmtILoffsx;
    failedContext->inlObservation = inlineResult->getObservation();
    failedContext->inlCallee = inlineResult->getCallee();
    failedContext->inlSuccess = false;
    failedContext->inlTreeID = inlineResult->getCall()->gtTreeID;

    return failedContext;
}

//------------------------------------------------------------------------
// Dump: Dump an InlineContext entry and all descendants to stdout
//
// Arguments:
//    compiler - compiler instance doing inlining
//    indent   - indentation level for this node

void InlineContext::Dump(Compiler* compiler, int indent)
{
    // Handle fact that siblings are in reverse order.
    if (inlSibling != nullptr)
    {
        inlSibling->Dump(compiler, indent);
    }

    // We may not know callee name in some of the failing cases
    const char* calleeName = nullptr;

    if (inlCallee == nullptr)
    {
        assert(!inlSuccess);
        calleeName = "<unknown>";
    }
    else
    {
        calleeName = compiler->eeGetMethodFullName(inlCallee);
    }

    // Dump this node
    if (inlParent == nullptr)
    {
        // Root method
        printf("Inlines into %s\n", calleeName);
    }
    else
    {
        // Inline attempt.
        const char* inlineReason = inlGetObservationString(inlObservation);
        const char* inlineResult = inlSuccess ? "" : "FAILED: ";

        for (int i = 0; i < indent; i++)
        {
            printf(" ");
        }

        if (inlOffset == BAD_IL_OFFSET)
        {
            printf("[IL=???? TR=%06u] [%s%s] %s\n", inlTreeID, inlineResult, inlineReason, calleeName);
        }
        else
        {
            IL_OFFSET offset = jitGetILoffs(inlOffset);
            printf("[IL=%04d TR=%06u] [%s%s] %s\n", offset, inlTreeID, inlineResult, inlineReason, calleeName);
        }
    }

    // Recurse to first child
    if (inlChild != nullptr)
    {
        inlChild->Dump(compiler, indent + 2);
    }
}

#endif // DEBUG

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
    , inlPolicy(nullptr)
    , inlCall(call)
    , inlCaller(nullptr)
    , inlCallee(nullptr)
    , inlContext(context)
    , inlReported(false)
{
    // Set the policy
    inlPolicy = InlinePolicy::getPolicy(compiler);

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
// Arguments:
//    compiler - the compiler instance doing the prejitting
//    method   - the method in question
//    context  - descrptive string to describe the context of the decision
//
// Notes:
//    Used only during prejitting to try and pre-identify methods that
//    cannot be inlined, to help subsequent jit throughput.
//
//    We use the inlCallee member to track the method since logically
//    it is the callee here.

InlineResult::InlineResult(Compiler*              compiler,
                           CORINFO_METHOD_HANDLE  method,
                           const char*            context)
    : inlCompiler(compiler)
    , inlPolicy(nullptr)
    , inlCall(nullptr)
    , inlCaller(nullptr)
    , inlCallee(method)
    , inlContext(context)
    , inlReported(false)
{
    // Set the policy
    inlPolicy = InlinePolicy::getPolicy(compiler);
}

//------------------------------------------------------------------------
// report: Dump, log, and report information about an inline decision.
//
// Notes:
//    Called (automatically via the InlineResult dtor) when the
//    inliner is done evaluating a candidate.
//
//    Dumps state of the inline candidate, and if a decision was
//    reached, sends it to the log and reports the decision back to the
//    EE. Optionally update the method attribute to NOINLINE if
//    observation and policy warrant.
//
//    All this can be suppressed if desired by calling setReported()
//    before the InlineResult goes out of scope.

void InlineResult::report()
{
    // User may have suppressed reporting via setReported(). If so, do nothing.
    if (inlReported)
    {
        return;
    }

    inlReported = true;

#ifdef DEBUG

    const char* callee = nullptr;

    // Optionally dump the result
    if (VERBOSE)
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s' for '%s' calling '%s'\n";
        const char* caller = (inlCaller == nullptr) ? "n/a" : inlCompiler->eeGetMethodFullName(inlCaller);

        callee = (inlCallee == nullptr) ? "n/a" : inlCompiler->eeGetMethodFullName(inlCallee);

        JITDUMP(format, inlContext, resultString(), reasonString(), caller, callee);
    }

    // If the inline failed, leave information on the call so we can
    // later recover what observation lead to the failure.
    if (isFailure() && (inlCall != nullptr))
    {
        // compiler should have revoked candidacy on the call by now
        assert((inlCall->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);

        inlCall->gtInlineObservation = inlPolicy->getObservation();
    }

#endif // DEBUG

    // Was the result NEVER? If so we might want to propagate this to
    // the runtime.

    if (isNever() && inlPolicy->propagateNeverToRuntime())
    {
        // If we know the callee, and if the observation that got us
        // to this Never inline state is something *other* than
        // IS_NOINLINE, then we've uncovered a reason why this method
        // can't ever be inlined. Update the callee method attributes
        // so that future inline attempts for this callee fail faster.

        InlineObservation obs = inlPolicy->getObservation();

        if ((inlCallee != nullptr) && (obs != InlineObservation::CALLEE_IS_NOINLINE))
        {

#ifdef DEBUG

            if (VERBOSE)
            {
                const char* obsString = inlGetObservationString(obs);
                JITDUMP("\nINLINER: Marking %s as NOINLINE because of %s\n", callee, obsString);
            }

#endif  // DEBUG

            COMP_HANDLE comp = inlCompiler->info.compCompHnd;
            comp->setMethodAttribs(inlCallee, CORINFO_FLG_BAD_INLINEE);
        }
    }


    if (isDecided())
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s'\n";
        JITLOG_THIS(inlCompiler, (LL_INFO100000, format, inlContext, resultString(), reasonString()));
        COMP_HANDLE comp = inlCompiler->info.compCompHnd;
        comp->reportInliningDecision(inlCaller, inlCallee, result(), reasonString());
    }
}
