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
    InlinePolicy* policy = new (compiler, CMK_Inlining) LegacyPolicy();

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

    // Update the status
    setCommon(InlineDecision::CANDIDATE, obs);
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
    (void) value;
    note(obs);
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
// setNever: helper for handling an observation
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
// setNever: helper for setting a failling decision
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::setFailure(InlineObservation obs)
{
    assert(!inlDecisionIsSuccess(inlDecision));
    setCommon(InlineDecision::FAILURE, obs);
}

//------------------------------------------------------------------------
// setNever: helper for setting a never decision
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::setNever(InlineObservation obs)
{
    assert(!inlDecisionIsSuccess(inlDecision));
    setCommon(InlineDecision::NEVER, obs);
}

//------------------------------------------------------------------------
// setCommon: helper for updating decision and observation
//
// Arguments:
//    decision - the updated decision
//    obs      - the current obsevation

void LegacyPolicy::setCommon(InlineDecision decision, InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    assert(decision != InlineDecision::UNDECIDED);
    inlDecision = decision;
    inlObservation = obs;
}
