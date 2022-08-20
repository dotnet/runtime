// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
// There are 4 situations where inline candidacy is evaluated.  In each
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
// During the inlining optimization pass, each candidate is further
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

const unsigned int MAX_INL_ARGS = 32; // does not include obj pointer
const unsigned int MAX_INL_LCLS = 32;

// Forward declarations

class InlineStrategy;

// InlineCallsiteFrequency gives a rough classification of how
// often a call site will be executed at runtime.

enum class InlineCallsiteFrequency
{
    UNUSED, // n/a
    RARE,   // once in a blue moon
    BORING, // normal call site
    WARM,   // seen during profiling
    LOOP,   // in a loop
    HOT     // very frequent
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
    CALLEE,  // observation applies to all calls to this callee
    CALLER,  // observation applies to all calls made by this caller
    CALLSITE // observation applies to a specific call site
};

// InlineImpact describe the possible impact of an inline observation.

enum class InlineImpact
{
    FATAL,       // inlining impossible, unsafe to evaluate further
    FUNDAMENTAL, // inlining impossible for fundamental reasons, deeper exploration safe
    LIMITATION,  // inlining impossible because of jit limitations, deeper exploration safe
    PERFORMANCE, // inlining inadvisable because of performance concerns
    INFORMATION  // policy-free observation to provide data for later decision making
};

// InlineObservation describes the set of possible inline observations.

enum class InlineObservation
{
#define INLINE_OBSERVATION(name, type, description, impact, scope) scope##_##name,
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
    virtual ~InlinePolicy()
    {
    }

    // Get the current decision
    InlineDecision GetDecision() const
    {
        return m_Decision;
    }

    // Get the observation responsible for the result
    InlineObservation GetObservation() const
    {
        return m_Observation;
    }

    // Policy observations
    virtual void NoteSuccess() = 0;
    virtual void NoteBool(InlineObservation obs, bool value) = 0;
    virtual void NoteFatal(InlineObservation obs) = 0;
    virtual void NoteInt(InlineObservation obs, int value)       = 0;
    virtual void NoteDouble(InlineObservation obs, double value) = 0;

    // Optional observations. Most policies ignore these.
    virtual void NoteContext(InlineContext* context)
    {
        (void)context;
    }
    virtual void NoteOffset(IL_OFFSET offset)
    {
        (void)offset;
    }

    // Policy determinations
    virtual void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) = 0;
    virtual bool BudgetCheck() const                                     = 0;

    // Policy policies
    virtual bool PropagateNeverToRuntime() const = 0;

    // Policy estimates
    virtual int CodeSizeEstimate() = 0;

    // Does Policy require a more precise IL scan?
    virtual bool RequiresPreciseScan()
    {
        return false;
    }

#if defined(DEBUG) || defined(INLINE_DATA)

    // Record observation for prior failure
    virtual void NotePriorFailure(InlineObservation obs) = 0;

    // Name of the policy
    virtual const char* GetName() const = 0;
    // Detailed data value dump
    virtual void DumpData(FILE* file) const
    {
    }
    // Detailed data name dump
    virtual void DumpSchema(FILE* file) const
    {
    }

#define XATTR_I4(x)                                                                                                    \
    if ((INT32)x != 0)                                                                                                 \
    {                                                                                                                  \
        fprintf(file, " " #x "=\"%d\"", (INT32)x);                                                                     \
    }
#define XATTR_R8(x)                                                                                                    \
    if (fabs(x) > 0.01)                                                                                                \
    {                                                                                                                  \
        fprintf(file, " " #x "=\"%.2lf\"", x);                                                                         \
    }
#define XATTR_B(x)                                                                                                     \
    if (x)                                                                                                             \
    {                                                                                                                  \
        fprintf(file, " " #x "=\"True\"");                                                                             \
    }

    // Detailed data value dump as XML
    void DumpXml(FILE* file, unsigned indent = 0)
    {
        fprintf(file, "%*s<%s", indent, "", GetName());
        OnDumpXml(file);
        fprintf(file, " />\n");
    }

    virtual void OnDumpXml(FILE* file, unsigned indent = 0) const
    {
    }

    // True if this is the inline targeted by data collection
    bool IsDataCollectionTarget()
    {
        return m_IsDataCollectionTarget;
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

protected:
    InlinePolicy(bool isPrejitRoot)
        : m_Decision(InlineDecision::UNDECIDED)
        , m_Observation(InlineObservation::CALLEE_UNUSED_INITIAL)
        , m_IsPrejitRoot(isPrejitRoot)
#if defined(DEBUG) || defined(INLINE_DATA)
        , m_IsDataCollectionTarget(false)
#endif // defined(DEBUG) || defined(INLINE_DATA)

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

#if defined(DEBUG) || defined(INLINE_DATA)

    bool m_IsDataCollectionTarget;

#endif // defined(DEBUG) || defined(INLINE_DATA)
};

// InlineResult summarizes what is known about the viability of a
// particular inline candidate.

class InlineResult
{
public:
    // Construct a new InlineResult to help evaluate a
    // particular call for inlining.
    InlineResult(
        Compiler* compiler, GenTreeCall* call, Statement* stmt, const char* description, bool doNotReport = false);

    // Construct a new InlineResult to evaluate a particular
    // method to see if it is inlineable.
    InlineResult(Compiler* compiler, CORINFO_METHOD_HANDLE method, const char* description, bool doNotReport = false);

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

    // Make an observation with a double value
    void NoteDouble(InlineObservation obs, double value)
    {
        m_Policy->NoteDouble(obs, value);
    }

#if defined(DEBUG) || defined(INLINE_DATA)

    // Record observation from an earlier failure.
    void NotePriorFailure(InlineObservation obs)
    {
        m_Policy->NotePriorFailure(obs);
        assert(IsFailure());
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Determine if this inline is profitable
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
    {
        m_Policy->DetermineProfitability(methodInfo);
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
        if (m_reportFailureAsVmFailure)
            return INLINE_CHECK_CAN_INLINE_VMFAIL;

        if (m_successResult != INLINE_PASS)
            return m_successResult;

        return InlGetCorInfoInlineDecision(m_Policy->GetDecision());
    }

    // String describing the decision made
    const char* ResultString() const
    {
        if (m_reportFailureAsVmFailure)
            return "VM Reported !CanInline";

        if (m_successResult == INLINE_PREJIT_SUCCESS)
            return "PreJIT Success";

        if (m_successResult == INLINE_CHECK_CAN_INLINE_SUCCESS)
            return "CheckCanInline Success";

        return InlGetDecisionString(m_Policy->GetDecision());
    }

    // String describing the reason for the decision
    const char* ReasonString() const
    {
        if (m_reportFailureAsVmFailure)
            return "VM Reported !CanInline";

        if (m_successResult == INLINE_PREJIT_SUCCESS)
            return "PreJIT Success";

        if (m_successResult == INLINE_CHECK_CAN_INLINE_SUCCESS)
            return "CheckCanInline Success";

        return InlGetObservationString(m_Policy->GetObservation());
    }

    // Get the policy that evaluated this result.
    InlinePolicy* GetPolicy() const
    {
        return m_Policy;
    }

    // Set the code that shall be reported if the InlineResult is a success
    void SetSuccessResult(CorInfoInline inlineSuccessCode)
    {
        m_successResult = inlineSuccessCode;
    }

    void SetVMFailure()
    {
        m_reportFailureAsVmFailure = true;
    }

    // Get the InlineContext for this inline.
    InlineContext* GetInlineContext() const
    {
        return m_InlineContext;
    }

    unsigned GetImportedILSize() const
    {
        return m_ImportedILSize;
    }

    void SetImportedILSize(unsigned x)
    {
        m_ImportedILSize = x;
    }

private:
    // No copying or assignment allowed.
    InlineResult(const InlineResult&) = delete;
    InlineResult& operator=(const InlineResult&) = delete;

    // Report/log/dump decision as appropriate
    void Report();

    Compiler*             m_RootCompiler;
    InlinePolicy*         m_Policy;
    GenTreeCall*          m_Call;
    InlineContext*        m_InlineContext;
    CORINFO_METHOD_HANDLE m_Caller; // immediate caller's handle
    CORINFO_METHOD_HANDLE m_Callee;
    unsigned              m_ImportedILSize; // estimated size of imported IL
    const char*           m_Description;
    CorInfoInline         m_successResult;
    bool                  m_DoNotReport;
    bool                  m_reportFailureAsVmFailure;
};

// HandleHistogramProfileCandidateInfo provides information about
// profiling an indirect or virtual call.
//
struct HandleHistogramProfileCandidateInfo
{
    IL_OFFSET ilOffset;
    unsigned  probeIndex;
};

// GuardedDevirtualizationCandidateInfo provides information about
// a potential target of a virtual or interface call.
//
struct GuardedDevirtualizationCandidateInfo : HandleHistogramProfileCandidateInfo
{
    CORINFO_CLASS_HANDLE  guardedClassHandle;
    CORINFO_METHOD_HANDLE guardedMethodHandle;
    CORINFO_METHOD_HANDLE guardedMethodUnboxedEntryHandle;
    unsigned              likelihood;
    bool                  requiresInstMethodTableArg;
};

// InlineCandidateInfo provides basic information about a particular
// inline candidate.
//
// It is a superset of GuardedDevirtualizationCandidateInfo: calls
// can start out as GDv candidates and turn into inline candidates
//
struct InlineCandidateInfo : public GuardedDevirtualizationCandidateInfo
{
    CORINFO_METHOD_INFO    methInfo;
    CORINFO_METHOD_HANDLE  ilCallerHandle; // the logical IL caller of this inlinee.
    CORINFO_CLASS_HANDLE   clsHandle;
    CORINFO_CONTEXT_HANDLE exactContextHnd;
    GenTree*               retExpr;
    unsigned               preexistingSpillTemp;
    unsigned               clsAttr;
    unsigned               methAttr;
    IL_OFFSET              ilOffset; // actual IL offset of instruction that resulted in this inline candidate
    CorInfoInitClassResult initClassResult;
    var_types              fncRetType;
    bool                   exactContextNeedsRuntimeLookup;
    InlineContext*         inlinersContext;
};

// LateDevirtualizationInfo
//
// Used to fill in missing contexts during late devirtualization.
//
struct LateDevirtualizationInfo
{
    CORINFO_CONTEXT_HANDLE exactContextHnd;
};

// InlArgInfo describes inline candidate argument properties.

struct InlArgInfo
{
    CallArg* arg;                         // the caller argument
    GenTree* argBashTmpNode;              // tmp node created, if it may be replaced with actual arg
    unsigned argTmpNum;                   // the argument tmp number
    unsigned argIsUsed : 1;               // is this arg used at all?
    unsigned argIsInvariant : 1;          // the argument is a constant or a local variable address
    unsigned argIsLclVar : 1;             // the argument is a local variable
    unsigned argIsThis : 1;               // the argument is the 'this' pointer
    unsigned argHasSideEff : 1;           // the argument has side effects
    unsigned argHasGlobRef : 1;           // the argument has a global ref
    unsigned argHasCallerLocalRef : 1;    // the argument value depends on an aliased caller local
    unsigned argHasTmp : 1;               // the argument will be evaluated to a temp
    unsigned argHasLdargaOp : 1;          // Is there LDARGA(s) operation on this argument?
    unsigned argHasStargOp : 1;           // Is there STARG(s) operation on this argument?
    unsigned argIsByRefToStructLocal : 1; // Is this arg an address of a struct local or a normed struct local or a
                                          // field in them?
    unsigned argIsExact : 1;              // Is this arg of an exact class?
};

// InlLclVarInfo describes inline candidate argument and local variable properties.

struct InlLclVarInfo
{
    typeInfo  lclVerTypeInfo;
    var_types lclTypeInfo;
    unsigned  lclHasLdlocaOp : 1;        // Is there LDLOCA(s) operation on this local?
    unsigned  lclHasStlocOp : 1;         // Is there a STLOC on this local?
    unsigned  lclHasMultipleStlocOp : 1; // Is there more than one STLOC on this local
    unsigned  lclIsPinned : 1;
};

// InlineInfo provides detailed information about a particular inline candidate.

struct InlineInfo
{
    Compiler* InlinerCompiler; // The Compiler instance for the caller (i.e. the inliner)
    Compiler* InlineRoot; // The Compiler instance that is the root of the inlining tree of which the owner of "this" is
                          // a member.

    CORINFO_METHOD_HANDLE fncHandle;
    InlineCandidateInfo*  inlineCandidateInfo;
    InlineContext*        inlineContext;

    InlineResult* inlineResult;

    GenTree*             retExpr; // The return expression of the inlined candidate.
    BasicBlock*          retBB;   // The basic block of the return expression of the inlined candidate.
    CORINFO_CLASS_HANDLE retExprClassHnd;
    bool                 retExprClassHndIsExact;

    CORINFO_CONTEXT_HANDLE tokenLookupContextHandle; // The context handle that will be passed to
                                                     // impTokenLookupContextHandle in Inlinee's Compiler.

    unsigned      argCnt;
    InlArgInfo    inlArgInfo[MAX_INL_ARGS + 1];
    int           lclTmpNum[MAX_INL_LCLS];                     // map local# -> temp# (-1 if unused)
    InlLclVarInfo lclVarInfo[MAX_INL_LCLS + MAX_INL_ARGS + 1]; // type information from local sig

    unsigned numberOfGcRefLocals; // Number of TYP_REF and TYP_BYREF locals

    bool HasGcRefLocals() const
    {
        return numberOfGcRefLocals > 0;
    }

    bool thisDereferencedFirst;

#ifdef FEATURE_SIMD
    bool hasSIMDTypeArgLocalOrReturn;
#endif // FEATURE_SIMD

    GenTreeCall* iciCall;  // The GT_CALL node to be inlined.
    Statement*   iciStmt;  // The statement iciCall is in.
    BasicBlock*  iciBlock; // The basic block iciStmt is in.
};

// InlineContext tracks the inline history in a method.
//
// Notes:
//
// InlineContexts form a tree with the root method as the root and
// inlines as children. Nested inlines are represented as grandchildren
// and so on.
//
// Leaves in the tree represent successful inlines of leaf methods.
// In DEBUG builds we also keep track of failed inline attempts.
//
// During inlining, all statements in the IR refer back to the
// InlineContext that is responsible for those statements existing.
// This makes it possible to detect recursion and to keep track of the
// depth of each inline attempt.

#define FMT_INL_CTX "INL%02u"

class InlineContext
{
    // InlineContexts are created by InlineStrategies
    friend class InlineStrategy;

public:
#if defined(DEBUG) || defined(INLINE_DATA)

    // Dump the full subtree, including failures
    void Dump(bool verbose, unsigned indent = 0);

    // Dump only the success subtree, with rich data
    void DumpData(unsigned indent = 0);

    // Dump full subtree in xml format
    void DumpXml(FILE* file = stderr, unsigned indent = 0);
#endif // defined(DEBUG) || defined(INLINE_DATA)

    IL_OFFSET GetActualCallOffset()
    {
        return m_ActualCallOffset;
    }

    // Get callee handle
    CORINFO_METHOD_HANDLE GetCallee() const
    {
        return m_Callee;
    }

    unsigned GetOrdinal() const
    {
        return m_Ordinal;
    }

    // Get the parent context for this context.
    InlineContext* GetParent() const
    {
        return m_Parent;
    }

    // Get the sibling context.
    InlineContext* GetSibling() const
    {
        return m_Sibling;
    }

    // Get the first child context.
    InlineContext* GetChild() const
    {
        return m_Child;
    }

    // Get the code pointer for this context.
    const BYTE* GetCode() const
    {
        return m_Code;
    }

    // True if this context describes a successful inline.
    bool IsSuccess() const
    {
        return m_Success;
    }

    // Get the observation that supported or disqualified this inline.
    InlineObservation GetObservation() const
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

    // Get the loation of the call site within the parent
    ILLocation GetLocation() const
    {
        return m_Location;
    }

    // True if this is the root context
    bool IsRoot() const
    {
        return m_Parent == nullptr;
    }

    bool IsDevirtualized() const
    {
        return m_Devirtualized;
    }

    bool IsGuarded() const
    {
        return m_Guarded;
    }

    bool IsUnboxed() const
    {
        return m_Unboxed;
    }

    unsigned GetImportedILSize() const
    {
        return m_ImportedILSize;
    }

    void SetSucceeded(const InlineInfo* info);
    void SetFailed(const InlineResult* result);

#ifdef DEBUG
    FixedBitVect* GetILInstsSet() const
    {
        return m_ILInstsSet;
    }

    void SetILInstsSet(FixedBitVect* set)
    {
        m_ILInstsSet = set;
    }
#endif

private:
    InlineContext(InlineStrategy* strategy);

    InlineStrategy*       m_InlineStrategy;    // overall strategy
    InlineContext*        m_Parent;            // logical caller (parent)
    InlineContext*        m_Child;             // first child
    InlineContext*        m_Sibling;           // next child of the parent
    const BYTE*           m_Code;              // address of IL buffer for the method
    CORINFO_METHOD_HANDLE m_Callee;            // handle to the method
    unsigned              m_ILSize;            // size of IL buffer for the method
    unsigned              m_ImportedILSize;    // estimated size of imported IL
    ILLocation            m_Location;          // inlining statement location within parent
    IL_OFFSET             m_ActualCallOffset;  // IL offset of actual call instruction leading to the inline
    InlineObservation     m_Observation;       // what lead to this inline success or failure
    int                   m_CodeSizeEstimate;  // in bytes * 10
    unsigned              m_Ordinal;           // Ordinal number of this inline
    bool                  m_Success : 1;       // true if this was a successful inline
    bool                  m_Devirtualized : 1; // true if this was a devirtualized call
    bool                  m_Guarded : 1;       // true if this was a guarded call
    bool                  m_Unboxed : 1;       // true if this call now invokes the unboxed entry

#if defined(DEBUG) || defined(INLINE_DATA)

    InlinePolicy* m_Policy; // policy that evaluated this inline
    unsigned      m_TreeID; // ID of the GenTreeCall in the parent

#endif // defined(DEBUG) || defined(INLINE_DATA)

#ifdef DEBUG
    FixedBitVect* m_ILInstsSet; // Set of offsets where instructions begin
#endif
};

// The InlineStrategy holds the per-method persistent inline state.
// It is responsible for providing information that applies to
// multiple inlining decisions.

class InlineStrategy
{
    friend class InlineContext;

public:
    // Construct a new inline strategy.
    InlineStrategy(Compiler* compiler);

    // Create context for the specified inline candidate contained in the specified statement.
    InlineContext* NewContext(InlineContext* parentContext, Statement* stmt, GenTreeCall* call);

    // Compiler associated with this strategy
    Compiler* GetCompiler() const
    {
        return m_Compiler;
    }

    // Root context
    InlineContext* GetRootContext();

    // Context for the last successful inline
    // (or root if no inlines)
    InlineContext* GetLastContext() const
    {
        return m_LastContext;
    }

    // Get IL size for maximum allowable inline
    unsigned GetMaxInlineILSize() const
    {
        return m_MaxInlineSize;
    }

    // Get depth of maximum allowable inline
    unsigned GetMaxInlineDepth() const
    {
        return m_MaxInlineDepth;
    }

    // Number of successful inlines into the root
    unsigned GetInlineCount() const
    {
        return m_InlineCount;
    }

    // Return the current code size estimate for this method
    int GetCurrentSizeEstimate() const
    {
        return m_CurrentSizeEstimate;
    }

    // Return the initial code size estimate for this method
    int GetInitialSizeEstimate() const
    {
        return m_InitialSizeEstimate;
    }

    // Inform strategy that there's another call
    void NoteCall()
    {
        m_CallCount++;
    }

    // Inform strategy that there's a new inline candidate.
    void NoteCandidate()
    {
        m_CandidateCount++;
    }

    // Inform strategy that a candidate was assessed and determined to
    // be unprofitable.
    void NoteUnprofitable()
    {
        m_UnprofitableCandidateCount++;
    }

    // Inform strategy that a candidate has passed screening
    // and that the jit will attempt to inline.
    void NoteAttempt(InlineResult* result);

    // Inform strategy that jit is about to import the inlinee IL.
    void NoteImport()
    {
        m_ImportCount++;
    }

    // Inform strategy about the inline decision for a prejit root
    void NotePrejitDecision(const InlineResult& r)
    {
        m_PrejitRootDecision    = r.GetPolicy()->GetDecision();
        m_PrejitRootObservation = r.GetPolicy()->GetObservation();
    }

    // Dump csv header for inline stats to indicated file.
    static void DumpCsvHeader(FILE* f);

    // Dump csv data for inline stats to indicated file.
    void DumpCsvData(FILE* f);

    // See if an inline of this size would fit within the current jit
    // time budget.
    bool BudgetCheck(unsigned ilSize);

    // Check if inlining is disabled for the method being jitted
    bool IsInliningDisabled();

#if defined(DEBUG) || defined(INLINE_DATA)

    // Dump textual description of inlines done so far.
    void Dump(bool verbose);

    // Dump data-format description of inlines done so far.
    void DumpData();
    void DumpDataEnsurePolicyIsSet();
    void DumpDataHeader(FILE* file);
    void DumpDataSchema(FILE* file);
    void DumpDataContents(FILE* file);

    // Dump xml-formatted description of inlines
    void DumpXml(FILE* file = stderr, unsigned indent = 0);
    static void FinalizeXml(FILE* file = stderr);

    // Cache for file position of this method in the inline xml
    long GetMethodXmlFilePosition()
    {
        return m_MethodXmlFilePosition;
    }

    void SetMethodXmlFilePosition(long val)
    {
        m_MethodXmlFilePosition = val;
    }

    // Set up or access random state (for use by RandomPolicy)
    CLRRandom* GetRandom(int optionalSeed = 0);

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Some inline limit values
    enum
    {
        ALWAYS_INLINE_SIZE              = 16,
        IMPLEMENTATION_MAX_INLINE_SIZE  = _UI16_MAX,
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
    static bool          s_HasDumpedDataHeader;
    static bool          s_HasDumpedXmlHeader;
    static CritSecObject s_XmlWriterLock;
#endif // defined(DEBUG) || defined(INLINE_DATA)

    Compiler*         m_Compiler;
    InlineContext*    m_RootContext;
    InlinePolicy*     m_LastSuccessfulPolicy;
    InlineContext*    m_LastContext;
    InlineDecision    m_PrejitRootDecision;
    InlineObservation m_PrejitRootObservation;
    unsigned          m_CallCount;
    unsigned          m_CandidateCount;
    unsigned          m_AlwaysCandidateCount;
    unsigned          m_ForceCandidateCount;
    unsigned          m_DiscretionaryCandidateCount;
    unsigned          m_UnprofitableCandidateCount;
    unsigned          m_ImportCount;
    unsigned          m_InlineCount;
    unsigned          m_MaxInlineSize;
    unsigned          m_MaxInlineDepth;
    int               m_InitialTimeBudget;
    int               m_InitialTimeEstimate;
    int               m_CurrentTimeBudget;
    int               m_CurrentTimeEstimate;
    int               m_InitialSizeEstimate;
    int               m_CurrentSizeEstimate;
    bool              m_HasForceViaDiscretionary;

#if defined(DEBUG) || defined(INLINE_DATA)
    long       m_MethodXmlFilePosition;
    CLRRandom* m_Random;
#endif // defined(DEBUG) || defined(INLINE_DATA)
};

#endif // _INLINE_H_
