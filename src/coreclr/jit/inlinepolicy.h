// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Inlining Policies
//
// This file contains class definitions for various inlining
// policies used by the jit.
//
// -- CLASSES --
//
// LegalPolicy           - partial class providing common legality checks
// DefaultPolicy         - default inliner policy
// ExtendedDefaultPolicy - a more aggressive and profile-driven variation of DefaultPolicy
// DiscretionaryPolicy   - default variant with uniform size policy
// ModelPolicy           - policy based on statistical modelling
// ProfilePolicy         - policy based on statistical modelling and profile feedback
//
// These experimental policies are available only in
// DEBUG or release+INLINE_DATA builds of the jit.
//
// RandomPolicy         - randomized inlining
// FullPolicy           - inlines everything up to size and depth limits
// SizePolicy           - tries not to increase method sizes
//
// The default policy in use is the DefaultPolicy.

#ifndef _INLINE_POLICY_H_
#define _INLINE_POLICY_H_

#include "jit.h"
#include "inline.h"

// LegalPolicy is a partial policy that encapsulates the common
// legality and ability checks the inliner must make.
//
// Generally speaking, the legal policy expects the inlining attempt
// to fail fast when a fatal or equivalent observation is made. So
// once an observation causes failure, no more observations are
// expected. However for the prejit scan case (where the jit is not
// actually inlining, but is assessing a method's general
// inlinability) the legal policy allows multiple failing
// observations provided they have the same impact. Only the first
// observation that puts the policy into a failing state is
// remembered. Transitions from failing states to candidate or success
// states are not allowed.

class LegalPolicy : public InlinePolicy
{

public:
    // Constructor
    LegalPolicy(bool isPrejitRoot) : InlinePolicy(isPrejitRoot)
    {
        // empty
    }

    // Handle an observation that must cause inlining to fail.
    void NoteFatal(InlineObservation obs) override;

#if defined(DEBUG) || defined(INLINE_DATA)

    // Record observation for prior failure
    void NotePriorFailure(InlineObservation obs) override;

#endif // defined(DEBUG) || defined(INLINE_DATA)

protected:
    // Helper methods
    void NoteInternal(InlineObservation obs);
    void SetCandidate(InlineObservation obs);
    void SetFailure(InlineObservation obs);
    void SetNever(InlineObservation obs);
};

// Forward declaration for the state machine class used by the
// DefaultPolicy

class CodeSeqSM;

// DefaultPolicy implements the default inlining policy for the jit.

class DefaultPolicy : public LegalPolicy
{
public:
    // Construct a DefaultPolicy
    DefaultPolicy(Compiler* compiler, bool isPrejitRoot)
        : LegalPolicy(isPrejitRoot)
        , m_RootCompiler(compiler)
        , m_StateMachine(nullptr)
        , m_Multiplier(0.0)
        , m_CodeSize(0)
        , m_CallsiteFrequency(InlineCallsiteFrequency::UNUSED)
        , m_CallsiteDepth(0)
        , m_InstructionCount(0)
        , m_LoadStoreCount(0)
        , m_ArgFeedsTest(0)
        , m_ArgFeedsConstantTest(0)
        , m_ArgFeedsRangeCheck(0)
        , m_ConstantArgFeedsConstantTest(0)
        , m_CalleeNativeSizeEstimate(0)
        , m_CallsiteNativeSizeEstimate(0)
        , m_IsForceInline(false)
        , m_IsForceInlineKnown(false)
        , m_IsInstanceCtor(false)
        , m_IsFromPromotableValueClass(false)
        , m_HasSimd(false)
        , m_LooksLikeWrapperMethod(false)
        , m_MethodIsMostlyLoadStore(false)
        , m_CallsiteIsInTryRegion(false)
        , m_CallsiteIsInLoop(false)
        , m_IsNoReturn(false)
        , m_IsNoReturnKnown(false)
        , m_ConstArgFeedsIsKnownConst(false)
        , m_ArgFeedsIsKnownConst(false)
        , m_InsideThrowBlock(false)
    {
        // empty
    }

    // Policy observations
    void NoteSuccess() override;
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;
    void NoteDouble(InlineObservation obs, double value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;
    bool BudgetCheck() const override;

    virtual unsigned EstimatedTotalILSize() const
    {
        return m_CodeSize;
    }

    // Policy policies
    bool PropagateNeverToRuntime() const override;

    // Policy estimates
    int CodeSizeEstimate() override;

#if defined(DEBUG) || defined(INLINE_DATA)
    void OnDumpXml(FILE* file, unsigned indent = 0) const override;

    const char* GetName() const override
    {
        return "DefaultPolicy";
    }
#endif // (DEBUG) || defined(INLINE_DATA)

protected:
    // Constants
    enum
    {
        MAX_BASIC_BLOCKS = 5,
        SIZE_SCALE       = 10
    };

    // Helper methods
    virtual double DetermineMultiplier();
    int            DetermineNativeSizeEstimate();
    int DetermineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methodInfo);

    // Data members
    Compiler*               m_RootCompiler; // root compiler instance
    CodeSeqSM*              m_StateMachine;
    double                  m_Multiplier;
    unsigned                m_CodeSize;
    InlineCallsiteFrequency m_CallsiteFrequency;
    unsigned                m_CallsiteDepth;
    unsigned                m_InstructionCount;
    unsigned                m_LoadStoreCount;
    unsigned                m_ArgFeedsTest;
    unsigned                m_ArgFeedsConstantTest;
    unsigned                m_ArgFeedsRangeCheck;
    unsigned                m_ConstantArgFeedsConstantTest;
    int                     m_CalleeNativeSizeEstimate;
    int                     m_CallsiteNativeSizeEstimate;
    bool                    m_IsForceInline : 1;
    bool                    m_IsForceInlineKnown : 1;
    bool                    m_IsInstanceCtor : 1;
    bool                    m_IsFromPromotableValueClass : 1;
    bool                    m_HasSimd : 1;
    bool                    m_LooksLikeWrapperMethod : 1;
    bool                    m_MethodIsMostlyLoadStore : 1;
    bool                    m_CallsiteIsInTryRegion : 1;
    bool                    m_CallsiteIsInLoop : 1;
    bool                    m_IsNoReturn : 1;
    bool                    m_IsNoReturnKnown : 1;
    bool                    m_ConstArgFeedsIsKnownConst : 1;
    bool                    m_ArgFeedsIsKnownConst : 1;
    bool                    m_InsideThrowBlock : 1;
};

// ExtendedDefaultPolicy is a slightly more aggressive variant of
// DefaultPolicy with an extended list of observations including profile data.
class ExtendedDefaultPolicy : public DefaultPolicy
{
public:
    ExtendedDefaultPolicy(Compiler* compiler, bool isPrejitRoot)
        : DefaultPolicy(compiler, isPrejitRoot)
        , m_ProfileFrequency(0.0)
        , m_BinaryExprWithCns(0)
        , m_ArgCasted(0)
        , m_ArgIsStructByValue(0)
        , m_FldAccessOverArgStruct(0)
        , m_FoldableBox(0)
        , m_Intrinsic(0)
        , m_BackwardJump(0)
        , m_ThrowBlock(0)
        , m_ArgIsExactCls(0)
        , m_ArgIsExactClsSigIsNot(0)
        , m_ArgIsConst(0)
        , m_ArgIsBoxedAtCallsite(0)
        , m_FoldableIntrinsic(0)
        , m_FoldableExpr(0)
        , m_FoldableExprUn(0)
        , m_FoldableBranch(0)
        , m_FoldableSwitch(0)
        , m_UnrollableMemop(0)
        , m_Switch(0)
        , m_DivByCns(0)
        , m_ReturnsStructByValue(false)
        , m_IsFromValueClass(false)
        , m_NonGenericCallsGeneric(false)
        , m_IsCallsiteInNoReturnRegion(false)
        , m_HasProfileWeights(false)
    {
        // Empty
    }

    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;
    void NoteDouble(InlineObservation obs, double value) override;

    double DetermineMultiplier() override;

    unsigned EstimatedTotalILSize() const override;

    bool RequiresPreciseScan() override
    {
        return true;
    }

#if defined(DEBUG) || defined(INLINE_DATA)
    void OnDumpXml(FILE* file, unsigned indent = 0) const override;

    const char* GetName() const override
    {
        return "ExtendedDefaultPolicy";
    }
#endif // defined(DEBUG) || defined(INLINE_DATA)

protected:
    double   m_ProfileFrequency;
    unsigned m_BinaryExprWithCns;
    unsigned m_ArgCasted;
    unsigned m_ArgIsStructByValue;
    unsigned m_FldAccessOverArgStruct;
    unsigned m_FoldableBox;
    unsigned m_Intrinsic;
    unsigned m_BackwardJump;
    unsigned m_ThrowBlock;
    unsigned m_ArgIsExactCls;
    unsigned m_ArgIsExactClsSigIsNot;
    unsigned m_ArgIsConst;
    unsigned m_ArgIsBoxedAtCallsite;
    unsigned m_FoldableIntrinsic;
    unsigned m_FoldableExpr;
    unsigned m_FoldableExprUn;
    unsigned m_FoldableBranch;
    unsigned m_FoldableSwitch;
    unsigned m_UnrollableMemop;
    unsigned m_Switch;
    unsigned m_DivByCns;
    bool     m_ReturnsStructByValue : 1;
    bool     m_IsFromValueClass : 1;
    bool     m_NonGenericCallsGeneric : 1;
    bool     m_IsCallsiteInNoReturnRegion : 1;
    bool     m_HasProfileWeights : 1;
};

// DiscretionaryPolicy is a variant of the default policy.  It
// differs in that there is no ALWAYS_INLINE class, there is no IL
// size limit, and in prejit mode, discretionary failures do not
// propagate the "NEVER" inline bit to the runtime.
//
// It is useful for gathering data about inline costs.

class DiscretionaryPolicy : public DefaultPolicy
{
public:
    // Construct a DiscretionaryPolicy
    DiscretionaryPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;
    void NoteDouble(InlineObservation obs, double value) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy estimates
    int CodeSizeEstimate() override;

#if defined(DEBUG) || defined(INLINE_DATA)

    // Externalize data
    void DumpData(FILE* file) const override;
    void DumpSchema(FILE* file) const override;

    // Miscellaneous
    const char* GetName() const override
    {
        return "DiscretionaryPolicy";
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

protected:
    void ComputeOpcodeBin(OPCODE opcode);
    void EstimateCodeSize();
    void EstimatePerformanceImpact();
    void MethodInfoObservations(CORINFO_METHOD_INFO* methodInfo);
    enum
    {
        MAX_ARGS = 6
    };

    double      m_ProfileFrequency;
    unsigned    m_BlockCount;
    unsigned    m_Maxstack;
    unsigned    m_ArgCount;
    CorInfoType m_ArgType[MAX_ARGS];
    size_t      m_ArgSize[MAX_ARGS];
    unsigned    m_LocalCount;
    CorInfoType m_ReturnType;
    size_t      m_ReturnSize;
    unsigned    m_ArgAccessCount;
    unsigned    m_LocalAccessCount;
    unsigned    m_IntConstantCount;
    unsigned    m_FloatConstantCount;
    unsigned    m_IntLoadCount;
    unsigned    m_FloatLoadCount;
    unsigned    m_IntStoreCount;
    unsigned    m_FloatStoreCount;
    unsigned    m_SimpleMathCount;
    unsigned    m_ComplexMathCount;
    unsigned    m_OverflowMathCount;
    unsigned    m_IntArrayLoadCount;
    unsigned    m_FloatArrayLoadCount;
    unsigned    m_RefArrayLoadCount;
    unsigned    m_StructArrayLoadCount;
    unsigned    m_IntArrayStoreCount;
    unsigned    m_FloatArrayStoreCount;
    unsigned    m_RefArrayStoreCount;
    unsigned    m_StructArrayStoreCount;
    unsigned    m_StructOperationCount;
    unsigned    m_ObjectModelCount;
    unsigned    m_FieldLoadCount;
    unsigned    m_FieldStoreCount;
    unsigned    m_StaticFieldLoadCount;
    unsigned    m_StaticFieldStoreCount;
    unsigned    m_LoadAddressCount;
    unsigned    m_ThrowCount;
    unsigned    m_ReturnCount;
    unsigned    m_CallCount;
    unsigned    m_CallSiteWeight;
    int         m_ModelCodeSizeEstimate;
    int         m_PerCallInstructionEstimate;
    bool        m_HasProfileWeights;
    bool        m_IsClassCtor;
    bool        m_IsSameThis;
    bool        m_CallerHasNewArray;
    bool        m_CallerHasNewObj;
    bool        m_CalleeHasGCStruct;
};

// ModelPolicy is an experimental policy that uses the results
// of data modelling to make estimates.

class ModelPolicy : public DiscretionaryPolicy
{
public:
    // Construct a ModelPolicy
    ModelPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override
    {
        return true;
    }

#if defined(DEBUG) || defined(INLINE_DATA)

    // Miscellaneous
    const char* GetName() const override
    {
        return "ModelPolicy";
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)
};

// ProfilePolicy is an experimental policy that uses the results
// of data modelling and profile feedback to make estimates.

class ProfilePolicy : public DiscretionaryPolicy
{
public:
    // Construct a ProfilePolicy
    ProfilePolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

#if defined(DEBUG) || defined(INLINE_DATA)

    // Miscellaneous
    const char* GetName() const override
    {
        return "ProfilePolicy";
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)
};

#if defined(DEBUG) || defined(INLINE_DATA)

// RandomPolicy implements a policy that inlines at random.
// It is mostly useful for stress testing.

class RandomPolicy : public DiscretionaryPolicy
{
public:
    // Construct a RandomPolicy
    RandomPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    const char* GetName() const override
    {
        return "RandomPolicy";
    }

private:
    // Data members
    CLRRandom* m_Random;
};

#endif // defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG) || defined(INLINE_DATA)

// FullPolicy is an experimental policy that will always inline if
// possible, subject to externally settable depth and size limits.
//
// It's useful for uncovering the full set of possible inlines for
// methods.

class FullPolicy : public DiscretionaryPolicy
{
public:
    // Construct a FullPolicy
    FullPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;
    bool BudgetCheck() const override;

    // Miscellaneous
    const char* GetName() const override
    {
        return "FullPolicy";
    }
};

// SizePolicy is an experimental policy that will inline as much
// as possible without increasing the (estimated) method size.
//
// It may be useful down the road as a policy to use for methods
// that are rarely executed (eg class constructors).

class SizePolicy : public DiscretionaryPolicy
{
public:
    // Construct a SizePolicy
    SizePolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Miscellaneous
    const char* GetName() const override
    {
        return "SizePolicy";
    }
};

// The ReplayPolicy performs only inlines specified by an external
// inline replay log.

class ReplayPolicy : public DiscretionaryPolicy
{
public:
    // Construct a ReplayPolicy
    ReplayPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteBool(InlineObservation obs, bool value) override;

    // Optional observations
    void NoteContext(InlineContext* context) override
    {
        m_InlineContext = context;
    }

    void NoteOffset(IL_OFFSET offset) override
    {
        m_Offset = offset;
    }

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Miscellaneous
    const char* GetName() const override
    {
        return "ReplayPolicy";
    }

    static void FinalizeXml();

private:
    bool FindMethod();
    bool FindContext(InlineContext* context);
    bool FindInline(CORINFO_METHOD_HANDLE callee);
    bool FindInline(unsigned token, unsigned hash, unsigned offset);

    static bool          s_WroteReplayBanner;
    static FILE*         s_ReplayFile;
    static CritSecObject s_XmlReaderLock;
    InlineContext*       m_InlineContext;
    IL_OFFSET            m_Offset;
    bool                 m_WasForceInline;
};

#endif // defined(DEBUG) || defined(INLINE_DATA)

#endif // _INLINE_POLICY_H_
