// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MEMSTATS_H_
#define _MEMSTATS_H_

#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <algorithm>

// MemStats tracks memory allocation statistics for profiling purposes.
// This is a template class parameterized by a traits type.
//
// Template parameters:
//   TMemKindTraits - A traits struct that must provide:
//     - MemKind: The enum type for memory kinds (e.g., InterpMemKind)
//     - Count: A static constexpr int giving the number of enum values
//     - Names: A static const char* const[] array of names for each kind
//
// Example traits struct:
//   struct InterpMemKindTraits {
//       using MemKind = InterpMemKind;
//       static constexpr int Count = IMK_Count;
//       static const char* const Names[];
//   };

template <typename TMemKindTraits>
struct MemStats
{
    using MemKind = typename TMemKindTraits::MemKind;
    static constexpr int MemKindCount = TMemKindTraits::Count;

    unsigned allocCnt;                      // # of allocs
    uint64_t allocSz;                       // total size of those alloc.
    uint64_t allocSzMax;                    // Maximum single allocation.
    uint64_t allocSzByKind[MemKindCount];   // Classified by "kind".
    uint64_t nraTotalSizeAlloc;
    uint64_t nraTotalSizeUsed;

    MemStats()
        : allocCnt(0)
        , allocSz(0)
        , allocSzMax(0)
        , nraTotalSizeAlloc(0)
        , nraTotalSizeUsed(0)
    {
        for (int i = 0; i < MemKindCount; i++)
        {
            allocSzByKind[i] = 0;
        }
    }

    void AddAlloc(size_t sz, MemKind kind)
    {
        allocCnt += 1;
        allocSz += sz;
        if (sz > allocSzMax)
        {
            allocSzMax = sz;
        }
        allocSzByKind[static_cast<int>(kind)] += sz;
    }

    void Print(FILE* f) const
    {
        fprintf(f, "count: %10u, size: %10llu, max = %10llu\n",
                allocCnt, (unsigned long long)allocSz, (unsigned long long)allocSzMax);
        fprintf(f, "allocateMemory: %10llu, nraUsed: %10llu\n",
                (unsigned long long)nraTotalSizeAlloc, (unsigned long long)nraTotalSizeUsed);
        PrintByKind(f);
    }

    void PrintByKind(FILE* f) const
    {
        fprintf(f, "\nAlloc'd bytes by kind:\n  %20s | %10s | %7s\n", "kind", "size", "pct");
        fprintf(f, "  %20s-+-%10s-+-%7s\n", "--------------------", "----------", "-------");
        float allocSzF = static_cast<float>(allocSz);
        for (int i = 0; i < MemKindCount; i++)
        {
            float pct = (allocSzF > 0) ? (100.0f * static_cast<float>(allocSzByKind[i]) / allocSzF) : 0.0f;
            fprintf(f, "  %20s | %10llu | %6.2f%%\n", TMemKindTraits::Names[i], (unsigned long long)allocSzByKind[i], pct);
        }
        fprintf(f, "\n");
    }
};

// AggregateMemStats accumulates statistics across multiple compilations.
template <typename TMemKindTraits>
struct AggregateMemStats : public MemStats<TMemKindTraits>
{
    unsigned nMethods;

    AggregateMemStats()
        : MemStats<TMemKindTraits>()
        , nMethods(0)
    {
    }

    void Add(const MemStats<TMemKindTraits>& ms)
    {
        nMethods++;
        this->allocCnt += ms.allocCnt;
        this->allocSz += ms.allocSz;
        this->allocSzMax = std::max(this->allocSzMax, ms.allocSzMax);
        for (int i = 0; i < TMemKindTraits::Count; i++)
        {
            this->allocSzByKind[i] += ms.allocSzByKind[i];
        }
        this->nraTotalSizeAlloc += ms.nraTotalSizeAlloc;
        this->nraTotalSizeUsed += ms.nraTotalSizeUsed;
    }

    void Print(FILE* f) const
    {
        fprintf(f, "For %9u methods:\n", nMethods);
        if (nMethods == 0)
        {
            return;
        }
        fprintf(f, "  count:       %12u (avg %7u per method)\n", this->allocCnt, this->allocCnt / nMethods);
        fprintf(f, "  alloc size : %12llu (avg %7llu per method)\n",
                (unsigned long long)this->allocSz, (unsigned long long)(this->allocSz / nMethods));
        fprintf(f, "  max alloc  : %12llu\n", (unsigned long long)this->allocSzMax);
        fprintf(f, "\n");
        fprintf(f, "  allocateMemory   : %12llu (avg %7llu per method)\n",
                (unsigned long long)this->nraTotalSizeAlloc, (unsigned long long)(this->nraTotalSizeAlloc / nMethods));
        fprintf(f, "  nraUsed    : %12llu (avg %7llu per method)\n",
                (unsigned long long)this->nraTotalSizeUsed, (unsigned long long)(this->nraTotalSizeUsed / nMethods));
        this->PrintByKind(f);
    }
};

#endif // _MEMSTATS_H_
