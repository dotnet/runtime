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
// LegacyPolicy        - policy to provide legacy inline behavior
// RandomPolicy        - randomized inlining
// DiscretionaryPolicy - legacy variant with uniform size policy

#ifndef _INLINE_POLICY_H_
#define _INLINE_POLICY_H_

#include "jit.h"
#include "inline.h"

class CodeSeqSM;

// LegacyPolicy implements the inlining policy used by the jit in its
// initial release.
//
// Generally speaking, the legacy policy expects the inlining attempt
// to fail fast when a fatal or equivalent observation is made. So
// once an observation causes failure, no more observations are
// expected. However for the prejit scan case (where the jit is not
// actually inlining, but is assessing a method's general
// inlinability) the legacy policy allows multiple failing
// observations provided they have the same impact. Only the first
// observation that puts the policy into a failing state is
// remembered. Transitions from failing states to candidate or success
// states are not allowed.

class LegacyPolicy : public InlinePolicy
{
public:

    // Construct a LegacyPolicy
    LegacyPolicy(Compiler* compiler, bool isPrejitRoot)
        : InlinePolicy(isPrejitRoot)
        , m_RootCompiler(compiler)
        , m_StateMachine(nullptr)
        , m_CodeSize(0)
        , m_CallsiteFrequency(InlineCallsiteFrequency::UNUSED)
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
    void NoteFatal(InlineObservation obs) override;
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override { return true; }

#ifdef DEBUG
    const char* GetName() const override { return "LegacyPolicy"; }
#endif

protected:

    // Helper methods
    void NoteInternal(InlineObservation obs);
    void SetCandidate(InlineObservation obs);
    void SetFailure(InlineObservation obs);
    void SetNever(InlineObservation obs);
    double DetermineMultiplier();
    int DetermineNativeSizeEstimate();
    int DetermineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methodInfo);

    // Constants
    const unsigned MAX_BASIC_BLOCKS = 5;

    // Data members
    Compiler*               m_RootCompiler;                      // root compiler instance
    CodeSeqSM*              m_StateMachine;
    unsigned                m_CodeSize;
    InlineCallsiteFrequency m_CallsiteFrequency;
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

class RandomPolicy : public InlinePolicy
{
public:

    // Construct a RandomPolicy
    RandomPolicy(Compiler* compiler, bool isPrejitRoot, unsigned seed);

    // Policy observations
    void NoteSuccess() override;
    void NoteBool(InlineObservation obs, bool value) override;
    void NoteFatal(InlineObservation obs) override;
    void NoteInt(InlineObservation obs, int value) override;

    // Policy determinations
    void DetermineProfitability(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override { return true; }

    const char* GetName() const override { return "RandomPolicy"; }

private:

    // Helper methods
    void NoteInternal(InlineObservation obs);
    void SetCandidate(InlineObservation obs);
    void SetFailure(InlineObservation obs);
    void SetNever(InlineObservation obs);

    // Data members
    Compiler*               m_RootCompiler;
    CLRRandom*              m_Random;
    unsigned                m_CodeSize;
    bool                    m_IsForceInline :1;
    bool                    m_IsForceInlineKnown :1;
};

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
    void NoteInt(InlineObservation obs, int value) override;

    // Policy policies
    bool PropagateNeverToRuntime() const override;

    const char* GetName() const override { return "DiscretionaryPolicy"; }
};

#endif // DEBUG

#endif // _INLINE_POLICY_H_
