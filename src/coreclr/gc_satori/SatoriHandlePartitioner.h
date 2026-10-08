// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriHandlePartitioner.h
//

#ifndef __SATORI_HANDLE_PARTITIONER_H__
#define __SATORI_HANDLE_PARTITIONER_H__

#include "common.h"
#include "gcenv.h"
#include "../env/gcenv.os.h"

#include "SatoriUtil.h"

class SatoriHandlePartitioner
{
public:
    static void Initialize()
    {
        s_partitionCount = SatoriUtil::HandlePartitionsCount();
        s_scanTickets = new (nothrow) uint8_t[s_partitionCount]{};
    }

    static void StartNextScan()
    {
        s_currentTicket++;
    }

    static int PartitionCount()
    {
        return s_partitionCount;
    }

    static int CurrentThreadPartition()
    {
        if (!s_partitionCount)
        {
            return 0;
        }

        uint32_t value;
        if (GCToOSInterface::CanGetCurrentProcessorNumber())
        {
            value = GCToOSInterface::GetCurrentProcessorNumber();
        }
        else
        {
            size_t thread = SatoriUtil::GetCurrentThreadTag();
            value = (uint32_t)((thread * 11400714819323198485llu) >> 32);
        }

        return (int)(value % (uint32_t)s_partitionCount);
    }

    template <typename F>
    static bool ForEachUnscannedPartition(F lambda, int64_t deadline = 0)
    {
        if (s_completedTicket != s_currentTicket)
        {
            int startPartition = CurrentThreadPartition();
             for (int i = 0; i < s_partitionCount; i++)
            {
                int partition = (i + startPartition) % s_partitionCount;
                uint8_t partitionTicket = VolatileLoadWithoutBarrier(&s_scanTickets[partition]);
                if (partitionTicket != s_currentTicket)
                {
                    if (Interlocked::CompareExchange(&s_scanTickets[partition], s_currentTicket, partitionTicket) == partitionTicket)
                    {
                        lambda(partition);

                        if (deadline && (minipal_hires_ticks() - deadline > 0))
                        {
                            // timed out, there could be more work
                            return true;
                        }
                    }
                }
            }

            if (s_completedTicket != s_currentTicket)
            {
                s_completedTicket = s_currentTicket;
            }
        }

        // done
        return false;
    }

private:
    static int s_partitionCount;
    static uint8_t* s_scanTickets;
    static uint8_t s_currentTicket;
    static uint8_t s_completedTicket;
};

#endif
