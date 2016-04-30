// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Inlining Support
//
// This file contains enum and class definitions and related
// information that the jit uses to make inlining decisions.
//
// -- ENUMS --
//
// InlineCallFrequency - rough assessment of call site frequency
// InlineDecision      - overall decision made about an inline
// InlineTarget        - target of a particular observation
// InlineImpact        - impact of a particular observation
// InlineObservation   - facts observed when considering an inline
//
// -- CLASSES --
//
// InlineResult        - accumulates observations, consults with policy
// InlineCandidateInfo - basic information needed for inlining
// InlArgInfo          - information about a candidate's argument
// InlLclVarInfo       - information about a candidate's local variable
// InlineInfo          - detailed information needed for inlining
// InlineContext       - class, remembers what inlines happened
// InlinePolicy        - class, determines policy for inlining
// InlineStrategy      - class, determines overall inline strategy
//
// Enums are used throughout to provide various descriptions.
//
// There are 4 sitations where inline candidacy is evaluated.  In each
// case an InlineResult is allocated on the stack to collect
// information about the inline candidate.  Each InlineResult refers
// to an InlinePolicy.
//
// 1. Importer Candidate Screen (impMarkInlineCandidate)
//
// Creates: InlineCandidateInfo
//
// During importing, the IL being imported is scanned to identify
// inline candidates. This happens both when the root method is being
// imported as well as when prospective inlines are being imported.
// Candidates are marked in the IL and given an InlineCandidateInfo.
//
// 2. Inlining Optimization Pass -- candidates (fgInline)
//
// Creates / Uses: InlineContext
// Creates: InlineInfo, InlArgInfo, InlLocalVarInfo
//
// During the inlining optimation pass, each candidate is further
// analyzed. Viable candidates will eventually inspire creation of an
// InlineInfo and a set of InlArgInfos (for call arguments) and
// InlLocalVarInfos (for callee locals).
//
// The analysis will also examine InlineContexts from relevant prior
// inlines. If the inline is successful, a new InlineContext will be
// created to remember this inline. In DEBUG builds, failing inlines
// also create InlineContexts.
//
// 3. Inlining Optimization Pass -- non-candidates (fgNoteNotInlineCandidate)
//
// Creates / Uses: InlineContext
//
// In DEBUG, the jit also searches for non-candidate calls to try
// and get a complete picture of the set of failed inlines.
//
// 4. Prejit suitability screen (compCompileHelper)
//
// When prejitting, each method is scanned to see if it is a viable
// inline candidate.

#ifndef _INLINE_H_
#define _INLINE_H_

#include "jit.h"
#include "gentree.h"

// Implementation limits

#ifndef LEGACY_BACKEND
const unsigned int   MAX_INL_ARGS =      32;     // does not include obj pointer
const unsigned int   MAX_INL_LCLS =      32;
#else // LEGACY_BACKEND
const unsigned int   MAX_INL_ARGS =      10;     // does not include obj pointer
const unsigned int   MAX_INL_LCLS =      8;
#endif // LEGACY_BACKEND

// Flags lost during inlining.

#define CORJIT_FLG_LOST_WHEN_INLINING   (CORJIT_FLG_BBOPT |                         \
                                         CORJIT_FLG_BBINSTR |                       \
                                         CORJIT_FLG_PROF_ENTERLEAVE |               \
                                         CORJIT_FLG_DEBUG_EnC |                     \
                                         CORJIT_FLG_DEBUG_INFO                      \
                                        )

// Forward declarations

class InlineStrategy;

// InlineCallsiteFrequency gives a rough classification of how
// often a call site will be excuted at runtime.

enum class InlineCallsiteFrequency
{
    UNUSED,    // n/a
    RARE,      // once in a blue moon
    BORING,    // normal call site
    WARM,      // seen during profiling
    LOOP,      // in a loop
    HOT        // very frequent
};

// InlineDecision describes the various states the jit goes through when
// evaluating an inline candidate. It is distinct from CorInfoInline
// because it must capture internal states that don't get reported back
// to the runtime.

enum class InlineDecision
{
    UNDECIDED,
    CANDIDATE,
    SUCCESS,
    FAILURE,
    NEVER
};

// Translate a decision into a CorInfoInline for reporting back to the runtime.

CorInfoInline InlGetCorInfoInlineDecision(InlineDecision d);

// Get a string describing this InlineDecision

const char* InlGetDecisionString(InlineDecision d);

// True if this InlineDecsion describes a failing inline

bool InlDecisionIsFailure(InlineDecision d);

// True if this decision describes a successful inline

bool InlDecisionIsSuccess(InlineDecision d);

// True if this InlineDecision is a never inline decision

bool InlDecisionIsNever(InlineDecision d);

// True if this InlineDecision describes a viable candidate

bool InlDecisionIsCandidate(InlineDecision d);

// True if this InlineDecsion describes a decision

bool InlDecisionIsDecided(InlineDecision d);

// InlineTarget describes the possible targets of an inline observation.

enum class InlineTarget
{
    CALLEE,         // observation applies to all calls to this callee
    CALLER,         // observation applies to all calls made by this caller
    CALLSITE        // observation applies to a specific call site
};

// InlineImpact describe the possible impact of an inline observation.

enum class InlineImpact
{
    FATAL,          // inlining impossible, unsafe to evaluate further
    FUNDAMENTAL,    // inlining impossible for fundamental reasons, deeper exploration safe
    LIMITATION,     // inlining impossible because of jit limitations, deeper exploration safe
    PERFORMANCE,    // inlining inadvisable because of performance concerns
    INFORMATION     // policy-free observation to provide data for later decision making
};

// InlineObservation describes the set of possible inline observations.

enum class InlineObservation
{
#define INLINE_OBSERVATION(name, type, description, impact, scope) scope ## _ ## name,
#include "inline.def"
#undef INLINE_OBSERVATION
};

#ifdef DEBUG

// Sanity check the observation value

bool InlIsValidObservation(InlineObservation obs);

#endif // DEBUG

// Get a string describing this observation

const char* InlGetObservationString(InlineObservation obs);

// Get a string describing the target of this observation

const char* InlGetTargetString(InlineObservation obs);

// Get a string describing the impact of this observation

const char* InlGetImpactString(InlineObservation obs);

// Get the target of this observation

InlineTarget InlGetTarget(InlineObservation obs);

// Get the impact of this observation

InlineImpact InlGetImpact(InlineObservation obs);

// InlinePolicy is an abstract base class for a family of inline
// policies.

class InlinePolicy
{
public:

    // Factory method for getting policies
    static InlinePolicy* GetPolicy(Compiler* compiler, bool isPrejitRoot);

    // Obligatory virtual dtor
    virtual ~InlinePolicy() {}

    // Get the current decision
    InlineDecision GetDecision() const { return m_Decision; }

    // Get the observation responsible for the result
    InlineObservation GetObservation() const { return m_Observation; }

    // Policy observations
    virtual void NoteSuccess() = 0;
    virtual void NoteBool(InlineObservation obs, bool value) = 0;
    virtual void NoteFatal(InlineObservation obs) = 0;
    virtual void NoteInt(InlineObservation obs, int value) = 0;

    // Policy determinations
    virtual void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) = 0;

    // Policy policies
    virtual bool PropagateNeverToRuntime() const = 0;

    // Policy estimates
    virtual int CodeSizeEstimate() = 0;

#if defined(DEBUG) || defined(INLINE_DATA)

    // Name of the policy
    virtual const char* GetName() const = 0;
    // Detailed data value dump
    virtual void DumpData(FILE* file) const { }
    // Detailed data name dump
    virtual void DumpSchema(FILE* file) const { }

#endif // defined(DEBUG) || defined(INLINE_DATA)

protected:

    InlinePolicy(bool isPrejitRoot)
        : m_Decision(InlineDecision::UNDECIDED)
        , m_Observation(InlineObservation::CALLEE_UNUSED_INITIAL)
        , m_IsPrejitRoot(isPrejitRoot)
    {
        // empty
    }

private:

    // No copying or assignment supported
    InlinePolicy(const InlinePolicy&) = delete;
    InlinePolicy& operator=(const InlinePolicy&) = delete;

protected:

    InlineDecision    m_Decision;
    InlineObservation m_Observation;
    bool              m_IsPrejitRoot;
};

// InlineResult summarizes what is known about the viability of a
// particular inline candiate.

class InlineResult
{
public:

    // Construct a new InlineResult to help evaluate a
    // particular call for inlining.
    InlineResult(Compiler*              compiler,
                 GenTreeCall*           call,
                 const char*            context);

    // Construct a new InlineResult to evaluate a particular
    // method to see if it is inlineable.
    InlineResult(Compiler*              compiler,
                 CORINFO_METHOD_HANDLE  method,
                 const char*            context);

    // Has the policy determined this inline should fail?
    bool IsFailure() const
    {
        return InlDecisionIsFailure(m_Policy->GetDecision());
    }

    // Has the policy determined this inline will succeed?
    bool IsSuccess() const
    {
        return InlDecisionIsSuccess(m_Policy->GetDecision());
    }

    // Has the policy determined this inline will fail,
    // and that the callee should never be inlined?
    bool IsNever() const
    {
        return InlDecisionIsNever(m_Policy->GetDecision());
    }

    // Has the policy determined this inline attempt is still viable?
    bool IsCandidate() const
    {
        return InlDecisionIsCandidate(m_Policy->GetDecision());
    }

    // Has the policy determined this inline attempt is still viable
    // and is a discretionary inline?
    bool IsDiscretionaryCandidate() const
    {
        bool result = InlDecisionIsCandidate(m_Policy->GetDecision()) &&
            (m_Policy->GetObservation() == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);

        return result;
    }

    // Has the policy made a determination?
    bool IsDecided() const
    {
        return InlDecisionIsDecided(m_Policy->GetDecision());
    }

    // NoteSuccess means the all the various checks have passed and
    // the inline can happen.
    void NoteSuccess()
    {
        assert(IsCandidate());
        m_Policy->NoteSuccess();
    }

    // Make a true observation, and update internal state
    // appropriately.
    //
    // Caller is expected to call isFailure after this to see whether
    // more observation is desired.
    void Note(InlineObservation obs)
    {
        m_Policy->NoteBool(obs, true);
    }

    // Make a boolean observation, and update internal state
    // appropriately.
    //
    // Caller is expected to call isFailure after this to see whether
    // more observation is desired.
    void NoteBool(InlineObservation obs, bool value)
    {
        m_Policy->NoteBool(obs, value);
    }

    // Make an observation that must lead to immediate failure.
    void NoteFatal(InlineObservation obs)
    {
        m_Policy->NoteFatal(obs);
        assert(IsFailure());
    }

    // Make an observation with an int value
    void NoteInt(InlineObservation obs, int value)
    {
        m_Policy->NoteInt(obs, value);
    }

    // Determine if this inline is profitable
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
    {
        return m_Policy->DetermineProfitability(methodInfo);
    }

    // Ensure details of this inlining process are appropriately
    // reported when the result goes out of scope.
    ~InlineResult()
    {
        Report();
    }

    // The observation leading to this particular result
    InlineObservation GetObservation() const
    {
        return m_Policy->GetObservation();
    }

    // The callee handle for this result
    CORINFO_METHOD_HANDLE GetCallee() const
    {
        return m_Callee;
    }

    // The call being considered
    GenTreeCall* GetCall() const
    {
        return m_Call;
    }

    // Result that can be reported back to the runtime
    CorInfoInline Result() const
    {
        return InlGetCorInfoInlineDecision(m_Policy->GetDecision());
    }

    // String describing the decision made
    const char* ResultString() const
    {
        return InlGetDecisionString(m_Policy->GetDecision());
    }

    // String describing the reason for the decision
    const char* ReasonString() const
    {
        return InlGetObservationString(m_Policy->GetObservation());
    }

    // Get the policy that evaluated this result.
    InlinePolicy* GetPolicy() const
    {
        return m_Policy;
    }

    // SetReported indicates that this particular result doesn't need
    // to be reported back to the runtime, either because the runtime
    // already knows, or we aren't actually inlining yet.
    void SetReported()
    {
        m_Reported = true;
    }

private:

    // No copying or assignment allowed.
    InlineResult(const InlineResult&) = delete;
    InlineResult& operator=(const InlineResult&) = delete;

    // Report/log/dump decision as appropriate
    void Report();

    Compiler*               m_RootCompiler;
    InlinePolicy*           m_Policy;
    GenTreeCall*            m_Call;
    CORINFO_METHOD_HANDLE   m_Caller;     // immediate caller's handle
    CORINFO_METHOD_HANDLE   m_Callee;
    const char*             m_Context;
    bool                    m_Reported;
};

// InlineCandidateInfo provides basic information about a particular
// inline candidate.

struct InlineCandidateInfo
{
    DWORD                 dwRestrictions;
    CORINFO_METHOD_INFO   methInfo;
    unsigned              methAttr;
    CORINFO_CLASS_HANDLE  clsHandle;
    unsigned              clsAttr;
    var_types             fncRetType;
    CORINFO_METHOD_HANDLE ilCallerHandle; //the logical IL caller of this inlinee.
    CORINFO_CONTEXT_HANDLE exactContextHnd;
    CorInfoInitClassResult initClassResult;
};

// InlArgInfo describes inline candidate argument properties.

struct InlArgInfo
{
    unsigned    argIsUsed     :1;   // is this arg used at all?
    unsigned    argIsInvariant:1;   // the argument is a constant or a local variable address
    unsigned    argIsLclVar   :1;   // the argument is a local variable
    unsigned    argIsThis     :1;   // the argument is the 'this' pointer
    unsigned    argHasSideEff :1;   // the argument has side effects
    unsigned    argHasGlobRef :1;   // the argument has a global ref
    unsigned    argHasTmp     :1;   // the argument will be evaluated to a temp
    unsigned    argIsByRefToStructLocal:1;  // Is this arg an address of a struct local or a normed struct local or a field in them?
    unsigned    argHasLdargaOp:1;   // Is there LDARGA(s) operation on this argument?

    unsigned    argTmpNum;          // the argument tmp number
    GenTreePtr  argNode;
    GenTreePtr  argBashTmpNode;     // tmp node created, if it may be replaced with actual arg
};

// InlArgInfo describes inline candidate local variable properties.

struct InlLclVarInfo
{
    var_types       lclTypeInfo;
    typeInfo        lclVerTypeInfo;
    bool            lclHasLdlocaOp; // Is there LDLOCA(s) operation on this argument?
};

// InlineInfo provides detailed information about a particular inline candidate.

struct InlineInfo
{
    Compiler        * InlinerCompiler;  // The Compiler instance for the caller (i.e. the inliner)
    Compiler        * InlineRoot;       // The Compiler instance that is the root of the inlining tree of which the owner of "this" is a member.

    CORINFO_METHOD_HANDLE fncHandle;
    InlineCandidateInfo * inlineCandidateInfo;

    InlineResult*  inlineResult;

    GenTreePtr retExpr;      // The return expression of the inlined candidate.

    CORINFO_CONTEXT_HANDLE tokenLookupContextHandle; // The context handle that will be passed to
                                                     // impTokenLookupContextHandle in Inlinee's Compiler.

    unsigned          argCnt;
    InlArgInfo        inlArgInfo[MAX_INL_ARGS + 1];
    int               lclTmpNum[MAX_INL_LCLS];    // map local# -> temp# (-1 if unused)
    InlLclVarInfo     lclVarInfo[MAX_INL_LCLS + MAX_INL_ARGS + 1];  // type information from local sig

    bool              thisDereferencedFirst;
#ifdef FEATURE_SIMD
    bool              hasSIMDTypeArgLocalOrReturn;
#endif // FEATURE_SIMD

    GenTree         * iciCall;       // The GT_CALL node to be inlined.
    GenTree         * iciStmt;       // The statement iciCall is in.
    BasicBlock      * iciBlock;      // The basic block iciStmt is in.
};

// InlineContext tracks the inline history in a method.
//
// Notes:
//
// InlineContexts form a tree with the root method as the root and
// inlines as children. Nested inlines are represented as granchildren
// and so on.
//
// Leaves in the tree represent successful inlines of leaf methods.
// In DEBUG builds we also keep track of failed inline attempts.
//
// During inlining, all statements in the IR refer back to the
// InlineContext that is responsible for those statements existing.
// This makes it possible to detect recursion and to keep track of the
// depth of each inline attempt.

class InlineContext
{
    // InlineContexts are created by InlineStrategies
    friend class InlineStrategy;

public:

#if defined(DEBUG) || defined(INLINE_DATA)

    // Dump the full subtree, including failures
    void Dump(unsigned indent = 0);

    // Dump only the success subtree, with rich data
    void DumpData(unsigned indent = 0);

    // Dump full subtree in xml format
    void DumpXml(FILE* file = stderr, unsigned indent = 0);

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Get the parent context for this context.
    InlineContext* GetParent() const
    {
        return m_Parent;
    }

    // Get the code pointer for this context.
    BYTE* GetCode() const
    {
        return m_Code;
    }

    // True if this context describes a successful inline.
    bool IsSuccess() const
    {
        return m_Success;
    }

    // Get the observation that supported or disqualified this inline.
    InlineObservation GetObservation()
    {
        return m_Observation;
    }

    // Get the IL code size for this inline.
    unsigned GetILSize() const
    {
        return m_ILSize;
    }

    // Get the native code size estimate for this inline.
    unsigned GetCodeSizeEstimate() const
    {
        return m_CodeSizeEstimate;
    }

private:

    InlineContext(InlineStrategy* strategy);

private:

    InlineStrategy*       m_InlineStrategy;   // overall strategy
    InlineContext*        m_Parent;           // logical caller (parent)
    InlineContext*        m_Child;            // first child
    InlineContext*        m_Sibling;          // next child of the parent
    BYTE*                 m_Code;             // address of IL buffer for the method
    unsigned              m_ILSize;           // size of IL buffer for the method
    IL_OFFSETX            m_Offset;           // call site location within parent
    InlineObservation     m_Observation;      // what lead to this inline
    int                   m_CodeSizeEstimate; // in bytes * 10
    bool                  m_Success;          // true if this was a successful inline

#if defined(DEBUG) || defined(INLINE_DATA)

    InlinePolicy*         m_Policy;           // policy that evaluated this inline
    CORINFO_METHOD_HANDLE m_Callee;           // handle to the method
    unsigned              m_TreeID;           // ID of the GenTreeCall
    unsigned              m_Ordinal;          // Ordinal number of this inline

#endif // defined(DEBUG) || defined(INLINE_DATA)

};

// The InlineStrategy holds the per-method persistent inline state.
// It is responsible for providing information that applies to
// multiple inlining decisions.

class InlineStrategy
{

public:

    // Construct a new inline strategy.
    InlineStrategy(Compiler* compiler);

    // Create context for a successful inline.
    InlineContext* NewSuccess(InlineInfo*     inlineInfo);

    // Create context for a failing inline.
    InlineContext* NewFailure(GenTree*        stmt,
                              InlineResult*   inlineResult);

    // Compiler associated with this strategy
    Compiler* GetCompiler()
    {
        return m_Compiler;
    }

    // Root context
    InlineContext* GetRootContext();

    // Get IL size for maximum allowable inline
    unsigned GetMaxInlineILSize()
    {
        return m_MaxInlineSize;
    }

    // Get depth of maximum allowable inline
    unsigned GetMaxInlineDepth()
    {
        return m_MaxInlineDepth;
    }

    // Number of successful inlines into the root.
    unsigned GetInlineCount()
    {
        return m_InlineCount;
    }

    // Inform strategy that there's a new inline candidate.
    void NoteCandidate();

    // See if an inline of this size would fit within the current jit
    // time budget.
    bool BudgetCheck(unsigned ilSize);

    // Check if this method is not allowing inlines.
    static bool IsNoInline(ICorJitInfo* info, CORINFO_METHOD_HANDLE method);

#if defined(DEBUG) || defined(INLINE_DATA)

    // Dump textual description of inlines done so far.
    void Dump();

    // Dump data-format description of inlines done so far.
    void DumpData();

    // Dump xml-formatted description of inlines
    void DumpXml(FILE* file = stderr, unsigned indent = 0);
    static void FinalizeXml(FILE* file = stderr);

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Some inline limit values
    enum
    {
        ALWAYS_INLINE_SIZE = 16,
        IMPLEMENTATION_MAX_INLINE_SIZE = _UI16_MAX,
        IMPLEMENTATION_MAX_INLINE_DEPTH = 1000
    };

private:

    // Create a context for the root method.
    InlineContext* NewRoot();

    // Accounting updates for a successful or failed inline.
    void NoteOutcome(InlineContext* context);

    // Cap on allowable increase in jit time due to inlining.
    // Multiplicative, so BUDGET = 10 means up to 10x increase
    // in jit time.
    enum
    {
        BUDGET = 10
    };

    // Estimate the jit time change because of this inline.
    int EstimateTime(InlineContext* context);

    // EstimateTime helpers
    int EstimateRootTime(unsigned ilSize);
    int EstimateInlineTime(unsigned ilSize);

    // Estimate native code size change because of this inline.
    int EstimateSize(InlineContext* context);

#if defined(DEBUG) || defined(INLINE_DATA)
    static bool    s_HasDumpedDataHeader;
    static bool    s_HasDumpedXmlHeader;
#endif // defined(DEBUG) || defined(INLINE_DATA)

    Compiler*      m_Compiler;
    InlineContext* m_RootContext;
    InlinePolicy*  m_LastSuccessfulPolicy;
    unsigned       m_CandidateCount;
    unsigned       m_InlineAttemptCount;
    unsigned       m_InlineCount;
    unsigned       m_MaxInlineSize;
    unsigned       m_MaxInlineDepth;
    int            m_InitialTimeBudget;
    int            m_InitialTimeEstimate;
    int            m_CurrentTimeBudget;
    int            m_CurrentTimeEstimate;
    int            m_InitialSizeEstimate;
    int            m_CurrentSizeEstimate;
    bool           m_HasForceViaDiscretionary;
};

#endif // _INLINE_H_
