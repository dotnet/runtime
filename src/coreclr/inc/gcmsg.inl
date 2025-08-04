
    static const char* gcStartMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "{ =========== BEGINGC %d, (requested generation = %lu, collect_classes = %lu) ==========\n";
    }

    static const char* gcEndMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "========== ENDGC %d (gen = %lu, collect_classes = %lu) ===========}\n";
    }

    static const char* gcRootMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "    GC Root %p RELOCATED %p -> %p  MT = %pT\n";
    }

    static const char* gcRootPromoteMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "    IGCHeap::Promote: Promote GC Root *%p = %p MT = %pT\n";
    }

    static const char* gcPlugMoveMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "GC_HEAP RELOCATING Objects in heap within range [%p %p) by -0x%x bytes\n";
    }

    static const char* gcServerThread0StartMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "%d gc thread waiting...";
    }

    static const char* gcServerThreadNStartMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "%d gc thread waiting... Done";
    }

#define GC_DETAILED_START_PREFIX "*GC* %d(gen0:%d)(%d)"
#define GC_DETAILED_START_STRESSLOG "(alloced for %.3fms, g0 %zd (b: %zd, %zd/h) (%.3fmb/ms), g3 %zd (%.3fmb/ms), g4 %zd (%.3fmb/ms))(%s)(%d)(%d)"
#define GC_DETAILED_START_DPRINTF_EXTRA "(heap size: %.3fmb max: %.3fmb)"

    static const char* gcDetailedStartPrefix()
    {
        STATIC_CONTRACT_LEAF;
        return GC_DETAILED_START_PREFIX;
    }

    static const char* gcDetailedStartMsg(bool compatibleWithStressLog)
    {
        STATIC_CONTRACT_LEAF;
        if (compatibleWithStressLog)
        {
            return GC_DETAILED_START_PREFIX GC_DETAILED_START_STRESSLOG;
        }
        else
        {
            return GC_DETAILED_START_PREFIX GC_DETAILED_START_STRESSLOG GC_DETAILED_START_DPRINTF_EXTRA;
        }
    }

#undef GC_DETAILED_START_PREFIX
#undef GC_DETAILED_START_STRESSLOG
#undef GC_DETAILED_START_DPRINTF_EXTRA

    static const char* gcDetailedEndMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "*EGC* %zd(gen0:%zd)(heap size: %.3fmb)(%d)(%s)(%s)(%s)(ml: %d->%d)\n";
    }

    static const char* gcStartMarkMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- Mark Phase on heap %d condemning %d ----";
    }

    static const char* gcStartPlanMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- Plan Phase on heap %d ---- Condemned generation %d, promotion: %d";
    }

    static const char* gcStartRelocateMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- Relocate phase on heap %d -----";
    }

    static const char* gcEndRelocateMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- End of Relocate phase on heap %d ----";
    }

    static const char* gcStartCompactMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- Compact Phase on heap %d: %zx(%zx)----";
    }

    static const char* gcEndCompactMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "---- End of Compact phase on heap %d ----";
    }

    static const char* gcMemCopyMsg()
    {
        STATIC_CONTRACT_LEAF;
        return " mc: [%zx->%zx, %zx->%zx[";
    }

    static const char* gcPlanPlugMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "(%zx)[%zx->%zx, NA: [%zx(%zd), %zx[: %zx(%d), x: %zx (%s)";
    }

    static const char* gcPlanPinnedPlugMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "(%zx)PP: [%zx, %zx[%zx](m:%d)";
    }

    static const char* gcDesiredNewAllocationMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "h%d g%d surv: %zd current: %zd alloc: %zd (%d%%) f: %d%% new-size: %zd new-alloc: %zd";
    }

    static const char* gcMakeUnusedArrayMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "Making unused array [%zx, %zx[";
    }

    static const char* gcStartBgcThread()
    {
        STATIC_CONTRACT_LEAF;
        return "beginning of bgc on heap %d: gen2 FL: %d, FO: %d, frag: %d";
    }

    static const char* gcRelocateReferenceMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "Relocating reference *(%p) from %p to %p";
    }

    static const char* gcLoggingIsOffMsg()
    {
        STATIC_CONTRACT_LEAF;
        return "TraceGC is not turned on";
    }
