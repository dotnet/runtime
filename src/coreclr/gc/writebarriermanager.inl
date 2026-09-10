// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void InitializeWriteBarrierManager()
{
    g_writeBarrierManager.Initialize();
}

// Updates the active write barrier when the ephemeral bounds change
// while the ephemeral region remains at the top of the GC heap.
int StompWriteBarrierEphemeral(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    return g_writeBarrierManager.StompEphemeral(state, isRuntimeSuspended);
}

// Updates the active write barrier when the ephemeral region or card table moves.
int StompWriteBarrierResize(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended,
    bool requiresUpperBoundsCheck)
{
    return g_writeBarrierManager.StompResize(
        state,
        isRuntimeSuspended,
        requiresUpperBoundsCheck);
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int SwitchToWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    return g_writeBarrierManager.SwitchToWriteWatchBarrier(state, isRuntimeSuspended);
}

int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    return g_writeBarrierManager.SwitchToNonWriteWatchBarrier(state, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

void FlushWriteBarrierInstructionCache()
{
    g_writeBarrierManager.FlushInstructionCache();
}
