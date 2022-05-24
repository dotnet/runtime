// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Likely Class Processing                         XX
XX                                                                           XX
XX   Parses Pgo data to find the most likely class in use at a given         XX
XX   IL offset in a method. Used by both the JIT, and by crossgen            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include <algorithm.h>

#ifndef DLLEXPORT
#define DLLEXPORT
#endif // !DLLEXPORT

// Data item in class profile histogram
//
struct LikelyClassHistogramEntry
{
    // Class that was observed at runtime
    INT_PTR m_mt; // This may be an "unknown type handle"
    // Number of observations in the table
    unsigned m_count;
};

// Summarizes a ClassProfile table by forming a Histogram
//
struct LikelyClassHistogram
{
    LikelyClassHistogram(INT_PTR* histogramEntries, unsigned entryCount);

    // Sum of counts from all entries in the histogram. This includes "unknown" entries which are not captured in
    // m_histogram
    unsigned m_totalCount;
    // Rough guess at count of unknown types
    unsigned m_unknownTypes;
    // Histogram entries, in no particular order.
    LikelyClassHistogramEntry m_histogram[HISTOGRAM_MAX_SIZE_COUNT];
    UINT32                    countHistogramElements = 0;

    LikelyClassHistogramEntry HistogramEntryAt(unsigned index)
    {
        return m_histogram[index];
    }
};

//------------------------------------------------------------------------
// LikelyClassHistogram::LikelyClassHistgram: construct a new histogram
//
// Arguments:
//    histogramEntries - pointer to the table portion of a ClassProfile* object (see corjit.h)
//    entryCount - number of entries in the table to examine
//
LikelyClassHistogram::LikelyClassHistogram(INT_PTR* histogramEntries, unsigned entryCount)
{
    m_unknownTypes                 = 0;
    m_totalCount                   = 0;
    uint32_t unknownTypeHandleMask = 0;

    for (unsigned k = 0; k < entryCount; k++)
    {
        if (histogramEntries[k] == 0)
        {
            continue;
        }

        m_totalCount++;

        INT_PTR currentEntry = histogramEntries[k];

        bool     found = false;
        unsigned h     = 0;
        for (; h < countHistogramElements; h++)
        {
            if (m_histogram[h].m_mt == currentEntry)
            {
                m_histogram[h].m_count++;
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (countHistogramElements >= ArrLen(m_histogram))
            {
                continue;
            }
            LikelyClassHistogramEntry newEntry;
            newEntry.m_mt                         = currentEntry;
            newEntry.m_count                      = 1;
            m_histogram[countHistogramElements++] = newEntry;
        }
    }
}

//------------------------------------------------------------------------
// getLikelyClasses: find class profile data for an IL offset, and return the most likely classes
//
// Arguments:
//    pLikelyClasses - [OUT] array of likely classes sorted by likelihood (descending). It must be
//                     at least of 'maxLikelyClasses' (next argument) length.
//                     The array consists of pairs "clsHandle - likelihood" ordered by likelihood
//                     (descending) where likelihood can be any value in [0..100] range. clsHandle
//                     is never null for [0..<return value of this function>) range, Items in
//                     [<return value of this function>..maxLikelyClasses) are zeroed if the number
//                     of classes seen is less than maxLikelyClasses provided.
//    maxLikelyClasses - limit for likely classes to output
//    schema - profile schema
//    countSchemaItems - number of items in the schema
//    pInstrumentationData - associated data
//    ilOffset - il offset of the callvirt
//
// Returns:
//    Estimated number of classes seen at runtime
//
// Notes:
//    A "monomorphic" call site will return likelihood 100 and number of entries = 1.
//
//   This is used by the devirtualization logic below, and by crossgen2 when producing
//   the R2R image (to reduce the sizecost of carrying the type histogram)
//
//   This code can runs without a jit instance present, so JITDUMP and related
//   cannot be used.
//
extern "C" DLLEXPORT UINT32 WINAPI getLikelyClasses(LikelyClassRecord*                     pLikelyClasses,
                                                    UINT32                                 maxLikelyClasses,
                                                    ICorJitInfo::PgoInstrumentationSchema* schema,
                                                    UINT32                                 countSchemaItems,
                                                    BYTE*                                  pInstrumentationData,
                                                    int32_t                                ilOffset)
{
    ZeroMemory(pLikelyClasses, maxLikelyClasses * sizeof(*pLikelyClasses));

    if (schema == nullptr)
    {
        return 0;
    }

    for (COUNT_T i = 0; i < countSchemaItems; i++)
    {
        if (schema[i].ILOffset != ilOffset)
            continue;

        if ((schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::GetLikelyClass) &&
            (schema[i].Count == 1))
        {
            INT_PTR result = *(INT_PTR*)(pInstrumentationData + schema[i].Offset);
            if (ICorJitInfo::IsUnknownHandle(result))
            {
                return 0;
            }
            assert(result != 0); // we don't expect zero in GetLikelyClass
            pLikelyClasses[0].likelihood = (UINT32)(schema[i].Other & 0xFF);
            pLikelyClasses[0].clsHandle  = (CORINFO_CLASS_HANDLE)result;
            return 1;
        }

        const bool isHistogramCount =
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramIntCount) ||
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramLongCount);

        if (isHistogramCount && (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramTypes))
        {
            // Form a histogram
            //
            LikelyClassHistogram h((INT_PTR*)(pInstrumentationData + schema[i + 1].Offset), schema[i + 1].Count);

            // Use histogram count as number of classes estimate
            // Report back what we've learned
            // (perhaps, use count to augment likelihood?)
            //
            switch (h.countHistogramElements)
            {
                case 0:
                    return 0;

                case 1:
                {
                    LikelyClassHistogramEntry const hist0 = h.HistogramEntryAt(0);
                    // Fast path for monomorphic cases
                    if (ICorJitInfo::IsUnknownHandle(hist0.m_mt))
                    {
                        return 0;
                    }
                    pLikelyClasses[0].likelihood = 100;
                    pLikelyClasses[0].clsHandle  = (CORINFO_CLASS_HANDLE)hist0.m_mt;
                    return 1;
                }

                case 2:
                {
                    LikelyClassHistogramEntry const hist0 = h.HistogramEntryAt(0);
                    LikelyClassHistogramEntry const hist1 = h.HistogramEntryAt(1);
                    // Fast path for two classes
                    if ((hist0.m_count >= hist1.m_count) && !ICorJitInfo::IsUnknownHandle(hist0.m_mt))
                    {
                        pLikelyClasses[0].likelihood = (100 * hist0.m_count) / h.m_totalCount;
                        pLikelyClasses[0].clsHandle  = (CORINFO_CLASS_HANDLE)hist0.m_mt;

                        if ((maxLikelyClasses > 1) && !ICorJitInfo::IsUnknownHandle(hist1.m_mt))
                        {
                            pLikelyClasses[1].likelihood = (100 * hist1.m_count) / h.m_totalCount;
                            pLikelyClasses[1].clsHandle  = (CORINFO_CLASS_HANDLE)hist1.m_mt;
                            return 2;
                        }
                        return 1;
                    }

                    if (!ICorJitInfo::IsUnknownHandle(hist1.m_mt))
                    {
                        pLikelyClasses[0].likelihood = (100 * hist1.m_count) / h.m_totalCount;
                        pLikelyClasses[0].clsHandle  = (CORINFO_CLASS_HANDLE)hist1.m_mt;

                        if ((maxLikelyClasses > 1) && !ICorJitInfo::IsUnknownHandle(hist0.m_mt))
                        {
                            pLikelyClasses[1].likelihood = (100 * hist0.m_count) / h.m_totalCount;
                            pLikelyClasses[1].clsHandle  = (CORINFO_CLASS_HANDLE)hist0.m_mt;
                            return 2;
                        }
                        return 1;
                    }
                    return 0;
                }

                default:
                {
                    LikelyClassHistogramEntry sortedEntries[HISTOGRAM_MAX_SIZE_COUNT];

                    // Since this method can be invoked without a jit instance we can't use any existing allocators
                    unsigned knownHandles = 0;
                    for (unsigned m = 0; m < h.countHistogramElements; m++)
                    {
                        LikelyClassHistogramEntry const hist = h.HistogramEntryAt(m);
                        if (!ICorJitInfo::IsUnknownHandle(hist.m_mt))
                        {
                            sortedEntries[knownHandles++] = hist;
                        }
                    }

                    // sort by m_count (descending)
                    jitstd::sort(sortedEntries, sortedEntries + knownHandles,
                                 [](const LikelyClassHistogramEntry& h1, const LikelyClassHistogramEntry& h2) -> bool {
                                     return h1.m_count > h2.m_count;
                                 });

                    const UINT32 numberOfClasses = min(knownHandles, maxLikelyClasses);

                    for (size_t hIdx = 0; hIdx < numberOfClasses; hIdx++)
                    {
                        LikelyClassHistogramEntry const hc = sortedEntries[hIdx];
                        pLikelyClasses[hIdx].clsHandle     = (CORINFO_CLASS_HANDLE)hc.m_mt;
                        pLikelyClasses[hIdx].likelihood    = hc.m_count * 100 / h.m_totalCount;
                    }
                    return numberOfClasses;
                }
            }
        }
    }

    // Failed to find histogram data for this method
    //
    return 0;
}

//------------------------------------------------------------------------
// getRandomClass: find class profile data for an IL offset, and return
//   one of the possible classes at random
//
// Arguments:
//    schema - profile schema
//    countSchemaItems - number of items in the schema
//    pInstrumentationData - associated data
//    ilOffset - il offset of the callvirt
//    random - randomness generator
//
// Returns:
//    Randomly observed class, or nullptr.
//
CORINFO_CLASS_HANDLE Compiler::getRandomClass(ICorJitInfo::PgoInstrumentationSchema* schema,
                                              UINT32                                 countSchemaItems,
                                              BYTE*                                  pInstrumentationData,
                                              int32_t                                ilOffset,
                                              CLRRandom*                             random)
{
    if (schema == nullptr)
    {
        return NO_CLASS_HANDLE;
    }

    for (COUNT_T i = 0; i < countSchemaItems; i++)
    {
        if (schema[i].ILOffset != (int32_t)ilOffset)
        {
            continue;
        }

        if ((schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::GetLikelyClass) &&
            (schema[i].Count == 1))
        {
            INT_PTR result = *(INT_PTR*)(pInstrumentationData + schema[i].Offset);
            if (ICorJitInfo::IsUnknownHandle(result))
            {
                return NO_CLASS_HANDLE;
            }
            else
            {
                return (CORINFO_CLASS_HANDLE)result;
            }
        }

        bool isHistogramCount =
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramIntCount) ||
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramLongCount);

        if (isHistogramCount && (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramTypes))
        {
            // Form a histogram
            //
            LikelyClassHistogram h((INT_PTR*)(pInstrumentationData + schema[i + 1].Offset), schema[i + 1].Count);

            if (h.countHistogramElements == 0)
            {
                return NO_CLASS_HANDLE;
            }

            // Choose an entry at random.
            //
            unsigned                  randomEntryIndex = random->Next(0, h.countHistogramElements);
            LikelyClassHistogramEntry randomEntry      = h.HistogramEntryAt(randomEntryIndex);

            if (ICorJitInfo::IsUnknownHandle(randomEntry.m_mt))
            {
                return NO_CLASS_HANDLE;
            }

            return (CORINFO_CLASS_HANDLE)randomEntry.m_mt;
        }
    }

    return NO_CLASS_HANDLE;
}
