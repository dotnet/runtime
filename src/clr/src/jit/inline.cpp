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
#if defined(DEBUG) || defined(INLINE_DATA)
    , m_Policy(nullptr)
    , m_Callee(nullptr)
    , m_TreeID(0)
    , m_Ordinal(0)
    , m_Success(true)
#endif // defined(DEBUG) || defined(INLINE_DATA)
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

#if defined(DEBUG) || defined(INLINE_DATA)

    rootContext->m_Callee = compiler->info.compMethodHnd;

#endif // defined(DEBUG) || defined(INLINE_DATA)

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

#if defined(DEBUG) || defined(INLINE_DATA)

    calleeContext->m_Policy = inlineInfo->inlineResult->GetPolicy();
    calleeContext->m_Callee = inlineInfo->fncHandle;
    // +1 here since we construct this context when we know the inline
    // will happen -- but it hasn't actually happened yet, so the count
    // reflects the number of inlines before this one.
    calleeContext->m_Ordinal = compiler->fgInlinedCount + 1;

#endif // defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG)

    calleeContext->m_TreeID = inlineInfo->inlineResult->GetCall()->gtTreeID;

#endif // defined(DEBUG)

    return calleeContext;
}

#if defined(DEBUG) || defined(INLINE_DATA)

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

#if defined(DEBUG)

    failedContext->m_TreeID = inlineResult->GetCall()->gtTreeID;

#endif // defined(DEBUG)

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

#if defined(DEBUG)
        calleeName = compiler->eeGetMethodFullName(m_Callee);
#else
        calleeName = "callee";
#endif // defined(DEBUG)

    }

    mdMethodDef calleeToken =
        compiler->info.compCompHnd->getMethodDefFromMethod(m_Callee);

    // Dump this node
    if (m_Parent == nullptr)
    {
        // Root method
        printf("Inlines into %08X %s\n", calleeToken, calleeName);
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
            printf("[%u IL=???? TR=%06u %08X] [%s%s] %s\n", m_Ordinal, m_TreeID, calleeToken,
                   inlineResult, inlineReason, calleeName);
        }
        else
        {
            IL_OFFSET offset = jitGetILoffs(m_Offset);
            printf("[%u IL=%04d TR=%06u %08X] [%s%s] %s\n", m_Ordinal, offset, m_TreeID, calleeToken,
                   inlineResult, inlineReason, calleeName);
        }
    }

    // Recurse to first child
    if (m_Child != nullptr)
    {
        m_Child->Dump(compiler, indent + 2);
    }
}

//------------------------------------------------------------------------
// DumpData: Dump a successful InlineContext entry, detailed data, and
//  any successful descendant inlines
//
// Arguments:
//    compiler - compiler instance doing inlining
//    indent   - indentation level for this node

void InlineContext::DumpData(Compiler* compiler, int indent)
{
    // Handle fact that siblings are in reverse order.
    if (m_Sibling != nullptr)
    {
        m_Sibling->DumpData(compiler, indent);
    }

#if defined(DEBUG)
    const char* calleeName = compiler->eeGetMethodFullName(m_Callee);
#else
    const char* calleeName = "callee";
#endif // defined(DEBUG)

    if (m_Parent == nullptr)
    {
        // Root method... cons up a policy so we can display the name
        InlinePolicy* policy = InlinePolicy::GetPolicy(compiler, true);
        printf("\nInlines [%u] into \"%s\" [%s]\n", compiler->fgInlinedCount, calleeName, policy->GetName());
    }
    else if (m_Success)
    {
        const char* inlineReason = InlGetObservationString(m_Observation);

        for (int i = 0; i < indent; i++)
        {
            printf(" ");
        }

        printf("%u,\"%s\",\"%s\"", m_Ordinal, inlineReason, calleeName);
        m_Policy->DumpData(stdout);
        printf("\n");
    }

    // Recurse to first child
    if (m_Child != nullptr)
    {
        m_Child->DumpData(compiler, indent + 2);
    }
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular call
// for inlining.
//
// Arguments
//   compiler - the compiler instance examining a call for inlining
//   call     - the call in question
//   context  - descrptive string to describe the context of the decision

InlineResult::InlineResult(Compiler*    compiler,
                           GenTreeCall* call,
                           const char*  context)
    : m_RootCompiler(nullptr)
    , m_Policy(nullptr)
    , m_Call(call)
    , m_Caller(nullptr)
    , m_Callee(nullptr)
    , m_Context(context)
    , m_Reported(false)
{
    // Set the compiler instance
    m_RootCompiler = compiler->impInlineRoot();

    // Set the policy
    const bool isPrejitRoot = false;
    m_Policy = InlinePolicy::GetPolicy(m_RootCompiler, isPrejitRoot);

    // Get method handle for caller. Note we use the
    // handle for the "immediate" caller here.
    m_Caller = compiler->info.compMethodHnd;

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
    : m_RootCompiler(nullptr)
    , m_Policy(nullptr)
    , m_Call(nullptr)
    , m_Caller(nullptr)
    , m_Callee(method)
    , m_Context(context)
    , m_Reported(false)
{
    // Set the compiler instance
    m_RootCompiler = compiler->impInlineRoot();

    // Set the policy
    const bool isPrejitRoot = true;
    m_Policy = InlinePolicy::GetPolicy(m_RootCompiler, isPrejitRoot);
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

#if defined(DEBUG) || defined(INLINE_DATA)

    // Stash a pointer to the latest successful policy for later stats
    // reporting.
    if (IsSuccess())
    {
        m_RootCompiler->inlLastSuccessfulPolicy = GetPolicy();
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

#ifdef DEBUG
    const char* callee = nullptr;

    // Optionally dump the result
    if (VERBOSE)
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s' for '%s' calling '%s'\n";
        const char* caller = (m_Caller == nullptr) ? "n/a" : m_RootCompiler->eeGetMethodFullName(m_Caller);

        callee = (m_Callee == nullptr) ? "n/a" : m_RootCompiler->eeGetMethodFullName(m_Callee);

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

            COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;
            comp->setMethodAttribs(m_Callee, CORINFO_FLG_BAD_INLINEE);
        }
    }

    if (IsDecided())
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s'\n";
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, format, m_Context, ResultString(), ReasonString()));
        COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;
        comp->reportInliningDecision(m_Caller, m_Callee, Result(), ReasonString());
    }
}
