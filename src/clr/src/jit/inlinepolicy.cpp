// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "inlinepolicy.h"
#include "sm.h"

//------------------------------------------------------------------------
// getPolicy: Factory method for getting an InlinePolicy
//
// Arguments:
//    compiler     - the compiler instance that will evaluate inlines
//    isPrejitRoot - true if this policy is evaluating a prejit root
//
// Return Value:
//    InlinePolicy to use in evaluating the inlines
//
// Notes:
//    Determines which of the various policies should apply,
//    and creates (or reuses) a policy instance to use.

InlinePolicy* InlinePolicy::getPolicy(Compiler* compiler, bool isPrejitRoot)
{
    // For now, always create a Legacy policy.
    InlinePolicy* policy = new (compiler, CMK_Inlining) LegacyPolicy(compiler, isPrejitRoot);

    return policy;
}

//------------------------------------------------------------------------
// noteSuccess: handle finishing all the inlining checks successfully

void LegacyPolicy::noteSuccess()
{
    assert(inlDecisionIsCandidate(inlDecision));
    inlDecision = InlineDecision::SUCCESS;
}

//------------------------------------------------------------------------
// noteBool: handle a boolean observation with non-fatal impact
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value of the observation
void LegacyPolicy::noteBool(InlineObservation obs, bool value)
{
    // Check the impact
    InlineImpact impact = inlGetImpact(obs);

    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(impact != InlineImpact::FATAL);

    // Handle most information here
    bool isInformation = (impact == InlineImpact::INFORMATION);
    bool propagate = !isInformation;

    if (isInformation)
    {
        switch (obs)
        {
        case InlineObservation::CALLEE_IS_FORCE_INLINE:
            // We may make the force-inline observation more than
            // once.  All observations should agree.
            assert(!inlIsForceInlineKnown || (inlIsForceInline == value));
            inlIsForceInline = value;
            inlIsForceInlineKnown = true;
            break;
        case InlineObservation::CALLEE_IS_INSTANCE_CTOR:
            inlIsInstanceCtor = value;
            break;
        case InlineObservation::CALLEE_CLASS_PROMOTABLE:
            inlIsFromPromotableValueClass = value;
            break;
        case InlineObservation::CALLEE_HAS_SIMD:
            inlHasSimd = value;
            break;
        case InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER:
            inlLooksLikeWrapperMethod = value;
            break;
        case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
            inlArgFeedsConstantTest = value;
            break;
        case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
            inlArgFeedsRangeCheck = value;
            break;
        case InlineObservation::CALLEE_IS_MOSTLY_LOAD_STORE:
            inlMethodIsMostlyLoadStore = value;
            break;
        case InlineObservation::CALLEE_HAS_SWITCH:
            // Pass this one on, it should cause inlining to fail.
            propagate = true;
            break;
        case InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST:
            inlConstantFeedsConstantTest = value;
            break;
        case InlineObservation::CALLSITE_NATIVE_SIZE_ESTIMATE_OK:
            // Passed the profitability screen. Update candidacy.
            setCandidate(obs);
            break;
        case InlineObservation::CALLEE_BEGIN_OPCODE_SCAN:
            {
                // Set up the state machine, if this inline is
                // discretionary and is still a candidate.
                if (inlDecisionIsCandidate(inlDecision)
                    && (inlObservation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE))
                {
                    // Better not have a state machine already.
                    assert(inlStateMachine == nullptr);
                    inlStateMachine = new (inlCompiler, CMK_Inlining) CodeSeqSM;
                    inlStateMachine->Start(inlCompiler);
                }
                break;
            }

        case InlineObservation::CALLEE_END_OPCODE_SCAN:
            {
                // We only expect to see this observation once, so the
                // native size estimate should have its initial value.
                assert(inlNativeSizeEstimate == NATIVE_SIZE_INVALID);

                // If we were using the state machine, get it to kick
                // out a size estimate.
                if (inlStateMachine != nullptr)
                {
                    inlStateMachine->End();
                    inlNativeSizeEstimate = inlStateMachine->NativeSize;
                    assert(inlNativeSizeEstimate != NATIVE_SIZE_INVALID);
                }

                break;
            }

        default:
            // Ignore the remainder for now
            break;
        }
    }

    if (propagate)
    {
        noteInternal(obs);
    }
}

//------------------------------------------------------------------------
// noteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::noteFatal(InlineObservation obs)
{
    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(inlGetImpact(obs) == InlineImpact::FATAL);
    noteInternal(obs);
    assert(inlDecisionIsFailure(inlDecision));
}

//------------------------------------------------------------------------
// noteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void LegacyPolicy::noteInt(InlineObservation obs, int value)
{
    switch (obs)
    {
    case InlineObservation::CALLEE_MAXSTACK:
        {
            assert(inlIsForceInlineKnown);

            unsigned calleeMaxStack = static_cast<unsigned>(value);

            if (!inlIsForceInline && (calleeMaxStack > SMALL_STACK_SIZE))
            {
                setNever(InlineObservation::CALLEE_MAXSTACK_TOO_BIG);
            }

            break;
        }

    case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
        {
            assert(inlIsForceInlineKnown);
            assert(value != 0);

            unsigned basicBlockCount = static_cast<unsigned>(value);

            if (!inlIsForceInline && (basicBlockCount > MAX_BASIC_BLOCKS))
            {
                setNever(InlineObservation::CALLEE_TOO_MANY_BASIC_BLOCKS);
            }

            break;
        }

    case InlineObservation::CALLEE_IL_CODE_SIZE:
        {
            assert(inlIsForceInlineKnown);
            assert(value != 0);
            inlCodeSize = static_cast<unsigned>(value);

            // Now that we know size and forceinline state,
            // update candidacy.
            if (inlCodeSize <= ALWAYS_INLINE_SIZE)
            {
                // Candidate based on small size
                setCandidate(InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE);
            }
            else if (inlIsForceInline)
            {
                // Candidate based on force inline
                setCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
            }
            else if (inlCodeSize <= inlCompiler->getImpInlineSize())
            {
                // Candidate, pending profitability evaluation
                setCandidate(InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);
            }
            else
            {
                // Callee too big, not a candidate
                setNever(InlineObservation::CALLEE_TOO_MUCH_IL);
            }

            break;
        }

    case InlineObservation::CALLEE_OPCODE_NORMED:
    case InlineObservation::CALLEE_OPCODE:
        {
            if (inlStateMachine != nullptr)
            {
                OPCODE opcode = static_cast<OPCODE>(value);
                SM_OPCODE smOpcode = CodeSeqSM::MapToSMOpcode(opcode);
                noway_assert(smOpcode < SM_COUNT);
                noway_assert(smOpcode != SM_PREFIX_N);
                if (obs == InlineObservation::CALLEE_OPCODE_NORMED)
                {
                    if (smOpcode == SM_LDARGA_S)
                    {
                        smOpcode = SM_LDARGA_S_NORMED;
                    }
                    else if (smOpcode == SM_LDLOCA_S)
                    {
                        smOpcode = SM_LDLOCA_S_NORMED;
                    }
                }

                inlStateMachine->Run(smOpcode DEBUGARG(0));
            }
            break;
        }

    default:
        // Ignore all other information
        break;
    }
}

//------------------------------------------------------------------------
// noteDouble: handle an observed double value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void LegacyPolicy::noteDouble(InlineObservation obs, double value)
{
    // Ignore for now...
    (void) value;
    (void) obs;
}

//------------------------------------------------------------------------
// noteInternal: helper for handling an observation
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::noteInternal(InlineObservation obs)
{
    // Note any INFORMATION that reaches here will now cause failure.
    // Non-fatal INFORMATION observations must be handled higher up.
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

//------------------------------------------------------------------------
// setFailure: helper for setting a failing decision
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::setFailure(InlineObservation obs)
{
    // Expect a valid observation
    assert(inlIsValidObservation(obs));

    switch (inlDecision)
    {
    case InlineDecision::FAILURE:
        // Repeated failure only ok if evaluating a prejit root
        assert(inlIsPrejitRoot);
        break;
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        inlDecision = InlineDecision::FAILURE;
        inlObservation = obs;
        break;
    default:
        // SUCCESS, NEVER, or ??
        assert(!"Unexpected inlDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// setNever: helper for setting a never decision
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::setNever(InlineObservation obs)
{
    // Expect a valid observation
    assert(inlIsValidObservation(obs));

    switch (inlDecision)
    {
    case InlineDecision::NEVER:
        // Repeated never only ok if evaluating a prejit root
        assert(inlIsPrejitRoot);
        break;
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        inlDecision = InlineDecision::NEVER;
        inlObservation = obs;
        break;
    default:
        // SUCCESS, FAILURE or ??
        assert(!"Unexpected inlDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// setCandidate: helper updating candidacy
//
// Arguments:
//    obs      - the current obsevation
//
// Note:
//    Candidate observations are handled here. If the inline has already
//    failed, they're ignored. If there's already a candidate reason,
//    this new reason trumps it.

void LegacyPolicy::setCandidate(InlineObservation obs)
{
    // Ignore if this inline is going to fail.
    if (inlDecisionIsFailure(inlDecision))
    {
        return;
    }

    // We should not have declared success yet.
    assert(!inlDecisionIsSuccess(inlDecision));

    // Update, overriding any previous candidacy.
    inlDecision = InlineDecision::CANDIDATE;
    inlObservation = obs;
}

//------------------------------------------------------------------------
// determineMultiplier: determine benefit multiplier for this inline
//
// Notes: uses the accumulated set of observations to compute a
// profitability boost for the inline candidate.

double LegacyPolicy::determineMultiplier()
{
    double multiplier = 0;

    // Bump up the multiplier for instance constructors

    if (inlIsInstanceCtor)
    {
        multiplier += 1.5;
        JITDUMP("\nmultiplier in instance constructors increased to %g.", multiplier);
    }

    // Bump up the multiplier for methods in promotable struct

    if (inlIsFromPromotableValueClass)
    {
        multiplier += 3;
        JITDUMP("\nmultiplier in methods of promotable struct increased to %g.", multiplier);
    }

#ifdef FEATURE_SIMD

    if (inlHasSimd)
    {
        multiplier += JitConfig.JitInlineSIMDMultiplier();
        JITDUMP("\nInline candidate has SIMD type args, locals or return value.  Multiplier increased to %g.", multiplier);
    }

#endif // FEATURE_SIMD

    if (inlLooksLikeWrapperMethod)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate looks like a wrapper method.  Multiplier increased to %g.", multiplier);
    }

    if (inlArgFeedsConstantTest)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate has an arg that feeds a constant test.  Multiplier increased to %g.", multiplier);
    }

    if (inlMethodIsMostlyLoadStore)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate is mostly loads and stores.  Multiplier increased to %g.", multiplier);
    }

    if (inlArgFeedsRangeCheck)
    {
        multiplier += 0.5;
        JITDUMP("\nInline candidate has arg that feeds range check.  Multiplier increased to %g.", multiplier);
    }

    if (inlConstantFeedsConstantTest)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate has const arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
    }

    return multiplier;
}

//------------------------------------------------------------------------
// hasNativeCodeSizeEstimate: did this policy estimate native code size
//
// Notes: 
//    Temporary scaffolding for refactoring work.

bool LegacyPolicy::hasNativeSizeEstimate()
{
    return (inlStateMachine != nullptr);
}

//------------------------------------------------------------------------
// determineNativeCodeSizeEstimate: return estimated native code size for
// this inline candidate.
//
// Notes: 
//    Uses the results of a state machine model for discretionary
//    candidates.  Should not be needed for forcded or always
//    candidates.

int LegacyPolicy::determineNativeSizeEstimate()
{
    return inlNativeSizeEstimate;
}
