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

InlinePolicy* InlinePolicy::GetPolicy(Compiler* compiler, bool isPrejitRoot)
{

#ifdef DEBUG

    // Optionally install the RandomPolicy.
    bool useRandomPolicy = compiler->compRandomInlineStress();

    if (useRandomPolicy)
    {
        unsigned seed = getJitStressLevel();
        assert(seed != 0);
        return new (compiler, CMK_Inlining) RandomPolicy(compiler, isPrejitRoot, seed);
    }

#endif // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)

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

    // Optionally install the ModelPolicy.
    bool useModelPolicy = JitConfig.JitInlinePolicyModel() != 0;

    if (useModelPolicy)
    {
        return new (compiler, CMK_Inlining) ModelPolicy(compiler, isPrejitRoot);
    }

    // Optionally install the DiscretionaryPolicy.
    bool useDiscretionaryPolicy = JitConfig.JitInlinePolicyDiscretionary() != 0;

    if (useDiscretionaryPolicy)
    {
        return new (compiler, CMK_Inlining) DiscretionaryPolicy(compiler, isPrejitRoot);
    }

#endif // defined(DEBUG) || defined(INLINE_DATA)

    // Use the legacy policy
    InlinePolicy* policy = new (compiler, CMK_Inlining) LegacyPolicy(compiler, isPrejitRoot);

    return policy;
}

//------------------------------------------------------------------------
// NoteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegalPolicy::NoteFatal(InlineObservation obs)
{
    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(InlGetImpact(obs) == InlineImpact::FATAL);
    NoteInternal(obs);
    assert(InlDecisionIsFailure(m_Decision));
}

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
        assert(m_IsPrejitRoot ||
               (obs == InlineObservation::CALLSITE_TOO_MANY_LOCALS));
        break;
    case InlineDecision::UNDECIDED:
    case InlineDecision::CANDIDATE:
        m_Decision = InlineDecision::FAILURE;
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
        m_Decision = InlineDecision::NEVER;
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
    m_Decision = InlineDecision::CANDIDATE;
    m_Observation = obs;
}

//------------------------------------------------------------------------
// NoteSuccess: handle finishing all the inlining checks successfully

void LegacyPolicy::NoteSuccess()
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
void LegacyPolicy::NoteBool(InlineObservation obs, bool value)
{
    // Check the impact
    InlineImpact impact = InlGetImpact(obs);

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
            assert(!m_IsForceInlineKnown || (m_IsForceInline == value));
            m_IsForceInline = value;
            m_IsForceInlineKnown = true;
            break;

        case InlineObservation::CALLEE_IS_INSTANCE_CTOR:
            m_IsInstanceCtor = value;
            break;

        case InlineObservation::CALLEE_CLASS_PROMOTABLE:
            m_IsFromPromotableValueClass = value;
            break;

        case InlineObservation::CALLEE_HAS_SIMD:
            m_HasSimd = value;
            break;

        case InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER:
            // LegacyPolicy ignores this for prejit roots.
            if (!m_IsPrejitRoot)
            {
                m_LooksLikeWrapperMethod = value;
            }
            break;

        case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
            // LegacyPolicy ignores this for prejit roots.
            if (!m_IsPrejitRoot)
            {
                m_ArgFeedsConstantTest = value;
            }
            break;

        case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
            // LegacyPolicy ignores this for prejit roots.
            if (!m_IsPrejitRoot)
            {
                m_ArgFeedsRangeCheck = value;
            }
            break;

        case InlineObservation::CALLEE_HAS_SWITCH:
        case InlineObservation::CALLEE_UNSUPPORTED_OPCODE:
        case InlineObservation::CALLEE_STORES_TO_ARGUMENT:
            // LegacyPolicy ignores these for prejit roots.
            if (!m_IsPrejitRoot)
            {
                // Pass these on, they should cause inlining to fail.
                propagate = true;
            }
            break;

        case InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST:
            // We shouldn't see this for a prejit root since
            // we don't know anything about callers.
            assert(!m_IsPrejitRoot);
            m_ConstantFeedsConstantTest = value;
            break;

        case InlineObservation::CALLEE_BEGIN_OPCODE_SCAN:
            {
                // Set up the state machine, if this inline is
                // discretionary and is still a candidate.
                if (InlDecisionIsCandidate(m_Decision)
                    && (m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE))
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
                    (((double)m_LoadStoreCount/(double)m_InstructionCount) > .90))
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

                if (!m_IsPrejitRoot)
                {
                    InlineStrategy* strategy = m_RootCompiler->m_inlineStrategy;
                    bool overBudget = strategy->BudgetCheck(m_CodeSize);
                    if (overBudget)
                    {
                        SetFailure(InlineObservation::CALLSITE_OVER_BUDGET);
                    }
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
        NoteInternal(obs);
    }
}

//------------------------------------------------------------------------
// NoteInt: handle an observed integer value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void LegacyPolicy::NoteInt(InlineObservation obs, int value)
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

            unsigned basicBlockCount = static_cast<unsigned>(value);

            if (!m_IsForceInline && (basicBlockCount > MAX_BASIC_BLOCKS))
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
            if (m_CodeSize <= InlineStrategy::ALWAYS_INLINE_SIZE)
            {
                // Candidate based on small size
                SetCandidate(InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE);
            }
            else if (m_IsForceInline)
            {
                // Candidate based on force inline
                SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
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
            unsigned depth = static_cast<unsigned>(value);

            if (depth > m_RootCompiler->m_inlineStrategy->GetMaxInlineDepth())
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
            if ((opcode >= CEE_LDARG_0  && opcode <= CEE_STLOC_S)  ||
                (opcode >= CEE_LDARG    && opcode <= CEE_STLOC)    ||
                (opcode >= CEE_LDNULL   && opcode <= CEE_LDC_R8)   ||
                (opcode >= CEE_LDIND_I1 && opcode <= CEE_STIND_R8) ||
                (opcode >= CEE_LDFLD    && opcode <= CEE_STOBJ)    ||
                (opcode >= CEE_LDELEMA  && opcode <= CEE_STELEM)   ||
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
// DetermineMultiplier: determine benefit multiplier for this inline
//
// Notes: uses the accumulated set of observations to compute a
// profitability boost for the inline candidate.

double LegacyPolicy::DetermineMultiplier()
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
        JITDUMP("\nInline candidate has SIMD type args, locals or return value.  Multiplier increased to %g.", multiplier);
    }

#endif // FEATURE_SIMD

    if (m_LooksLikeWrapperMethod)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate looks like a wrapper method.  Multiplier increased to %g.", multiplier);
    }

    if (m_ArgFeedsConstantTest)
    {
        multiplier += 1.0;
        JITDUMP("\nInline candidate has an arg that feeds a constant test.  Multiplier increased to %g.", multiplier);
    }

    if (m_MethodIsMostlyLoadStore)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate is mostly loads and stores.  Multiplier increased to %g.", multiplier);
    }

    if (m_ArgFeedsRangeCheck)
    {
        multiplier += 0.5;
        JITDUMP("\nInline candidate has arg that feeds range check.  Multiplier increased to %g.", multiplier);
    }

    if (m_ConstantFeedsConstantTest)
    {
        multiplier += 3.0;
        JITDUMP("\nInline candidate has const arg that feeds a conditional.  Multiplier increased to %g.", multiplier);
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
        JITDUMP("\nmultiplier increased via JitInlineAdditonalMultiplier=%d to %g.", additionalMultiplier, multiplier);
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

int LegacyPolicy::DetermineNativeSizeEstimate()
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

int LegacyPolicy::DetermineCallsiteNativeSizeEstimate(CORINFO_METHOD_INFO* methInfo)
{
    int callsiteSize = 55;   // Direct call take 5 native bytes; indirect call takes 6 native bytes.

    bool hasThis = methInfo->args.hasThis();

    if (hasThis)
    {
        callsiteSize += 30;  // "mov" or "lea"
    }

    CORINFO_ARG_LIST_HANDLE argLst = methInfo->args.args;
    COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;

    for (unsigned i = (hasThis ? 1 : 0);
         i < methInfo->args.totalILArgs();
         i++, argLst = comp->getArgNext(argLst))
    {
        var_types sigType = (var_types) m_RootCompiler->eeGetArgType(argLst, &methInfo->args);

        if (sigType == TYP_STRUCT)
        {
            typeInfo  verType  = m_RootCompiler->verParseArgSigToTypeInfo(&methInfo->args, argLst);

            /*

            IN0028: 00009B      lea     EAX, bword ptr [EBP-14H]
            IN0029: 00009E      push    dword ptr [EAX+4]
            IN002a: 0000A1      push    gword ptr [EAX]
            IN002b: 0000A3      call    [MyStruct.staticGetX2(struct):int]

            */

            callsiteSize += 10; // "lea     EAX, bword ptr [EBP-14H]"

            // NB sizeof (void*) fails to convey intent when cross-jitting.

            unsigned opsz = (unsigned)(roundUp(comp->getClassSize(verType.GetClassHandle()), sizeof(void*)));
            unsigned slots = opsz / sizeof(void*);

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

void LegacyPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    assert(InlDecisionIsCandidate(m_Decision));
    assert(m_Observation == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);

    m_CalleeNativeSizeEstimate = DetermineNativeSizeEstimate();
    m_CallsiteNativeSizeEstimate = DetermineCallsiteNativeSizeEstimate(methodInfo);
    m_Multiplier = DetermineMultiplier();
    const int threshold = (int)(m_CallsiteNativeSizeEstimate * m_Multiplier);

    // Note the LegacyPolicy estimates are scaled up by SIZE_SCALE
    JITDUMP("\ncalleeNativeSizeEstimate=%d\n", m_CalleeNativeSizeEstimate)
    JITDUMP("callsiteNativeSizeEstimate=%d\n", m_CallsiteNativeSizeEstimate);
    JITDUMP("benefit multiplier=%g\n", m_Multiplier);
    JITDUMP("threshold=%d\n", threshold);

    // Reject if callee size is over the threshold
    if (m_CalleeNativeSizeEstimate > threshold)
    {
        // Inline appears to be unprofitable
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000,
                     "Native estimate for function size exceeds threshold"
                     " for inlining %g > %g (multiplier = %g)\n",
                     (double) m_CalleeNativeSizeEstimate / SIZE_SCALE,
                     (double) threshold / SIZE_SCALE,
                     m_Multiplier));

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
                    (LL_INFO100000,
                     "Native estimate for function size is within threshold"
                     " for inlining %g <= %g (multiplier = %g)\n",
                     (double) m_CalleeNativeSizeEstimate / SIZE_SCALE,
                     (double) threshold / SIZE_SCALE,
                     m_Multiplier));

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

int LegacyPolicy::CodeSizeEstimate()
{
    if (m_StateMachine != nullptr)
    {
        // This is not something the LegacyPolicy explicitly computed,
        // since it uses a blended evaluation model (mixing size and time
        // together for overall profitability). But it's effecitvely an
        // estimate of the size impact.
        return (m_CalleeNativeSizeEstimate - m_CallsiteNativeSizeEstimate);
    }
    else
    {
        return 0;
    }
}

#ifdef DEBUG

//------------------------------------------------------------------------
// RandomPolicy: construct a new RandomPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method
//    seed -- seed value for the random number generator

RandomPolicy::RandomPolicy(Compiler* compiler, bool isPrejitRoot, unsigned seed)
    : LegalPolicy(isPrejitRoot)
    , m_RootCompiler(compiler)
    , m_Random(nullptr)
    , m_CodeSize(0)
    , m_IsForceInline(false)
    , m_IsForceInlineKnown(false)
{
    // If necessary, setup and seed the random state.
    if (compiler->inlRNG == nullptr)
    {
        compiler->inlRNG = new (compiler, CMK_Inlining) CLRRandom();

        unsigned hash = m_RootCompiler->info.compMethodHash();
        assert(hash != 0);
        assert(seed != 0);
        int hashSeed = static_cast<int>(hash ^ seed);
        compiler->inlRNG->Init(hashSeed);
    }

    m_Random = compiler->inlRNG;
}

//------------------------------------------------------------------------
// NoteSuccess: handle finishing all the inlining checks successfully

void RandomPolicy::NoteSuccess()
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
void RandomPolicy::NoteBool(InlineObservation obs, bool value)
{
    // Check the impact
    InlineImpact impact = InlGetImpact(obs);

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
            // The RandomPolicy still honors force inlines.
            //
            // We may make the force-inline observation more than
            // once.  All observations should agree.
            assert(!m_IsForceInlineKnown || (m_IsForceInline == value));
            m_IsForceInline = value;
            m_IsForceInlineKnown = true;
            break;

        case InlineObservation::CALLEE_HAS_SWITCH:
        case InlineObservation::CALLEE_UNSUPPORTED_OPCODE:
        case InlineObservation::CALLEE_STORES_TO_ARGUMENT:
            // Pass these on, they should cause inlining to fail.
            propagate = true;
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
        // Ignore all other information
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

    // Use a probability curve that roughly matches the observed
    // behavior of the LegacyPolicy. That way we're inlining
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

#endif // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)

#ifdef _MSC_VER
// Disable warning about new array member initialization behavior
#pragma warning( disable : 4351 )
#endif

//------------------------------------------------------------------------
// DiscretionaryPolicy: construct a new DiscretionaryPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

DiscretionaryPolicy::DiscretionaryPolicy(Compiler* compiler, bool isPrejitRoot)
    : LegacyPolicy(compiler, isPrejitRoot)
    , m_Depth(0)
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
    , m_CallCount(0)
    , m_ModelCodeSizeEstimate(0)
{
    // Empty
}

//------------------------------------------------------------------------
// NoteBool: handle an observed boolean value
//
// Arguments:
//    obs      - the current obsevation
//    value    - the value being observed

void DiscretionaryPolicy::NoteBool(InlineObservation obs, bool value)
{
    switch(obs)
    {
    case InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER:
        m_LooksLikeWrapperMethod = value;
        break;

    case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
        m_ArgFeedsConstantTest = value;
        break;

    case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
        m_ArgFeedsRangeCheck = value;
        break;

    default:
        LegacyPolicy::NoteBool(obs, value);
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
            LegacyPolicy::NoteInt(obs, value);
            break;
        }

    case InlineObservation::CALLEE_MAXSTACK:
        m_Maxstack = value;
        break;

    case InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS:
        m_BlockCount = value;
        break;

    case InlineObservation::CALLSITE_DEPTH:
        m_Depth = value;
        break;

    default:
        // Delegate remainder to the LegacyPolicy.
        LegacyPolicy::NoteInt(obs, value);
        break;
    }
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
    // was viable but unprofitable.
    bool propagate = (m_Observation != InlineObservation::CALLEE_NOT_PROFITABLE_INLINE);

    return propagate;
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void DiscretionaryPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // Punt if we're inlining and we've reached the acceptance limit.
    int limit = JitConfig.JitInlineLimit();
    unsigned current = m_RootCompiler->m_inlineStrategy->GetInlineCount();

    if (!m_IsPrejitRoot &&
        (limit >= 0) &&
        (current >= static_cast<unsigned>(limit)))
    {
        SetFailure(InlineObservation::CALLSITE_OVER_INLINE_LIMIT);
        return;
    }

    // Make additional observations based on the method info
    MethodInfoObservations(methodInfo);

    // Estimate the code size impact. This is just for model
    // evaluation purposes -- we'll still use the legacy policy's
    // model for actual inlining.
    EstimateCodeSize();

    // Delegate to LegacyPolicy for the rest
    LegacyPolicy::DetermineProfitability(methodInfo);
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
    m_LocalCount = locals.numArgs;

    CORINFO_SIG_INFO& args = methodInfo->args;
    const unsigned argCount = args.numArgs;
    m_ArgCount = argCount;

    const unsigned pointerSize = sizeof(void*);
    unsigned i = 0;

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

    unsigned j = 0;
    CORINFO_ARG_LIST_HANDLE argListHandle = args.args;
    COMP_HANDLE comp = m_RootCompiler->info.compCompHnd;

    while ((i < MAX_ARGS) && (j < argCount))
    {
        CORINFO_CLASS_HANDLE classHandle;
        CorInfoType type = strip(comp->getArgType(&args, argListHandle, &classHandle));

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
// On the inlines in CoreCLR's mscorlib, release windows x64, this
// yields scores of R=0.42, MSE=228, and MAE=7.25.
//
// This estimate can be improved slighly by refitting, resulting in
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
         -7.901 * m_ConstantFeedsConstantTest +
          0.065 * m_CalleeNativeSizeEstimate;

    // Scaled up and reported as an integer value.
    m_ModelCodeSizeEstimate = (int) (SIZE_SCALE * sizeEstimate);
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

//------------------------------------------------------------------------
// DumpSchema: dump names for all the supporting data for the
// inline decision in CSV format.
//
// Arguments:
//    file -- file to write to

void DiscretionaryPolicy::DumpSchema(FILE* file) const
{
    fprintf(file, ",ILSize");
    fprintf(file, ",CallsiteFrequency");
    fprintf(file, ",InstructionCount");
    fprintf(file, ",LoadStoreCount");
    fprintf(file, ",Depth");
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
    fprintf(file, ",CallCount");
    fprintf(file, ",IsForceInline");
    fprintf(file, ",IsInstanceCtor");
    fprintf(file, ",IsFromPromotableValueClass");
    fprintf(file, ",HasSimd");
    fprintf(file, ",LooksLikeWrapperMethod");
    fprintf(file, ",ArgFeedsConstantTest");
    fprintf(file, ",IsMostlyLoadStore");
    fprintf(file, ",ArgFeedsRangeCheck");
    fprintf(file, ",ConstantFeedsConstantTest");
    fprintf(file, ",CalleeNativeSizeEstimate");
    fprintf(file, ",CallsiteNativeSizeEstimate");
    fprintf(file, ",ModelCodeSizeEstimate");
}

//------------------------------------------------------------------------
// DumpData: dump all the supporting data for the inline decision
// in CSV format.
//
// Arguments:
//    file -- file to write to

void DiscretionaryPolicy::DumpData(FILE* file) const
{
    fprintf(file, ",%u", m_CodeSize);
    fprintf(file, ",%u", m_CallsiteFrequency);
    fprintf(file, ",%u", m_InstructionCount);
    fprintf(file, ",%u", m_LoadStoreCount);
    fprintf(file, ",%u", m_Depth);
    fprintf(file, ",%u", m_BlockCount);
    fprintf(file, ",%u", m_Maxstack);
    fprintf(file, ",%u", m_ArgCount);

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",%u", m_ArgType[i]);
    }

    for (unsigned i = 0; i < MAX_ARGS; i++)
    {
        fprintf(file, ",%u", (unsigned) m_ArgSize[i]);
    }

    fprintf(file, ",%u", m_LocalCount);
    fprintf(file, ",%u", m_ReturnType);
    fprintf(file, ",%u", (unsigned) m_ReturnSize);
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
    fprintf(file, ",%u", m_ThrowCount);
    fprintf(file, ",%u", m_CallCount);
    fprintf(file, ",%u", m_IsForceInline ? 1 : 0);
    fprintf(file, ",%u", m_IsInstanceCtor ? 1 : 0);
    fprintf(file, ",%u", m_IsFromPromotableValueClass ? 1 : 0);
    fprintf(file, ",%u", m_HasSimd ? 1 : 0);
    fprintf(file, ",%u", m_LooksLikeWrapperMethod ? 1 : 0);
    fprintf(file, ",%u", m_ArgFeedsConstantTest ? 1 : 0);
    fprintf(file, ",%u", m_MethodIsMostlyLoadStore ? 1 : 0);
    fprintf(file, ",%u", m_ArgFeedsRangeCheck ? 1 : 0);
    fprintf(file, ",%u", m_ConstantFeedsConstantTest ? 1 : 0);
    fprintf(file, ",%d", m_CalleeNativeSizeEstimate);
    fprintf(file, ",%d", m_CallsiteNativeSizeEstimate);
    fprintf(file, ",%d", m_ModelCodeSizeEstimate);
}

//------------------------------------------------------------------------/
// ModelPolicy: construct a new ModelPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

ModelPolicy::ModelPolicy(Compiler* compiler, bool isPrejitRoot)
    : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
}

//------------------------------------------------------------------------
// DetermineProfitability: determine if this inline is profitable
//
// Arguments:
//    methodInfo -- method info for the callee

void ModelPolicy::DetermineProfitability(CORINFO_METHOD_INFO* methodInfo)
{
    // Do some homework
    MethodInfoObservations(methodInfo);
    EstimateCodeSize();

    // Preliminary inline model.
    //
    // If code size is estimated to increase, look at
    // the profitability model for guidance.
    //
    // If code size will decrease, just inline.

    if (m_ModelCodeSizeEstimate <= 0)
    {
        // Inline will likely decrease code size
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000,
                     "Inline profitable, will decrease code size by %g bytes\n",
                     (double) -m_ModelCodeSizeEstimate / SIZE_SCALE));

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
        // This is a very crude profitability model, based on what
        // the LegacyPolicy does. It will be updated over time.
        m_Multiplier = DetermineMultiplier();
        double benefit = SIZE_SCALE * (m_Multiplier / m_ModelCodeSizeEstimate);
        double threshold = 0.25;
        bool shouldInline = (benefit > threshold);

        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000,
                     "Inline %s profitable: benefit=%g (mult=%g / size=%d)\n",
                     shouldInline ? "is" : "is not",
                     benefit, m_Multiplier, (double) m_ModelCodeSizeEstimate / SIZE_SCALE));
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
// FullPolicy: construct a new FullPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method

FullPolicy::FullPolicy(Compiler* compiler, bool isPrejitRoot)
    : DiscretionaryPolicy(compiler, isPrejitRoot)
{
    // Empty
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

    if (m_Depth > depthLimit) 
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

SizePolicy::SizePolicy(Compiler* compiler, bool isPrejitRoot)
    : DiscretionaryPolicy(compiler, isPrejitRoot)
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
    const InlineStrategy* strategy = m_RootCompiler->m_inlineStrategy;
    const int initialSize = strategy->GetInitialSizeEstimate();
    const int currentSize = strategy->GetCurrentSizeEstimate();
    const int newSize     = currentSize + m_ModelCodeSizeEstimate;

    if (newSize <= initialSize)
    {
        // Estimated size impact is acceptable, so inline here.
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000,
                     "Inline profitable, root size estimate %d is less than initial size %d\n",
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

#endif // defined(DEBUG) || defined(INLINE_DATA)
