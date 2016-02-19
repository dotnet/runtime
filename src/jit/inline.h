// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Inlining Support
//
// This file contains enum and class definitions and related
// information that the jit uses to make inlining decisions.
//
// -- Overview of classes and enums defined here --
//
// InlineDecision -- enum, overall decision made about an inline
// InlineTarget -- enum, target of a particular observation
// InlineImpact -- enum, impact of a particular observation
// InlineObservation -- enum, facts observed when considering an inline
// InlineResult -- class, accumulates observations and makes a decision
// InlineCandidateInfo -- struct, detailed information needed for inlining
// InlArgInfo -- struct, information about a candidate's argument
// InlLclVarInfo -- struct, information about a candidate's local variable
// InlineHints -- enum, alternative form of observations
// InlineInfo -- struct, basic information needed for inlining
// InlineContext -- class, remembers what inlines happened

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

bool inlIsValidObservation(InlineObservation obs);

#endif // DEBUG

// Get a string describing this observation

const char* inlGetDescriptionString(InlineObservation obs);

// Get a string describing the target of this observation

const char* inlGetTargetString(InlineObservation obs);

// Get a string describing the impact of this observation

const char* inlGetImpactString(InlineObservation obs);

// Get the target of this observation

InlineTarget inlGetTarget(InlineObservation obs);

// Get the impact of this observation

InlineImpact inlGetImpact(InlineObservation obs);

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

    // Translate into CorInfoInline for reporting back to the runtime.
    //
    // Before calling this, the Jit must have made a decision.
    // Interim states are not meaningful to the runtime.
    CorInfoInline result() const
    {
        switch (inlDecision) {
            case InlineDecision::SUCCESS:
                return INLINE_PASS;
            case InlineDecision::FAILURE:
                return INLINE_FAIL;
            case InlineDecision::NEVER:
                return INLINE_NEVER;
            default:
                assert(!"Unexpected: interim inline result");
                unreached();
        }
    }

    // Translate into string for dumping
    const char* resultString() const
    {
        switch (inlDecision) {
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
                assert(!"Unexpected: interim inline result");
                unreached();
        }
    }

    // True if this definitely a failed inline candidate
    bool isFailure() const
    {
        switch (inlDecision) {
            case InlineDecision::SUCCESS:
            case InlineDecision::UNDECIDED:
            case InlineDecision::CANDIDATE:
                return false;
            case InlineDecision::FAILURE:
            case InlineDecision::NEVER:
                return true;
            default:
                assert(!"Invalid inline result");
                unreached();
        }
    }

    // True if this is definitely a successful inline candidate
    bool isSuccess() const
    {
        switch (inlDecision) {
            case InlineDecision::SUCCESS:
                return true;
            case InlineDecision::FAILURE:
            case InlineDecision::NEVER:
            case InlineDecision::UNDECIDED:
            case InlineDecision::CANDIDATE:
                return false;
            default:
                assert(!"Invalid inline result");
                unreached();
        }
    }

    // True if this definitely a never inline candidate
    bool isNever() const
    {
        switch (inlDecision) {
            case InlineDecision::NEVER:
                return true;
            case InlineDecision::FAILURE:
            case InlineDecision::SUCCESS:
            case InlineDecision::UNDECIDED:
            case InlineDecision::CANDIDATE:
                return false;
            default:
                assert(!"Invalid inline result");
                unreached();
        }
   }

    // True if this is still a viable inline candidate
    // at this stage of the evaluation process. This will
    // change as more checks are run.
    bool isCandidate() const
    {
        return !isFailure();
    }

    // True if all checks have been made and we know whether
    // or not this inline happened.
    bool isDecided() const
    {
        return (isSuccess() || isFailure());
    }

    // noteCandidate indicates the prospective inline has passed at least
    // some of the correctness checks and is still a viable inline
    // candidate, but no decision has been made yet.
    //
    // This may be called multiple times as various tests are performed
    // and the candidate gets closer and closer to actually getting
    // inlined.
    void noteCandidate(InlineObservation obs)
    {
        assert(!isDecided());

        // Check the impact, it should be INFORMATION
        InlineImpact impact = inlGetImpact(obs);
        assert(impact == InlineImpact::INFORMATION);

        // Update the status
        setCommon(InlineDecision::CANDIDATE, obs);
    }

    // noteSuccess means the inline happened.
    void noteSuccess()
    {
        assert(isCandidate());
        inlDecision = InlineDecision::SUCCESS;
    }

    // Make an observation, and update internal state appropriately.
    //
    // Caller is expected to call isFailure after this to see whether
    // more observation is desired.
    void note(InlineObservation obs)
    {
        // Check the impact
        InlineImpact impact = inlGetImpact(obs);

        // As a safeguard, all fatal impact must be
        // reported via noteFatal.
        assert(impact != InlineImpact::FATAL);
        noteInternal(obs, impact);
    }

    // Make an observation where caller knows for certain that this
    // inline cannot happen, and so there's no point in any further
    // scrutiny of this inline. Update internal state to mark the
    // inline result as a failure.
    void noteFatal(InlineObservation obs)
    {
        // Check the impact
        InlineImpact impact = inlGetImpact(obs);

        // As a safeguard, all fatal impact must be
        // reported via noteFatal.
        assert(impact == InlineImpact::FATAL);
        noteInternal(obs, impact);
        assert(isFailure());
    }

    // Ignore values for now
    void noteInt(InlineObservation obs, int value)
    {
        (void) value;
        note(obs);
    }

    // Ignore values for now
    void noteDouble(InlineObservation obs, double value)
    {
        (void) value;
        note(obs);
    }

    // Ensure result is appropriately reported when the result goes
    // out of scope.
    ~InlineResult()
    {
        report();
    }

    // The observation leading to this particular result
    InlineObservation getObservation() const
    {
        return inlObservation;
    }

    // The callee handle for this result
    CORINFO_METHOD_HANDLE getCallee() const
    {
        return inlCallee;
    }

    // The call being considered
    GenTreeCall* getCall() const
    {
        return inlCall;
    }

    // The reason for this particular result
    const char * reasonString() const
    {
        return inlGetDescriptionString(inlObservation);
    }

    // setReported indicates that this particular result doesn't need
    // to be reported back to the runtime, either because the runtime
    // already knows, or we weren't actually inlining yet.
    void setReported() { inlReported = true; }

private:

    // No copying or assignment allowed.
    InlineResult(const InlineResult&) = delete;
    InlineResult operator=(const InlineResult&) = delete;

    // Handle implications of an inline observation
    void noteInternal(InlineObservation obs, InlineImpact impact)
    {
        // Ignore INFORMATION for now, since policy
        // is still embedded at the observation sites.
        if (impact == InlineImpact::INFORMATION)
        {
            return;
        }

        InlineTarget target = inlGetTarget(obs);

        if (target == InlineTarget::CALLEE)
        {
            this->setNever(obs);
        }
        else
        {
            this->setFailure(obs);
        }
    }

    // setFailure means this particular instance can't be inlined.
    // It can override setCandidate, but not setSuccess
    void setFailure(InlineObservation obs)
    {
        assert(!isSuccess());
        setCommon(InlineDecision::FAILURE, obs);
    }

    // setNever means this callee can never be inlined anywhere.
    // It can override setCandidate, but not setSuccess
    void setNever(InlineObservation obs)
    {
        assert(!isSuccess());
        setCommon(InlineDecision::NEVER, obs);
    }

    // Helper for setting decision and reason
    void setCommon(InlineDecision decision, InlineObservation obs)
    {
        assert(inlIsValidObservation(obs));
        assert(decision != InlineDecision::UNDECIDED);
        inlDecision = decision;
        inlObservation = obs;
    }

    // Report/log/dump decision as appropriate
    void report();

    Compiler*               inlCompiler;
    InlineDecision          inlDecision;
    InlineObservation       inlObservation;
    GenTreeCall*            inlCall;
    CORINFO_METHOD_HANDLE   inlCaller;
    CORINFO_METHOD_HANDLE   inlCallee;
    const char*             inlContext;
    bool                    inlReported;
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

// InlineHints are a legacy form of observations.

enum InlineHints
{
    //Static inline hints are here.
    InlLooksLikeWrapperMethod = 0x0001,     // The inline candidate looks like it's a simple wrapper method.

    InlArgFeedsConstantTest   = 0x0002,     // One or more of the incoming arguments feeds into a test
                                            //against a constant.  This is a good candidate for assertion
                                            //prop.

    InlMethodMostlyLdSt       = 0x0004,     //This method is mostly loads and stores.

    InlMethodContainsCondThrow= 0x0008,     //Method contains a conditional throw, so it does not bloat the
                                            //code as much.
    InlArgFeedsRngChk         = 0x0010,     //Incoming arg feeds an array bounds check.  A good assertion
                                            //prop candidate.

    //Dynamic inline hints are here.  Only put hints that add to the multiplier in here.
    InlIncomingConstFeedsCond = 0x0100,     //Incoming argument is constant and feeds a conditional.
    InlAllDynamicHints        = InlIncomingConstFeedsCond
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
public:

    // New context for the root instance
    static InlineContext* newRoot(Compiler* compiler);

    // New context for a successful inline
    static InlineContext* newSuccess(Compiler*   compiler,
                                     InlineInfo* inlineInfo);

#ifdef DEBUG

    // New context for a failing inline
    static InlineContext* newFailure(Compiler *    compiler,
                                     GenTree*      stmt,
                                     InlineResult* inlineResult);

    // Dump the context and all descendants
    void Dump(Compiler* compiler, int indent = 0);

#endif

    // Get the parent context for this context.
    InlineContext* getParent() const
    {
        return inlParent;
    }

    // Get the code pointer for this context.
    BYTE* getCode() const
    {
        return inlCode;
    }

private:

    InlineContext();

private:

    InlineContext*        inlParent;      // logical caller (parent)
    InlineContext*        inlChild;       // first child
    InlineContext*        inlSibling;     // next child of the parent
    IL_OFFSETX            inlOffset;      // call site location within parent
    BYTE*                 inlCode;        // address of IL buffer for the method
    InlineObservation     inlObservation; // what lead to this inline

#ifdef DEBUG
    CORINFO_METHOD_HANDLE inlCallee;      // handle to the method
    unsigned              inlTreeID;      // ID of the GenTreeCall
    bool                  inlSuccess;     // true if this was a successful inline
#endif

};

#endif // _INLINE_H_

