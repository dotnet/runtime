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
    // For now, always create a Legacy policy.
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
            m_LooksLikeWrapperMethod = value;
            break;
        case InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST:
            m_ArgFeedsConstantTest = value;
            break;
        case InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK:
            m_ArgFeedsRangeCheck = value;
            break;
        case InlineObservation::CALLEE_IS_MOSTLY_LOAD_STORE:
            m_MethodIsMostlyLoadStore = value;
            break;
        case InlineObservation::CALLEE_HAS_SWITCH:
            // Pass this one on, it should cause inlining to fail.
            propagate = true;
            break;
        case InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST:
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
                    m_StateMachine = new (m_Compiler, CMK_Inlining) CodeSeqSM;
                    m_StateMachine->Start(m_Compiler);
                }
                break;
            }

        case InlineObservation::CALLEE_END_OPCODE_SCAN:
            {
                if (m_StateMachine != nullptr)
                {
                    m_StateMachine->End();
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
// noteInt: handle an observed integer value
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
            else if (m_CodeSize <= m_Compiler->getImpInlineSize())
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
            if (m_StateMachine != nullptr)
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

                m_StateMachine->Run(smOpcode DEBUGARG(0));
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

void LegacyPolicy::NoteDouble(InlineObservation obs, double value)
{
    // Ignore for now...
    (void) value;
    (void) obs;
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

    if (m_Compiler->compInlineStress())
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
    COMP_HANDLE comp = m_Compiler->info.compCompHnd;

    for (unsigned i = (hasThis ? 1 : 0);
         i < methInfo->args.totalILArgs();
         i++, argLst = comp->getArgNext(argLst))
    {
        var_types sigType = (var_types) m_Compiler->eeGetArgType(argLst, &methInfo->args);

        if (sigType == TYP_STRUCT)
        {
            typeInfo  verType  = m_Compiler->verParseArgSigToTypeInfo(&methInfo->args, argLst);

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

    const int calleeNativeSizeEstimate = DetermineNativeSizeEstimate();
    const int callsiteNativeSizeEstimate = DetermineCallsiteNativeSizeEstimate(methodInfo);
    const double multiplier = DetermineMultiplier();
    const int threshold = (int)(callsiteNativeSizeEstimate * multiplier);

    JITDUMP("calleeNativeSizeEstimate=%d\n", calleeNativeSizeEstimate)
    JITDUMP("callsiteNativeSizeEstimate=%d\n", callsiteNativeSizeEstimate);
    JITDUMP("benefit multiplier=%g\n", multiplier);
    JITDUMP("threshold=%d\n", threshold);

#if DEBUG
    // Size estimates are in bytes * 10
    const double sizeDescaler = 10.0;
#endif

    // Reject if callee size is over the threshold
    if (calleeNativeSizeEstimate > threshold)
    {
        // Inline appears to be unprofitable
        JITLOG_THIS(m_Compiler,
                    (LL_INFO100000,
                     "Native estimate for function size exceedsn threshold"
                     " for inlining %g > %g (multiplier = %g)\n",
                     calleeNativeSizeEstimate / sizeDescaler,
                     threshold / sizeDescaler,
                     multiplier));

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
        JITLOG_THIS(m_Compiler,
                    (LL_INFO100000,
                     "Native estimate for function size is within threshold"
                     " for inlining %g <= %g (multiplier = %g)\n",
                     calleeNativeSizeEstimate / sizeDescaler,
                     threshold / sizeDescaler,
                     multiplier));

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
