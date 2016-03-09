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
// InlIsValidObservation: run a validity check on an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    true if the observation is valid

bool InlIsValidObservation(InlineObservation obs)
{
    return((obs > InlineObservation::CALLEE_UNUSED_INITIAL) &&
           (obs < InlineObservation::CALLEE_UNUSED_FINAL));
}

#endif // DEBUG

//------------------------------------------------------------------------
// InlGetObservationString: get a string describing this inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the observation

const char* InlGetObservationString(InlineObservation obs)
{
    assert(InlIsValidObservation(obs));
    return InlineDescriptions[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// InlGetTarget: get the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum describing the target

InlineTarget InlGetTarget(InlineObservation obs)
{
    assert(InlIsValidObservation(obs));
    return InlineTargets[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// InlGetTargetString: get a string describing the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the target

const char* InlGetTargetString(InlineObservation obs)
{
    InlineTarget t = InlGetTarget(obs);
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
// InlGetImpact: get the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum value describing the impact

InlineImpact InlGetImpact(InlineObservation obs)
{
    assert(InlIsValidObservation(obs));
    return InlineImpacts[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// InlGetImpactString: get a string describing the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the impact

const char* InlGetImpactString(InlineObservation obs)
{
    InlineImpact i = InlGetImpact(obs);
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
// InlGetCorInfoInlineDecision: translate decision into a CorInfoInline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    CorInfoInline value representing the decision

CorInfoInline InlGetCorInfoInlineDecision(InlineDecision d)
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
// InlGetDecisionString: get a string representing this decision
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    string representing the decision

const char* InlGetDecisionString(InlineDecision d)
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
// InlDecisionIsFailure: check if this decision describes a failing inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is definitely a failure

bool InlDecisionIsFailure(InlineDecision d)
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
// InlDecisionIsSuccess: check if this decision describes a sucessful inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is definitely a success

bool InlDecisionIsSuccess(InlineDecision d)
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
// InlDecisionIsNever: check if this decision describes a never inline
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if the inline is a never inline case

bool InlDecisionIsNever(InlineDecision d)
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
// InlDecisionIsCandidate: check if this decision describes a viable candidate
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if this inline still might happen

bool InlDecisionIsCandidate(InlineDecision d)
{
    return !InlDecisionIsFailure(d);
}

//------------------------------------------------------------------------
// InlDecisionIsDecided: check if this decision has been made
//
// Arguments:
//    d - the decision in question
//
// Return Value:
//    true if this inline has been decided one way or another

bool InlDecisionIsDecided(InlineDecision d)
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
    : m_Parent(nullptr)
    , m_Child(nullptr)
    , m_Sibling(nullptr)
    , m_Offset(BAD_IL_OFFSET)
    , m_Code(nullptr)
    , m_Observation(InlineObservation::CALLEE_UNUSED_INITIAL)
#ifdef DEBUG
    , m_Callee(nullptr)
    , m_TreeID(0)
    , m_Success(true)
#endif
{
    // Empty
}

//------------------------------------------------------------------------
// NewRoot: construct an InlineContext for the root method
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

InlineContext* InlineContext::NewRoot(Compiler* compiler)
{
    InlineContext* rootContext = new (compiler, CMK_Inlining) InlineContext;

#if defined(DEBUG)
    rootContext->m_Callee = compiler->info.compMethodHnd;
#endif

    return rootContext;
}

//------------------------------------------------------------------------
// NewSuccess: construct an InlineContext for a successful inline
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

InlineContext* InlineContext::NewSuccess(Compiler*   compiler,
                                         InlineInfo* inlineInfo)
{
    InlineContext* calleeContext = new (compiler, CMK_Inlining) InlineContext;

    GenTree*       stmt          = inlineInfo->iciStmt;
    BYTE*          calleeIL      = inlineInfo->inlineCandidateInfo->methInfo.ILCode;
    InlineContext* parentContext = stmt->gtStmt.gtInlineContext;

    noway_assert(parentContext != nullptr);

    calleeContext->m_Code = calleeIL;
    calleeContext->m_Parent = parentContext;
    // Push on front here will put siblings in reverse lexical
    // order which we undo in the dumper
    calleeContext->m_Sibling = parentContext->m_Child;
    parentContext->m_Child = calleeContext;
    calleeContext->m_Child = nullptr;
    calleeContext->m_Offset = stmt->AsStmt()->gtStmtILoffsx;
    calleeContext->m_Observation = inlineInfo->inlineResult->GetObservation();
#ifdef DEBUG
    calleeContext->m_Callee = inlineInfo->fncHandle;
    calleeContext->m_TreeID = inlineInfo->inlineResult->GetCall()->gtTreeID;
#endif

    return calleeContext;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// NewFailure: construct an InlineContext for a failing inline
// and link it into the context tree
//
// Arguments:
//    compiler     - compiler doing the inlining
//    stmt         - statement containing the attempted inline
//    inlineResult - inlineResult for the attempt
//
// Return Value:
//    A new InlineContext for diagnostic purposes, or nullptr if
//    the desired context could not be created.

InlineContext* InlineContext::NewFailure(Compiler*     compiler,
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

    failedContext->m_Parent = parentContext;
    // Push on front here will put siblings in reverse lexical
    // order which we undo in the dumper
    failedContext->m_Sibling = parentContext->m_Child;
    parentContext->m_Child = failedContext;
    failedContext->m_Child = nullptr;
    failedContext->m_Offset = stmt->AsStmt()->gtStmtILoffsx;
    failedContext->m_Observation = inlineResult->GetObservation();
    failedContext->m_Callee = inlineResult->GetCallee();
    failedContext->m_Success = false;
    failedContext->m_TreeID = inlineResult->GetCall()->gtTreeID;

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
    if (m_Sibling != nullptr)
    {
        m_Sibling->Dump(compiler, indent);
    }

    // We may not know callee name in some of the failing cases
    const char* calleeName = nullptr;

    if (m_Callee == nullptr)
    {
        assert(!m_Success);
        calleeName = "<unknown>";
    }
    else
    {
        calleeName = compiler->eeGetMethodFullName(m_Callee);
    }

    // Dump this node
    if (m_Parent == nullptr)
    {
        // Root method
        printf("Inlines into %s\n", calleeName);
    }
    else
    {
        // Inline attempt.
        const char* inlineReason = InlGetObservationString(m_Observation);
        const char* inlineResult = m_Success ? "" : "FAILED: ";

        for (int i = 0; i < indent; i++)
        {
            printf(" ");
        }

        if (m_Offset == BAD_IL_OFFSET)
        {
            printf("[IL=???? TR=%06u] [%s%s] %s\n", m_TreeID, inlineResult, inlineReason, calleeName);
        }
        else
        {
            IL_OFFSET offset = jitGetILoffs(m_Offset);
            printf("[IL=%04d TR=%06u] [%s%s] %s\n", offset, m_TreeID, inlineResult, inlineReason, calleeName);
        }
    }

    // Recurse to first child
    if (m_Child != nullptr)
    {
        m_Child->Dump(compiler, indent + 2);
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
    : m_Compiler(compiler)
    , m_Policy(nullptr)
    , m_Call(call)
    , m_Caller(nullptr)
    , m_Callee(nullptr)
    , m_Context(context)
    , m_Reported(false)
{
    // Set the policy
    const bool isPrejitRoot = false;
    m_Policy = InlinePolicy::GetPolicy(m_Compiler, isPrejitRoot);

    // Get method handle for caller
    m_Caller = m_Compiler->info.compMethodHnd;

    // Get method handle for callee, if known
    if (m_Call->gtCall.gtCallType == CT_USER_FUNC)
    {
        m_Callee = m_Call->gtCall.gtCallMethHnd;
    }
}

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular
// method as a possible inline candidate, while prejtting.
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
    : m_Compiler(compiler)
    , m_Policy(nullptr)
    , m_Call(nullptr)
    , m_Caller(nullptr)
    , m_Callee(method)
    , m_Context(context)
    , m_Reported(false)
{
    // Set the policy
    const bool isPrejitRoot = true;
    m_Policy = InlinePolicy::GetPolicy(m_Compiler, isPrejitRoot);
}

//------------------------------------------------------------------------
// Report: Dump, log, and report information about an inline decision.
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

void InlineResult::Report()
{
    // User may have suppressed reporting via setReported(). If so, do nothing.
    if (m_Reported)
    {
        return;
    }

    m_Reported = true;

#ifdef DEBUG

    const char* callee = nullptr;

    // Optionally dump the result
    if (VERBOSE)
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s' for '%s' calling '%s'\n";
        const char* caller = (m_Caller == nullptr) ? "n/a" : m_Compiler->eeGetMethodFullName(m_Caller);

        callee = (m_Callee == nullptr) ? "n/a" : m_Compiler->eeGetMethodFullName(m_Callee);

        JITDUMP(format, m_Context, ResultString(), ReasonString(), caller, callee);
    }

    // If the inline failed, leave information on the call so we can
    // later recover what observation lead to the failure.
    if (IsFailure() && (m_Call != nullptr))
    {
        // compiler should have revoked candidacy on the call by now
        assert((m_Call->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);

        m_Call->gtInlineObservation = m_Policy->GetObservation();
    }

#endif // DEBUG

    // Was the result NEVER? If so we might want to propagate this to
    // the runtime.

    if (IsNever() && m_Policy->PropagateNeverToRuntime())
    {
        // If we know the callee, and if the observation that got us
        // to this Never inline state is something *other* than
        // IS_NOINLINE, then we've uncovered a reason why this method
        // can't ever be inlined. Update the callee method attributes
        // so that future inline attempts for this callee fail faster.

        InlineObservation obs = m_Policy->GetObservation();

        if ((m_Callee != nullptr) && (obs != InlineObservation::CALLEE_IS_NOINLINE))
        {

#ifdef DEBUG

            if (VERBOSE)
            {
                const char* obsString = InlGetObservationString(obs);
                JITDUMP("\nINLINER: Marking %s as NOINLINE because of %s\n", callee, obsString);
            }

#endif  // DEBUG

            COMP_HANDLE comp = m_Compiler->info.compCompHnd;
            comp->setMethodAttribs(m_Callee, CORINFO_FLG_BAD_INLINEE);
        }
    }


    if (IsDecided())
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s'\n";
        JITLOG_THIS(m_Compiler, (LL_INFO100000, format, m_Context, ResultString(), ReasonString()));
        COMP_HANDLE comp = m_Compiler->info.compCompHnd;
        comp->reportInliningDecision(m_Caller, m_Callee, Result(), ReasonString());
    }
}
