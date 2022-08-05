// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
//    compiler      - the compiler instance that will evaluate inlines
//    isPrejitRoot  - true if this policy is evaluating a prejit root
//
// Return Value:
//    InlinePolicy to use in evaluating an inline.
//
// Notes:
//    Determines which of the various policies should apply,
//    and creates (or reuses) a policy instance to use.

InlinePolicy* InlinePolicy::GetPolicy(Compiler* compiler, bool isPrejitRoot)
{

#if defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG)
    const bool useRandomPolicyForStress = compiler->compRandomInlineStress();
#else
    const bool useRandomPolicyForStress = false;
#endif // defined(DEBUG)

    const bool useRandomPolicy = (JitConfig.JitInlinePolicyRandom() != 0);

    // Optionally install the RandomPolicy.
    if (useRandomPolicyForStress || useRandomPolicy)
    {
        return new (compiler, CMK_Inlining) RandomPolicy(compiler, isPrejitRoot);
    }

    // Optionally install the ReplayPolicy.
    bool useReplayPolicy = JitConfig.JitInlinePolicyReplay() != 0;

    if (useReplayPolicy)
    {
        return new (compiler, CMK_Inlining) ReplayPolicy(compiler, isPrejitRoot);
    }

    // Optionally install the SizePolicy.
    bool useSizePolicy = JitConfig.JitInlinePolicySize() != 0;

    if (useSizePolicy)
    {
        return new (compiler, CMK_Inlining) SizePolicy(compiler, isPrejitRoot);
    }

    // Optionally install the FullPolicy.
    bool useFullPolicy = JitConfig.JitInlinePolicyFull() != 0;

    if (useFullPolicy)
    {
        return new (compiler, CMK_Inlining) FullPolicy(compiler, isPrejitRoot);
    }

    // Optionally install the DiscretionaryPolicy.
    bool useDiscretionaryPolicy = JitConfig.JitInlinePolicyDiscretionary() != 0;

    if (useDiscretionaryPolicy)
    {
        return new (compiler, CMK_Inlining) DiscretionaryPolicy(compiler, isPrejitRoot);
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Optionally install the ModelPolicy.
    bool useModelPolicy = JitConfig.JitInlinePolicyModel() != 0;

    if (useModelPolicy)
    {
        return new (compiler, CMK_Inlining) ModelPolicy(compiler, isPrejitRoot);
    }

    // Optionally install the ProfilePolicy, if the method has profile data.
    //
    bool enableProfilePolicy = JitConfig.JitInlinePolicyProfile() != 0;
    bool hasProfileData      = compiler->fgIsUsingProfileWeights();

    if (enableProfilePolicy && hasProfileData)
    {
        return new (compiler, CMK_Inlining) ProfilePolicy(compiler, isPrejitRoot);
    }

    const bool isPrejit   = compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT);
    const bool isSpeedOpt = compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SPEED_OPT);

    if ((JitConfig.JitExtDefaultPolicy() != 0))
    {
        if (isPrejitRoot || !isPrejit || (isPrejit && isSpeedOpt))
        {
            return new (compiler, CMK_Inlining) ExtendedDefaultPolicy(compiler, isPrejitRoot);
        }
    }

    return new (compiler, CMK_Inlining) DefaultPolicy(compiler, isPrejitRoot);
}

//------------------------------------------------------------------------
// NoteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegalPolicy::NoteFatal(InlineObservation obs)
{
    // As a safeguard, all fatal impact must be
    // reported via NoteFatal.
    assert(InlGetImpact(obs) == InlineImpact::FATAL);
    NoteInternal(obs);
    assert(InlDecisionIsFailure(m_Decision));
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// NotePriorFailure: record reason for earlier inline failure
//
// Arguments:
//    obs      - the current obsevation
//
// Notes:
//    Used to "resurrect" failure observations from the early inline
//    screen when building the inline context tree. Only used during
//    debug modes.

void LegalPolicy::NotePriorFailure(InlineObservation obs)
{
    NoteInternal(obs);
    assert(InlDecisionIsFailure(m_Decision));
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// NoteInternal: helper for handling an observation
//
// Arguments:
//    obs      - the current obsevation

void LegalPolicy::NoteInternal(InlineObservation obs)
{
    // Note any INFORMATION that reaches here will now cause failure.
    // Non-fatal INFORMATION observations must be handled higher up.
    InlineTarget target = InlGetTarget(obs);

    if (target == InlineTarget::CALLEE)
    {
        this->SetNever(obs);
    }
    else
    {
        this->SetFailure(obs);
    }
}

//------------------------------------------------------------------------
// SetFailure: helper for setting a failing decision
//
// Arguments:
//    obs      - the current obsevation

void LegalPolicy::SetFailure(InlineObservation obs)
{
    // Expect a valid observation
    assert(InlIsValidObservation(obs));

    switch (m_Decision)
    {
        case InlineDecision::FAILURE:
            // Repeated failure only ok if evaluating a prejit root
            // (since we can't fail fast because we're not inlining)
            // or if inlining and the observation is CALLSITE_TOO_MANY_LOCALS
            // (since we can't fail fast from lvaGrabTemp).
            assert(m_IsPrejitRoot || (obs == InlineObservation::CALLSITE_TOO_MANY_LOCALS));
            break;
        case InlineDecision::UNDECIDED:
        case InlineDecision::CANDIDATE:
            m_Decision    = InlineDecision::FAILURE;
            m_Observation = obs;
            break;
        default:
            // SUCCESS, NEVER, or ??
            assert(!"Unexpected m_Decision");
            unreached();
    }
}

//------------------------------------------------------------------------
// SetNever: helper for setting a never decision
//
// Arguments:
//    obs      - the current obsevation

void LegalPolicy::SetNever(InlineObservation obs)
{
    // Expect a valid observation
    assert(InlIsValidObservation(obs));

    switch (m_Decision)
    {
        case InlineDecision::NEVER:
            // Repeated never only ok if evaluating a prejit root
            assert(m_IsPrejitRoot);
            break;
        case InlineDecision::UNDECIDED:
        case InlineDecision::CANDIDATE:
            m_Decision    = InlineDecision::NEVER;
            m_Observation = obs;
            break;
        default:
            // SUCCESS, FAILURE or ??
            assert(!"Unexpected m_Decision");
            unreached();
    }
}

//------------------------------------------------------------------------
// SetCandidate: helper updating candidacy
//
// Arguments:
//    obs      - the current obsevation
//
// Note:
//    Candidate observations are handled here. If the inline has already
//    failed, they're ignored. If there's already a candidate reason,
//    this new reason trumps it.

void LegalPolicy::SetCandidate(InlineObservation obs)
{
    // Ignore if this inline is going to fail.
    if (InlDecisionIsFailure(m_Decision))
    {
        return;
    }

    // We should not have declared success yet.
    assert(!InlDecisionIsSuccess(m_Decision));

    // Update, overriding any previous candidacy.
    m_Decision    = InlineDecision::CANDIDATE;
    m_Observation = obs;
}

//------------------------------------------------------------------------
// NoteSuccess: handle finishing all the inlining checks successfully

void DefaultPolicy::NoteSuccess()
{
    assert(InlDecisionIsCandidate(m_Decision));
    m_Decision = InlineDecision::SUCCESS;
}

//------------------------------------------------------------------------
// NoteBool: handle a boolean observation with non-fatal impact
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value of the observation
void DefaultPolicy::NoteBool(InlineObservation obs, bool value)
{
    // Check the impact
    InlineImpact impact = InlGetImpact(obs);

    // As a safeguard, all fatal impact must be
    // reported via NoteFatal.
    assert(impact != InlineImpact::FATAL);

    // Handle most information here
    bool isInformation = (impact == InlineImpact::INFORMATION);
    bool propagate     = !isInformation;

    if (isInformation)
    {
        switch (obs)
        {
            case InlineObservation::CALLEE_IS_FORCE_INLINE:
                // We may make the force-inline observation more than
                // once.  All observations should agree.
                assert(!m_IsForceInlineKnown || (m_IsForceInline == value));
                m_IsForceInline      = value;
                m_IsForceInlineKnown = true;
                break;

            case InlineObservation::CALLEE_IS_INSTANCE_CTOR:
                m_IsInstanceCtor = value;
                break;

            case InlineObservation::CALLEE_CLASS_PROMOTABLE:
                m_IsFromPromotableValueClass = value;
                break;

            case InlineObservation::CALLSITE_IN_TRY_REGION:
                m_CallsiteIsInTryRegion = value;
                break;

            case InlineObservation::CALLEE_HAS_SIMD:
                m_HasSimd = value;
                break;

            case InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER:
                m_LooksLikeWrapperMethod = value;
                break;

            case InlineObservation::CALLEE_ARG_FEEDS_TEST:
                m_ArgFeedsTest++;
                break;

            case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
                m_ArgFeedsConstantTest++;
                break;

            case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
                m_ArgFeedsRangeCheck++;
                break;

            case InlineObservation::CALLEE_CONST_ARG_FEEDS_ISCONST:
                m_ConstArgFeedsIsKnownConst = true;
                break;

            case InlineObservation::CALLEE_ARG_FEEDS_ISCONST:
                m_ArgFeedsIsKnownConst = true;
                break;

            case InlineObservation::CALLEE_UNSUPPORTED_OPCODE:
                propagate = true;
                break;

            case InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST:
                // We shouldn't see this for a prejit root since
                // we don't know anything about callers.
                assert(!m_IsPrejitRoot);
                m_ConstantArgFeedsConstantTest++;
                break;

            case InlineObservation::CALLEE_BEGIN_OPCODE_SCAN:
            {
                // Set up the state machine, if this inline is
                // discretionary and is still a candidate.
                if (InlDecisionIsCandidate(m_Decision) &&
                    (m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE))
                {
                    // Better not have a state machine already.
                    assert(m_StateMachine == nullptr);
                    m_StateMachine = new (m_RootCompiler, CMK_Inlining) CodeSeqSM;
                    m_StateMachine->Start(m_RootCompiler);
                }
                break;
            }

            case InlineObservation::CALLEE_END_OPCODE_SCAN:
            {
                if (m_StateMachine != nullptr)
                {
                    m_StateMachine->End();
                }

                // If this function is mostly loads and stores, we
                // should try harder to inline it.  You can't just use
                // the percentage test because if the method has 8
                // instructions and 6 are loads, it's only 75% loads.
                // This allows for CALL, RET, and one more non-ld/st
                // instruction.
                if (((m_InstructionCount - m_LoadStoreCount) < 4) ||
                    (((double)m_LoadStoreCount / (double)m_InstructionCount) > .90))
                {
                    m_MethodIsMostlyLoadStore = true;
                }

                // Budget check.
                //
                // Conceptually this should happen when we
                // observe the candidate's IL size.
                //
                // However, we do this here to avoid potential
                // inconsistency between the state of the budget
                // during candidate scan and the state when the IL is
                // being scanned.
                //
                // Consider the case where we're just below the budget
                // during candidate scan, and we have three possible
                // inlines, any two of which put us over budget. We
                // allow them all to become candidates. We then move
                // on to inlining and the first two get inlined and
                // put us over budget. Now the third can't be inlined
                // anymore, but we have a policy that when we replay
                // the candidate IL size during the inlining pass it
                // "reestablishes" candidacy rather than alters
                // candidacy ... so instead we bail out here.
                //
                bool overBudget = this->BudgetCheck();

                if (overBudget)
                {
                    SetFailure(InlineObservation::CALLSITE_OVER_BUDGET);
                    return;
                }
                break;
            }

            case InlineObservation::CALLSITE_IN_LOOP:
                m_CallsiteIsInLoop = true;
                break;

            case InlineObservation::CALLEE_DOES_NOT_RETURN:
                m_IsNoReturn      = value;
                m_IsNoReturnKnown = true;
                break;

            case InlineObservation::CALLSITE_RARE_GC_STRUCT:
                // If this is a discretionary or always inline candidate
                // with a gc struct, we may change our mind about inlining
                // if the call site is rare, to avoid costs associated with
                // zeroing the GC struct up in the root prolog.
                if (m_Observation == InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE)
                {
                    assert(m_CallsiteFrequency == InlineCallsiteFrequency::UNUSED);
                    SetFailure(obs);
                    return;
                }
                else if (m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE)
                {
                    assert(m_CallsiteFrequency == InlineCallsiteFrequency::RARE);
                    SetFailure(obs);
                    return;
                }
                break;

            case InlineObservation::CALLEE_HAS_PINNED_LOCALS:
                if (m_CallsiteIsInTryRegion)
                {
                    // Inlining a method with pinned locals in a try
                    // region requires wrapping the inline body in a
                    // try/finally to ensure unpinning. Bail instead.
                    SetFailure(InlineObservation::CALLSITE_PIN_IN_TRY_REGION);
                    return;
                }
                break;

            case InlineObservation::CALLEE_HAS_LOCALLOC:
                // We see this during the IL prescan. Ignore for now, we will
                // bail out, if necessary, during importation
                break;

            default:
                // Ignore the remainder for now
                break;
        }
    }

    if (propagate)
    {
        NoteInternal(obs);
    }
}

//------------------------------------------------------------------------
// BudgetCheck: see if this inline would exceed the current budget
//
// Returns:
//   True if inline would exceed the budget.
//
bool DefaultPolicy::BudgetCheck() const
{
    // Only relevant if we're actually inlining.
    //
    if (m_IsPrejitRoot)
    {
        return false;
    }

    // The strategy tracks the amout of inlining done so far,
    // so it performs the actual check.
    //
    InlineStrategy* strategy   = m_RootCompiler->m_inlineStrategy;
    const bool      overBudget = strategy->BudgetCheck(m_CodeSize);

    if (overBudget)
    {
        // If the candidate is a forceinline and the callsite is
        // not too deep, allow the inline even if it goes over budget.
        //
        // For now, "not too deep" means a top-level inline. Note
        // depth 0 is used for the root method, so inline candidate depth
        // will be 1 or more.
        //
        assert(m_IsForceInlineKnown);
        assert(m_CallsiteDepth > 0);
        const bool allowOverBudget = m_IsForceInline && (m_CallsiteDepth == 1);

        if (allowOverBudget)
        {
            JITDUMP("Allowing over-budget top-level forceinline\n");
        }
        else
        {
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DefaultPolicy::NoteInt(InlineObservation obs, int value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_MAXSTACK:
        {
            assert(m_IsForceInlineKnown);

            unsigned calleeMaxStack = static_cast<unsigned>(value);

            if (!m_IsForceInline && (calleeMaxStack > SMALL_STACK_SIZE))
            {
                SetNever(InlineObservation::CALLEE_MAXSTACK_TOO_BIG);
            }

            break;
        }

        case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
        {
            assert(m_IsForceInlineKnown);
            assert(value != 0);
            assert(m_IsNoReturnKnown);

            //
            // Let's be conservative for now and reject inlining of "no return" methods only
            // if the callee contains a single basic block. This covers most of the use cases
            // (typical throw helpers simply do "throw new X();" and so they have a single block)
            // without affecting more exotic cases (loops that do actual work for example) where
            // failure to inline could negatively impact code quality.
            //

            unsigned basicBlockCount = static_cast<unsigned>(value);

            // CALLEE_IS_FORCE_INLINE overrides CALLEE_DOES_NOT_RETURN
            if (!m_IsForceInline && m_IsNoReturn && (basicBlockCount == 1))
            {
                SetNever(InlineObservation::CALLEE_DOES_NOT_RETURN);
            }
            else if (!m_IsForceInline && (basicBlockCount > MAX_BASIC_BLOCKS))
            {
                SetNever(InlineObservation::CALLEE_TOO_MANY_BASIC_BLOCKS);
            }

            break;
        }

        case InlineObservation::CALLEE_IL_CODE_SIZE:
        {
            assert(m_IsForceInlineKnown);
            assert(value != 0);
            m_CodeSize = static_cast<unsigned>(value);

            // Now that we know size and forceinline state,
            // update candidacy.
            if (m_IsForceInline)
            {
                // Candidate based on force inline
                SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
            }
            else if (m_CodeSize <= InlineStrategy::ALWAYS_INLINE_SIZE)
            {
                // Candidate based on small size
                SetCandidate(InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE);
            }
            else if (m_CodeSize <= m_RootCompiler->m_inlineStrategy->GetMaxInlineILSize())
            {
                // Candidate, pending profitability evaluation
                SetCandidate(InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);
            }
            else
            {
                // Callee too big, not a candidate
                SetNever(InlineObservation::CALLEE_TOO_MUCH_IL);
            }

            break;
        }

        case InlineObservation::CALLSITE_DEPTH:
        {
            m_CallsiteDepth = static_cast<unsigned>(value);

            if (m_CallsiteDepth > m_RootCompiler->m_inlineStrategy->GetMaxInlineDepth())
            {
                SetFailure(InlineObservation::CALLSITE_IS_TOO_DEEP);
            }

            break;
        }

        case InlineObservation::CALLEE_OPCODE_NORMED:
        case InlineObservation::CALLEE_OPCODE:
        {
            m_InstructionCount++;
            OPCODE opcode = static_cast<OPCODE>(value);

            if (m_StateMachine != nullptr)
            {
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

                m_StateMachine->Run(smOpcode DEBUGARG(0));
            }

            // Look for opcodes that imply loads and stores.
            // Logic here is as it is to match legacy behavior.
            if ((opcode >= CEE_LDARG_0 && opcode <= CEE_STLOC_S) || (opcode >= CEE_LDARG && opcode <= CEE_STLOC) ||
                (opcode >= CEE_LDNULL && opcode <= CEE_LDC_R8) || (opcode >= CEE_LDIND_I1 && opcode <= CEE_STIND_R8) ||
                (opcode >= CEE_LDFLD && opcode <= CEE_STOBJ) || (opcode >= CEE_LDELEMA && opcode <= CEE_STELEM) ||
                (opcode == CEE_POP))
            {
                m_LoadStoreCount++;
            }

            break;
        }

        case InlineObservation::CALLSITE_FREQUENCY:
            assert(m_CallsiteFrequency == InlineCallsiteFrequency::UNUSED);
            m_CallsiteFrequency = static_cast<InlineCallsiteFrequency>(value);
            assert(m_CallsiteFrequency != InlineCallsiteFrequency::UNUSED);
            break;

        default:
            // Ignore all other information
            break;
    }
}

//------------------------------------------------------------------------
// NoteDouble: handle an observed double value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DefaultPolicy::NoteDouble(InlineObservation obs, double value)
{
    assert(obs == InlineObservation::CALLSITE_PROFILE_FREQUENCY);
}

//------------------------------------------------------------------------
// DetermineMultiplier: determine benefit multiplier for this inline
//
// Notes: uses the accumulated set of observations to compute a
// profitability boost for the inline candidate.

double DefaultPolicy::DetermineMultiplier()
{
    double multiplier = 0;

    // Bump up the multiplier for instance constructors

    if (m_IsInstanceCtor)
    {
        multiplier += 1.5;
        JITDUMP("\nmultiplier in instance constructors increased to %g.", multiplier);
    }

    // Bump up the multiplier for methods in promotable struct

    if (m_IsFromPromotableValueClass)
    {
        multiplier += 3;
        JITDUMP("\nmultiplier in methods of promotable struct increased to %g.", multiplier);
    }

#ifdef FEATURE_SIMD

    if (m_HasSimd)
    {
        multiplier += JitConfig.JitInlineSIMDMultiplier();
        JITDUMP("\nInline candidate has SIMD type args, locals or return value.  Multiplier increased to %g.",
                multiplier);
    }

#endif // FEATURE_SIMD

    if (m_LooksLikeWrapperMethod)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate looks like a wrapper method.  Multiplier increased to %g.", multiplier);
    }

    if (m_ArgFeedsConstantTest > 0)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate has an arg that feeds a constant test.  Multiplier increased to %g.", multiplier);
    }

    if (m_MethodIsMostlyLoadStore)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate is mostly loads and stores.  Multiplier increased to %g.", multiplier);
    }

    if (m_ArgFeedsRangeCheck > 0)
    {
        multiplier += 0.5;
        JITDUMP("\nInline candidate has arg that feeds range check.  Multiplier increased to %g.", multiplier);
    }

    if (m_ConstantArgFeedsConstantTest > 0)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate has const arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
    }
    // For prejit roots we do not see the call sites. To be suitably optimistic
    // assume that call sites may pass constants.
    else if (m_IsPrejitRoot && ((m_ArgFeedsConstantTest > 0) || (m_ArgFeedsTest > 0)))
    {
        multiplier += 3.0;
        JITDUMP("\nPrejit root candidate has arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
    }

    switch (m_CallsiteFrequency)
    {
        case InlineCallsiteFrequency::RARE:
            // Note this one is not additive, it uses '=' instead of '+='
            multiplier = 1.3;
            JITDUMP("\nInline candidate callsite is rare.  Multiplier limited to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::BORING:
            multiplier += 1.3;
            JITDUMP("\nInline candidate callsite is boring.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::WARM:
            multiplier += 2.0;
            JITDUMP("\nInline candidate callsite is warm.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::LOOP:
            multiplier += 3.0;
            JITDUMP("\nInline candidate callsite is in a loop.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::HOT:
            multiplier += 3.0;
            JITDUMP("\nInline candidate callsite is hot.  Multiplier increased to %g.", multiplier);
            break;
        default:
            assert(!"Unexpected callsite frequency");
            break;
    }

#ifdef DEBUG

    int additionalMultiplier = JitConfig.JitInlineAdditionalMultiplier();

    if (additionalMultiplier != 0)
    {
        multiplier += additionalMultiplier;
        JITDUMP("\nmultiplier increased via JitInlineAdditionalMultiplier=%d to %g.", additionalMultiplier, multiplier);
    }

    if (m_RootCompiler->compInlineStress())
    {
        multiplier += 10;
        JITDUMP("\nmultiplier increased via inline stress to %g.", multiplier);
    }

#endif // DEBUG

    return multiplier;
}

//------------------------------------------------------------------------
// DetermineNativeSizeEstimate: return estimated native code size for
// this inline candidate.
//
// Notes:
//    This is an estimate for the size of the inlined callee.
//    It does not include size impact on the caller side.
//
//    Uses the results of a state machine model for discretionary
//    candidates.  Should not be needed for forced or always
//    candidates.

int DefaultPolicy::DetermineNativeSizeEstimate()
{
    // Should be a discretionary candidate.
    assert(m_StateMachine != nullptr);

    return m_StateMachine->NativeSize;
}

//------------------------------------------------------------------------
// DetermineCallsiteNativeSizeEstimate: estimate native size for the
// callsite.
//
// Arguments:
//    methInfo -- method info for the callee
//
// Notes:
//    Estimates the native size (in bytes, scaled up by 10x) for the
//    call site. While the quality of the estimate here is questionable
//    (especially for x64) it is being left as is for legacy compatibility.

int DefaultPolicy::DetermineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methInfo)
{
    int callsiteSize = 55; // Direct call take 5 native bytes; indirect call takes 6 native bytes.

    bool hasThis = methInfo->args.hasThis();

    if (hasThis)
    {
        callsiteSize += 30; // "mov" or "lea"
    }

    CORINFO_ARG_LIST_HANDLE argLst = methInfo->args.args;
    COMP_HANDLE             comp   = m_RootCompiler->info.compCompHnd;

    for (unsigned i = (hasThis ? 1 : 0); i < methInfo->args.totalILArgs(); i++, argLst = comp->getArgNext(argLst))
    {
        var_types sigType = (var_types)m_RootCompiler->eeGetArgType(argLst, &methInfo->args);

        if (sigType == TYP_STRUCT)
        {
            typeInfo verType = m_RootCompiler->verParseArgSigToTypeInfo(&methInfo->args, argLst);

            /*

            IN0028: 00009B      lea     EAX, bword ptr [EBP-14H]
            IN0029: 00009E      push    dword ptr [EAX+4]
            IN002a: 0000A1      push    gword ptr [EAX]
            IN002b: 0000A3      call    [MyStruct.staticGetX2(struct):int]

            */

            callsiteSize += 10; // "lea     EAX, bword ptr [EBP-14H]"

            unsigned opsz  = roundUp(comp->getClassSize(verType.GetClassHandle()), TARGET_POINTER_SIZE);
            unsigned slots = opsz / TARGET_POINTER_SIZE;

            callsiteSize += slots * 20; // "push    gword ptr [EAX+offs]  "
        }
        else
        {
            callsiteSize += 30; // push by average takes 3 bytes.
        }
    }

    return callsiteSize;
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee
//
// Notes:
//    A profitable inline is one that is projected to have a beneficial
//    size/speed tradeoff.
//
//    It is expected that this method is only invoked for discretionary
//    candidates, since it does not make sense to do this assessment for
//    failed, always, or forced inlines.

void DefaultPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{

#if defined(DEBUG)

    // Punt if we're inlining and we've reached the acceptance limit.
    int      limit   = JitConfig.JitInlineLimit();
    unsigned current = m_RootCompiler->m_inlineStrategy->GetInlineCount();

    if (!m_IsPrejitRoot && (limit >= 0) && (current >= static_cast<unsigned>(limit)))
    {
        SetFailure(InlineObservation::CALLSITE_OVER_INLINE_LIMIT);
        return;
    }

#endif // defined(DEBUG)

    assert(InlDecisionIsCandidate(m_Decision));
    assert(m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);

    m_CalleeNativeSizeEstimate   = DetermineNativeSizeEstimate();
    m_CallsiteNativeSizeEstimate = DetermineCallsiteNativeSizeEstimate(methodInfo);
    m_Multiplier                 = DetermineMultiplier();
    const int threshold          = (int)(m_CallsiteNativeSizeEstimate * m_Multiplier);

    // Note the DefaultPolicy estimates are scaled up by SIZE_SCALE
    JITDUMP("\ncalleeNativeSizeEstimate=%d\n", m_CalleeNativeSizeEstimate)
    JITDUMP("callsiteNativeSizeEstimate=%d\n", m_CallsiteNativeSizeEstimate);
    JITDUMP("benefit multiplier=%g\n", m_Multiplier);
    JITDUMP("threshold=%d\n", threshold);

    // Reject if callee size is over the threshold
    if (m_CalleeNativeSizeEstimate > threshold)
    {
        // Inline appears to be unprofitable
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000, "Native estimate for function size exceeds threshold"
                                    " for inlining %g > %g (multiplier = %g)\n",
                     (double)m_CalleeNativeSizeEstimate / SIZE_SCALE, (double)threshold / SIZE_SCALE, m_Multiplier));

        // Fail the inline
        if (m_IsPrejitRoot)
        {
            SetNever(InlineObservation::CALLEE_NOT_PROFITABLE_INLINE);
        }
        else
        {
            SetFailure(InlineObservation::CALLSITE_NOT_PROFITABLE_INLINE);
        }
    }
    else
    {
        // Inline appears to be profitable
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000, "Native estimate for function size is within threshold"
                                    " for inlining %g <= %g (multiplier = %g)\n",
                     (double)m_CalleeNativeSizeEstimate / SIZE_SCALE, (double)threshold / SIZE_SCALE, m_Multiplier));

        // Update candidacy
        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_IS_PROFITABLE_INLINE);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_IS_PROFITABLE_INLINE);
        }
    }
}

//------------------------------------------------------------------------
// CodeSizeEstimate: estimated code size impact of the inline
//
// Return Value:
//    Estimated code size impact, in bytes * 10
//
// Notes:
//    Only meaningful for discretionary inlines (whether successful or
//    not).  For always or force inlines the legacy policy doesn't
//    estimate size impact.

int DefaultPolicy::CodeSizeEstimate()
{
    if (m_StateMachine != nullptr)
    {
        // This is not something the DefaultPolicy explicitly computed,
        // since it uses a blended evaluation model (mixing size and time
        // together for overall profitability). But it's effectively an
        // estimate of the size impact.
        return (m_CalleeNativeSizeEstimate - m_CallsiteNativeSizeEstimate);
    }
    else
    {
        return 0;
    }
}

#if defined(DEBUG) || defined(INLINE_DATA)
//------------------------------------------------------------------------
// OnDumpXml: Dump DefaultPolicy data as XML
//
// Arguments:
//    file     - stream to output to
//    indent   - indent level

void DefaultPolicy::OnDumpXml(FILE* file, unsigned indent) const
{
    XATTR_R8(m_Multiplier);
    XATTR_I4(m_CodeSize);
    XATTR_I4(m_CallsiteFrequency);
    XATTR_I4(m_CallsiteDepth);
    XATTR_I4(m_InstructionCount);
    XATTR_I4(m_LoadStoreCount);
    XATTR_I4(m_ArgFeedsTest);
    XATTR_I4(m_ArgFeedsConstantTest);
    XATTR_I4(m_ArgFeedsRangeCheck);
    XATTR_I4(m_ConstantArgFeedsConstantTest);
    XATTR_I4(m_CalleeNativeSizeEstimate);
    XATTR_I4(m_CallsiteNativeSizeEstimate);
    XATTR_B(m_IsForceInline);
    XATTR_B(m_IsForceInlineKnown);
    XATTR_B(m_IsInstanceCtor);
    XATTR_B(m_IsFromPromotableValueClass);
    XATTR_B(m_HasSimd);
    XATTR_B(m_LooksLikeWrapperMethod);
    XATTR_B(m_MethodIsMostlyLoadStore);
    XATTR_B(m_CallsiteIsInTryRegion);
    XATTR_B(m_CallsiteIsInLoop);
    XATTR_B(m_IsNoReturn);
    XATTR_B(m_IsNoReturnKnown);
}
#endif

//------------------------------------------------------------------------
// PropagateNeverToRuntime: determine if a never result should cause the
// method to be marked as un-inlinable.

bool DefaultPolicy::PropagateNeverToRuntime() const
{
    //
    // Do not propagate the "no return" observation. If we do this then future inlining
    // attempts will fail immediately without marking the call node as "no return".
    // This can have an adverse impact on caller's code quality as it may have to preserve
    // registers across the call.
    // TODO-Throughput: We should persist the "no return" information in the runtime
    // so we don't need to re-analyze the inlinee all the time.
    //

    bool propagate = (m_Observation != InlineObservation::CALLEE_DOES_NOT_RETURN);

    return propagate;
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// RandomPolicy: construct a new RandomPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

RandomPolicy::RandomPolicy(Compiler* compiler, bool isPrejitRoot) : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    m_Random = compiler->m_inlineStrategy->GetRandom();
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void RandomPolicy::NoteInt(InlineObservation obs, int value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_IL_CODE_SIZE:
        {
            assert(m_IsForceInlineKnown);
            assert(value != 0);
            m_CodeSize = static_cast<unsigned>(value);

            if (m_IsForceInline)
            {
                // Candidate based on force inline
                SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
            }
            else
            {
                // Candidate, pending profitability evaluation
                SetCandidate(InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);
            }

            break;
        }

        default:
            // Defer to superclass for all other information
            DiscretionaryPolicy::NoteInt(obs, value);
            break;
    }
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee
//
// Notes:
//    The random policy makes random decisions about profitablity.
//    Generally we aspire to inline differently, not necessarily to
//    inline more.

void RandomPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    assert(InlDecisionIsCandidate(m_Decision));
    assert(m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);

    // Budget check.
    const bool overBudget = this->BudgetCheck();
    if (overBudget)
    {
        SetFailure(InlineObservation::CALLSITE_OVER_BUDGET);
        return;
    }

    // If we're also dumping inline data, make additional observations
    // based on the method info, and estimate code size and perf
    // impact, so that the reports have the necessary data.
    if (JitConfig.JitInlineDumpData() != 0)
    {
        MethodInfoObservations(methodInfo);
        EstimateCodeSize();
        EstimatePerformanceImpact();
    }

    // Use a probability curve that roughly matches the observed
    // behavior of the DefaultPolicy. That way we're inlining
    // differently but not creating enormous methods.
    //
    // We vary a bit at the extremes. The RandomPolicy won't always
    // inline the small methods (<= 16 IL bytes) and won't always
    // reject the large methods (> 100 IL bytes).

    unsigned threshold = 0;

    if (m_CodeSize <= 16)
    {
        threshold = 75;
    }
    else if (m_CodeSize <= 30)
    {
        threshold = 50;
    }
    else if (m_CodeSize <= 40)
    {
        threshold = 40;
    }
    else if (m_CodeSize <= 50)
    {
        threshold = 30;
    }
    else if (m_CodeSize <= 75)
    {
        threshold = 20;
    }
    else if (m_CodeSize <= 100)
    {
        threshold = 10;
    }
    else if (m_CodeSize <= 200)
    {
        threshold = 5;
    }
    else
    {
        threshold = 1;
    }

    unsigned randomValue = m_Random->Next(1, 100);

    // Reject if callee size is over the threshold
    if (randomValue > threshold)
    {
        // Inline appears to be unprofitable
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Random rejection (r=%d > t=%d)\n", randomValue, threshold));

        // Fail the inline
        if (m_IsPrejitRoot)
        {
            SetNever(InlineObservation::CALLEE_RANDOM_REJECT);
        }
        else
        {
            SetFailure(InlineObservation::CALLSITE_RANDOM_REJECT);
        }
    }
    else
    {
        // Inline appears to be profitable
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Random acceptance (r=%d <= t=%d)\n", randomValue, threshold));

        // Update candidacy
        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_RANDOM_ACCEPT);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_RANDOM_ACCEPT);
        }
    }
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

#ifdef _MSC_VER
// Disable warning about new array member initialization behavior
#pragma warning(disable : 4351)
#endif

//------------------------------------------------------------------------
// NoteInt: handle an observed boolean value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void ExtendedDefaultPolicy::NoteBool(InlineObservation obs, bool value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_RETURNS_STRUCT:
            m_ReturnsStructByValue = value;
            break;

        case InlineObservation::CALLEE_CLASS_VALUETYPE:
            m_IsFromValueClass = value;
            break;

        case InlineObservation::CALLSITE_NONGENERIC_CALLS_GENERIC:
            m_NonGenericCallsGeneric = value;
            break;

        case InlineObservation::CALLEE_BINARY_EXRP_WITH_CNS:
            m_BinaryExprWithCns++;
            break;

        case InlineObservation::CALLEE_ARG_STRUCT:
            m_ArgIsStructByValue++;
            break;

        case InlineObservation::CALLEE_ARG_STRUCT_FIELD_ACCESS:
            m_FldAccessOverArgStruct++;
            break;

        case InlineObservation::CALLEE_ARG_FEEDS_CAST:
            m_ArgCasted++;
            break;

        case InlineObservation::CALLEE_FOLDABLE_BOX:
            m_FoldableBox++;
            break;

        case InlineObservation::CALLEE_INTRINSIC:
            m_Intrinsic++;
            break;

        case InlineObservation::CALLEE_BACKWARD_JUMP:
            m_BackwardJump++;
            break;

        case InlineObservation::CALLEE_THROW_BLOCK:
            m_ThrowBlock++;
            break;

        case InlineObservation::CALLSITE_ARG_EXACT_CLS:
            m_ArgIsExactCls++;
            break;

        case InlineObservation::CALLSITE_ARG_BOXED:
            m_ArgIsBoxedAtCallsite++;
            break;

        case InlineObservation::CALLSITE_ARG_CONST:
            m_ArgIsConst++;
            break;

        case InlineObservation::CALLSITE_ARG_EXACT_CLS_SIG_IS_NOT:
            m_ArgIsExactClsSigIsNot++;
            break;

        case InlineObservation::CALLSITE_FOLDABLE_INTRINSIC:
            m_FoldableIntrinsic++;
            break;

        case InlineObservation::CALLSITE_FOLDABLE_EXPR:
            m_FoldableExpr++;
            break;

        case InlineObservation::CALLSITE_FOLDABLE_EXPR_UN:
            m_FoldableExprUn++;
            break;

        case InlineObservation::CALLSITE_FOLDABLE_BRANCH:
            m_FoldableBranch++;
            break;

        case InlineObservation::CALLSITE_FOLDABLE_SWITCH:
            m_FoldableSwitch++;
            break;

        case InlineObservation::CALLEE_HAS_SWITCH:
            m_Switch++;
            break;

        case InlineObservation::CALLSITE_DIV_BY_CNS:
            m_DivByCns++;
            break;

        case InlineObservation::CALLSITE_HAS_PROFILE:
            m_HasProfile = value;
            break;

        case InlineObservation::CALLSITE_IN_NORETURN_REGION:
            m_IsCallsiteInNoReturnRegion = value;
            break;

        default:
            DefaultPolicy::NoteBool(obs, value);
            break;
    }
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void ExtendedDefaultPolicy::NoteInt(InlineObservation obs, int value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_IL_CODE_SIZE:
        {
            assert(m_IsForceInlineKnown);
            assert(value != 0);
            m_CodeSize           = static_cast<unsigned>(value);
            unsigned maxCodeSize = static_cast<unsigned>(JitConfig.JitExtDefaultPolicyMaxIL());

            // TODO: Enable for PgoSource::Static as well if it's not the generic profile we bundle.
            if (m_HasProfile && (m_RootCompiler->fgHaveTrustedProfileData()))
            {
                maxCodeSize = static_cast<unsigned>(JitConfig.JitExtDefaultPolicyMaxILProf());
            }

            if (m_IsForceInline)
            {
                // Candidate based on force inline
                SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
            }
            else if (m_CodeSize <= InlineStrategy::ALWAYS_INLINE_SIZE)
            {
                // Candidate based on small size
                SetCandidate(InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE);
            }
            else if (m_CodeSize <= maxCodeSize)
            {
                // Candidate, pending profitability evaluation
                SetCandidate(InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);
            }
            else
            {
                // Callee too big, not a candidate
                SetNever(InlineObservation::CALLEE_TOO_MUCH_IL);
            }
            break;
        }
        case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
        {
            if (!m_IsForceInline && m_IsNoReturn && (value == 1))
            {
                SetNever(InlineObservation::CALLEE_DOES_NOT_RETURN);
            }
            else if (!m_IsForceInline && !m_HasProfile && !m_ConstArgFeedsIsKnownConst && !m_ArgFeedsIsKnownConst)
            {
                unsigned bbLimit = (unsigned)JitConfig.JitExtDefaultPolicyMaxBB();
                if (m_IsPrejitRoot)
                {
                    // We're not able to recognize arg-specific foldable branches
                    // in prejit-root mode.
                    bbLimit += 5 + m_Switch * 10;
                }
                bbLimit += m_FoldableBranch + m_FoldableSwitch * 10;

                if ((unsigned)value > bbLimit)
                {
                    SetNever(InlineObservation::CALLEE_TOO_MANY_BASIC_BLOCKS);
                }
            }
            break;
        }
        default:
            DefaultPolicy::NoteInt(obs, value);
            break;
    }
}

//------------------------------------------------------------------------
// NoteInt: handle an observed double value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void ExtendedDefaultPolicy::NoteDouble(InlineObservation obs, double value)
{
    // So far, CALLSITE_PROFILE_FREQUENCY is the only "double" property.
    assert(obs == InlineObservation::CALLSITE_PROFILE_FREQUENCY);
    m_ProfileFrequency = value;
}

//------------------------------------------------------------------------
// DetermineMultiplier: determine benefit multiplier for this inline
//
// Notes: uses the accumulated set of observations to compute a
//    profitability boost for the inline candidate.

double ExtendedDefaultPolicy::DetermineMultiplier()
{
    double multiplier = 0.0;

    if (m_IsInstanceCtor)
    {
        multiplier += 1.5;
        JITDUMP("\nmultiplier in instance constructors increased to %g.", multiplier);
    }

    if (m_IsFromValueClass)
    {
        multiplier += 3.0;
        JITDUMP("\nmultiplier in methods of struct increased to %g.", multiplier);
    }

    if (m_ReturnsStructByValue)
    {
        // For structs-passed-by-value we might avoid expensive copy operations if we inline.
        multiplier += 2.0;
        JITDUMP("\nInline candidate returns a struct by value.  Multiplier increased to %g.", multiplier);
    }
    else if (m_ArgIsStructByValue > 0)
    {
        // Same here
        multiplier += 2.0;
        JITDUMP("\n%d arguments are structs passed by value.  Multiplier increased to %g.", m_ArgIsStructByValue,
                multiplier);
    }
    else if (m_FldAccessOverArgStruct > 0)
    {
        multiplier += 1.0;
        // Such ldfld/stfld are cheap for promotable structs
        JITDUMP("\n%d ldfld or stfld over arguments which are structs.  Multiplier increased to %g.",
                m_FldAccessOverArgStruct, multiplier);
    }

    if (m_LooksLikeWrapperMethod)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate looks like a wrapper method.  Multiplier increased to %g.", multiplier);
    }

    if (m_MethodIsMostlyLoadStore)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate is mostly loads and stores.  Multiplier increased to %g.", multiplier);
    }

    if (m_ArgFeedsRangeCheck > 0)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate has arg that feeds range check.  Multiplier increased to %g.", multiplier);
    }

    if (m_NonGenericCallsGeneric)
    {
        multiplier += 2.0;
        JITDUMP("\nInline candidate is generic and caller is not.  Multiplier increased to %g.", multiplier);
    }

    if (m_FoldableBranch > 0)
    {
        // Examples:
        //
        //  if (typeof(T) == typeof(int)) {
        //  if (Avx2.IsSupported) {
        //  if (arg0 / 10 > 100) { // where arg0 is a constant at the callsite
        //  if (Math.Abs(arg0) > 10) { // same here
        //  etc.
        //
        multiplier += 3.0 + m_FoldableBranch;
        JITDUMP("\nInline candidate has %d foldable branches.  Multiplier increased to %g.", m_FoldableBranch,
                multiplier);
    }
    else if (m_ConstantArgFeedsConstantTest > 0)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate has const arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
    }
    else if ((m_ArgIsConst > 0) && (m_FoldableExpr < 1))
    {
        // TODO: handle 'if (SomeMethod(constArg))' patterns in fgFindJumpTargets
        // The previous version of inliner optimistically assumed this is "has const arg that feeds a conditional"
        multiplier += 3.0;
        JITDUMP("\nCallsite passes a constant.  Multiplier increased to %g.", multiplier);
    }

    if ((m_FoldableBox > 0) && m_NonGenericCallsGeneric)
    {
        // We met some BOX+ISINST+BR or BOX+UNBOX patterns (see impBoxPatternMatch).
        multiplier += 3.0;
        JITDUMP("\nInline has %d foldable BOX ops.  Multiplier increased to %g.", m_FoldableBox, multiplier);
    }

#ifdef FEATURE_SIMD
    if (m_HasSimd)
    {
        multiplier += JitConfig.JitInlineSIMDMultiplier();
        JITDUMP("\nInline candidate has SIMD type args, locals or return value.  Multiplier increased to %g.",
                multiplier);
    }
#endif

    if (m_Intrinsic > 0)
    {
        // In most cases such intrinsics are lowered as single CPU instructions
        multiplier += 1.0 + m_Intrinsic * 0.3;
        JITDUMP("\nInline has %d intrinsics.  Multiplier increased to %g.", m_Intrinsic, multiplier);
    }

    if (m_ArgIsBoxedAtCallsite > 0)
    {
        // Callsite is going to box n arguments. We might avoid boxing after inlining.
        // Example:
        //
        //  void DoNothing(object o) {} // o is unused, so the boxing is redundant
        //
        //  void Caller() => DoNothing(42); // 42 is going to be boxed at the call site.
        //
        multiplier += 0.5;
        JITDUMP("\nCallsite is going to box %d arguments.  Multiplier increased to %g.", m_ArgIsBoxedAtCallsite,
                multiplier);
    }

    if (m_ArgIsExactClsSigIsNot > 0)
    {
        // If we inline such a callee - we'll be able to devirtualize all the calls for such arguments
        // Example:
        //
        //  int Callee(object o) => o.GetHashCode(); // virtual call
        //
        //  int Caller(string s) => Callee(s); // String is 'exact' (sealed)
        //
        multiplier += 2.5;
        JITDUMP("\nCallsite passes %d arguments of exact classes while callee accepts non-exact ones.  Multiplier "
                "increased to %g.",
                m_ArgIsExactClsSigIsNot, multiplier);
    }

    if (m_FoldableIntrinsic > 0)
    {
        // Examples:
        //
        //   typeof(T1) == typeof(T2)
        //   Math.Abs(constArg)
        //   BitOperation.PopCount(10)
        multiplier += 1.0 + m_FoldableIntrinsic;
        JITDUMP("\nInline has %d foldable intrinsics.  Multiplier increased to %g.", m_FoldableIntrinsic, multiplier);
    }

    if (m_FoldableExpr > 0)
    {
        // E.g. add/mul/ceq, etc. over constant/constant arguments
        multiplier += 1.0 + m_FoldableExpr;
        JITDUMP("\nInline has %d foldable binary expressions.  Multiplier increased to %g.", m_FoldableExpr,
                multiplier);
    }

    if (m_FoldableExprUn > 0)
    {
        // E.g. casts, negations, etc. over constants/constant arguments
        multiplier += m_FoldableExprUn;
        JITDUMP("\nInline has %d foldable unary expressions.  Multiplier increased to %g.", m_FoldableExprUn,
                multiplier);
    }

    if (m_DivByCns > 0)
    {
        // E.g. callee has "x / arg0" where arg0 is a const at the call site -
        // we'll avoid a very expensive DIV instruction after inlining.
        multiplier += 3.0;
        JITDUMP("\nInline has %d Div-by-constArg expressions.  Multiplier increased to %g.", m_DivByCns, multiplier);
    }

    if (m_BinaryExprWithCns > 0)
    {
        // In some cases we're not able to detect potentially foldable expressions, e.g.:
        //
        //   ldc.i4.0
        //   call int SomeFoldableNonIntrinsicCall
        //   ceq
        //
        // so at least we can note potential constant tests
        multiplier += m_BinaryExprWithCns * 0.5;
        JITDUMP("\nInline candidate has %d binary expressions with constants.  Multiplier increased to %g.",
                m_BinaryExprWithCns, multiplier);

        // For prejit roots we do not see the call sites. To be suitably optimistic
        // assume that call sites may pass constants and make these m_BinaryExprWithCns
        // foldable.
        if (m_IsPrejitRoot)
        {
            multiplier += m_BinaryExprWithCns;
        }
    }

    if (m_ArgFeedsConstantTest > 0)
    {
        multiplier += m_IsPrejitRoot ? 3.0 : 1.0;
        JITDUMP("\nInline candidate has an arg that feeds a constant test.  Multiplier increased to %g.", multiplier);
    }
    else if (m_IsPrejitRoot && (m_ArgFeedsTest > 0))
    {
        multiplier += 3.0;
        JITDUMP("\nPrejit root candidate has arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
    }

    switch (m_CallsiteFrequency)
    {
        case InlineCallsiteFrequency::RARE:
            // Note this one is not additive, it uses '=' instead of '+='
            multiplier = 1.3;
            JITDUMP("\nInline candidate callsite is rare.  Multiplier limited to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::BORING:
            multiplier += 1.3;
            JITDUMP("\nInline candidate callsite is boring.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::WARM:
            multiplier += 2.0;
            JITDUMP("\nInline candidate callsite is warm.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::LOOP:
            multiplier += 3.0;
            JITDUMP("\nInline candidate callsite is in a loop.  Multiplier increased to %g.", multiplier);
            break;
        case InlineCallsiteFrequency::HOT:
            multiplier += 3.0;
            JITDUMP("\nInline candidate callsite is hot.  Multiplier increased to %g.", multiplier);
            break;
        default:
            assert(!"Unexpected callsite frequency");
            break;
    }

    if (m_FoldableSwitch > 0)
    {
        multiplier += 6.0;
        JITDUMP("\nInline candidate has %d foldable switches.  Multiplier increased to %g.", m_FoldableSwitch,
                multiplier);
    }
    else if (m_Switch > 0)
    {
        if (m_IsPrejitRoot)
        {
            // Assume the switches can be foldable in PrejitRoot mode.
            multiplier += 6.0;
            JITDUMP("\nPrejit root candidate has %d switches.  Multiplier increased to %g.", m_Switch, multiplier);
        }
        else
        {
            // TODO: Investigate cases where it makes sense to inline non-foldable switches
            multiplier = 0.0;
            JITDUMP("\nInline candidate has %d switches.  Multiplier limited to %g.", m_Switch, multiplier);
        }
    }

    if (m_HasProfile)
    {
        // There are cases when Profile Data can be misleading or polluted:
        //  1) We don't support context-sensitive instrumentation
        //  2) The static profile that we ship can be slightly irrelevant for the current app
        //  3) We don't support deoptimizations so we can't re-collect profiles if something changes
        //  4) Sometimes, it still makes sense to inline methods in cold blocks to improve type/esacape analysis
        //     for the whole caller.
        //
        const double profileTrustCoef = (double)JitConfig.JitExtDefaultPolicyProfTrust() / 10.0;
        const double profileScale     = (double)JitConfig.JitExtDefaultPolicyProfScale() / 10.0;

        if (m_RootCompiler->fgHaveTrustedProfileData())
        {
            multiplier *= (1.0 - profileTrustCoef) + min(m_ProfileFrequency, 1.0) * profileScale;
        }
        else
        {
            multiplier *= min(m_ProfileFrequency, 1.0) * profileScale;
        }
        JITDUMP("\nCallsite has profile data: %g.  Multiplier limited to %g.", m_ProfileFrequency, multiplier);
    }

    // Slow down if there are already too many locals
    if (m_RootCompiler->lvaTableCnt > 64)
    {
        // E.g. MaxLocalsToTrack = 1024 and lvaTableCnt = 512 -> multiplier *= 0.5;
        const double lclFullness = min(1.0, (double)m_RootCompiler->lvaTableCnt / JitConfig.JitMaxLocalsToTrack());
        multiplier *= (1.0 - lclFullness);
        JITDUMP("\nCaller has %d locals.  Multiplier decreased to %g.", m_RootCompiler->lvaTableCnt, multiplier);
    }

    if (m_BackwardJump)
    {
        // TODO: investigate in which cases we should [never] inline callees with loops.
        // For now let's add some friction.
        multiplier *= 0.7;
        JITDUMP("\nInline has %d backward jumps (loops?).  Multiplier decreased to %g.", m_BackwardJump, multiplier);
    }

    if (m_IsCallsiteInNoReturnRegion)
    {
        // E.g.
        //
        //   throw new ArgumentException(SR.GetMessage());
        //
        // ^ Here we have two calls inside a BBJ_THROW block
        // Unfortunately, we're not able to detect ThrowHelpers calls yet.

        // Try to avoid inlining such methods for now.
        multiplier = 1.0;
        JITDUMP("\nCallsite is in a no-return region.  Multiplier limited to %g.", multiplier);
    }

#ifdef DEBUG

    int additionalMultiplier = JitConfig.JitInlineAdditionalMultiplier();

    if (additionalMultiplier != 0)
    {
        multiplier += additionalMultiplier;
        JITDUMP("\nmultiplier increased via JitInlineAdditionalMultiplier=%d to %g.", additionalMultiplier, multiplier);
    }

    if (m_RootCompiler->compInlineStress())
    {
        multiplier += 10;
        JITDUMP("\nmultiplier increased via inline stress to %g.", multiplier);
    }

#endif // DEBUG

    return multiplier;
}

#if defined(DEBUG) || defined(INLINE_DATA)
//------------------------------------------------------------------------
// DumpXml: Dump ExtendedDefaultPolicy data as XML
//
// Arguments:
//    file     - stream to output to
//    indent   - indent level

void ExtendedDefaultPolicy::OnDumpXml(FILE* file, unsigned indent) const
{
    DefaultPolicy::OnDumpXml(file, indent);
    XATTR_R8(m_ProfileFrequency)
    XATTR_I4(m_BinaryExprWithCns)
    XATTR_I4(m_ArgCasted)
    XATTR_I4(m_ArgIsStructByValue)
    XATTR_I4(m_FldAccessOverArgStruct)
    XATTR_I4(m_FoldableBox)
    XATTR_I4(m_Intrinsic)
    XATTR_I4(m_BackwardJump)
    XATTR_I4(m_ThrowBlock)
    XATTR_I4(m_ArgIsExactCls)
    XATTR_I4(m_ArgIsExactClsSigIsNot)
    XATTR_I4(m_ArgIsConst)
    XATTR_I4(m_ArgIsBoxedAtCallsite)
    XATTR_I4(m_FoldableIntrinsic)
    XATTR_I4(m_FoldableExpr)
    XATTR_I4(m_FoldableExprUn)
    XATTR_I4(m_FoldableBranch)
    XATTR_I4(m_FoldableSwitch)
    XATTR_I4(m_Switch)
    XATTR_I4(m_DivByCns)
    XATTR_B(m_ReturnsStructByValue)
    XATTR_B(m_IsFromValueClass)
    XATTR_B(m_NonGenericCallsGeneric)
    XATTR_B(m_IsCallsiteInNoReturnRegion)
    XATTR_B(m_HasProfile)
}
#endif

//------------------------------------------------------------------------
// DiscretionaryPolicy: construct a new DiscretionaryPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

// clang-format off
DiscretionaryPolicy::DiscretionaryPolicy(Compiler* compiler, bool isPrejitRoot)
    : DefaultPolicy(compiler, isPrejitRoot)
    , m_ProfileFrequency(0.0)
    , m_BlockCount(0)
    , m_Maxstack(0)
    , m_ArgCount(0)
    , m_ArgType()
    , m_ArgSize()
    , m_LocalCount(0)
    , m_ReturnType(CORINFO_TYPE_UNDEF)
    , m_ReturnSize(0)
    , m_ArgAccessCount(0)
    , m_LocalAccessCount(0)
    , m_IntConstantCount(0)
    , m_FloatConstantCount(0)
    , m_IntLoadCount(0)
    , m_FloatLoadCount(0)
    , m_IntStoreCount(0)
    , m_FloatStoreCount(0)
    , m_SimpleMathCount(0)
    , m_ComplexMathCount(0)
    , m_OverflowMathCount(0)
    , m_IntArrayLoadCount(0)
    , m_FloatArrayLoadCount(0)
    , m_RefArrayLoadCount(0)
    , m_StructArrayLoadCount(0)
    , m_IntArrayStoreCount(0)
    , m_FloatArrayStoreCount(0)
    , m_RefArrayStoreCount(0)
    , m_StructArrayStoreCount(0)
    , m_StructOperationCount(0)
    , m_ObjectModelCount(0)
    , m_FieldLoadCount(0)
    , m_FieldStoreCount(0)
    , m_StaticFieldLoadCount(0)
    , m_StaticFieldStoreCount(0)
    , m_LoadAddressCount(0)
    , m_ThrowCount(0)
    , m_ReturnCount(0)
    , m_CallCount(0)
    , m_CallSiteWeight(0)
    , m_ModelCodeSizeEstimate(0)
    , m_PerCallInstructionEstimate(0)
    , m_HasProfile(false)
    , m_IsClassCtor(false)
    , m_IsSameThis(false)
    , m_CallerHasNewArray(false)
    , m_CallerHasNewObj(false)
    , m_CalleeHasGCStruct(false)
{
    // Empty
}
// clang-format on

//------------------------------------------------------------------------
// NoteBool: handle an observed boolean value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DiscretionaryPolicy::NoteBool(InlineObservation obs, bool value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_IS_CLASS_CTOR:
            m_IsClassCtor = value;
            break;

        case InlineObservation::CALLSITE_IS_SAME_THIS:
            m_IsSameThis = value;
            break;

        case InlineObservation::CALLER_HAS_NEWARRAY:
            m_CallerHasNewArray = value;
            break;

        case InlineObservation::CALLER_HAS_NEWOBJ:
            m_CallerHasNewObj = value;
            break;

        case InlineObservation::CALLEE_HAS_GC_STRUCT:
            m_CalleeHasGCStruct = value;
            break;

        case InlineObservation::CALLSITE_RARE_GC_STRUCT:
            // This is redundant since this policy tracks call site
            // hotness for all candidates. So ignore.
            break;

        case InlineObservation::CALLSITE_HAS_PROFILE:
            m_HasProfile = value;
            break;

        default:
            DefaultPolicy::NoteBool(obs, value);
            break;
    }
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DiscretionaryPolicy::NoteInt(InlineObservation obs, int value)
{
    switch (obs)
    {
        case InlineObservation::CALLEE_IL_CODE_SIZE:
            // Override how code size is handled
            {
                assert(m_IsForceInlineKnown);
                assert(value != 0);
                m_CodeSize = static_cast<unsigned>(value);

                if (m_IsForceInline)
                {
                    // Candidate based on force inline
                    SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
                }
                else
                {
                    // Candidate, pending profitability evaluation
                    SetCandidate(InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);
                }

                break;
            }

        case InlineObservation::CALLEE_OPCODE:
        {
            // This tries to do a rough binning of opcodes based
            // on similarity of impact on codegen.
            OPCODE opcode = static_cast<OPCODE>(value);
            ComputeOpcodeBin(opcode);
            DefaultPolicy::NoteInt(obs, value);
            break;
        }

        case InlineObservation::CALLEE_MAXSTACK:
            m_Maxstack = value;
            break;

        case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
            m_BlockCount = value;
            break;

        case InlineObservation::CALLSITE_WEIGHT:
            m_CallSiteWeight = static_cast<unsigned>(value);
            break;

        default:
            // Delegate remainder to the super class.
            DefaultPolicy::NoteInt(obs, value);
            break;
    }
}

//------------------------------------------------------------------------
// NoteDouble: handle an observed double value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DiscretionaryPolicy::NoteDouble(InlineObservation obs, double value)
{
    assert(obs == InlineObservation::CALLSITE_PROFILE_FREQUENCY);
    assert(value >= 0.0);

    m_ProfileFrequency = value;
}

//------------------------------------------------------------------------
// ComputeOpcodeBin: simple histogramming of opcodes based on presumably
// similar codegen impact.
//
// Arguments:
//    opcode - an MSIL opcode from the callee

void DiscretionaryPolicy::ComputeOpcodeBin(OPCODE opcode)
{
    switch (opcode)
    {
        case CEE_LDARG_0:
        case CEE_LDARG_1:
        case CEE_LDARG_2:
        case CEE_LDARG_3:
        case CEE_LDARG_S:
        case CEE_LDARG:
        case CEE_STARG_S:
        case CEE_STARG:
            m_ArgAccessCount++;
            break;

        case CEE_LDLOC_0:
        case CEE_LDLOC_1:
        case CEE_LDLOC_2:
        case CEE_LDLOC_3:
        case CEE_LDLOC_S:
        case CEE_STLOC_0:
        case CEE_STLOC_1:
        case CEE_STLOC_2:
        case CEE_STLOC_3:
        case CEE_STLOC_S:
        case CEE_LDLOC:
        case CEE_STLOC:
            m_LocalAccessCount++;
            break;

        case CEE_LDNULL:
        case CEE_LDC_I4_M1:
        case CEE_LDC_I4_0:
        case CEE_LDC_I4_1:
        case CEE_LDC_I4_2:
        case CEE_LDC_I4_3:
        case CEE_LDC_I4_4:
        case CEE_LDC_I4_5:
        case CEE_LDC_I4_6:
        case CEE_LDC_I4_7:
        case CEE_LDC_I4_8:
        case CEE_LDC_I4_S:
            m_IntConstantCount++;
            break;

        case CEE_LDC_R4:
        case CEE_LDC_R8:
            m_FloatConstantCount++;
            break;

        case CEE_LDIND_I1:
        case CEE_LDIND_U1:
        case CEE_LDIND_I2:
        case CEE_LDIND_U2:
        case CEE_LDIND_I4:
        case CEE_LDIND_U4:
        case CEE_LDIND_I8:
        case CEE_LDIND_I:
            m_IntLoadCount++;
            break;

        case CEE_LDIND_R4:
        case CEE_LDIND_R8:
            m_FloatLoadCount++;
            break;

        case CEE_STIND_I1:
        case CEE_STIND_I2:
        case CEE_STIND_I4:
        case CEE_STIND_I8:
        case CEE_STIND_I:
            m_IntStoreCount++;
            break;

        case CEE_STIND_R4:
        case CEE_STIND_R8:
            m_FloatStoreCount++;
            break;

        case CEE_SUB:
        case CEE_AND:
        case CEE_OR:
        case CEE_XOR:
        case CEE_SHL:
        case CEE_SHR:
        case CEE_SHR_UN:
        case CEE_NEG:
        case CEE_NOT:
        case CEE_CONV_I1:
        case CEE_CONV_I2:
        case CEE_CONV_I4:
        case CEE_CONV_I8:
        case CEE_CONV_U4:
        case CEE_CONV_U8:
        case CEE_CONV_U2:
        case CEE_CONV_U1:
        case CEE_CONV_I:
        case CEE_CONV_U:
            m_SimpleMathCount++;
            break;

        case CEE_MUL:
        case CEE_DIV:
        case CEE_DIV_UN:
        case CEE_REM:
        case CEE_REM_UN:
        case CEE_CONV_R4:
        case CEE_CONV_R8:
        case CEE_CONV_R_UN:
            m_ComplexMathCount++;
            break;

        case CEE_CONV_OVF_I1_UN:
        case CEE_CONV_OVF_I2_UN:
        case CEE_CONV_OVF_I4_UN:
        case CEE_CONV_OVF_I8_UN:
        case CEE_CONV_OVF_U1_UN:
        case CEE_CONV_OVF_U2_UN:
        case CEE_CONV_OVF_U4_UN:
        case CEE_CONV_OVF_U8_UN:
        case CEE_CONV_OVF_I_UN:
        case CEE_CONV_OVF_U_UN:
        case CEE_CONV_OVF_I1:
        case CEE_CONV_OVF_U1:
        case CEE_CONV_OVF_I2:
        case CEE_CONV_OVF_U2:
        case CEE_CONV_OVF_I4:
        case CEE_CONV_OVF_U4:
        case CEE_CONV_OVF_I8:
        case CEE_CONV_OVF_U8:
        case CEE_ADD_OVF:
        case CEE_ADD_OVF_UN:
        case CEE_MUL_OVF:
        case CEE_MUL_OVF_UN:
        case CEE_SUB_OVF:
        case CEE_SUB_OVF_UN:
        case CEE_CKFINITE:
            m_OverflowMathCount++;
            break;

        case CEE_LDELEM_I1:
        case CEE_LDELEM_U1:
        case CEE_LDELEM_I2:
        case CEE_LDELEM_U2:
        case CEE_LDELEM_I4:
        case CEE_LDELEM_U4:
        case CEE_LDELEM_I8:
        case CEE_LDELEM_I:
            m_IntArrayLoadCount++;
            break;

        case CEE_LDELEM_R4:
        case CEE_LDELEM_R8:
            m_FloatArrayLoadCount++;
            break;

        case CEE_LDELEM_REF:
            m_RefArrayLoadCount++;
            break;

        case CEE_LDELEM:
            m_StructArrayLoadCount++;
            break;

        case CEE_STELEM_I:
        case CEE_STELEM_I1:
        case CEE_STELEM_I2:
        case CEE_STELEM_I4:
        case CEE_STELEM_I8:
            m_IntArrayStoreCount++;
            break;

        case CEE_STELEM_R4:
        case CEE_STELEM_R8:
            m_FloatArrayStoreCount++;
            break;

        case CEE_STELEM_REF:
            m_RefArrayStoreCount++;
            break;

        case CEE_STELEM:
            m_StructArrayStoreCount++;
            break;

        case CEE_CPOBJ:
        case CEE_LDOBJ:
        case CEE_CPBLK:
        case CEE_INITBLK:
        case CEE_STOBJ:
            m_StructOperationCount++;
            break;

        case CEE_CASTCLASS:
        case CEE_ISINST:
        case CEE_UNBOX:
        case CEE_BOX:
        case CEE_UNBOX_ANY:
        case CEE_LDFTN:
        case CEE_LDVIRTFTN:
        case CEE_SIZEOF:
            m_ObjectModelCount++;
            break;

        case CEE_LDFLD:
        case CEE_LDLEN:
        case CEE_REFANYTYPE:
        case CEE_REFANYVAL:
            m_FieldLoadCount++;
            break;

        case CEE_STFLD:
            m_FieldStoreCount++;
            break;

        case CEE_LDSFLD:
            m_StaticFieldLoadCount++;
            break;

        case CEE_STSFLD:
            m_StaticFieldStoreCount++;
            break;

        case CEE_LDELEMA:
        case CEE_LDSFLDA:
        case CEE_LDFLDA:
        case CEE_LDSTR:
        case CEE_LDARGA:
        case CEE_LDLOCA:
            m_LoadAddressCount++;
            break;

        case CEE_CALL:
        case CEE_CALLI:
        case CEE_CALLVIRT:
        case CEE_NEWOBJ:
        case CEE_NEWARR:
        case CEE_JMP:
            m_CallCount++;
            break;

        case CEE_THROW:
        case CEE_RETHROW:
            m_ThrowCount++;
            break;

        case CEE_RET:
            m_ReturnCount++;
            break;

        default:
            break;
    }
}

//------------------------------------------------------------------------
// PropagateNeverToRuntime: determine if a never result should cause the
// method to be marked as un-inlinable.

bool DiscretionaryPolicy::PropagateNeverToRuntime() const
{
    // Propagate most failures, but don't propagate when the inline
    // was viable but unprofitable, or does not return..
    //
    switch (m_Observation)
    {
        case InlineObservation::CALLEE_NOT_PROFITABLE_INLINE:
        case InlineObservation::CALLEE_DOES_NOT_RETURN:
            return false;

        default:
            return true;
    }
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void DiscretionaryPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{

#if defined(DEBUG)

    // Punt if we're inlining and we've reached the acceptance limit.
    int      limit   = JitConfig.JitInlineLimit();
    unsigned current = m_RootCompiler->m_inlineStrategy->GetInlineCount();

    if (!m_IsPrejitRoot && (limit >= 0) && (current >= static_cast<unsigned>(limit)))
    {
        SetFailure(InlineObservation::CALLSITE_OVER_INLINE_LIMIT);
        return;
    }

#endif // defined(DEBUG)

    // Make additional observations based on the method info
    MethodInfoObservations(methodInfo);

    // Estimate the code size impact. This is just for model
    // evaluation purposes -- we'll still use the legacy policy's
    // model for actual inlining.
    EstimateCodeSize();

    // Estimate performance impact. This is just for model
    // evaluation purposes -- we'll still use the legacy policy's
    // model for actual inlining.
    EstimatePerformanceImpact();

    // Delegate to super class for the rest
    DefaultPolicy::DetermineProfitability(methodInfo);
}

//------------------------------------------------------------------------
// MethodInfoObservations: make observations based on information from
// the method info for the callee.
//
// Arguments:
//    methodInfo -- method info for the callee

void DiscretionaryPolicy::MethodInfoObservations(CORINFO_METHOD_INFO* methodInfo)
{
    CORINFO_SIG_INFO& locals = methodInfo->locals;
    m_LocalCount             = locals.numArgs;

    CORINFO_SIG_INFO& args     = methodInfo->args;
    const unsigned    argCount = args.numArgs;
    m_ArgCount                 = argCount;

    const unsigned pointerSize = TARGET_POINTER_SIZE;
    unsigned       i           = 0;

    // Implicit arguments

    const bool hasThis = args.hasThis();

    if (hasThis)
    {
        m_ArgType[i] = CORINFO_TYPE_CLASS;
        m_ArgSize[i] = pointerSize;
        i++;
        m_ArgCount++;
    }

    const bool hasTypeArg = args.hasTypeArg();

    if (hasTypeArg)
    {
        m_ArgType[i] = CORINFO_TYPE_NATIVEINT;
        m_ArgSize[i] = pointerSize;
        i++;
        m_ArgCount++;
    }

    // Explicit arguments

    unsigned                j             = 0;
    CORINFO_ARG_LIST_HANDLE argListHandle = args.args;
    COMP_HANDLE             comp          = m_RootCompiler->info.compCompHnd;

    while ((i < MAX_ARGS) && (j < argCount))
    {
        CORINFO_CLASS_HANDLE classHandle;
        CorInfoType          type = strip(comp->getArgType(&args, argListHandle, &classHandle));

        m_ArgType[i] = type;

        if (type == CORINFO_TYPE_VALUECLASS)
        {
            assert(classHandle != nullptr);
            m_ArgSize[i] = roundUp(comp->getClassSize(classHandle), pointerSize);
        }
        else
        {
            m_ArgSize[i] = pointerSize;
        }

        argListHandle = comp->getArgNext(argListHandle);
        i++;
        j++;
    }

    while (i < MAX_ARGS)
    {
        m_ArgType[i] = CORINFO_TYPE_UNDEF;
        m_ArgSize[i] = 0;
        i++;
    }

    // Return Type

    m_ReturnType = args.retType;

    if (m_ReturnType == CORINFO_TYPE_VALUECLASS)
    {
        assert(args.retTypeClass != nullptr);
        m_ReturnSize = roundUp(comp->getClassSize(args.retTypeClass), pointerSize);
    }
    else if (m_ReturnType == CORINFO_TYPE_VOID)
    {
        m_ReturnSize = 0;
    }
    else
    {
        m_ReturnSize = pointerSize;
    }
}

//------------------------------------------------------------------------
// EstimateCodeSize: produce (various) code size estimates based on
// observations.
//
// The "Baseline" code size model used by the legacy policy is
// effectively
//
//   0.100 * m_CalleeNativeSizeEstimate +
//  -0.100 * m_CallsiteNativeSizeEstimate
//
// On the inlines in CoreCLR's CoreLib, release windows x64, this
// yields scores of R=0.42, MSE=228, and MAE=7.25.
//
// This estimate can be improved slightly by refitting, resulting in
//
//  -1.451 +
//   0.095 * m_CalleeNativeSizeEstimate +
//  -0.104 * m_CallsiteNativeSizeEstimate
//
// With R=0.44, MSE=220, and MAE=6.93.

void DiscretionaryPolicy::EstimateCodeSize()
{
    // Ensure we have this available.
    m_CalleeNativeSizeEstimate = DetermineNativeSizeEstimate();

    // Size estimate based on GLMNET model.
    // R=0.55, MSE=177, MAE=6.59
    //
    // Suspect it doesn't handle factors properly...
    // clang-format off
    double sizeEstimate =
        -13.532 +
          0.359 * (int) m_CallsiteFrequency +
         -0.015 * m_ArgCount +
         -1.553 * m_ArgSize[5] +
          2.326 * m_LocalCount +
          0.287 * m_ReturnSize +
          0.561 * m_IntConstantCount +
          1.932 * m_FloatConstantCount +
         -0.822 * m_SimpleMathCount +
         -7.591 * m_IntArrayLoadCount +
          4.784 * m_RefArrayLoadCount +
         12.778 * m_StructArrayLoadCount +
          1.452 * m_FieldLoadCount +
          8.811 * m_StaticFieldLoadCount +
          2.752 * m_StaticFieldStoreCount +
         -6.566 * m_ThrowCount +
          6.021 * m_CallCount +
         -0.238 * m_IsInstanceCtor +
         -5.357 * m_IsFromPromotableValueClass +
         -7.901 * (m_ConstantArgFeedsConstantTest > 0 ? 1 : 0)  +
          0.065 * m_CalleeNativeSizeEstimate;
    // clang-format on

    // Scaled up and reported as an integer value.
    m_ModelCodeSizeEstimate = (int)(SIZE_SCALE * sizeEstimate);
}

//------------------------------------------------------------------------
// EstimatePerformanceImpact: produce performance estimates based on
// observations.
//
// Notes:
//    Attempts to predict the per-call savings in instructions executed.
//
//    A negative value indicates the doing the inline will save instructions
//    and likely time.

void DiscretionaryPolicy::EstimatePerformanceImpact()
{
    // Performance estimate based on GLMNET model.
    // R=0.24, RMSE=16.1, MAE=8.9.
    // clang-format off
    double perCallSavingsEstimate =
        -7.35
        + (m_CallsiteFrequency == InlineCallsiteFrequency::BORING ?  0.76 : 0)
        + (m_CallsiteFrequency == InlineCallsiteFrequency::LOOP   ? -2.02 : 0)
        + (m_ArgType[0] == CORINFO_TYPE_CLASS ?  3.51 : 0)
        + (m_ArgType[3] == CORINFO_TYPE_BOOL  ? 20.7  : 0)
        + (m_ArgType[4] == CORINFO_TYPE_CLASS ?  0.38 : 0)
        + (m_ReturnType == CORINFO_TYPE_CLASS ?  2.32 : 0);
    // clang-format on

    // Scaled up and reported as an integer value.
    m_PerCallInstructionEstimate = (int)(SIZE_SCALE * perCallSavingsEstimate);
}

//------------------------------------------------------------------------
// CodeSizeEstimate: estimated code size impact of the inline
//
// Return Value:
//    Estimated code size impact, in bytes * 10

int DiscretionaryPolicy::CodeSizeEstimate()
{
    return m_ModelCodeSizeEstimate;
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------
// DumpSchema: dump names for all the supporting data for the
// inline decision in CSV format.
//
// Arguments:
//    file -- file to write to

void DiscretionaryPolicy::DumpSchema(FILE* file) const
{
    fprintf(file, "ILSize");
    fprintf(file, ",CallsiteFrequency");
    fprintf(file, ",InstructionCount");
    fprintf(file, ",LoadStoreCount");
    fprintf(file, ",BlockCount");
    fprintf(file, ",Maxstack");
    fprintf(file, ",ArgCount");

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",Arg%uType", i);
    }

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",Arg%uSize", i);
    }

    fprintf(file, ",LocalCount");
    fprintf(file, ",ReturnType");
    fprintf(file, ",ReturnSize");
    fprintf(file, ",ArgAccessCount");
    fprintf(file, ",LocalAccessCount");
    fprintf(file, ",IntConstantCount");
    fprintf(file, ",FloatConstantCount");
    fprintf(file, ",IntLoadCount");
    fprintf(file, ",FloatLoadCount");
    fprintf(file, ",IntStoreCount");
    fprintf(file, ",FloatStoreCount");
    fprintf(file, ",SimpleMathCount");
    fprintf(file, ",ComplexMathCount");
    fprintf(file, ",OverflowMathCount");
    fprintf(file, ",IntArrayLoadCount");
    fprintf(file, ",FloatArrayLoadCount");
    fprintf(file, ",RefArrayLoadCount");
    fprintf(file, ",StructArrayLoadCount");
    fprintf(file, ",IntArrayStoreCount");
    fprintf(file, ",FloatArrayStoreCount");
    fprintf(file, ",RefArrayStoreCount");
    fprintf(file, ",StructArrayStoreCount");
    fprintf(file, ",StructOperationCount");
    fprintf(file, ",ObjectModelCount");
    fprintf(file, ",FieldLoadCount");
    fprintf(file, ",FieldStoreCount");
    fprintf(file, ",StaticFieldLoadCount");
    fprintf(file, ",StaticFieldStoreCount");
    fprintf(file, ",LoadAddressCount");
    fprintf(file, ",ThrowCount");
    fprintf(file, ",ReturnCount");
    fprintf(file, ",CallCount");
    fprintf(file, ",CallSiteWeight");
    fprintf(file, ",IsForceInline");
    fprintf(file, ",IsInstanceCtor");
    fprintf(file, ",IsFromPromotableValueClass");
    fprintf(file, ",HasSimd");
    fprintf(file, ",LooksLikeWrapperMethod");
    fprintf(file, ",ArgFeedsConstantTest");
    fprintf(file, ",IsMostlyLoadStore");
    fprintf(file, ",ArgFeedsRangeCheck");
    fprintf(file, ",ConstantArgFeedsConstantTest");
    fprintf(file, ",CalleeNativeSizeEstimate");
    fprintf(file, ",CallsiteNativeSizeEstimate");
    fprintf(file, ",ModelCodeSizeEstimate");
    fprintf(file, ",ModelPerCallInstructionEstimate");
    fprintf(file, ",IsClassCtor");
    fprintf(file, ",IsSameThis");
    fprintf(file, ",CallerHasNewArray");
    fprintf(file, ",CallerHasNewObj");
    fprintf(file, ",CalleeDoesNotReturn");
    fprintf(file, ",CalleeHasGCStruct");
    fprintf(file, ",CallsiteDepth");
}

//------------------------------------------------------------------------
// DumpData: dump all the supporting data for the inline decision
// in CSV format.
//
// Arguments:
//    file -- file to write to

void DiscretionaryPolicy::DumpData(FILE* file) const
{
    fprintf(file, "%u", m_CodeSize);
    fprintf(file, ",%u", m_CallsiteFrequency);
    fprintf(file, ",%u", m_InstructionCount);
    fprintf(file, ",%u", m_LoadStoreCount);
    fprintf(file, ",%u", m_BlockCount);
    fprintf(file, ",%u", m_Maxstack);
    fprintf(file, ",%u", m_ArgCount);

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",%u", m_ArgType[i]);
    }

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",%u", (unsigned)m_ArgSize[i]);
    }

    fprintf(file, ",%u", m_LocalCount);
    fprintf(file, ",%u", m_ReturnType);
    fprintf(file, ",%u", (unsigned)m_ReturnSize);
    fprintf(file, ",%u", m_ArgAccessCount);
    fprintf(file, ",%u", m_LocalAccessCount);
    fprintf(file, ",%u", m_IntConstantCount);
    fprintf(file, ",%u", m_FloatConstantCount);
    fprintf(file, ",%u", m_IntLoadCount);
    fprintf(file, ",%u", m_FloatLoadCount);
    fprintf(file, ",%u", m_IntStoreCount);
    fprintf(file, ",%u", m_FloatStoreCount);
    fprintf(file, ",%u", m_SimpleMathCount);
    fprintf(file, ",%u", m_ComplexMathCount);
    fprintf(file, ",%u", m_OverflowMathCount);
    fprintf(file, ",%u", m_IntArrayLoadCount);
    fprintf(file, ",%u", m_FloatArrayLoadCount);
    fprintf(file, ",%u", m_RefArrayLoadCount);
    fprintf(file, ",%u", m_StructArrayLoadCount);
    fprintf(file, ",%u", m_IntArrayStoreCount);
    fprintf(file, ",%u", m_FloatArrayStoreCount);
    fprintf(file, ",%u", m_RefArrayStoreCount);
    fprintf(file, ",%u", m_StructArrayStoreCount);
    fprintf(file, ",%u", m_StructOperationCount);
    fprintf(file, ",%u", m_ObjectModelCount);
    fprintf(file, ",%u", m_FieldLoadCount);
    fprintf(file, ",%u", m_FieldStoreCount);
    fprintf(file, ",%u", m_StaticFieldLoadCount);
    fprintf(file, ",%u", m_StaticFieldStoreCount);
    fprintf(file, ",%u", m_LoadAddressCount);
    fprintf(file, ",%u", m_ReturnCount);
    fprintf(file, ",%u", m_ThrowCount);
    fprintf(file, ",%u", m_CallCount);
    fprintf(file, ",%u", m_CallSiteWeight);
    fprintf(file, ",%u", m_IsForceInline ? 1 : 0);
    fprintf(file, ",%u", m_IsInstanceCtor ? 1 : 0);
    fprintf(file, ",%u", m_IsFromPromotableValueClass ? 1 : 0);
    fprintf(file, ",%u", m_HasSimd ? 1 : 0);
    fprintf(file, ",%u", m_LooksLikeWrapperMethod ? 1 : 0);
    fprintf(file, ",%u", m_ArgFeedsConstantTest);
    fprintf(file, ",%u", m_MethodIsMostlyLoadStore ? 1 : 0);
    fprintf(file, ",%u", m_ArgFeedsRangeCheck);
    fprintf(file, ",%u", m_ConstArgFeedsIsKnownConst ? 1 : 0);
    fprintf(file, ",%u", m_ArgFeedsIsKnownConst ? 1 : 0);
    fprintf(file, ",%u", m_ConstantArgFeedsConstantTest);
    fprintf(file, ",%d", m_CalleeNativeSizeEstimate);
    fprintf(file, ",%d", m_CallsiteNativeSizeEstimate);
    fprintf(file, ",%d", m_ModelCodeSizeEstimate);
    fprintf(file, ",%d", m_PerCallInstructionEstimate);
    fprintf(file, ",%u", m_IsClassCtor ? 1 : 0);
    fprintf(file, ",%u", m_IsSameThis ? 1 : 0);
    fprintf(file, ",%u", m_CallerHasNewArray ? 1 : 0);
    fprintf(file, ",%u", m_CallerHasNewObj ? 1 : 0);
    fprintf(file, ",%u", m_IsNoReturn ? 1 : 0);
    fprintf(file, ",%u", m_CalleeHasGCStruct ? 1 : 0);
    fprintf(file, ",%u", m_CallsiteDepth);
}

#endif // defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------/
// ModelPolicy: construct a new ModelPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

ModelPolicy::ModelPolicy(Compiler* compiler, bool isPrejitRoot) : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed
//
// Notes:
//    The ILSize threshold used here should be large enough that
//    it does not generally influence inlining decisions -- it only
//    helps to make them faster.
//
//    The value is determined as follows. We figure out the maximum
//    possible code size estimate that will lead to an inline. This is
//    found by determining the maximum possible inline benefit and
//    working backwards.
//
//    In the current ModelPolicy, the maximum benefit is -28.1, which
//    comes from a CallSiteWeight of 3 and a per call benefit of
//    -9.37.  This implies that any candidate with code size larger
//    than (28.1/0.2) will not pass the threshold. So maximum code
//    size estimate (in bytes) for any inlinee is 140.55, and hence
//    maximum estimate is 1405.
//
//    Since we are trying to short circuit early in the evaluation
//    process we don't have the code size estimate in hand. We need to
//    estimate the possible code size estimate based on something we
//    know cheaply and early -- the ILSize. So we use quantile
//    regression to project how ILSize predicts the model code size
//    estimate. Note that ILSize does not currently directly enter
//    into the model.
//
//    The median value for the model code size estimate based on
//    ILSize is given by -107 + 12.6 * ILSize for the V9 data.  This
//    means an ILSize of 120 is likely to lead to a size estimate of
//    at least 1405 at least 50% of the time. So we choose this as the
//    early rejection threshold.

void ModelPolicy::NoteInt(InlineObservation obs, int value)
{
    // Let underlying policy do its thing.
    DiscretionaryPolicy::NoteInt(obs, value);

    if (InlDecisionIsFailure(m_Decision))
    {
        return;
    }

    // Fail fast for inlinees that are too large to ever inline.
    // The value of 120 is model-dependent; see notes above.
    if (!m_IsForceInline && (obs == InlineObservation::CALLEE_IL_CODE_SIZE) && (value >= 120))
    {
        // Callee too big, not a candidate
        SetNever(InlineObservation::CALLEE_TOO_MUCH_IL);
        return;
    }
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee
//
// Notes:
//    There are currently two parameters that are ad-hoc: the
//    per-call-site weight and the size/speed threshold. Ideally this
//    policy would have just one tunable parameter, the threshold,
//    which describes how willing we are to trade size for speed.

void ModelPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // Do some homework
    MethodInfoObservations(methodInfo);
    EstimateCodeSize();
    EstimatePerformanceImpact();

    // Preliminary inline model.
    //
    // If code size is estimated to increase, look at
    // the profitability model for guidance.
    //
    // If code size will decrease, just inline.

    if (m_ModelCodeSizeEstimate <= 0)
    {
        // Inline will likely decrease code size
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Inline profitable, will decrease code size by %g bytes\n",
                                     (double)-m_ModelCodeSizeEstimate / SIZE_SCALE));

        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_IS_SIZE_DECREASING_INLINE);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_IS_SIZE_DECREASING_INLINE);
        }
    }
    else
    {
        // We estimate that this inline will increase code size.  Only
        // inline if the performance win is sufficiently large to
        // justify bigger code.

        // First compute the number of instruction executions saved
        // via inlining per call to the callee per byte of code size
        // impact.
        //
        // The per call instruction estimate is negative if the inline
        // will reduce instruction count. Flip the sign here to make
        // positive be better and negative worse.
        double perCallBenefit = -((double)m_PerCallInstructionEstimate / (double)m_ModelCodeSizeEstimate);

        // Now estimate the local call frequency.
        //
        // Todo: use IBC data, or a better local profile estimate, or
        // try and incorporate this into the model. For instance if we
        // tried to predict the benefit per call to the root method
        // then the model would have to incorporate the local call
        // frequency, somehow.
        double callSiteWeight = 1.0;

        switch (m_CallsiteFrequency)
        {
            case InlineCallsiteFrequency::RARE:
                callSiteWeight = 0.1;
                break;
            case InlineCallsiteFrequency::BORING:
                callSiteWeight = 1.0;
                break;
            case InlineCallsiteFrequency::WARM:
                callSiteWeight = 1.5;
                break;
            case InlineCallsiteFrequency::LOOP:
            case InlineCallsiteFrequency::HOT:
                callSiteWeight = 3.0;
                break;
            default:
                assert(false);
                break;
        }

        // Determine the estimated number of instructions saved per
        // call to the root method per byte of code size impact. This
        // is our benefit figure of merit.
        double benefit = callSiteWeight * perCallBenefit;

        // Compare this to the threshold, and inline if greater.
        //
        // The threshold is interpretable as a size/speed tradeoff:
        // the value of 0.2 below indicates we'll allow inlines that
        // grow code by as many as 5 bytes to save 1 instruction
        // execution (per call to the root method).
        double threshold    = 0.20;
        bool   shouldInline = (benefit > threshold);

        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000, "Inline %s profitable: benefit=%g (weight=%g, percall=%g, size=%g)\n",
                     shouldInline ? "is" : "is not", benefit, callSiteWeight,
                     (double)m_PerCallInstructionEstimate / SIZE_SCALE, (double)m_ModelCodeSizeEstimate / SIZE_SCALE));

        if (!shouldInline)
        {
            // Fail the inline
            if (m_IsPrejitRoot)
            {
                SetNever(InlineObservation::CALLEE_NOT_PROFITABLE_INLINE);
            }
            else
            {
                SetFailure(InlineObservation::CALLSITE_NOT_PROFITABLE_INLINE);
            }
        }
        else
        {
            // Update candidacy
            if (m_IsPrejitRoot)
            {
                SetCandidate(InlineObservation::CALLEE_IS_PROFITABLE_INLINE);
            }
            else
            {
                SetCandidate(InlineObservation::CALLSITE_IS_PROFITABLE_INLINE);
            }
        }
    }
}

//------------------------------------------------------------------------/
// ProfilePolicy: construct a new ProfilePolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

ProfilePolicy::ProfilePolicy(Compiler* compiler, bool isPrejitRoot) : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed
//
// Notes:
//    The ILSize threshold used here should be large enough that
//    it does not generally influence inlining decisions -- it only
//    helps to make them faster.
//
//    The value used below is just a guess and needs refinement.

void ProfilePolicy::NoteInt(InlineObservation obs, int value)
{
    // Let underlying policy do its thing.
    DiscretionaryPolicy::NoteInt(obs, value);

    if (InlDecisionIsFailure(m_Decision))
    {
        return;
    }

    // Fail fast for inlinees that are too large to ever inline.
    //
    if (!m_IsForceInline && (obs == InlineObservation::CALLEE_IL_CODE_SIZE) && (value >= 1000))
    {
        // Callee too big, not a candidate
        SetNever(InlineObservation::CALLEE_TOO_MUCH_IL);
        return;
    }

    // This observation happens after we determine profitability
    // so we need to special case it here.
    //
    if (obs == InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS)
    {
        // Fail if this is a throw helper.
        //
        assert(m_IsForceInlineKnown);
        assert(m_IsNoReturnKnown);
        assert(value > 0);

        if (!m_IsForceInline && m_IsNoReturn && (value == 1))
        {
            SetNever(InlineObservation::CALLEE_DOES_NOT_RETURN);
            return;
        }

        // If we're mimicking the default policy because there's no PGO
        // data for this call, also fail if thereare too many basic blocks.
        //
        if (!m_HasProfile && !m_IsForceInline && (value > MAX_BASIC_BLOCKS))
        {
            SetNever(InlineObservation::CALLEE_TOO_MANY_BASIC_BLOCKS);
            return;
        }
    }
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee
//
// Notes:
//    There are currently two parameters that are ad-hoc: the
//    "global importance" weight and the size/speed threshold. Ideally this
//    policy would have just one tunable parameter, the threshold,
//    which describes how willing we are to trade size for speed.

void ProfilePolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // We expect to have profile data, otherwise we should not
    // have used this policy.
    //
    if (!m_HasProfile)
    {
        // Todo: investigate these cases more carefully.
        //
        SetFailure(InlineObservation::CALLSITE_NOT_PROFITABLE_INLINE);
        return;
    }

    // Do some homework
    MethodInfoObservations(methodInfo);
    EstimateCodeSize();
    EstimatePerformanceImpact();

    // Preliminary inline model.
    //
    // If code size is estimated to increase, look at
    // the profitability model for guidance.
    //
    // If code size will decrease, just inline.
    //
    if (m_ModelCodeSizeEstimate <= 0)
    {
        // Inline will likely decrease code size
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Inline profitable, will decrease code size by %g bytes\n",
                                     (double)-m_ModelCodeSizeEstimate / SIZE_SCALE));

        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_IS_SIZE_DECREASING_INLINE);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_IS_SIZE_DECREASING_INLINE);
        }

        return;
    }

    JITDUMP("Have profile data for call site...\n");

    // This is a (projected) size increasing inline, and we have profile
    // data available at the call site.
    //
    // We estimate that this inline will increase code size.  Only
    // inline if the performance win is sufficiently large to
    // justify bigger code.

    // First compute the number of instruction executions saved
    // via inlining per call to the callee per byte of code size
    // impact.
    //
    // The per call instruction estimate is negative if the inline
    // will reduce instruction count. Flip the sign here to make
    // positive be better and negative worse.
    double perCallBenefit = -((double)m_PerCallInstructionEstimate / (double)m_ModelCodeSizeEstimate);

    // Multiply by the call frequency to scale the benefit by
    // the local importance.
    //
    double localBenefit = perCallBenefit * m_ProfileFrequency;

    // Account for "global importance"
    //
    double globalImportance = 1.0;
    double benefit          = globalImportance * localBenefit;

    // Compare this to the threshold, and inline if greater.
    //
    // The threshold is interpretable as a speed/size tradeoff,
    // roughly the number benefit units needed for one extra byte of code.
    // to spend to get one unit of benefit.
    //
    // Default is 65/245 = 0.25
    //
    double threshold    = JitConfig.JitInlinePolicyProfileThreshold() / 256.0;
    bool   shouldInline = (benefit > threshold);

    JITLOG_THIS(m_RootCompiler,
                (LL_INFO100000, "Inline %s profitable: benefit=%g (perCall=%g, local=%g, global=%g, size=%g)\n",
                 shouldInline ? "is" : "is not", benefit, perCallBenefit, localBenefit, globalImportance,
                 (double)m_PerCallInstructionEstimate / SIZE_SCALE, (double)m_ModelCodeSizeEstimate / SIZE_SCALE));

    if (!shouldInline)
    {
        // Fail the inline
        if (m_IsPrejitRoot)
        {
            SetNever(InlineObservation::CALLEE_NOT_PROFITABLE_INLINE);
        }
        else
        {
            SetFailure(InlineObservation::CALLSITE_NOT_PROFITABLE_INLINE);
        }
    }
    else
    {
        // Update candidacy
        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_IS_PROFITABLE_INLINE);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_IS_PROFITABLE_INLINE);
        }
    }
}

#if defined(DEBUG) || defined(INLINE_DATA)

//------------------------------------------------------------------------/
// FullPolicy: construct a new FullPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

FullPolicy::FullPolicy(Compiler* compiler, bool isPrejitRoot) : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
}

//------------------------------------------------------------------------
// BudgetCheck: see if this inline would exceed the current budget
//
// Returns:
//   True if inline would exceed the budget.
//
bool FullPolicy::BudgetCheck() const
{
    // There are no budget restrictions for the full policy.
    //
    return false;
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void FullPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // Check depth

    unsigned depthLimit = m_RootCompiler->m_inlineStrategy->GetMaxInlineDepth();

    if (m_CallsiteDepth > depthLimit)
    {
        SetFailure(InlineObservation::CALLSITE_IS_TOO_DEEP);
        return;
    }

    // Check size

    unsigned sizeLimit = m_RootCompiler->m_inlineStrategy->GetMaxInlineILSize();

    if (m_CodeSize > sizeLimit)
    {
        SetFailure(InlineObservation::CALLEE_TOO_MUCH_IL);
        return;
    }

    // Otherwise, we're good to go

    if (m_IsPrejitRoot)
    {
        SetCandidate(InlineObservation::CALLEE_IS_PROFITABLE_INLINE);
    }
    else
    {
        SetCandidate(InlineObservation::CALLSITE_IS_PROFITABLE_INLINE);
    }

    return;
}

//------------------------------------------------------------------------/
// SizePolicy: construct a new SizePolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

SizePolicy::SizePolicy(Compiler* compiler, bool isPrejitRoot) : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void SizePolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // Do some homework
    MethodInfoObservations(methodInfo);
    EstimateCodeSize();

    // Does this inline increase the estimated size beyond
    // the original size estimate?
    const InlineStrategy* strategy    = m_RootCompiler->m_inlineStrategy;
    const int             initialSize = strategy->GetInitialSizeEstimate();
    const int             currentSize = strategy->GetCurrentSizeEstimate();
    const int             newSize     = currentSize + m_ModelCodeSizeEstimate;

    if (newSize <= initialSize)
    {
        // Estimated size impact is acceptable, so inline here.
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000, "Inline profitable, root size estimate %d is less than initial size %d\n",
                     newSize / SIZE_SCALE, initialSize / SIZE_SCALE));

        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_IS_SIZE_DECREASING_INLINE);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_IS_SIZE_DECREASING_INLINE);
        }
    }
    else
    {
        // Estimated size increase is too large, so no inline here.
        //
        // Note that we ought to reconsider this inline if we make
        // room in the budget by inlining a bunch of size decreasing
        // inlines after this one. But for now, we won't do this.
        if (m_IsPrejitRoot)
        {
            SetNever(InlineObservation::CALLEE_NOT_PROFITABLE_INLINE);
        }
        else
        {
            SetFailure(InlineObservation::CALLSITE_NOT_PROFITABLE_INLINE);
        }
    }

    return;
}

// Statics to track emission of the replay banner
// and provide file access to the inline xml

bool          ReplayPolicy::s_WroteReplayBanner = false;
FILE*         ReplayPolicy::s_ReplayFile        = nullptr;
CritSecObject ReplayPolicy::s_XmlReaderLock;

//------------------------------------------------------------------------/
// ReplayPolicy: construct a new ReplayPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

ReplayPolicy::ReplayPolicy(Compiler* compiler, bool isPrejitRoot)
    : DiscretionaryPolicy(compiler, isPrejitRoot)
    , m_InlineContext(nullptr)
    , m_Offset(BAD_IL_OFFSET)
    , m_WasForceInline(false)
{
    // Is there a log file open already? If so, we can use it.
    if (s_ReplayFile == nullptr)
    {
        // Did we already try and open and fail?
        if (!s_WroteReplayBanner)
        {
            // Nope, open it up.
            const WCHAR* replayFileName = JitConfig.JitInlineReplayFile();
            s_ReplayFile                = _wfopen(replayFileName, W("r"));

            // Display banner to stderr, unless we're dumping inline Xml,
            // in which case the policy name is captured in the Xml.
            if (JitConfig.JitInlineDumpXml() == 0)
            {
                fprintf(stderr, "*** %s inlines from %ws\n", s_ReplayFile == nullptr ? "Unable to replay" : "Replaying",
                        replayFileName);
            }

            s_WroteReplayBanner = true;
        }
    }
}

//------------------------------------------------------------------------
// ReplayPolicy: Finalize reading of inline Xml
//
// Notes:
//    Called during jitShutdown()

void ReplayPolicy::FinalizeXml()
{
    if (s_ReplayFile != nullptr)
    {
        fclose(s_ReplayFile);
        s_ReplayFile = nullptr;
    }
}

//------------------------------------------------------------------------
// FindMethod: find the root method in the inline Xml
//
// ReturnValue:
//    true if found. File position left pointing just after the
//    <Token> entry for the method.

bool ReplayPolicy::FindMethod()
{
    if (s_ReplayFile == nullptr)
    {
        return false;
    }

    // See if we've already found this method.
    InlineStrategy* inlineStrategy = m_RootCompiler->m_inlineStrategy;
    long            filePosition   = inlineStrategy->GetMethodXmlFilePosition();

    if (filePosition == -1)
    {
        // Past lookup failed
        return false;
    }
    else if (filePosition > 0)
    {
        // Past lookup succeeded, jump there
        fseek(s_ReplayFile, filePosition, SEEK_SET);
        return true;
    }

    // Else, scan the file. Might be nice to build an index
    // or something, someday.
    const mdMethodDef methodToken =
        m_RootCompiler->info.compCompHnd->getMethodDefFromMethod(m_RootCompiler->info.compMethodHnd);
    const unsigned methodHash = m_RootCompiler->info.compMethodHash();

    bool foundMethod = false;
    char buffer[256];
    fseek(s_ReplayFile, 0, SEEK_SET);

    while (!foundMethod)
    {
        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // Look for next method entry
        if (strstr(buffer, "<Method>") == nullptr)
        {
            continue;
        }

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // See if token matches
        unsigned token = 0;
        int      count = sscanf_s(buffer, " <Token>%08x</Token> ", &token);
        if ((count != 1) || (token != methodToken))
        {
            continue;
        }

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // See if hash matches
        unsigned hash = 0;
        count         = sscanf_s(buffer, " <Hash>%08x</Hash> ", &hash);
        if ((count != 1) || (hash != methodHash))
        {
            continue;
        }

        // Found a match...
        foundMethod = true;
        break;
    }

    // Update file position cache for this method
    long foundPosition = -1;

    if (foundMethod)
    {
        foundPosition = ftell(s_ReplayFile);
    }

    inlineStrategy->SetMethodXmlFilePosition(foundPosition);

    return foundMethod;
}

//------------------------------------------------------------------------
// FindContext: find an inline context in the inline Xml
//
// Notes:
//    Assumes file position within the relevant method has just been
//    set by a successful call to FindMethod().
//
// Arguments:
//    context -- context of interest
//
// ReturnValue:
//    true if found. File position left pointing just after the
//    <Token> entry for the context.

bool ReplayPolicy::FindContext(InlineContext* context)
{
    // Make sure we've found the parent context.
    if (context->IsRoot())
    {
        // We've already found the method context so we're good.
        return true;
    }

    bool foundParent = FindContext(context->GetParent());

    if (!foundParent)
    {
        return false;
    }

    // File pointer should be pointing at the parent context level.
    // See if we see an inline entry for this context.
    //
    // Token and Hash we're looking for.
    mdMethodDef contextToken  = m_RootCompiler->info.compCompHnd->getMethodDefFromMethod(context->GetCallee());
    unsigned    contextHash   = m_RootCompiler->compMethodHash(context->GetCallee());
    unsigned    contextOffset = (unsigned)context->GetLocation().GetOffset();

    return FindInline(contextToken, contextHash, contextOffset);
}

//------------------------------------------------------------------------
// FindInline: find entry for the current inline in inline Xml.
//
// Arguments:
//    token -- token describing the inline
//    hash  -- hash describing the inline
//    offset -- IL offset of the call site in the parent method
//
// ReturnValue:
//    true if the inline entry was found
//
// Notes:
//    Assumes file position has just been set by a successful call to
//    FindMethod or FindContext.
//
//    Token and Hash will not be sufficiently unique to identify a
//    particular inline, if there are multiple calls to the same
//    method.

bool ReplayPolicy::FindInline(unsigned token, unsigned hash, unsigned offset)
{
    char buffer[256];
    bool foundInline = false;
    int  depth       = 0;

    while (!foundInline)
    {
        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // If we hit </Method> we've gone too far,
        // and the XML is messed up.
        if (strstr(buffer, "</Method>") != nullptr)
        {
            break;
        }

        // Look for <Inlines />....
        if (strstr(buffer, "<Inlines />") != nullptr)
        {
            if (depth == 0)
            {
                // Exited depth 1, failed to find the context
                break;
            }
            else
            {
                // Exited nested, keep looking
                continue;
            }
        }

        // Look for <Inlines>....
        if (strstr(buffer, "<Inlines>") != nullptr)
        {
            depth++;
            continue;
        }

        // If we hit </Inlines> we've exited a nested entry
        // or the current entry.
        if (strstr(buffer, "</Inlines>") != nullptr)
        {
            depth--;

            if (depth == 0)
            {
                // Exited depth 1, failed to find the context
                break;
            }
            else
            {
                // Exited nested, keep looking
                continue;
            }
        }

        // Look for start of inline section at the right depth
        if ((depth != 1) || (strstr(buffer, "<Inline>") == nullptr))
        {
            continue;
        }

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // Match token
        unsigned inlineToken = 0;
        int      count       = sscanf_s(buffer, " <Token>%08x</Token> ", &inlineToken);

        if ((count != 1) || (inlineToken != token))
        {
            continue;
        }

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // Match hash
        unsigned inlineHash = 0;
        count               = sscanf_s(buffer, " <Hash>%08x</Hash> ", &inlineHash);

        if ((count != 1) || (inlineHash != hash))
        {
            continue;
        }

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) == nullptr)
        {
            break;
        }

        // Match offset
        unsigned inlineOffset = 0;
        count                 = sscanf_s(buffer, " <Offset>%u</Offset> ", &inlineOffset);
        if ((count != 1) || (inlineOffset != offset))
        {
            continue;
        }

        // Token,Hash,Offset may still not be unique enough, but it's
        // all we have right now.

        // We're good!
        foundInline = true;

        // Check for a data collection marker. This does not affect
        // matching...

        // Get next line
        if (fgets(buffer, sizeof(buffer), s_ReplayFile) != nullptr)
        {
            unsigned collectData = 0;
            count                = sscanf_s(buffer, " <CollectData>%u</CollectData> ", &collectData);

            if (count == 1)
            {
                m_IsDataCollectionTarget = (collectData == 1);
            }
        }

        break;
    }

    return foundInline;
}

//------------------------------------------------------------------------
// FindInline: find entry for a particular callee in inline Xml.
//
// Arguments:
//    callee -- handle for the callee method
//
// ReturnValue:
//    true if the inline should be performed.
//
// Notes:
//    Assumes file position has just been set by a successful call to
//    FindContext(...);
//
//    callee handle will not be sufficiently unique to identify a
//    particular inline, if there are multiple calls to the same
//    method.

bool ReplayPolicy::FindInline(CORINFO_METHOD_HANDLE callee)
{
    // Token and Hash we're looking for
    mdMethodDef calleeToken = m_RootCompiler->info.compCompHnd->getMethodDefFromMethod(callee);
    unsigned    calleeHash  = m_RootCompiler->compMethodHash(callee);

    // Abstract this or just pass through raw bits
    // See matching code in xml writer
    int offset = -1;
    if (m_Offset != BAD_IL_OFFSET)
    {
        offset = m_Offset;
    }

    unsigned calleeOffset = (unsigned)offset;

    bool foundInline = FindInline(calleeToken, calleeHash, calleeOffset);

    return foundInline;
}

//------------------------------------------------------------------------
// NoteBool: handle an observed boolean value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed
//
// Notes:
//    Overrides parent so Replay can control force inlines.

void ReplayPolicy::NoteBool(InlineObservation obs, bool value)
{
    // When inlining, let log override force inline.
    // Make a note of the actual value for later reporting during observations.
    if (!m_IsPrejitRoot && (obs == InlineObservation::CALLEE_IS_FORCE_INLINE))
    {
        m_WasForceInline = value;
        value            = false;
    }

    DiscretionaryPolicy::NoteBool(obs, value);
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void ReplayPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // TODO: handle prejit root case....need to record this in the
    // root method XML.
    if (m_IsPrejitRoot)
    {
        // Fall back to discretionary policy for now.
        return DiscretionaryPolicy::DetermineProfitability(methodInfo);
    }

    // If we're also dumping inline data, make additional observations
    // based on the method info, and estimate code size and perf
    // impact, so that the reports have the necessary data.
    if (JitConfig.JitInlineDumpData() != 0)
    {
        MethodInfoObservations(methodInfo);
        EstimateCodeSize();
        EstimatePerformanceImpact();
        m_IsForceInline = m_WasForceInline;
    }

    // Try and find this candidate in the Xml.
    // If we fail to find it, then don't inline.
    bool accept = false;

    // Grab the reader lock, since we'll be manipulating
    // the file pointer as we look for the relevant inline xml.
    {
        CritSecHolder readerLock(s_XmlReaderLock);

        // First, locate the entries for the root method.
        bool foundMethod = FindMethod();

        if (foundMethod && (m_InlineContext != nullptr))
        {
            // Next, navigate the context tree to find the entries
            // for the context that contains this candidate.
            bool foundContext = FindContext(m_InlineContext);

            if (foundContext)
            {
                // Finally, find this candidate within its context
                CORINFO_METHOD_HANDLE calleeHandle = methodInfo->ftn;
                accept                             = FindInline(calleeHandle);
            }
        }
    }

    if (accept)
    {
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Inline accepted via log replay"));

        if (m_IsPrejitRoot)
        {
            SetCandidate(InlineObservation::CALLEE_LOG_REPLAY_ACCEPT);
        }
        else
        {
            SetCandidate(InlineObservation::CALLSITE_LOG_REPLAY_ACCEPT);
        }
    }
    else
    {
        JITLOG_THIS(m_RootCompiler, (LL_INFO100000, "Inline rejected via log replay"));

        if (m_IsPrejitRoot)
        {
            SetNever(InlineObservation::CALLEE_LOG_REPLAY_REJECT);
        }
        else
        {
            SetFailure(InlineObservation::CALLSITE_LOG_REPLAY_REJECT);
        }
    }

    return;
}

#endif // defined(DEBUG) || defined(INLINE_DATA)
