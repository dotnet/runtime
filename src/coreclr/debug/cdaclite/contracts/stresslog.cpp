// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stresslog.cpp
//
// Implementation of the stress-log enumeration declared in stresslog.h.
//*****************************************************************************

#include "stresslog.h"
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Stress-log globals (src/coreclr/vm/datadescriptor/datadescriptor.inc).
        const char* const GlobalStressLogEnabled = "StressLogEnabled"; // direct byte
        const char* const GlobalStressLog = "StressLog";               // resolved -> StressLog struct
        const char* const GlobalChunkSize = "StressLogChunkSize";      // direct: bytes per chunk buffer

        const int MaxThreadLogs = 100000;
        const int MaxChunksPerThread = 1000000;
    }

    int EnumerateStressLogRegions(const Target& target)
    {
        // Stress log is opt-in; if disabled there is nothing to capture.
        // COPILOT TODO: We shouldn't need this fallback for different sized things. Either it is pointer sized or a set size.
        uint64_t enabled = 0;
        if (!target.TryGetGlobalValue(GlobalStressLogEnabled, enabled))
        {
            // Fall back to reading it as a byte at the global's address.
            uint32_t enabled32 = 0;
            if (target.TryReadGlobalUInt32(GlobalStressLogEnabled, enabled32))
            {
                enabled = enabled32 & 0xFF;
            }
        }
        if (enabled == 0)
        {
            return 0;
        }

        // The StressLog struct: its resolved global value is the struct address (the cDAC uses
        // ReadGlobalPointer(StressLog) directly as the struct address).
        uint64_t stressLogAddr = 0;
        if (!target.TryGetGlobalValue(GlobalStressLog, stressLogAddr) || stressLogAddr == 0)
        {
            return -1;
        }
        data::StressLog stressLog;
        if (!target.TryRead(stressLogAddr, stressLog))
        {
            return -1;
        }

        uint64_t chunkSize = 0;
        target.TryGetGlobalValue(GlobalChunkSize, chunkSize);
        // Emit the whole chunk (header + message buffer + trailing signatures). A generous header
        // pad covers the fields around the inline Buf array.
        uint32_t chunkEmitSize = (uint32_t)chunkSize + 8u * (uint32_t)target.PointerSize();

        std::set<uint64_t> visitedThreads;
        std::set<uint64_t> visitedChunks;
        int chunkCount = 0;

        uint64_t threadLog = stressLog.Logs;
        for (int t = 0; threadLog != 0 && t < MaxThreadLogs; t++)
        {
            if (!visitedThreads.insert(threadLog).second)
            {
                break;
            }
            data::ThreadStressLog tsl;
            if (!target.TryRead(threadLog, tsl))
            {
                break;
            }

            // Walk the chunk ring (ChunkListHead -> Next), capturing each chunk's buffer.
            uint64_t chunk = tsl.ChunkListHead;
            for (int c = 0; chunk != 0 && c < MaxChunksPerThread; c++)
            {
                if (!visitedChunks.insert(chunk).second)
                {
                    break; // ring wrapped
                }
                data::StressLogChunk chunkData;
                if (!target.TryRead(chunk, chunkData))
                {
                    break;
                }
                if (chunkEmitSize > 0)
                {
                    target.EmitMemory(chunk, chunkEmitSize);
                }
                chunkCount++;

                if (chunk == tsl.ChunkListTail)
                {
                    break;
                }
                chunk = chunkData.Next;
            }

            threadLog = tsl.Next;
        }

        return chunkCount;
    }
}
} // namespace contracts
