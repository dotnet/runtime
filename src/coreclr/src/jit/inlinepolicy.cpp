// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "inlinepolicy.h"

//------------------------------------------------------------------------
// getPolicy: Factory method for getting an InlinePolicy
//
// Arguments:
//    compiler - the compiler instance that will evaluate inlines
//
// Return Value:
//    InlinePolicy to use in evaluating the inlines
//
// Notes:
//    Determines which of the various policies should apply,
//    and creates (or reuses) a policy instance to use.

InlinePolicy* InlinePolicy::getPolicy(Compiler* compiler)
{
    // For now, always create a Legacy policy.
    InlinePolicy* policy = new (compiler, CMK_Inlining) LegacyPolicy(compiler);

    return policy;
}

//------------------------------------------------------------------------
// noteCandidate: handle passing a set of inlining checks successfully
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::noteCandidate(InlineObservation obs)
{
    assert(!inlDecisionIsDecided(inlDecision));

    // Check the impact, it should be INFORMATION
    InlineImpact impact = inlGetImpact(obs);
    assert(impact == InlineImpact::INFORMATION);

    switch (obs)
    {
    case InlineObservation::CALLEE_IS_FORCE_INLINE:
        {
            inlIsForceInline = true;
            break;
        }

    default:
        break;
    }

    switch (inlDecision)
    {
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        // Candidate observations overwrite one another
        inlDecision = InlineDecision::CANDIDATE;
        inlObservation = obs;
        break;
    default:
        // SUCCESS or NEVER or FAILURE or ??
        assert(!"Unexpected inlDecision");
        unreached();
    }
}

//------------------------------------------------------------------------
// noteSuccess: handle finishing all the inlining checks successfully

void LegacyPolicy::noteSuccess()
{
    assert(inlDecisionIsCandidate(inlDecision));
    inlDecision = InlineDecision::SUCCESS;
}

//------------------------------------------------------------------------
// note: handle an observation with non-fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::note(InlineObservation obs)
{
    // Check the impact
    InlineImpact impact = inlGetImpact(obs);

    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(impact != InlineImpact::FATAL);

    noteInternal(obs, impact);
}

//------------------------------------------------------------------------
// noteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::noteFatal(InlineObservation obs)
{
    // Check the impact
    InlineImpact impact = inlGetImpact(obs);

    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(impact == InlineImpact::FATAL);
    noteInternal(obs, impact);
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
            unsigned calleeMaxStack = static_cast<unsigned>(value);

            if (!inlIsForceInline && (calleeMaxStack > SMALL_STACK_SIZE))
            {
                setNever(InlineObservation::CALLEE_MAXSTACK_TOO_BIG);
            }

            break;
        }

    case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
        {
            assert(value != 0);

            unsigned basicBlockCount = static_cast<unsigned>(value);

            if (!inlIsForceInline && (basicBlockCount > MAX_BASIC_BLOCKS))
            {
                setNever(InlineObservation::CALLEE_TOO_MANY_BASIC_BLOCKS);
            }

            break;
        }

    case InlineObservation::CALLEE_NUMBER_OF_IL_BYTES:
        {
            assert(value != 0);

            unsigned ilByteSize = static_cast<unsigned>(value);

            if (!inlIsForceInline && (ilByteSize > inlCompiler->getImpInlineSize()))
            {
                setNever(InlineObservation::CALLEE_TOO_MUCH_IL);
            }

            break;
        }

    default:
        // Ignore all other information
        note(obs);
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
    (void) value;
    note(obs);
}

//------------------------------------------------------------------------
// noteInternal: helper for handling an observation
//
// Arguments:
//    obs      - the current obsevation
//    impact   - impact of the current observation

void LegacyPolicy::noteInternal(InlineObservation obs, InlineImpact impact)
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
        // Repeated failure only ok if in prejit scan
        assert(isPrejitScan());
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
        // Repeated never only ok if in prejit scan
        assert(isPrejitScan());
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
