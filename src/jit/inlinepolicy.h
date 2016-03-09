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
        , inlCompiler(compiler)
        , inlStateMachine(nullptr)
        , inlCodeSize(0)
        , inlNativeSizeEstimate(NATIVE_SIZE_INVALID)
        , inlCallsiteFrequency(InlineCallsiteFrequency::UNUSED)
        , inlIsForceInline(false)
        , inlIsForceInlineKnown(false)
        , inlIsInstanceCtor(false)
        , inlIsFromPromotableValueClass(false)
        , inlHasSimd(false)
        , inlLooksLikeWrapperMethod(false)
        , inlArgFeedsConstantTest(false)
        , inlMethodIsMostlyLoadStore(false)
        , inlArgFeedsRangeCheck(false)
        , inlConstantFeedsConstantTest(false)
    {
        // empty
    }

    // Policy observations
    void noteSuccess() override;
    void noteBool(InlineObservation obs, bool value) override;
    void noteFatal(InlineObservation obs) override;
    void noteInt(InlineObservation obs, int value) override;
    void noteDouble(InlineObservation obs, double value) override;

    // Policy determinations
    double determineMultiplier() override;
    int determineNativeSizeEstimate() override;
    int determineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methodInfo) override;

    // Policy policies
    bool propagateNeverToRuntime() const override { return true; }

#ifdef DEBUG
    const char* getName() const override { return "LegacyPolicy"; }
#endif

private:

    // Helper methods
    void noteInternal(InlineObservation obs);
    void setCandidate(InlineObservation obs);
    void setFailure(InlineObservation obs);
    void setNever(InlineObservation obs);

    // Constants
    const unsigned MAX_BASIC_BLOCKS = 5;

    // Data members
    Compiler*               inlCompiler;
    CodeSeqSM*              inlStateMachine;
    unsigned                inlCodeSize;
    int                     inlNativeSizeEstimate;
    InlineCallsiteFrequency inlCallsiteFrequency;
    bool                    inlIsForceInline :1;
    bool                    inlIsForceInlineKnown :1;
    bool                    inlIsInstanceCtor :1;
    bool                    inlIsFromPromotableValueClass :1;
    bool                    inlHasSimd :1;
    bool                    inlLooksLikeWrapperMethod :1;
    bool                    inlArgFeedsConstantTest :1;
    bool                    inlMethodIsMostlyLoadStore :1;
    bool                    inlArgFeedsRangeCheck :1;
    bool                    inlConstantFeedsConstantTest :1;
};

#endif // _INLINE_POLICY_H_
