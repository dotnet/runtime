// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _HISTOGRAM_H_
#define _HISTOGRAM_H_

#include <atomic>
#include <stdio.h>
#include <string.h>

#include "dumpable.h"

// Maximum number of buckets in a histogram (including overflow bucket)
#define HISTOGRAM_MAX_SIZE_COUNT 64

// Histogram class for recording and displaying distribution of values.
//
// Usage:
//   static unsigned s_buckets[] = { 1, 2, 5, 10, 0 }; // Must have terminating 0
//   static Histogram s_histogram(s_buckets);
//   ...
//   s_histogram.record(someValue);
//
// The histogram displays how many recorded values fell into each bucket
// (<= 1, <= 2, <= 5, <= 10, > 10).
//
// Thread-safety: record() uses atomic operations for thread-safe counting.

class Histogram final : public Dumpable
{
public:
    Histogram(const unsigned* const sizeTable);

    void dump(FILE* output) const override;
    void record(unsigned size);

private:
    unsigned                m_sizeCount;
    const unsigned* const   m_sizeTable;
    std::atomic<unsigned>   m_counts[HISTOGRAM_MAX_SIZE_COUNT];
};

#endif // _HISTOGRAM_H_
