// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// Lookup table for inline description strings

static const char* InlineDescriptions[] = 
{
#define INLINE_OBSERVATION(name, type, description, impact, target) description,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline targets

static const InlineTarget InlineTargets[] =
{
#define INLINE_OBSERVATION(name, type, description, impact, target) InlineTarget::target,
#include "inline.def"
#undef INLINE_OBSERVATION
};

// Lookup table for inline impacts

static const InlineImpact InlineImpacts[] =
{
#define INLINE_OBSERVATION(name, type, description, impact, target) InlineImpact::impact,
#include "inline.def"
#undef INLINE_OBSERVATION
};

#ifdef DEBUG

//------------------------------------------------------------------------
// inlIsValidObservation: run a validity check on an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    true if the observation is valid

static bool inlIsValidObservation(InlineObservation obs)
{
    return((obs > InlineObservation::CALLEE_UNUSED_INITIAL) &&
           (obs < InlineObservation::CALLEE_UNUSED_FINAL));
}

#endif // DEBUG

//------------------------------------------------------------------------
// inlGetDescriptionString: get a string describing this inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the observation

const char* inlGetDescriptionString(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineDescriptions[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetTarget: get the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum describing the target

InlineTarget inlGetTarget(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineTargets[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetTargetString: get a string describing the target of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the target

const char* inlGetTargetstring(InlineObservation obs)
{
    InlineTarget t = inlGetTarget(obs);
    switch (t) 
    {
    case InlineTarget::CALLER:
        return "caller";
    case InlineTarget::CALLEE:
        return "callee";
    case InlineTarget::CALLSITE:
        return "call site";
    default:
        return "unexpected target";
    }
}

//------------------------------------------------------------------------
// inlGetImpact: get the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    enum value describing the impact

InlineImpact inlGetImpact(InlineObservation obs)
{
    assert(inlIsValidObservation(obs));
    return InlineImpacts[static_cast<int>(obs)];
}

//------------------------------------------------------------------------
// inlGetImpactString: get a string describing the impact of an inline observation
//
// Arguments:
//    obs - the observation in question
//
// Return Value:
//    string describing the impact

const char* inlGetImpactString(InlineObservation obs)
{
    InlineImpact i = inlGetImpact(obs);
    switch (i) 
    {
    case InlineImpact::FATAL:
        return "correctness -- fatal";
    case InlineImpact::FUNDAMENTAL:
        return "correctness -- fundamental limitation";
    case InlineImpact::LIMITATION:
        return "correctness -- jit limitation";
    case InlineImpact::PERFORMANCE:
        return "performance";
    case InlineImpact::INFORMATION:
        return "information";
    default:
        return "unexpected impact";
    }
}

