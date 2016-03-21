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

    // Optionally install the DiscretionaryPolicy.
    bool useDiscretionaryPolicy = JitConfig.JitInlinePolicyDiscretionary() != 0;

    if (useDiscretionaryPolicy)
    {
        return new (compiler, CMK_Inlining) DiscretionaryPolicy(compiler, isPrejitRoot);
    }

#endif // DEBUG

    // Use the legacy policy
    InlinePolicy* policy = new (compiler, CMK_Inlining) LegacyPolicy(compiler, isPrejitRoot);

    return policy;
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
// NoteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::NoteFatal(InlineObservation obs)
{
    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(InlGetImpact(obs) == InlineImpact::FATAL);
    NoteInternal(obs);
    assert(InlDecisionIsFailure(m_Decision));
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
            if (m_CodeSize <= ALWAYS_INLINE_SIZE)
            {
                // Candidate based on small size
                SetCandidate(InlineObservation::CALLEE_BELOW_ALWAYS_INLINE_SIZE);
            }
            else if (m_IsForceInline)
            {
                // Candidate based on force inline
                SetCandidate(InlineObservation::CALLEE_IS_FORCE_INLINE);
            }
            else if (m_CodeSize <= m_RootCompiler->getImpInlineSize())
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
// NoteInternal: helper for handling an observation
//
// Arguments:
//    obs      - the current obsevation

void LegacyPolicy::NoteInternal(InlineObservation obs)
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

void LegacyPolicy::SetFailure(InlineObservation obs)
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

void LegacyPolicy::SetNever(InlineObservation obs)
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

void LegacyPolicy::SetCandidate(InlineObservation obs)
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
// DetermineNativeCodeSizeEstimate: return estimated native code size for
// this inline candidate.
//
// Notes:
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
// DetermineNativeCallsiteSizeEstimate: estimate native size for the
// callsite.
//
// Arguments:
//    methInfo -- method info for the callee
//
// Notes:
//    Estimates the native size (in bytes, scaled up by 10x) for the
//    call site. While the quiality of the estimate here is questionable
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

    JITDUMP("calleeNativeSizeEstimate=%d\n", m_CalleeNativeSizeEstimate)
    JITDUMP("callsiteNativeSizeEstimate=%d\n", m_CallsiteNativeSizeEstimate);
    JITDUMP("benefit multiplier=%g\n", m_Multiplier);
    JITDUMP("threshold=%d\n", threshold);

#if DEBUG
    // Size estimates are in bytes * 10
    const double sizeDescaler = 10.0;
#endif

    // Reject if callee size is over the threshold
    if (m_CalleeNativeSizeEstimate > threshold)
    {
        // Inline appears to be unprofitable
        JITLOG_THIS(m_RootCompiler,
                    (LL_INFO100000,
                     "Native estimate for function size exceedsn threshold"
                     " for inlining %g > %g (multiplier = %g)\n",
                     m_CalleeNativeSizeEstimate / sizeDescaler,
                     threshold / sizeDescaler,
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
                     m_CalleeNativeSizeEstimate / sizeDescaler,
                     threshold / sizeDescaler,
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

#ifdef DEBUG

//------------------------------------------------------------------------
// RandomPolicy: construct a new RandomPolicy
//
// Arguments:
//    compiler -- compiler instance doing the inlining (root compiler)
//    isPrejitRoot -- true if this compiler is prejitting the root method
//    seed -- seed value for the random number generator

RandomPolicy::RandomPolicy(Compiler* compiler, bool isPrejitRoot, unsigned seed)
    : InlinePolicy(isPrejitRoot)
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
// NoteFatal: handle an observation with fatal impact
//
// Arguments:
//    obs      - the current obsevation

void RandomPolicy::NoteFatal(InlineObservation obs)
{
    // As a safeguard, all fatal impact must be
    // reported via noteFatal.
    assert(InlGetImpact(obs) == InlineImpact::FATAL);
    NoteInternal(obs);
    assert(InlDecisionIsFailure(m_Decision));
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
// NoteInternal: helper for handling an observation
//
// Arguments:
//    obs      - the current obsevation

void RandomPolicy::NoteInternal(InlineObservation obs)
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

void RandomPolicy::SetFailure(InlineObservation obs)
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

void RandomPolicy::SetNever(InlineObservation obs)
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

void RandomPolicy::SetCandidate(InlineObservation obs)
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
    , m_LocalCount(0)
    , m_ReturnType(CORINFO_TYPE_UNDEF)
    , m_ThrowCount(0)
    , m_CallCount(0)
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

    case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
        m_ArgFeedsConstantTest = value;

    case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
        m_ArgFeedsRangeCheck = value;
        break;

    default:
        LegacyPolicy::NoteBool(obs, value);
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
            OPCODE opcode = static_cast<OPCODE>(value);
            switch (opcode)
            {
            case CEE_THROW:
            case CEE_RETHROW:
                m_ThrowCount++;
                break;
            case CEE_CALL:
            case CEE_CALLI:
            case CEE_CALLVIRT:
                m_CallCount++;
                break;
            default:
                break;
            }

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

    if (!m_IsPrejitRoot &&
        (limit >= 0) && 
        (m_RootCompiler->getInlinedCount() >= static_cast<unsigned>(limit)))
    {
        SetFailure(InlineObservation::CALLSITE_OVER_INLINE_LIMIT);
        return;
    }

    // Make some additional observations
    m_ArgCount = methodInfo->args.numArgs;
    m_LocalCount = methodInfo->locals.numArgs;
    m_ReturnType = methodInfo->args.retType;

    // delegate to LegacyPolicy for now
    LegacyPolicy::DetermineProfitability(methodInfo);
}

//------------------------------------------------------------------------
// DumpSchema: dump names for all the supporting data for the
// inline decision in CSV format.

void DiscretionaryPolicy::DumpSchema() const
{
    printf(",Codesize");
    printf(",CallsiteFrequency");
    printf(",InstructionCount");
    printf(",LoadStoreCount");
    printf(",Depth");
    printf(",BlockCount");
    printf(",Maxstack");
    printf(",ArgCount");
    printf(",LocalCount");
    printf(",ReturnType");
    printf(",ThrowCount");
    printf(",CallCount");
    printf(",IsForceInline");
    printf(",IsInstanceCtor");
    printf(",IsFromPromotableValueClass");
    printf(",HasSimd");
    printf(",LooksLikeWrapperMethod");
    printf(",ArgFeedsConstantTest");
    printf(",IsMostlyLoadStore");
    printf(",ArgFeedsRangeCheck");
    printf(",ConstantFeedsConstantTest");
    printf(",CalleeNativeSizeEstimate");
    printf(",CallsiteNativeSizeEstimate");
}

//------------------------------------------------------------------------
// DumpData: dump all the supporting data for the inline decision
// in CSV format.

void DiscretionaryPolicy::DumpData() const
{
    printf(",%u", m_CodeSize);
    printf(",%u", m_CallsiteFrequency);
    printf(",%u", m_InstructionCount);
    printf(",%u", m_LoadStoreCount);
    printf(",%u", m_Depth);
    printf(",%u", m_BlockCount);
    printf(",%u", m_Maxstack);
    printf(",%u", m_ArgCount);
    printf(",%u", m_LocalCount);
    printf(",%u", m_ReturnType);
    printf(",%u", m_ThrowCount);
    printf(",%u", m_CallCount);
    printf(",%u", m_IsForceInline ? 1 : 0);
    printf(",%u", m_IsInstanceCtor ? 1 : 0);
    printf(",%u", m_IsFromPromotableValueClass ? 1 : 0);
    printf(",%u", m_HasSimd ? 1 : 0);
    printf(",%u", m_LooksLikeWrapperMethod ? 1 : 0);
    printf(",%u", m_ArgFeedsConstantTest ? 1 : 0);
    printf(",%u", m_MethodIsMostlyLoadStore ? 1 : 0);
    printf(",%u", m_ArgFeedsRangeCheck ? 1 : 0);
    printf(",%u", m_ConstantFeedsConstantTest ? 1 : 0);
    printf(",%d", m_CalleeNativeSizeEstimate);
    printf(",%d", m_CallsiteNativeSizeEstimate);
}

#endif // DEBUG
