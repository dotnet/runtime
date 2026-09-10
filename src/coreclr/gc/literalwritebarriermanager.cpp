// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "writebarrierservices.h"

#include <assert.h>

namespace
{
class WriteBarrierManager
{
public:
    void Initialize()
    {
        g_writeBarrierServices.ValidateWriteBarrierLayout();

        WriteBarrierParameters state = {};
        g_writeBarrierServices.UpdateWriteBarrierImplementationState(state);
    }

    int StompEphemeral(const WriteBarrierParameters& state, bool)
    {
        return Update(state);
    }

    int StompResize(const WriteBarrierParameters& state, bool, bool)
    {
        return Update(state);
    }

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(const WriteBarrierParameters& state, bool)
    {
        return Update(state);
    }

    int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& state, bool)
    {
        return Update(state);
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    void FlushInstructionCache()
    {
    }

private:
    int Update(const WriteBarrierParameters& state)
    {
        g_writeBarrierServices.UpdateWriteBarrierImplementationState(state);
        return SWB_PASS;
    }
};

WriteBarrierManager g_writeBarrierManager;
}

#include "writebarriermanager.inl"
