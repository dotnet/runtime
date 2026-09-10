// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "writebarrier.h"

namespace
{
class WriteBarrierManager
{
public:
    void Initialize()
    {
    }

    int StompEphemeral(const WriteBarrierParameters&, bool)
    {
        return SWB_PASS;
    }

    int StompResize(const WriteBarrierParameters&, bool, bool)
    {
        return SWB_PASS;
    }

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(const WriteBarrierParameters&, bool)
    {
        return SWB_PASS;
    }

    int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters&, bool)
    {
        return SWB_PASS;
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    void FlushInstructionCache()
    {
    }
};

WriteBarrierManager g_writeBarrierManager;
}

#include "writebarriermanager.inl"
