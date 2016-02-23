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

// LegacyPolicy implements the inlining policy used by the jit in its
// initial release.

class LegacyPolicy : public InlinePolicy
{
public:

    LegacyPolicy()
        : InlinePolicy()
    {
        // empty
    }

    // Policy observations
    void noteCandidate(InlineObservation obs) override;
    void noteSuccess() override;
    void note(InlineObservation obs) override;
    void noteFatal(InlineObservation obs) override;
    void noteInt(InlineObservation obs, int value) override;
    void noteDouble(InlineObservation obs, double value) override;

    // Policy decisions
    bool propagateNeverToRuntime() const override { return true; }

#ifdef DEBUG
    const char* getName() const override { return "LegacyPolicy"; }
#endif

private:

    // Helper methods
    void noteInternal(InlineObservation obs, InlineImpact impact);
    void setFailure(InlineObservation obs);
    void setNever(InlineObservation obs);
    void setCommon(InlineDecision decision, InlineObservation obs);
};

//
// Enums are used throughout to provide various descriptions.
//
// Classes are used as follows. There are 5 sitations where inline
// candidacy is evaluated.  In each case an InlineResult is allocated
// on the stack to collect information about the inline candidate.
//
// 1. Importer Candidate Screen (impMarkInlineCandidate)
//
// Creates: InlineCandidateInfo
//
// During importing, the IL being imported is scanned to identify
// inline candidates. This happens both when the root method is being
// imported as well as when prospective inlines are being imported.
// Candidates are marked in the IL and given an InlineCandidateInfo.
//
// 2. Inlining Optimization Pass -- candidates (fgInline)
//
// Creates / Uses: InlineContext
// Creates: InlineInfo, InlArgInfo, InlLocalVarInfo
//
// During the inlining optimation pass, each candidate is further
// analyzed. Viable candidates will eventually inspire creation of an
// InlineInfo and a set of InlArgInfos (for call arguments) and 
// InlLocalVarInfos (for callee locals).
//
// The analysis will also examine InlineContexts from relevant prior
// inlines. If the inline is successful, a new InlineContext will be
// created to remember this inline. In DEBUG builds, failing inlines
// also create InlineContexts.
//
// 3. Inlining Optimization Pass -- non-candidates (fgNoteNotInlineCandidate)
//
// Creates / Uses: InlineContext
//
// In DEBUG, the jit also searches for non-candidate calls to try
// and get a complete picture of the set of failed inlines.
//
// 4 & 5. Prejit suitability screens (compCompileHelper)
//
// When prejitting, each method is scanned to see if it is a viable
// inline candidate. The scanning happens in two stages.
//
// A note on InlinePolicy
//
// In the current code base, the inlining policy is distributed across
// the various parts of the code that drive the inlining process
// forward. Subsequent refactoring will extract some or all of this
// policy into a separate InlinePolicy object, to make it feasible to
// create and experiment with alternative policies, while preserving
// the existing policy as a baseline and fallback.


#endif // _INLINE_POLICY_H_
