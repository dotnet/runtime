// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Inlining Policies
//
// This file contains class definitions for various inlining
// policies used by the jit.
//
// -- CLASSES --
//
// LegalPolicy         - partial class providing common legality checks
// LegacyPolicy        - policy that provides legacy inline behavior
//
// These experimental policies are available only in
// DEBUG or release+INLINE_DATA builds of the jit.
//
// RandomPolicy        - randomized inlining
// DiscretionaryPolicy - legacy variant with uniform size policy
// ModelPolicy         - policy based on statistical modelling
// FullPolicy          - inlines everything up to size and depth limits
// SizePolicy          - tries not to increase method sizes

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
    LegalPolicy(bool isPrejitRoot)
        : InlinePolicy(isPrejitRoot)
    {
        // empty
    }

    // Handle an observation that must cause inlining to fail.
    void NoteFatal(InlineObservation obs) override;

protected:

    // Helper methods
    void NoteInternal(InlineObservation obs);
    void SetCandidate(InlineObservation obs);
    void SetFailure(InlineObservation obs);
    void SetNever(InlineObservation obs);
};

// Forward declaration for the state machine class used by the
// LegacyPolicy

class CodeSeqSM;

// LegacyPolicy implements the inlining policy used by the jit in its
// initial release.

class LegacyPolicy : public LegalPolicy
{
public:

    // Construct a LegacyPolicy
    LegacyPolicy(Compiler* compiler, bool isPrejitRoot)
        : LegalPolicy(isPrejitRoot)
        , m_RootCompiler(compiler)
        , m_StateMachine(nullptr)
        , m_CodeSize(0)
        , m_CallsiteFrequency(InlineCallsiteFrequency::UNUSED)
        , m_InstructionCount(0)
        , m_LoadStoreCount(0)
        , m_CalleeNativeSizeEstimate(0)
        , m_CallsiteNativeSizeEstimate(0)
        , m_Multiplier(0.0)
        , m_IsForceInline(false)
        , m_IsForceInlineKnown(false)
        , m_IsInstanceCtor(false)
        , m_IsFromPromotableValueClass(false)
        , m_HasSimd(false)
        , m_LooksLikeWrapperMethod(false)
        , m_ArgFeedsConstantTest(false)
        , m_MethodIsMostlyLoadStore(false)
        , m_ArgFeedsRangeCheck(false)
        , m_ConstantFeedsConstantTest(false)
    {
        // empty
    }

    // Policy observations
    void NoteSuccess() override;
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override { return true; }

    // Policy estimates
    int CodeSizeEstimate() override;

#if defined(DEBUG) || defined(INLINE_DATA)

    const char* GetName() const override { return "LegacyPolicy"; }

#endif // (DEBUG) || defined(INLINE_DATA)

protected:

    // Constants
    enum { MAX_BASIC_BLOCKS = 5, SIZE_SCALE = 10 };

    // Helper methods
    double DetermineMultiplier();
    int DetermineNativeSizeEstimate();
    int DetermineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methodInfo);

    // Data members
    Compiler*               m_RootCompiler;                      // root compiler instance
    CodeSeqSM*              m_StateMachine;
    unsigned                m_CodeSize;
    InlineCallsiteFrequency m_CallsiteFrequency;
    unsigned                m_InstructionCount;
    unsigned                m_LoadStoreCount;
    int                     m_CalleeNativeSizeEstimate;
    int                     m_CallsiteNativeSizeEstimate;
    double                  m_Multiplier;
    bool                    m_IsForceInline :1;
    bool                    m_IsForceInlineKnown :1;
    bool                    m_IsInstanceCtor :1;
    bool                    m_IsFromPromotableValueClass :1;
    bool                    m_HasSimd :1;
    bool                    m_LooksLikeWrapperMethod :1;
    bool                    m_ArgFeedsConstantTest :1;
    bool                    m_MethodIsMostlyLoadStore :1;
    bool                    m_ArgFeedsRangeCheck :1;
    bool                    m_ConstantFeedsConstantTest :1;
};

#ifdef DEBUG

// RandomPolicy implements a policy that inlines at random.
// It is mostly useful for stress testing.

class RandomPolicy : public LegalPolicy
{
public:

    // Construct a RandomPolicy
    RandomPolicy(Compiler* compiler, bool isPrejitRoot, unsigned seed);

    // Policy observations
    void NoteSuccess() override;
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override { return true; }

    // Policy estimates
    int CodeSizeEstimate() override
    {
        return 0;
    }

    const char* GetName() const override { return "RandomPolicy"; }

private:

    // Data members
    Compiler*               m_RootCompiler;
    CLRRandom*              m_Random;
    unsigned                m_CodeSize;
    bool                    m_IsForceInline :1;
    bool                    m_IsForceInlineKnown :1;
};

#endif // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)

// DiscretionaryPolicy is a variant of the legacy policy.  It differs
// in that there is no ALWAYS_INLINE class, there is no IL size limit,
// and in prejit mode, discretionary failures do not set the "NEVER"
// inline bit.
//
// It is useful for gathering data about inline costs.

class DiscretionaryPolicy : public LegacyPolicy
{
public:

    // Construct a DiscretionaryPolicy
    DiscretionaryPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy observations
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteInt(InlineObservation obs, int value) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy estimates
    int CodeSizeEstimate() override;

    // Externalize data
    void DumpData(FILE* file) const override;
    void DumpSchema(FILE* file) const override;

    // Miscellaneous
    const char* GetName() const override { return "DiscretionaryPolicy"; }

protected:

    void ComputeOpcodeBin(OPCODE opcode);
    void EstimateCodeSize();
    void MethodInfoObservations(CORINFO_METHOD_INFO* methodInfo);
    enum { MAX_ARGS = 6 };

    unsigned    m_Depth;
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
    unsigned    m_CallCount;
    int         m_ModelCodeSizeEstimate;
};

// ModelPolicy is an experimental policy that uses the results
// of data modelling to make estimates.

class ModelPolicy : public DiscretionaryPolicy
{
public:

    // Construct a ModelPolicy
    ModelPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Miscellaneous
    const char* GetName() const override { return "ModelPolicy"; }
};

// FullPolicy is an experimental policy that will always inline if
// possible, subject to externally settable depth and size limits.
//
// It's useful for unconvering the full set of possible inlines for
// methods.

class FullPolicy : public DiscretionaryPolicy
{
public:

    // Construct a FullPolicy
    FullPolicy(Compiler* compiler, bool isPrejitRoot);

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Miscellaneous
    const char* GetName() const override { return "FullPolicy"; }
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
    const char* GetName() const override { return "SizePolicy"; }
};


#endif // defined(DEBUG) || defined(INLINE_DATA)

#endif // _INLINE_POLICY_H_
