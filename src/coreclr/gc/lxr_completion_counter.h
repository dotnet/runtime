// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_COMPLETION_COUNTER_H
#define LXR_COMPLETION_COUNTER_H

#include <atomic>
#include <cstdint>

enum class LxrCompletionResult : uint8_t
{
    Success,
    InvalidArgument,
    InvalidState,
    StaleGeneration,
    Overflow,
    Underflow,
    AlreadyCompleted,
    Cancelled,
    CallbackFailed,
};

enum class LxrCompletionWorkKind : uint8_t
{
    Overall,
    Decrement,
    MatureSweep,
};

enum class LxrCompletionLifecycle : uint8_t
{
    Uninitialized,
    Registering,
    Published,
    Draining,
    DecisionRunning,
    Drained,
    Cancelling,
    Cancelled,
    Failed,
};

enum class LxrCompletionCallbackResult : uint8_t
{
    Success,
    Failure,
};

class LxrLazyCompletionGenerationState;
class LxrMatureSweepContinuation;
class LxrBlockReleaseGuard;

class LxrLazyCompletionRegistration final
{
public:
    LxrLazyCompletionRegistration();
    ~LxrLazyCompletionRegistration();
    LxrLazyCompletionRegistration(const LxrLazyCompletionRegistration&) = delete;
    LxrLazyCompletionRegistration& operator=(const LxrLazyCompletionRegistration&) = delete;
    LxrLazyCompletionRegistration(LxrLazyCompletionRegistration&& other) noexcept;
    LxrLazyCompletionRegistration& operator=(LxrLazyCompletionRegistration&& other) noexcept;

    LxrCompletionResult Complete();
    LxrCompletionResult RegisterChild(
        LxrCompletionWorkKind kind,
        LxrLazyCompletionRegistration* registration) const;
    LxrCompletionResult CreateReleaseGuard(LxrBlockReleaseGuard* guard) const;
    bool IsActive() const;
    uint64_t GetGeneration() const;
    LxrCompletionWorkKind GetKind() const;

private:
    friend class LxrLazyCompletionGenerationState;
    friend class LxrMatureSweepContinuation;

    LxrLazyCompletionRegistration(
        LxrLazyCompletionGenerationState* state,
        uint64_t generation,
        LxrCompletionWorkKind kind);
    void MoveFrom(LxrLazyCompletionRegistration&& other);

    LxrLazyCompletionGenerationState* m_state;
    uint64_t m_generation;
    LxrCompletionWorkKind m_kind;
    bool m_active;
};

class LxrBlockReleaseGuard final
{
public:
    LxrBlockReleaseGuard();
    bool IsValid() const;
    uint64_t GetGeneration() const;

private:
    friend class LxrLazyCompletionGenerationState;

    LxrBlockReleaseGuard(
        const LxrLazyCompletionGenerationState* state,
        uint64_t generation);

    const LxrLazyCompletionGenerationState* m_state;
    uint64_t m_generation;
};

class LxrMatureSweepContinuation final
{
public:
    LxrMatureSweepContinuation(const LxrMatureSweepContinuation&) = delete;
    LxrMatureSweepContinuation& operator=(const LxrMatureSweepContinuation&) = delete;
    LxrMatureSweepContinuation(LxrMatureSweepContinuation&& other) noexcept;
    LxrMatureSweepContinuation& operator=(LxrMatureSweepContinuation&& other) noexcept;

    LxrCompletionResult RegisterSweep(LxrLazyCompletionRegistration* registration);
    uint64_t GetGeneration() const;

private:
    friend class LxrLazyCompletionGenerationState;

    explicit LxrMatureSweepContinuation(
        LxrLazyCompletionRegistration&& registration);

    LxrLazyCompletionRegistration m_registration;
};

using LxrDecrementsDrainedCallback = LxrCompletionCallbackResult (*)(
    LxrMatureSweepContinuation* continuation,
    void* context);
using LxrOverallDrainedCallback = LxrCompletionCallbackResult (*)(
    uint64_t generation,
    void* context);

struct LxrCompletionSnapshot
{
    uint64_t generation;
    uint64_t overallOutstanding;
    uint64_t decrementOutstanding;
    uint64_t registrations;
    uint64_t completions;
    uint64_t decrementZeroTransitions;
    uint64_t overallZeroTransitions;
    uint64_t decrementCallbacks;
    uint64_t overallCallbacks;
    uint64_t ownershipViolations;
    LxrCompletionLifecycle lifecycle;
    LxrCompletionCallbackResult decrementCallbackResult;
    LxrCompletionCallbackResult overallCallbackResult;
    bool callbackRunning;
};

class LxrLazyCompletionGenerationState final
{
public:
    LxrLazyCompletionGenerationState();
    LxrLazyCompletionGenerationState(const LxrLazyCompletionGenerationState&) = delete;
    LxrLazyCompletionGenerationState& operator=(const LxrLazyCompletionGenerationState&) = delete;

    LxrCompletionResult Initialize(
        uint64_t generation,
        LxrDecrementsDrainedCallback decrementsDrained,
        LxrOverallDrainedCallback overallDrained,
        void* callbackContext,
        LxrLazyCompletionRegistration* seed,
        uint64_t maximumOutstanding = UINT64_MAX);
    LxrCompletionResult Seal(uint64_t generation);
    LxrCompletionResult MarkPublished(uint64_t generation);
    LxrCompletionResult RegisterRoot(
        uint64_t generation,
        LxrCompletionWorkKind kind,
        LxrLazyCompletionRegistration* registration);
    LxrCompletionResult RegisterChild(
        const LxrLazyCompletionRegistration& parent,
        LxrCompletionWorkKind kind,
        LxrLazyCompletionRegistration* registration);
    LxrCompletionResult Complete(LxrLazyCompletionRegistration* registration);
    LxrCompletionResult CreateReleaseGuard(
        const LxrLazyCompletionRegistration& registration,
        LxrBlockReleaseGuard* guard) const;
    LxrCompletionResult Cancel(uint64_t generation);
    LxrCompletionResult FinalizeCancellation(uint64_t generation);
    LxrCompletionResult PrepareForReuse(uint64_t generation);
    LxrCompletionSnapshot GetSnapshot() const;
    bool IsReleaseAllowed(uint64_t generation) const;

private:
    LxrCompletionResult RegisterInternal(
        uint64_t generation,
        LxrCompletionWorkKind kind,
        bool root,
        LxrLazyCompletionRegistration* registration);
    LxrCompletionResult IncrementChecked(std::atomic<uint64_t>* value);
    LxrCompletionResult DecrementChecked(
        std::atomic<uint64_t>* value,
        uint64_t* previous);
    LxrCompletionResult CompleteOverall(uint64_t generation);
    bool IsRegistrationLifecycle(LxrCompletionLifecycle lifecycle) const;
    void RecordOwnershipViolation();

    std::atomic<uint64_t> m_generation;
    std::atomic<uint64_t> m_overall_outstanding;
    std::atomic<uint64_t> m_decrement_outstanding;
    std::atomic<uint64_t> m_registrations;
    std::atomic<uint64_t> m_completions;
    std::atomic<uint64_t> m_decrement_zero_transitions;
    std::atomic<uint64_t> m_overall_zero_transitions;
    std::atomic<uint64_t> m_decrement_callbacks;
    std::atomic<uint64_t> m_overall_callbacks;
    std::atomic<uint64_t> m_ownership_violations;
    std::atomic<LxrCompletionLifecycle> m_lifecycle;
    std::atomic<LxrCompletionCallbackResult> m_decrement_callback_result;
    std::atomic<LxrCompletionCallbackResult> m_overall_callback_result;
    std::atomic<bool> m_callback_running;
    LxrDecrementsDrainedCallback m_decrements_drained;
    LxrOverallDrainedCallback m_overall_drained;
    void* m_callback_context;
    uint64_t m_maximum_outstanding;
};

#endif // LXR_COMPLETION_COUNTER_H
