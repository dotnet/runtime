// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "inlinepolicy.h"

// Lookup table for inline description strings

static const char* InlineDescriptions[] = {
#define INLINE_OBSERVATION(name, type, description, impact, target) description,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline targets

static const InlineTarget InlineTargets[] = {
#define INLINE_OBSERVATION(name, type, description, impact, target) InlineTarget::target,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline impacts

static const InlineImpact InlineImpacts[] = {
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
    return ((obs > InlineObservation::CALLEE_UNUSED_INITIAL) && (obs < InlineObservation::CALLEE_UNUSED_FINAL));
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
    switch (d)
    {
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
    switch (d)
    {
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
    switch (d)
    {
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
    switch (d)
    {
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
    switch (d)
    {
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
    switch (d)
    {
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

InlineContext::InlineContext(InlineStrategy* strategy)
    : m_InlineStrategy(strategy)
    , m_Parent(nullptr)
    , m_Child(nullptr)
    , m_Sibling(nullptr)
    , m_Code(nullptr)
    , m_ILSize(0)
    , m_ImportedILSize(0)
    , m_Offset(BAD_IL_OFFSET)
    , m_Observation(InlineObservation::CALLEE_UNUSED_INITIAL)
    , m_CodeSizeEstimate(0)
    , m_Success(true)
    , m_Devirtualized(false)
    , m_Guarded(false)
    , m_Unboxed(false)
#if defined(DEBUG) || defined(INLINE_DATA)
    , m_Policy(nullptr)
    , m_Callee(nullptr)
    , m_TreeID(0)
    , m_Ordinal(0)
#endif // defined(DEBUG) || defined(INLINE_DATA)
{
    // Empty
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// Dump: Dump an InlineContext entry and all descendants to jitstdout
//
// Arguments:
//    indent   - indentation level for this node

void InlineContext::Dump(unsigned indent)
{
    // Handle fact that siblings are in reverse order.
    if (m_Sibling != nullptr)
    {
        m_Sibling->Dump(indent);
    }

    // We may not know callee name in some of the failing cases
    Compiler*   compiler   = m_InlineStrategy->GetCompiler();
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
        calleeName         = "callee";
#endif // defined(DEBUG)
    }

    mdMethodDef calleeToken = compiler->info.compCompHnd->getMethodDefFromMethod(m_Callee);

    // Dump this node
    if (m_Parent == nullptr)
    {
        // Root method
        InlinePolicy* policy = InlinePolicy::GetPolicy(compiler, true);
        printf("Inlines into %08X [via %s] %s\n", calleeToken, policy->GetName(), calleeName);
    }
    else
    {
        // Inline attempt.
        const char* inlineReason  = InlGetObservationString(m_Observation);
        const char* inlineResult  = m_Success ? "" : "FAILED: ";
        const char* devirtualized = m_Devirtualized ? " devirt" : "";
        const char* guarded       = m_Guarded ? " guarded" : "";
        const char* unboxed       = m_Unboxed ? " unboxed" : "";

        if (m_Offset == BAD_IL_OFFSET)
        {
            printf("%*s[%u IL=???? TR=%06u %08X] [%s%s%s%s%s] %s\n", indent, "", m_Ordinal, m_TreeID, calleeToken,
                   inlineResult, inlineReason, guarded, devirtualized, unboxed, calleeName);
        }
        else
        {
            IL_OFFSET offset = jitGetILoffs(m_Offset);
            printf("%*s[%u IL=%04d TR=%06u %08X] [%s%s%s%s%s] %s\n", indent, "", m_Ordinal, offset, m_TreeID,
                   calleeToken, inlineResult, inlineReason, guarded, devirtualized, unboxed, calleeName);
        }
    }

    // Recurse to first child
    if (m_Child != nullptr)
    {
        m_Child->Dump(indent + 2);
    }
}

//------------------------------------------------------------------------
// DumpData: Dump a successful InlineContext entry, detailed data, and
//  any successful descendant inlines
//
// Arguments:
//    indent   - indentation level for this node

void InlineContext::DumpData(unsigned indent)
{
    // Handle fact that siblings are in reverse order.
    if (m_Sibling != nullptr)
    {
        m_Sibling->DumpData(indent);
    }

    Compiler* compiler = m_InlineStrategy->GetCompiler();

#if defined(DEBUG)
    const char* calleeName = compiler->eeGetMethodFullName(m_Callee);
#else
    const char* calleeName = "callee";
#endif // defined(DEBUG)

    if (m_Parent == nullptr)
    {
        // Root method... cons up a policy so we can display the name
        InlinePolicy* policy = InlinePolicy::GetPolicy(compiler, true);
        printf("\nInlines [%u] into \"%s\" [%s]\n", m_InlineStrategy->GetInlineCount(), calleeName, policy->GetName());
    }
    else if (m_Success)
    {
        const char* inlineReason = InlGetObservationString(m_Observation);
        printf("%*s%u,\"%s\",\"%s\",", indent, "", m_Ordinal, inlineReason, calleeName);
        m_Policy->DumpData(jitstdout);
        printf("\n");
    }

    // Recurse to first child
    if (m_Child != nullptr)
    {
        m_Child->DumpData(indent + 2);
    }
}

//------------------------------------------------------------------------
// DumpXml: Dump an InlineContext entry and all descendants in xml format
//
// Arguments:
//    file     - file for output
//    indent   - indentation level for this node

void InlineContext::DumpXml(FILE* file, unsigned indent)
{
    // Handle fact that siblings are in reverse order.
    if (m_Sibling != nullptr)
    {
        m_Sibling->DumpXml(file, indent);
    }

    // Optionally suppress failing inline records
    if ((JitConfig.JitInlineDumpXml() == 3) && !m_Success)
    {
        return;
    }

    const bool  isRoot     = m_Parent == nullptr;
    const bool  hasChild   = m_Child != nullptr;
    const char* inlineType = m_Success ? "Inline" : "FailedInline";
    unsigned    newIndent  = indent;

    if (!isRoot)
    {
        Compiler* compiler = m_InlineStrategy->GetCompiler();

        mdMethodDef calleeToken  = compiler->info.compCompHnd->getMethodDefFromMethod(m_Callee);
        unsigned    calleeHash   = compiler->compMethodHash(m_Callee);
        const char* inlineReason = InlGetObservationString(m_Observation);

        int offset = -1;
        if (m_Offset != BAD_IL_OFFSET)
        {
            offset = (int)jitGetILoffs(m_Offset);
        }

        fprintf(file, "%*s<%s>\n", indent, "", inlineType);
        fprintf(file, "%*s<Token>%08x</Token>\n", indent + 2, "", calleeToken);
        fprintf(file, "%*s<Hash>%08x</Hash>\n", indent + 2, "", calleeHash);
        fprintf(file, "%*s<Offset>%u</Offset>\n", indent + 2, "", offset);
        fprintf(file, "%*s<Reason>%s</Reason>\n", indent + 2, "", inlineReason);

        // Optionally, dump data about the inline
        const int dumpDataSetting = JitConfig.JitInlineDumpData();

        // JitInlineDumpData=1 -- dump data plus deltas for last inline only
        if ((dumpDataSetting == 1) && (this == m_InlineStrategy->GetLastContext()))
        {
            fprintf(file, "%*s<Data>", indent + 2, "");
            m_InlineStrategy->DumpDataContents(file);
            fprintf(file, "</Data>\n");
        }

        // JitInlineDumpData=2 -- dump data for all inlines, no deltas
        if ((dumpDataSetting == 2) && (m_Policy != nullptr))
        {
            fprintf(file, "%*s<Data>", indent + 2, "");
            m_Policy->DumpData(file);
            fprintf(file, "</Data>\n");
        }

        newIndent = indent + 2;
    }

    // Handle children

    if (hasChild)
    {
        fprintf(file, "%*s<Inlines>\n", newIndent, "");
        m_Child->DumpXml(file, newIndent + 2);
        fprintf(file, "%*s</Inlines>\n", newIndent, "");
    }
    else
    {
        fprintf(file, "%*s<Inlines />\n", newIndent, "");
    }

    // Close out

    if (!isRoot)
    {
        fprintf(file, "%*s</%s>\n", indent, "", inlineType);
    }
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular call
// for inlining.
//
// Arguments:
//   compiler      - the compiler instance examining a call for inlining
//   call          - the call in question
//   stmt          - statement containing the call (if known)
//   description   - string describing the context of the decision

InlineResult::InlineResult(Compiler* compiler, GenTreeCall* call, Statement* stmt, const char* description)
    : m_RootCompiler(nullptr)
    , m_Policy(nullptr)
    , m_Call(call)
    , m_InlineContext(nullptr)
    , m_Caller(nullptr)
    , m_Callee(nullptr)
    , m_ImportedILSize(0)
    , m_Description(description)
    , m_Reported(false)
{
    // Set the compiler instance
    m_RootCompiler = compiler->impInlineRoot();

    // Set the policy
    const bool isPrejitRoot = false;
    m_Policy                = InlinePolicy::GetPolicy(m_RootCompiler, isPrejitRoot);

    // Pass along some optional information to the policy.
    if (stmt != nullptr)
    {
        m_InlineContext = stmt->GetInlineContext();
        m_Policy->NoteContext(m_InlineContext);

#if defined(DEBUG) || defined(INLINE_DATA)
        m_Policy->NoteOffset(call->gtRawILOffset);
#else
        m_Policy->NoteOffset(stmt->GetILOffsetX());
#endif // defined(DEBUG) || defined(INLINE_DATA)
    }

    // Get method handle for caller. Note we use the
    // handle for the "immediate" caller here.
    m_Caller = compiler->info.compMethodHnd;

    // Get method handle for callee, if known
    if (m_Call->AsCall()->gtCallType == CT_USER_FUNC)
    {
        m_Callee = m_Call->AsCall()->gtCallMethHnd;
    }
}

//------------------------------------------------------------------------
// InlineResult: Construct an InlineResult to evaluate a particular
// method as a possible inline candidate, while prejtting.
//
// Arguments:
//    compiler    - the compiler instance doing the prejitting
//    method      - the method in question
//    description - string describing the context of the decision
//
// Notes:
//    Used only during prejitting to try and pre-identify methods that
//    cannot be inlined, to help subsequent jit throughput.
//
//    We use the inlCallee member to track the method since logically
//    it is the callee here.

InlineResult::InlineResult(Compiler* compiler, CORINFO_METHOD_HANDLE method, const char* description)
    : m_RootCompiler(nullptr)
    , m_Policy(nullptr)
    , m_Call(nullptr)
    , m_InlineContext(nullptr)
    , m_Caller(nullptr)
    , m_Callee(method)
    , m_Description(description)
    , m_Reported(false)
{
    // Set the compiler instance
    m_RootCompiler = compiler->impInlineRoot();

    // Set the policy
    const bool isPrejitRoot = true;
    m_Policy                = InlinePolicy::GetPolicy(m_RootCompiler, isPrejitRoot);
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

#ifdef DEBUG
    // If this is a failure of a specific inline candidate and we haven't captured
    // a failing observation yet, do so now.
    if (IsFailure() && (m_Call != nullptr))
    {
        // compiler should have revoked candidacy on the call by now
        assert((m_Call->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);

        if (m_Call->gtInlineObservation == InlineObservation::CALLEE_UNUSED_INITIAL)
        {
            m_Call->gtInlineObservation = m_Policy->GetObservation();
        }
    }
#endif // DEBUG

    // If we weren't actually inlining, user may have suppressed
    // reporting via setReported(). If so, do nothing.
    if (m_Reported)
    {
        return;
    }

    m_Reported = true;

#ifdef DEBUG
    const char* callee = nullptr;

    // Optionally dump the result
    if (VERBOSE || m_RootCompiler->fgPrintInlinedMethods)
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s' for '%s' calling '%s'\n";
        const char* caller = (m_Caller == nullptr) ? "n/a" : m_RootCompiler->eeGetMethodFullName(m_Caller);

        callee = (m_Callee == nullptr) ? "n/a" : m_RootCompiler->eeGetMethodFullName(m_Callee);

        JITDUMP(format, m_Description, ResultString(), ReasonString(), caller, callee);
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

            const char* obsString = InlGetObservationString(obs);

            if (VERBOSE)
            {
                JITDUMP("\nINLINER: Marking %s as NOINLINE because of %s\n", callee, obsString);
            }
            else if (m_RootCompiler->fgPrintInlinedMethods)
            {
                printf("Marking %s as NOINLINE because of %s\n", callee, obsString);
            }

#endif // DEBUG

            COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;
            comp->setMethodAttribs(m_Callee, CORINFO_FLG_BAD_INLINEE);
        }
    }

    if (IsDecided())
    {
        const char* format = "INLINER: during '%s' result '%s' reason '%s'\n";
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, format, m_Description, ResultString(), ReasonString()));
        COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;
        comp->reportInliningDecision(m_Caller, m_Callee, Result(), ReasonString());
    }
}

//------------------------------------------------------------------------
// InlineStrategy construtor
//
// Arguments
//    compiler - root compiler instance

InlineStrategy::InlineStrategy(Compiler* compiler)
    : m_Compiler(compiler)
    , m_RootContext(nullptr)
    , m_LastSuccessfulPolicy(nullptr)
    , m_LastContext(nullptr)
    , m_PrejitRootDecision(InlineDecision::UNDECIDED)
    , m_CallCount(0)
    , m_CandidateCount(0)
    , m_AlwaysCandidateCount(0)
    , m_ForceCandidateCount(0)
    , m_DiscretionaryCandidateCount(0)
    , m_UnprofitableCandidateCount(0)
    , m_ImportCount(0)
    , m_InlineCount(0)
    , m_MaxInlineSize(DEFAULT_MAX_INLINE_SIZE)
    , m_MaxInlineDepth(DEFAULT_MAX_INLINE_DEPTH)
    , m_InitialTimeBudget(0)
    , m_InitialTimeEstimate(0)
    , m_CurrentTimeBudget(0)
    , m_CurrentTimeEstimate(0)
    , m_InitialSizeEstimate(0)
    , m_CurrentSizeEstimate(0)
    , m_HasForceViaDiscretionary(false)
#if defined(DEBUG) || defined(INLINE_DATA)
    , m_MethodXmlFilePosition(0)
    , m_Random(nullptr)
#endif // defined(DEBUG) || defined(INLINE_DATA)

{
    // Verify compiler is a root compiler instance
    assert(m_Compiler->impInlineRoot() == m_Compiler);

#ifdef DEBUG

    // Possibly modify the max inline size.
    //
    // Default value of JitInlineSize is the same as our default.
    // So normally this next line does not change the size.
    m_MaxInlineSize = JitConfig.JitInlineSize();

    // Up the max size under stress
    if (m_Compiler->compInlineStress())
    {
        m_MaxInlineSize *= 10;
    }

    // But don't overdo it
    if (m_MaxInlineSize > IMPLEMENTATION_MAX_INLINE_SIZE)
    {
        m_MaxInlineSize = IMPLEMENTATION_MAX_INLINE_SIZE;
    }

    // Verify: not too small, not too big.
    assert(m_MaxInlineSize >= ALWAYS_INLINE_SIZE);
    assert(m_MaxInlineSize <= IMPLEMENTATION_MAX_INLINE_SIZE);

    // Possibly modify the max inline depth
    //
    // Default value of JitInlineDepth is the same as our default.
    // So normally this next line does not change the size.
    m_MaxInlineDepth = JitConfig.JitInlineDepth();

    // But don't overdo it
    if (m_MaxInlineDepth > IMPLEMENTATION_MAX_INLINE_DEPTH)
    {
        m_MaxInlineDepth = IMPLEMENTATION_MAX_INLINE_DEPTH;
    }

#endif // DEBUG
}

//------------------------------------------------------------------------
// GetRootContext: get the InlineContext for the root method
//
// Return Value:
//    Root context; describes the method being jitted.
//
// Note:
//    Also initializes the jit time estimate and budget.

InlineContext* InlineStrategy::GetRootContext()
{
    if (m_RootContext == nullptr)
    {
        // Allocate on first demand.
        m_RootContext = NewRoot();

        // Estimate how long the jit will take if there's no inlining
        // done to this method.
        m_InitialTimeEstimate = EstimateTime(m_RootContext);
        m_CurrentTimeEstimate = m_InitialTimeEstimate;

        // Set the initial budget for inlining. Note this is
        // deliberately set very high and is intended to catch
        // only pathological runaway inline cases.
        m_InitialTimeBudget = BUDGET * m_InitialTimeEstimate;
        m_CurrentTimeBudget = m_InitialTimeBudget;

        // Estimate the code size  if there's no inlining
        m_InitialSizeEstimate = EstimateSize(m_RootContext);
        m_CurrentSizeEstimate = m_InitialSizeEstimate;

        // Sanity check
        assert(m_CurrentTimeEstimate > 0);
        assert(m_CurrentSizeEstimate > 0);

        // Cache as the "last" context created
        m_LastContext = m_RootContext;
    }

    return m_RootContext;
}

//------------------------------------------------------------------------
// NoteAttempt: do bookkeeping for an inline attempt
//
// Arguments:
//    result -- InlineResult for successful inline candidate

void InlineStrategy::NoteAttempt(InlineResult* result)
{
    assert(result->IsCandidate());
    InlineObservation obs = result->GetObservation();

    if (obs == InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE)
    {
        m_AlwaysCandidateCount++;
    }
    else if (obs == InlineObservation::CALLEE_IS_FORCE_INLINE)
    {
        m_ForceCandidateCount++;
    }
    else
    {
        m_DiscretionaryCandidateCount++;
    }
}

//------------------------------------------------------------------------
// DumpCsvHeader: dump header for csv inline stats
//
// Argument:
//     fp -- file for dump output

void InlineStrategy::DumpCsvHeader(FILE* fp)
{
    fprintf(fp, "\"InlineCalls\",");
    fprintf(fp, "\"InlineCandidates\",");
    fprintf(fp, "\"InlineAlways\",");
    fprintf(fp, "\"InlineForce\",");
    fprintf(fp, "\"InlineDiscretionary\",");
    fprintf(fp, "\"InlineUnprofitable\",");
    fprintf(fp, "\"InlineEarlyFail\",");
    fprintf(fp, "\"InlineImport\",");
    fprintf(fp, "\"InlineLateFail\",");
    fprintf(fp, "\"InlineSuccess\",");
}

//------------------------------------------------------------------------
// DumpCsvData: dump data for csv inline stats
//
// Argument:
//     fp -- file for dump output

void InlineStrategy::DumpCsvData(FILE* fp)
{
    fprintf(fp, "%u,", m_CallCount);
    fprintf(fp, "%u,", m_CandidateCount);
    fprintf(fp, "%u,", m_AlwaysCandidateCount);
    fprintf(fp, "%u,", m_ForceCandidateCount);
    fprintf(fp, "%u,", m_DiscretionaryCandidateCount);
    fprintf(fp, "%u,", m_UnprofitableCandidateCount);

    // Early failures are cases where candates are rejected between
    // the time the jit invokes the inlinee compiler and the time it
    // starts to import the inlinee IL.
    //
    // So they are "cheaper" that late failures.

    unsigned profitableCandidateCount = m_DiscretionaryCandidateCount - m_UnprofitableCandidateCount;

    unsigned earlyFailCount =
        m_CandidateCount - m_AlwaysCandidateCount - m_ForceCandidateCount - profitableCandidateCount;

    fprintf(fp, "%u,", earlyFailCount);

    unsigned lateFailCount = m_ImportCount - m_InlineCount;

    fprintf(fp, "%u,", m_ImportCount);
    fprintf(fp, "%u,", lateFailCount);
    fprintf(fp, "%u,", m_InlineCount);
}

//------------------------------------------------------------------------
// EstimateTime: estimate impact of this inline on the method jit time
//
// Arguments:
//     context - context describing this inline
//
// Return Value:
//    Nominal estimate of jit time.

int InlineStrategy::EstimateTime(InlineContext* context)
{
    // Simple linear models based on observations
    // show time is fairly well predicted by IL size.
    //
    // Prediction varies for root and inlines.
    if (context == m_RootContext)
    {
        return EstimateRootTime(context->GetILSize());
    }
    else
    {
        // Use amount of IL actually imported
        return EstimateInlineTime(context->GetImportedILSize());
    }
}

//------------------------------------------------------------------------
// EstimteRootTime: estimate jit time for method of this size with
// no inlining.
//
// Arguments:
//    ilSize - size of the method's IL
//
// Return Value:
//    Nominal estimate of jit time.
//
// Notes:
//    Based on observational data. Time is nominally microseconds.

int InlineStrategy::EstimateRootTime(unsigned ilSize)
{
    return 60 + 3 * ilSize;
}

//------------------------------------------------------------------------
// EstimteInlineTime: estimate time impact on jitting for an inline
// of this size.
//
// Arguments:
//    ilSize - size of the method's IL
//
// Return Value:
//    Nominal increase in jit time.
//
// Notes:
//    Based on observational data. Time is nominally microseconds.
//    Small inlines will make the jit a bit faster.

int InlineStrategy::EstimateInlineTime(unsigned ilSize)
{
    return -14 + 2 * ilSize;
}

//------------------------------------------------------------------------
// EstimateSize: estimate impact of this inline on the method size
//
// Arguments:
//     context - context describing this inline
//
// Return Value:
//    Nominal estimate of method size (bytes * 10)

int InlineStrategy::EstimateSize(InlineContext* context)
{
    // Prediction varies for root and inlines.
    if (context == m_RootContext)
    {
        // Simple linear models based on observations show root method
        // native code size is fairly well predicted by IL size.
        //
        // Model below is for x64 on windows.
        unsigned ilSize   = context->GetILSize();
        int      estimate = (1312 + 228 * ilSize) / 10;

        return estimate;
    }
    else
    {
        // Use context's code size estimate.
        return context->GetCodeSizeEstimate();
    }
}

//------------------------------------------------------------------------
// NoteOutcome: do bookkeeping for an inline
//
// Arguments:
//    context - context for the inlie

void InlineStrategy::NoteOutcome(InlineContext* context)
{
    // Note we can't generally count up failures here -- we only
    // create contexts for failures in debug modes, and even then
    // we may not get them all.
    if (context->IsSuccess())
    {
        m_InlineCount++;

#if defined(DEBUG) || defined(INLINE_DATA)

        // Keep track of the inline targeted for data collection or,
        // if we don't have one (yet), the last successful inline.
        bool updateLast = (m_LastSuccessfulPolicy == nullptr) || !m_LastSuccessfulPolicy->IsDataCollectionTarget();

        if (updateLast)
        {
            m_LastContext          = context;
            m_LastSuccessfulPolicy = context->m_Policy;
        }
        else
        {
            // We only expect one inline to be a data collection
            // target.
            assert(!context->m_Policy->IsDataCollectionTarget());
        }

#endif // defined(DEBUG) || defined(INLINE_DATA)

        // Budget update.
        //
        // If callee is a force inline, increase budget, provided all
        // parent contexts are likewise force inlines.
        //
        // If callee is discretionary or has a discretionary ancestor,
        // increase expense.

        InlineContext* currentContext = context;
        bool           isForceInline  = false;

        while (currentContext != m_RootContext)
        {
            InlineObservation observation = currentContext->GetObservation();

            if (observation != InlineObservation::CALLEE_IS_FORCE_INLINE)
            {
                if (isForceInline)
                {
                    // Interesting case where discretionary inlines pull
                    // in a force inline...
                    m_HasForceViaDiscretionary = true;
                }

                isForceInline = false;
                break;
            }

            isForceInline  = true;
            currentContext = currentContext->GetParent();
        }

        int timeDelta = EstimateTime(context);

        if (isForceInline)
        {
            // Update budget since this inline was forced.  Only allow
            // budget to increase.
            if (timeDelta > 0)
            {
                m_CurrentTimeBudget += timeDelta;
            }
        }

        // Update time estimate.
        m_CurrentTimeEstimate += timeDelta;

        // Update size estimate.
        //
        // Sometimes estimates don't make sense. Don't let the method
        // size go negative.
        int sizeDelta = EstimateSize(context);

        if (m_CurrentSizeEstimate + sizeDelta <= 0)
        {
            sizeDelta = 0;
        }

        // Update the code size estimate.
        m_CurrentSizeEstimate += sizeDelta;
    }
}

//------------------------------------------------------------------------
// BudgetCheck: return true if an inline of this size would likely
//     exceed the jit time budget for this method
//
// Arguments:
//     ilSize - size of the method's IL
//
// Return Value:
//     true if the inline would go over budget
//
// Notes:
//     Presumes all IL in the method will be imported.

bool InlineStrategy::BudgetCheck(unsigned ilSize)
{
    const int  timeDelta = EstimateInlineTime(ilSize);
    const bool result    = (timeDelta + m_CurrentTimeEstimate > m_CurrentTimeBudget);

    if (result)
    {
        JITDUMP("\nBudgetCheck: for IL Size %d, timeDelta %d +  currentEstimate %d > currentBudget %d\n", ilSize,
                timeDelta, m_CurrentTimeEstimate, m_CurrentTimeBudget);
    }

    return result;
}

//------------------------------------------------------------------------
// NewRoot: construct an InlineContext for the root method
//
// Return Value:
//    InlineContext for use as the root context
//
// Notes:
//    We leave m_Code as nullptr here (rather than the IL buffer
//    address of the root method) to preserve existing behavior, which
//    is to allow one recursive inline.

InlineContext* InlineStrategy::NewRoot()
{
    InlineContext* rootContext = new (m_Compiler, CMK_Inlining) InlineContext(this);

    rootContext->m_ILSize = m_Compiler->info.compILCodeSize;
    rootContext->m_Code   = m_Compiler->info.compCode;

#if defined(DEBUG) || defined(INLINE_DATA)

    rootContext->m_Callee = m_Compiler->info.compMethodHnd;

#endif // defined(DEBUG) || defined(INLINE_DATA)

    return rootContext;
}

//------------------------------------------------------------------------
// NewSuccess: construct an InlineContext for a successful inline
// and link it into the context tree
//
// Arguments:
//    inlineInfo - information about this inline
//
// Return Value:
//    A new InlineContext for statements brought into the method by
//    this inline.

InlineContext* InlineStrategy::NewSuccess(InlineInfo* inlineInfo)
{
    InlineContext* calleeContext = new (m_Compiler, CMK_Inlining) InlineContext(this);
    Statement*     stmt          = inlineInfo->iciStmt;
    BYTE*          calleeIL      = inlineInfo->inlineCandidateInfo->methInfo.ILCode;
    unsigned       calleeILSize  = inlineInfo->inlineCandidateInfo->methInfo.ILCodeSize;
    InlineContext* parentContext = stmt->GetInlineContext();
    GenTreeCall*   originalCall  = inlineInfo->inlineResult->GetCall();

    noway_assert(parentContext != nullptr);

    calleeContext->m_Code   = calleeIL;
    calleeContext->m_ILSize = calleeILSize;
    calleeContext->m_Parent = parentContext;
    // Push on front here will put siblings in reverse lexical
    // order which we undo in the dumper
    calleeContext->m_Sibling        = parentContext->m_Child;
    parentContext->m_Child          = calleeContext;
    calleeContext->m_Child          = nullptr;
    calleeContext->m_Offset         = stmt->GetILOffsetX();
    calleeContext->m_Observation    = inlineInfo->inlineResult->GetObservation();
    calleeContext->m_Success        = true;
    calleeContext->m_Devirtualized  = originalCall->IsDevirtualized();
    calleeContext->m_Guarded        = originalCall->IsGuarded();
    calleeContext->m_Unboxed        = originalCall->IsUnboxed();
    calleeContext->m_ImportedILSize = inlineInfo->inlineResult->GetImportedILSize();

#if defined(DEBUG) || defined(INLINE_DATA)

    InlinePolicy* policy = inlineInfo->inlineResult->GetPolicy();

    calleeContext->m_Policy           = policy;
    calleeContext->m_CodeSizeEstimate = policy->CodeSizeEstimate();
    calleeContext->m_Callee           = inlineInfo->fncHandle;
    // +1 here since we set this before calling NoteOutcome.
    calleeContext->m_Ordinal = m_InlineCount + 1;
    // Update offset with more accurate info
    calleeContext->m_Offset = originalCall->gtRawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG)

    calleeContext->m_TreeID = originalCall->gtTreeID;

#endif // defined(DEBUG)

    NoteOutcome(calleeContext);

    return calleeContext;
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// NewFailure: construct an InlineContext for a failing inline
// and link it into the context tree
//
// Arguments:
//    stmt         - statement containing the attempted inline
//    inlineResult - inlineResult for the attempt
//
// Return Value:
//    A new InlineContext for diagnostic purposes

InlineContext* InlineStrategy::NewFailure(Statement* stmt, InlineResult* inlineResult)
{
    // Check for a parent context first. We should now have a parent
    // context for all statements.
    InlineContext* parentContext = stmt->GetInlineContext();
    assert(parentContext != nullptr);
    InlineContext* failedContext = new (m_Compiler, CMK_Inlining) InlineContext(this);
    GenTreeCall*   originalCall  = inlineResult->GetCall();

    // Pushing the new context on the front of the parent child list
    // will put siblings in reverse lexical order which we undo in the
    // dumper.
    failedContext->m_Parent        = parentContext;
    failedContext->m_Sibling       = parentContext->m_Child;
    parentContext->m_Child         = failedContext;
    failedContext->m_Child         = nullptr;
    failedContext->m_Offset        = stmt->GetILOffsetX();
    failedContext->m_Observation   = inlineResult->GetObservation();
    failedContext->m_Callee        = inlineResult->GetCallee();
    failedContext->m_Success       = false;
    failedContext->m_Devirtualized = originalCall->IsDevirtualized();
    failedContext->m_Guarded       = originalCall->IsGuarded();
    failedContext->m_Unboxed       = originalCall->IsUnboxed();

    assert(InlIsValidObservation(failedContext->m_Observation));

#if defined(DEBUG) || defined(INLINE_DATA)

    // Update offset with more accurate info
    failedContext->m_Offset = originalCall->gtRawILOffset;

#endif // #if defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG)

    failedContext->m_TreeID = originalCall->gtTreeID;

#endif // defined(DEBUG)

    NoteOutcome(failedContext);

    return failedContext;
}

//------------------------------------------------------------------------
// Dump: dump description of inline behavior
//
// Arguments:
//   showBudget - also dump final budget values

void InlineStrategy::Dump(bool showBudget)
{
    m_RootContext->Dump();

    if (!showBudget)
    {
        return;
    }

    printf("Budget: initialTime=%d, finalTime=%d, initialBudget=%d, currentBudget=%d\n", m_InitialTimeEstimate,
           m_CurrentTimeEstimate, m_InitialTimeBudget, m_CurrentTimeBudget);

    if (m_CurrentTimeBudget > m_InitialTimeBudget)
    {
        printf("Budget: increased by %d because of force inlines\n", m_CurrentTimeBudget - m_InitialTimeBudget);
    }

    if (m_CurrentTimeEstimate > m_CurrentTimeBudget)
    {
        printf("Budget: went over budget by %d\n", m_CurrentTimeEstimate - m_CurrentTimeBudget);
    }

    if (m_HasForceViaDiscretionary)
    {
        printf("Budget: discretionary inline caused a force inline\n");
    }

    printf("Budget: initialSize=%d, finalSize=%d\n", m_InitialSizeEstimate, m_CurrentSizeEstimate);
}

// Static to track emission of the inline data header

bool InlineStrategy::s_HasDumpedDataHeader = false;

//------------------------------------------------------------------------
// DumpData: dump data about the last successful inline into this method
// in a format suitable for automated analysis.

void InlineStrategy::DumpData()
{
    // Is dumping enabled? If not, nothing to do.
    if (JitConfig.JitInlineDumpData() == 0)
    {
        return;
    }

    // If we're also dumping inline XML, we'll let it dump the data.
    if (JitConfig.JitInlineDumpXml() != 0)
    {
        return;
    }

    // Don't dump anything if limiting is on and we didn't reach
    // the limit while inlining.
    //
    // This serves to filter out duplicate data.
    const int limit = JitConfig.JitInlineLimit();

    if ((limit >= 0) && (m_InlineCount < static_cast<unsigned>(limit)))
    {
        return;
    }

    // Dump header, if not already dumped
    if (!s_HasDumpedDataHeader)
    {
        DumpDataHeader(stderr);
        s_HasDumpedDataHeader = true;
    }

    // Dump contents
    DumpDataContents(stderr);
    fprintf(stderr, "\n");
}

//------------------------------------------------------------------------
// DumpDataEnsurePolicyIsSet: ensure m_LastSuccessfulPolicy describes the
//    inline policy in effect.
//
// Notes:
//    Needed for methods that don't have any successful inlines.

void InlineStrategy::DumpDataEnsurePolicyIsSet()
{
    // Cache references to compiler substructures.
    const Compiler::Info&    info = m_Compiler->info;
    const Compiler::Options& opts = m_Compiler->opts;

    // If there weren't any successful inlines, we won't have a
    // successful policy, so fake one up.
    if (m_LastSuccessfulPolicy == nullptr)
    {
        const bool isPrejitRoot = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT);
        m_LastSuccessfulPolicy  = InlinePolicy::GetPolicy(m_Compiler, isPrejitRoot);

        // Add in a bit of data....
        const bool isForceInline = (info.compFlags & CORINFO_FLG_FORCEINLINE) != 0;
        m_LastSuccessfulPolicy->NoteBool(InlineObservation::CALLEE_IS_FORCE_INLINE, isForceInline);
        m_LastSuccessfulPolicy->NoteInt(InlineObservation::CALLEE_IL_CODE_SIZE, info.compMethodInfo->ILCodeSize);
    }
}

//------------------------------------------------------------------------
// DumpDataHeader: dump header for inline data.
//
// Arguments:
//    file - file for data output

void InlineStrategy::DumpDataHeader(FILE* file)
{
    DumpDataEnsurePolicyIsSet();
    const int limit = JitConfig.JitInlineLimit();
    fprintf(file, "*** Inline Data: Policy=%s JitInlineLimit=%d ***\n", m_LastSuccessfulPolicy->GetName(), limit);
    DumpDataSchema(file);
    fprintf(file, "\n");
}

//------------------------------------------------------------------------
// DumpSchema: dump schema for inline data.
//
// Arguments:
//    file - file for data output

void InlineStrategy::DumpDataSchema(FILE* file)
{
    DumpDataEnsurePolicyIsSet();
    fprintf(file, "Method,Version,HotSize,ColdSize,JitTime,SizeEstimate,TimeEstimate,");
    m_LastSuccessfulPolicy->DumpSchema(file);
}

//------------------------------------------------------------------------
// DumpDataContents: dump contents of inline data
//
// Arguments:
//    file - file for data output

void InlineStrategy::DumpDataContents(FILE* file)
{
    DumpDataEnsurePolicyIsSet();

    // Cache references to compiler substructures.
    const Compiler::Info& info = m_Compiler->info;

    // We'd really like the method identifier to be unique and
    // durable across crossgen invocations. Not clear how to
    // accomplish this, so we'll use the token for now.
    //
    // Post processing will have to filter out all data from
    // methods where the root entry appears multiple times.
    mdMethodDef currentMethodToken = info.compCompHnd->getMethodDefFromMethod(info.compMethodHnd);

    // Convert time spent jitting into microseconds
    unsigned         microsecondsSpentJitting = 0;
    unsigned __int64 compCycles               = m_Compiler->getInlineCycleCount();
    if (compCycles > 0)
    {
        double countsPerSec      = CachedCyclesPerSecond();
        double counts            = (double)compCycles;
        microsecondsSpentJitting = (unsigned)((counts / countsPerSec) * 1000 * 1000);
    }

    fprintf(file, "%08X,%u,%u,%u,%u,%d,%d,", currentMethodToken, m_InlineCount, info.compTotalHotCodeSize,
            info.compTotalColdCodeSize, microsecondsSpentJitting, m_CurrentSizeEstimate / 10, m_CurrentTimeEstimate);
    m_LastSuccessfulPolicy->DumpData(file);
}

// Static to track emission of the xml data header
// and lock to prevent interleaved file writes

bool          InlineStrategy::s_HasDumpedXmlHeader = false;
CritSecObject InlineStrategy::s_XmlWriterLock;

//------------------------------------------------------------------------
// DumpXml: dump xml-formatted version of the inline tree.
//
// Arguments
//    file - file for data output
//    indent - indent level of this element

void InlineStrategy::DumpXml(FILE* file, unsigned indent)
{
    if (JitConfig.JitInlineDumpXml() == 0)
    {
        return;
    }

    // Lock to prevent interleaving of trees.
    CritSecHolder writeLock(s_XmlWriterLock);

    // Dump header
    if (!s_HasDumpedXmlHeader)
    {
        DumpDataEnsurePolicyIsSet();

        fprintf(file, "<?xml version=\"1.0\"?>\n");
        fprintf(file, "<InlineForest>\n");
        fprintf(file, "<Policy>%s</Policy>\n", m_LastSuccessfulPolicy->GetName());

        const int dumpDataSetting = JitConfig.JitInlineDumpData();
        if (dumpDataSetting != 0)
        {
            fprintf(file, "<DataSchema>");

            if (dumpDataSetting == 1)
            {
                // JitInlineDumpData=1 -- dump schema for data plus deltas
                DumpDataSchema(file);
            }
            else if (dumpDataSetting == 2)
            {
                // JitInlineDumpData=2 -- dump schema for data only
                m_LastSuccessfulPolicy->DumpSchema(file);
            }

            fprintf(file, "</DataSchema>\n");
        }

        fprintf(file, "<Methods>\n");
        s_HasDumpedXmlHeader = true;
    }

    // If we're dumping "minimal" Xml, and we didn't do
    // any inlines into this method, then there's nothing
    // to emit here.
    if ((m_InlineCount == 0) && (JitConfig.JitInlineDumpXml() >= 2))
    {
        return;
    }

    // Cache references to compiler substructures.
    const Compiler::Info&    info = m_Compiler->info;
    const Compiler::Options& opts = m_Compiler->opts;

    const bool isPrejitRoot  = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT);
    const bool isForceInline = (info.compFlags & CORINFO_FLG_FORCEINLINE) != 0;

    // We'd really like the method identifier to be unique and
    // durable across crossgen invocations. Not clear how to
    // accomplish this, so we'll use the token for now.
    //
    // Post processing will have to filter out all data from
    // methods where the root entry appears multiple times.
    mdMethodDef currentMethodToken = info.compCompHnd->getMethodDefFromMethod(info.compMethodHnd);

    unsigned hash = info.compMethodHash();

    // Convert time spent jitting into microseconds
    unsigned         microsecondsSpentJitting = 0;
    unsigned __int64 compCycles               = m_Compiler->getInlineCycleCount();
    if (compCycles > 0)
    {
        double countsPerSec      = CachedCyclesPerSecond();
        double counts            = (double)compCycles;
        microsecondsSpentJitting = (unsigned)((counts / countsPerSec) * 1000 * 1000);
    }

    // Get method name just for root method, to make it a bit easier
    // to search for things in the inline xml.
    const char* methodName = info.compCompHnd->getMethodName(info.compMethodHnd, nullptr);

    // Cheap xml quoting for values. Only < and & are troublemakers,
    // but change > for symmetry.
    //
    // Ok to truncate name, just ensure it's null terminated.
    char buf[64];
    strncpy(buf, methodName, sizeof(buf));
    buf[sizeof(buf) - 1] = 0;

    for (size_t i = 0; i < _countof(buf); i++)
    {
        switch (buf[i])
        {
            case '<':
                buf[i] = '[';
                break;
            case '>':
                buf[i] = ']';
                break;
            case '&':
                buf[i] = '#';
                break;
            default:
                break;
        }
    }

    fprintf(file, "%*s<Method>\n", indent, "");
    fprintf(file, "%*s<Token>%08x</Token>\n", indent + 2, "", currentMethodToken);
    fprintf(file, "%*s<Hash>%08x</Hash>\n", indent + 2, "", hash);
    fprintf(file, "%*s<Name>%s</Name>\n", indent + 2, "", buf);
    fprintf(file, "%*s<InlineCount>%u</InlineCount>\n", indent + 2, "", m_InlineCount);
    fprintf(file, "%*s<HotSize>%u</HotSize>\n", indent + 2, "", info.compTotalHotCodeSize);
    fprintf(file, "%*s<ColdSize>%u</ColdSize>\n", indent + 2, "", info.compTotalColdCodeSize);
    fprintf(file, "%*s<JitTime>%u</JitTime>\n", indent + 2, "", microsecondsSpentJitting);
    fprintf(file, "%*s<SizeEstimate>%u</SizeEstimate>\n", indent + 2, "", m_CurrentSizeEstimate / 10);
    fprintf(file, "%*s<TimeEstimate>%u</TimeEstimate>\n", indent + 2, "", m_CurrentTimeEstimate);

    // For prejit roots also propagate out the assessment of the root method
    if (isPrejitRoot)
    {
        fprintf(file, "%*s<PrejitDecision>%s</PrejitDecision>\n", indent + 2, "",
                InlGetDecisionString(m_PrejitRootDecision));
        fprintf(file, "%*s<PrejitObservation>%s</PrejitObservation>\n", indent + 2, "",
                InlGetObservationString(m_PrejitRootObservation));
    }

    // Root context will be null if we're not optimizing the method.
    //
    // Note there are cases of this in System.Private.CoreLib even in release builds,
    // eg Task.NotifyDebuggerOfWaitCompletion.
    //
    // For such methods there aren't any inlines.
    if (m_RootContext != nullptr)
    {
        m_RootContext->DumpXml(file, indent + 2);
    }
    else
    {
        fprintf(file, "%*s<Inlines/>\n", indent + 2, "");
    }

    fprintf(file, "%*s</Method>\n", indent, "");
}

//------------------------------------------------------------------------
// FinalizeXml: finalize the xml-formatted version of the inline tree.
//
// Arguments
//    file - file for data output

void InlineStrategy::FinalizeXml(FILE* file)
{
    // If we dumped the header, dump a footer
    if (s_HasDumpedXmlHeader)
    {
        fprintf(file, "</Methods>\n");
        fprintf(file, "</InlineForest>\n");
        fflush(file);

        // Workaroud compShutdown getting called twice.
        s_HasDumpedXmlHeader = false;
    }

    // Finalize reading inline xml
    ReplayPolicy::FinalizeXml();
}

//------------------------------------------------------------------------
// GetRandom: setup or access random state
//
// Arguments:
//   seed -- seed value to use if not doing random inlines
//
// Return Value:
//    New or pre-existing random state.
//
// Notes:
//    Random state is kept per jit compilation request. Seed is partially
//    specified externally (via stress or policy setting) and partially
//    specified internally via method hash.

CLRRandom* InlineStrategy::GetRandom(int optionalSeed)
{
    if (m_Random == nullptr)
    {
        int externalSeed = optionalSeed;

#ifdef DEBUG

        if (m_Compiler->compRandomInlineStress())
        {
            externalSeed = getJitStressLevel();
            // We can set COMPlus_JitStressModeNames without setting COMPlus_JitStress,
            // but we need external seed to be non-zero.
            if (externalSeed == 0)
            {
                externalSeed = 2;
            }
        }

#endif // DEBUG

        int randomPolicyFlag = JitConfig.JitInlinePolicyRandom();
        if (randomPolicyFlag != 0)
        {
            externalSeed = randomPolicyFlag;
        }

        int internalSeed = m_Compiler->info.compMethodHash();

        assert(externalSeed != 0);
        assert(internalSeed != 0);

        int seed = externalSeed ^ internalSeed;

        JITDUMP("\n*** Using random seed ext(%u) ^ int(%u) = %u\n", externalSeed, internalSeed, seed);

        m_Random = new (m_Compiler, CMK_Inlining) CLRRandom();
        m_Random->Init(seed);
    }

    return m_Random;
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// IsInliningDisabled: allow strategy to disable inlining in the method being jitted
//
// Notes:
//    Only will return true in debug or special release builds.
//    Expects JitNoInlineRange to be set to the hashes of methods
//    where inlining is disabled.

bool InlineStrategy::IsInliningDisabled()
{

#if defined(DEBUG) || defined(INLINE_DATA)

    static ConfigMethodRange range;
    const WCHAR*             noInlineRange = JitConfig.JitNoInlineRange();

    if (noInlineRange == nullptr)
    {
        return false;
    }

    // If we have a config string we have at least one entry.  Count
    // number of spaces in our config string to see if there are
    // more. Number of ranges we need is 2x that value.
    unsigned entryCount = 1;
    for (const WCHAR* p = noInlineRange; *p != 0; p++)
    {
        if (*p == L' ')
        {
            entryCount++;
        }
    }

    range.EnsureInit(noInlineRange, 2 * entryCount);
    assert(!range.Error());

    return range.Contains(m_Compiler->info.compMethodHash());

#else

    return false;

#endif // defined(DEBUG) || defined(INLINE_DATA)
}
