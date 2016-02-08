// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INLINE_H_
#define _INLINE_H_

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

// Possible targets of an inline observation

enum class InlineTarget
{
    CALLEE,         // observation applies to all calls to this callee
    CALLER,         // observation applies to all calls made by this caller
    CALLSITE        // observation applies to a specific call site
};

// Possible impact of an inline observation

enum class InlineImpact
{
    FATAL,          // inlining impossible, unsafe to evaluate further
    FUNDAMENTAL,    // inlining impossible for fundamental reasons, deeper exploration safe
    LIMITATION,     // inlining impossible because of jit limitations, deeper exploration safe
    PERFORMANCE,    // inlining inadvisable because of performance concerns
    INFORMATION     // policy-free observation to provide data for later decision making
};

// The set of possible inline observations

enum class InlineObservation
{
#define INLINE_OBSERVATION(name, type, description, impact, scope) scope ## _ ## name,
#include "inline.def"
#undef INLINE_OBSERVATION
};

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

#endif // _INLINE_H_

