// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_completion_counter.h"

#include <utility>

LxrLazyCompletionRegistration::LxrLazyCompletionRegistration()
    : m_state(nullptr),
      m_generation(0),
      m_kind(LxrCompletionWorkKind::Overall),
      m_active(false)
{
}

LxrLazyCompletionRegistration::LxrLazyCompletionRegistration(
    LxrLazyCompletionGenerationState* state,
    uint64_t generation,
    LxrCompletionWorkKind kind)
    : m_state(state),
      m_generation(generation),
      m_kind(kind),
      m_active(true)
{
}

LxrLazyCompletionRegistration::~LxrLazyCompletionRegistration()
{
    Complete();
}

LxrLazyCompletionRegistration::LxrLazyCompletionRegistration(
    LxrLazyCompletionRegistration&& other) noexcept
    : LxrLazyCompletionRegistration()
{
    MoveFrom(std::move(other));
}

LxrLazyCompletionRegistration& LxrLazyCompletionRegistration::operator=(
    LxrLazyCompletionRegistration&& other) noexcept
{
    if (this != &other)
    {
        Complete();
        MoveFrom(std::move(other));
    }
    return *this;
}

void LxrLazyCompletionRegistration::MoveFrom(
    LxrLazyCompletionRegistration&& other)
{
    m_state = other.m_state;
    m_generation = other.m_generation;
    m_kind = other.m_kind;
    m_active = other.m_active;
    other.m_state = nullptr;
    other.m_generation = 0;
    other.m_kind = LxrCompletionWorkKind::Overall;
    other.m_active = false;
}

LxrCompletionResult LxrLazyCompletionRegistration::Complete()
{
    if (!m_active)
    {
        return LxrCompletionResult::AlreadyCompleted;
    }
    if (m_state == nullptr)
    {
        m_active = false;
        return LxrCompletionResult::InvalidState;
    }

    return m_state->Complete(this);
}

LxrCompletionResult LxrLazyCompletionRegistration::RegisterChild(
    LxrCompletionWorkKind kind,
    LxrLazyCompletionRegistration* registration) const
{
    return m_state != nullptr
        ? m_state->RegisterChild(*this, kind, registration)
        : LxrCompletionResult::InvalidState;
}

LxrCompletionResult LxrLazyCompletionRegistration::CreateReleaseGuard(
    LxrBlockReleaseGuard* guard) const
{
    return m_state != nullptr
        ? m_state->CreateReleaseGuard(*this, guard)
        : LxrCompletionResult::InvalidState;
}

bool LxrLazyCompletionRegistration::IsActive() const
{
    return m_active;
}

uint64_t LxrLazyCompletionRegistration::GetGeneration() const
{
    return m_generation;
}

LxrCompletionWorkKind LxrLazyCompletionRegistration::GetKind() const
{
    return m_kind;
}

LxrBlockReleaseGuard::LxrBlockReleaseGuard()
    : m_state(nullptr),
      m_generation(0)
{
}

LxrBlockReleaseGuard::LxrBlockReleaseGuard(
    const LxrLazyCompletionGenerationState* state,
    uint64_t generation)
    : m_state(state),
      m_generation(generation)
{
}

bool LxrBlockReleaseGuard::IsValid() const
{
    return m_state != nullptr && m_state->IsReleaseAllowed(m_generation);
}

uint64_t LxrBlockReleaseGuard::GetGeneration() const
{
    return m_generation;
}

LxrMatureSweepContinuation::LxrMatureSweepContinuation(
    LxrLazyCompletionRegistration&& registration)
    : m_registration(std::move(registration))
{
}

LxrMatureSweepContinuation::LxrMatureSweepContinuation(
    LxrMatureSweepContinuation&& other) noexcept
    : m_registration(std::move(other.m_registration))
{
}

LxrMatureSweepContinuation& LxrMatureSweepContinuation::operator=(
    LxrMatureSweepContinuation&& other) noexcept
{
    if (this != &other)
    {
        m_registration = std::move(other.m_registration);
    }
    return *this;
}

LxrCompletionResult LxrMatureSweepContinuation::RegisterSweep(
    LxrLazyCompletionRegistration* registration)
{
    if (!m_registration.IsActive() || m_registration.m_state == nullptr)
    {
        return LxrCompletionResult::InvalidState;
    }

    return m_registration.m_state->RegisterChild(
        m_registration,
        LxrCompletionWorkKind::MatureSweep,
        registration);
}

uint64_t LxrMatureSweepContinuation::GetGeneration() const
{
    return m_registration.GetGeneration();
}

LxrLazyCompletionGenerationState::LxrLazyCompletionGenerationState()
    : m_generation(0),
      m_overall_outstanding(0),
      m_decrement_outstanding(0),
      m_registrations(0),
      m_completions(0),
      m_decrement_zero_transitions(0),
      m_overall_zero_transitions(0),
      m_decrement_callbacks(0),
      m_overall_callbacks(0),
      m_ownership_violations(0),
      m_lifecycle(LxrCompletionLifecycle::Uninitialized),
      m_decrement_callback_result(LxrCompletionCallbackResult::Success),
      m_overall_callback_result(LxrCompletionCallbackResult::Success),
      m_callback_running(false),
      m_decrements_drained(nullptr),
      m_overall_drained(nullptr),
      m_callback_context(nullptr),
      m_maximum_outstanding(UINT64_MAX)
{
    static_assert(std::atomic<uint64_t>::is_always_lock_free);
}

LxrCompletionResult LxrLazyCompletionGenerationState::Initialize(
    uint64_t generation,
    LxrDecrementsDrainedCallback decrementsDrained,
    LxrOverallDrainedCallback overallDrained,
    void* callbackContext,
    LxrLazyCompletionRegistration* seed,
    uint64_t maximumOutstanding)
{
    if (generation == 0 ||
        decrementsDrained == nullptr ||
        overallDrained == nullptr ||
        seed == nullptr ||
        seed->IsActive() ||
        maximumOutstanding == 0)
    {
        return LxrCompletionResult::InvalidArgument;
    }
    if (m_lifecycle.load(std::memory_order_seq_cst) !=
            LxrCompletionLifecycle::Uninitialized ||
        m_overall_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_decrement_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_callback_running.load(std::memory_order_seq_cst))
    {
        return LxrCompletionResult::InvalidState;
    }

    m_decrements_drained = decrementsDrained;
    m_overall_drained = overallDrained;
    m_callback_context = callbackContext;
    m_maximum_outstanding = maximumOutstanding;
    m_registrations.store(1, std::memory_order_seq_cst);
    m_completions.store(0, std::memory_order_seq_cst);
    m_decrement_zero_transitions.store(0, std::memory_order_seq_cst);
    m_overall_zero_transitions.store(0, std::memory_order_seq_cst);
    m_decrement_callbacks.store(0, std::memory_order_seq_cst);
    m_overall_callbacks.store(0, std::memory_order_seq_cst);
    m_ownership_violations.store(0, std::memory_order_seq_cst);
    m_decrement_callback_result.store(
        LxrCompletionCallbackResult::Success,
        std::memory_order_seq_cst);
    m_overall_callback_result.store(
        LxrCompletionCallbackResult::Success,
        std::memory_order_seq_cst);
    m_overall_outstanding.store(1, std::memory_order_seq_cst);
    m_decrement_outstanding.store(1, std::memory_order_seq_cst);
    m_generation.store(generation, std::memory_order_seq_cst);
    m_lifecycle.store(
        LxrCompletionLifecycle::Registering,
        std::memory_order_seq_cst);
    *seed = LxrLazyCompletionRegistration(
        this,
        generation,
        LxrCompletionWorkKind::Decrement);
    return LxrCompletionResult::Success;
}

LxrCompletionResult LxrLazyCompletionGenerationState::Seal(uint64_t generation)
{
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }

    LxrCompletionLifecycle expected = LxrCompletionLifecycle::Registering;
    return m_lifecycle.compare_exchange_strong(
        expected,
        LxrCompletionLifecycle::Published,
        std::memory_order_seq_cst,
        std::memory_order_seq_cst)
        ? LxrCompletionResult::Success
        : LxrCompletionResult::InvalidState;
}

LxrCompletionResult LxrLazyCompletionGenerationState::MarkPublished(
    uint64_t generation)
{
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }
    return m_lifecycle.load(std::memory_order_seq_cst) ==
        LxrCompletionLifecycle::Published
        ? LxrCompletionResult::Success
        : LxrCompletionResult::InvalidState;
}

LxrCompletionResult LxrLazyCompletionGenerationState::RegisterRoot(
    uint64_t generation,
    LxrCompletionWorkKind kind,
    LxrLazyCompletionRegistration* registration)
{
    if (kind == LxrCompletionWorkKind::MatureSweep)
    {
        return LxrCompletionResult::InvalidArgument;
    }
    return RegisterInternal(generation, kind, true, registration);
}

LxrCompletionResult LxrLazyCompletionGenerationState::RegisterChild(
    const LxrLazyCompletionRegistration& parent,
    LxrCompletionWorkKind kind,
    LxrLazyCompletionRegistration* registration)
{
    if (!parent.m_active ||
        parent.m_state != this ||
        parent.m_generation != m_generation.load(std::memory_order_seq_cst))
    {
        return LxrCompletionResult::StaleGeneration;
    }
    if ((kind == LxrCompletionWorkKind::Decrement &&
            parent.m_kind != LxrCompletionWorkKind::Decrement) ||
        (kind == LxrCompletionWorkKind::MatureSweep &&
            parent.m_kind != LxrCompletionWorkKind::MatureSweep))
    {
        return LxrCompletionResult::InvalidArgument;
    }

    return RegisterInternal(parent.m_generation, kind, false, registration);
}

LxrCompletionResult LxrLazyCompletionGenerationState::RegisterInternal(
    uint64_t generation,
    LxrCompletionWorkKind kind,
    bool root,
    LxrLazyCompletionRegistration* registration)
{
    if (registration == nullptr ||
        registration->IsActive() ||
        kind > LxrCompletionWorkKind::MatureSweep)
    {
        return LxrCompletionResult::InvalidArgument;
    }
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }

    LxrCompletionLifecycle lifecycle =
        m_lifecycle.load(std::memory_order_seq_cst);
    if (root)
    {
        if (lifecycle != LxrCompletionLifecycle::Registering)
        {
            return LxrCompletionResult::InvalidState;
        }
    }
    else if (!IsRegistrationLifecycle(lifecycle))
    {
        return lifecycle == LxrCompletionLifecycle::Cancelling ||
                lifecycle == LxrCompletionLifecycle::Cancelled
            ? LxrCompletionResult::Cancelled
            : LxrCompletionResult::InvalidState;
    }
    if (kind == LxrCompletionWorkKind::Decrement &&
        m_decrement_outstanding.load(std::memory_order_seq_cst) == 0)
    {
        return LxrCompletionResult::InvalidState;
    }

    if (kind == LxrCompletionWorkKind::Decrement)
    {
        LxrCompletionResult result = IncrementChecked(&m_decrement_outstanding);
        if (result != LxrCompletionResult::Success)
        {
            return result;
        }
    }

    LxrCompletionResult result = IncrementChecked(&m_overall_outstanding);
    if (result != LxrCompletionResult::Success)
    {
        if (kind == LxrCompletionWorkKind::Decrement)
        {
            uint64_t previous = 0;
            DecrementChecked(&m_decrement_outstanding, &previous);
        }
        return result;
    }

    m_registrations.fetch_add(1, std::memory_order_seq_cst);
    *registration = LxrLazyCompletionRegistration(this, generation, kind);
    return LxrCompletionResult::Success;
}

LxrCompletionResult LxrLazyCompletionGenerationState::Complete(
    LxrLazyCompletionRegistration* registration)
{
    if (registration == nullptr || !registration->m_active)
    {
        return LxrCompletionResult::AlreadyCompleted;
    }
    if (registration->m_state != this ||
        registration->m_generation != m_generation.load(std::memory_order_seq_cst))
    {
        registration->m_active = false;
        RecordOwnershipViolation();
        return LxrCompletionResult::StaleGeneration;
    }

    uint64_t generation = registration->m_generation;
    LxrCompletionWorkKind kind = registration->m_kind;
    registration->m_active = false;
    registration->m_state = nullptr;
    registration->m_generation = 0;

    LxrCompletionResult result = LxrCompletionResult::Success;
    if (kind == LxrCompletionWorkKind::Decrement)
    {
        uint64_t decrementPrevious = 0;
        result = DecrementChecked(&m_decrement_outstanding, &decrementPrevious);
        if (result != LxrCompletionResult::Success)
        {
            RecordOwnershipViolation();
            return result;
        }

        if (decrementPrevious == 1)
        {
            m_decrement_zero_transitions.fetch_add(1, std::memory_order_seq_cst);
            LxrLazyCompletionRegistration continuationRegistration;
            result = RegisterInternal(
                generation,
                LxrCompletionWorkKind::MatureSweep,
                false,
                &continuationRegistration);
            if (result != LxrCompletionResult::Success)
            {
                m_lifecycle.store(
                    LxrCompletionLifecycle::Failed,
                    std::memory_order_seq_cst);
            }
            else
            {
                m_lifecycle.store(
                    LxrCompletionLifecycle::Draining,
                    std::memory_order_seq_cst);
                LxrMatureSweepContinuation continuation(
                    std::move(continuationRegistration));
                m_callback_running.store(true, std::memory_order_seq_cst);
                m_decrement_callbacks.fetch_add(1, std::memory_order_seq_cst);
                LxrCompletionCallbackResult callbackResult =
                    m_decrements_drained(&continuation, m_callback_context);
                m_decrement_callback_result.store(
                    callbackResult,
                    std::memory_order_seq_cst);
                if (callbackResult != LxrCompletionCallbackResult::Success)
                {
                    m_lifecycle.store(
                        LxrCompletionLifecycle::Failed,
                        std::memory_order_seq_cst);
                    result = LxrCompletionResult::CallbackFailed;
                }
                m_callback_running.store(false, std::memory_order_seq_cst);
            }
        }
    }

    LxrCompletionResult overallResult = CompleteOverall(generation);
    m_completions.fetch_add(1, std::memory_order_seq_cst);
    if (result != LxrCompletionResult::Success)
    {
        return result;
    }
    return overallResult;
}

LxrCompletionResult LxrLazyCompletionGenerationState::CompleteOverall(
    uint64_t generation)
{
    uint64_t previous = 0;
    LxrCompletionResult result =
        DecrementChecked(&m_overall_outstanding, &previous);
    if (result != LxrCompletionResult::Success)
    {
        RecordOwnershipViolation();
        return result;
    }
    if (previous != 1)
    {
        return LxrCompletionResult::Success;
    }

    m_overall_zero_transitions.fetch_add(1, std::memory_order_seq_cst);
    LxrCompletionLifecycle lifecycle =
        m_lifecycle.load(std::memory_order_seq_cst);
    if (lifecycle == LxrCompletionLifecycle::Cancelling ||
        lifecycle == LxrCompletionLifecycle::Cancelled)
    {
        m_lifecycle.store(
            LxrCompletionLifecycle::Cancelled,
            std::memory_order_seq_cst);
        return LxrCompletionResult::Cancelled;
    }
    if (lifecycle == LxrCompletionLifecycle::Failed)
    {
        return LxrCompletionResult::CallbackFailed;
    }

    m_lifecycle.store(
        LxrCompletionLifecycle::DecisionRunning,
        std::memory_order_seq_cst);
    m_callback_running.store(true, std::memory_order_seq_cst);
    m_overall_callbacks.fetch_add(1, std::memory_order_seq_cst);
    LxrCompletionCallbackResult callbackResult =
        m_overall_drained(generation, m_callback_context);
    m_overall_callback_result.store(callbackResult, std::memory_order_seq_cst);
    m_callback_running.store(false, std::memory_order_seq_cst);
    m_lifecycle.store(
        callbackResult == LxrCompletionCallbackResult::Success
            ? LxrCompletionLifecycle::Drained
            : LxrCompletionLifecycle::Failed,
        std::memory_order_seq_cst);
    return callbackResult == LxrCompletionCallbackResult::Success
        ? LxrCompletionResult::Success
        : LxrCompletionResult::CallbackFailed;
}

LxrCompletionResult LxrLazyCompletionGenerationState::CreateReleaseGuard(
    const LxrLazyCompletionRegistration& registration,
    LxrBlockReleaseGuard* guard) const
{
    if (guard == nullptr)
    {
        return LxrCompletionResult::InvalidArgument;
    }
    if (!registration.m_active ||
        registration.m_state != this ||
        registration.m_kind != LxrCompletionWorkKind::MatureSweep)
    {
        return LxrCompletionResult::InvalidState;
    }
    if (!IsReleaseAllowed(registration.m_generation))
    {
        return LxrCompletionResult::InvalidState;
    }

    *guard = LxrBlockReleaseGuard(this, registration.m_generation);
    return LxrCompletionResult::Success;
}

LxrCompletionResult LxrLazyCompletionGenerationState::Cancel(uint64_t generation)
{
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }

    LxrCompletionLifecycle lifecycle =
        m_lifecycle.load(std::memory_order_seq_cst);
    if (lifecycle == LxrCompletionLifecycle::Uninitialized)
    {
        return LxrCompletionResult::InvalidState;
    }
    if (lifecycle == LxrCompletionLifecycle::Drained ||
        lifecycle == LxrCompletionLifecycle::Cancelled)
    {
        return LxrCompletionResult::Success;
    }

    m_lifecycle.store(
        LxrCompletionLifecycle::Cancelling,
        std::memory_order_seq_cst);
    if (m_overall_outstanding.load(std::memory_order_seq_cst) == 0)
    {
        m_lifecycle.store(
            LxrCompletionLifecycle::Cancelled,
            std::memory_order_seq_cst);
    }
    return LxrCompletionResult::Success;
}

LxrCompletionResult LxrLazyCompletionGenerationState::FinalizeCancellation(
    uint64_t generation)
{
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }
    if (m_overall_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_decrement_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_callback_running.load(std::memory_order_seq_cst))
    {
        RecordOwnershipViolation();
        return LxrCompletionResult::InvalidState;
    }

    m_lifecycle.store(
        LxrCompletionLifecycle::Cancelled,
        std::memory_order_seq_cst);
    return LxrCompletionResult::Success;
}

LxrCompletionResult LxrLazyCompletionGenerationState::PrepareForReuse(
    uint64_t generation)
{
    if (m_generation.load(std::memory_order_seq_cst) != generation)
    {
        return LxrCompletionResult::StaleGeneration;
    }
    if (m_lifecycle.load(std::memory_order_seq_cst) !=
            LxrCompletionLifecycle::Drained ||
        m_overall_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_decrement_outstanding.load(std::memory_order_seq_cst) != 0 ||
        m_callback_running.load(std::memory_order_seq_cst))
    {
        return LxrCompletionResult::InvalidState;
    }

    m_decrements_drained = nullptr;
    m_overall_drained = nullptr;
    m_callback_context = nullptr;
    m_lifecycle.store(
        LxrCompletionLifecycle::Uninitialized,
        std::memory_order_seq_cst);
    return LxrCompletionResult::Success;
}

LxrCompletionSnapshot LxrLazyCompletionGenerationState::GetSnapshot() const
{
    LxrCompletionSnapshot snapshot{};
    snapshot.generation = m_generation.load(std::memory_order_seq_cst);
    snapshot.overallOutstanding =
        m_overall_outstanding.load(std::memory_order_seq_cst);
    snapshot.decrementOutstanding =
        m_decrement_outstanding.load(std::memory_order_seq_cst);
    snapshot.registrations = m_registrations.load(std::memory_order_seq_cst);
    snapshot.completions = m_completions.load(std::memory_order_seq_cst);
    snapshot.decrementZeroTransitions =
        m_decrement_zero_transitions.load(std::memory_order_seq_cst);
    snapshot.overallZeroTransitions =
        m_overall_zero_transitions.load(std::memory_order_seq_cst);
    snapshot.decrementCallbacks =
        m_decrement_callbacks.load(std::memory_order_seq_cst);
    snapshot.overallCallbacks =
        m_overall_callbacks.load(std::memory_order_seq_cst);
    snapshot.ownershipViolations =
        m_ownership_violations.load(std::memory_order_seq_cst);
    snapshot.lifecycle = m_lifecycle.load(std::memory_order_seq_cst);
    snapshot.decrementCallbackResult =
        m_decrement_callback_result.load(std::memory_order_seq_cst);
    snapshot.overallCallbackResult =
        m_overall_callback_result.load(std::memory_order_seq_cst);
    snapshot.callbackRunning = m_callback_running.load(std::memory_order_seq_cst);
    return snapshot;
}

bool LxrLazyCompletionGenerationState::IsReleaseAllowed(
    uint64_t generation) const
{
    if (m_generation.load(std::memory_order_seq_cst) != generation ||
        m_decrement_outstanding.load(std::memory_order_seq_cst) != 0)
    {
        return false;
    }

    LxrCompletionLifecycle lifecycle =
        m_lifecycle.load(std::memory_order_seq_cst);
    return lifecycle == LxrCompletionLifecycle::Draining ||
        lifecycle == LxrCompletionLifecycle::DecisionRunning;
}

LxrCompletionResult LxrLazyCompletionGenerationState::IncrementChecked(
    std::atomic<uint64_t>* value)
{
    uint64_t current = value->load(std::memory_order_seq_cst);
    while (true)
    {
        if (current >= m_maximum_outstanding)
        {
            return LxrCompletionResult::Overflow;
        }
        if (value->compare_exchange_weak(
                current,
                current + 1,
                std::memory_order_seq_cst,
                std::memory_order_seq_cst))
        {
            return LxrCompletionResult::Success;
        }
    }
}

LxrCompletionResult LxrLazyCompletionGenerationState::DecrementChecked(
    std::atomic<uint64_t>* value,
    uint64_t* previous)
{
    uint64_t current = value->load(std::memory_order_seq_cst);
    while (true)
    {
        if (current == 0)
        {
            return LxrCompletionResult::Underflow;
        }
        if (value->compare_exchange_weak(
                current,
                current - 1,
                std::memory_order_seq_cst,
                std::memory_order_seq_cst))
        {
            *previous = current;
            return LxrCompletionResult::Success;
        }
    }
}

bool LxrLazyCompletionGenerationState::IsRegistrationLifecycle(
    LxrCompletionLifecycle lifecycle) const
{
    return lifecycle == LxrCompletionLifecycle::Registering ||
        lifecycle == LxrCompletionLifecycle::Published ||
        lifecycle == LxrCompletionLifecycle::Draining;
}

void LxrLazyCompletionGenerationState::RecordOwnershipViolation()
{
    m_ownership_violations.fetch_add(1, std::memory_order_seq_cst);
}
