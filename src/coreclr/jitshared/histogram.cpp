// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "histogram.h"
#include <cassert>

//------------------------------------------------------------------------
// Histogram::Histogram:
//    Constructs a histogram with the specified bucket boundaries.
//
// Arguments:
//    sizeTable - Array of bucket upper bounds, terminated by 0.
//                For example, {64, 128, 256, 0} creates buckets:
//                <= 64, 65..128, 129..256, > 256
//
Histogram::Histogram(const unsigned* const sizeTable)
    : m_sizeTable(sizeTable)
{
    unsigned sizeCount = 0;
    while ((sizeTable[sizeCount] != 0) && (sizeCount < HISTOGRAM_MAX_SIZE_COUNT - 1))
    {
        sizeCount++;
    }

    assert(sizeCount < HISTOGRAM_MAX_SIZE_COUNT);

    m_sizeCount = sizeCount;

    for (unsigned i = 0; i <= m_sizeCount; i++)
    {
        m_counts[i].store(0, std::memory_order_relaxed);
    }
}

//------------------------------------------------------------------------
// Histogram::dump:
//    Prints the histogram distribution to the specified file.
//
// Arguments:
//    output - File to write to (e.g., stdout)
//
void Histogram::dump(FILE* output) const
{
    unsigned t = 0;
    for (unsigned i = 0; i < m_sizeCount; i++)
    {
        t += m_counts[i].load(std::memory_order_relaxed);
    }
    t += m_counts[m_sizeCount].load(std::memory_order_relaxed); // overflow bucket

    if (t == 0)
    {
        fprintf(output, "  (no data recorded)\n");
        return;
    }

    for (unsigned c = 0, i = 0; i <= m_sizeCount; i++)
    {
        unsigned count = m_counts[i].load(std::memory_order_relaxed);

        if (i == m_sizeCount)
        {
            if (count == 0)
            {
                break;
            }

            fprintf(output, "      >    %7u", m_sizeTable[i - 1]);
        }
        else
        {
            if (i == 0)
            {
                fprintf(output, "     <=    ");
            }
            else
            {
                fprintf(output, "%7u .. ", m_sizeTable[i - 1] + 1);
            }

            fprintf(output, "%7u", m_sizeTable[i]);
        }

        c += count;

        fprintf(output, " ===> %7u count (%3u%% of total)\n",
                count,
                (int)(100.0 * c / t));
    }
}

//------------------------------------------------------------------------
// Histogram::record:
//    Records a value in the appropriate bucket.
//    Thread-safe via atomic increment.
//
// Arguments:
//    size - The value to record
//
void Histogram::record(unsigned size)
{
    unsigned i;
    for (i = 0; i < m_sizeCount; i++)
    {
        if (m_sizeTable[i] >= size)
        {
            break;
        }
    }

    m_counts[i].fetch_add(1, std::memory_order_relaxed);
}
