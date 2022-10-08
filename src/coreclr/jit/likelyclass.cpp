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
struct LikelyClassMethodHistogramEntry
{
    // Handle that was observed at runtime
    INT_PTR m_handle; // This may be an "unknown handle"
    // Number of observations in the table
    unsigned m_count;
};

// Summarizes a ClassProfile table by forming a Histogram
//
struct LikelyClassMethodHistogram
{
    LikelyClassMethodHistogram(INT_PTR* histogramEntries, unsigned entryCount);

    // Sum of counts from all entries in the histogram. This includes "unknown" entries which are not captured in
    // m_histogram
    unsigned m_totalCount;
    // Rough guess at count of unknown handles
    unsigned m_unknownHandles;
    // Histogram entries, in no particular order.
    LikelyClassMethodHistogramEntry m_histogram[HISTOGRAM_MAX_SIZE_COUNT];
    UINT32                          countHistogramElements = 0;

    LikelyClassMethodHistogramEntry HistogramEntryAt(unsigned index)
    {
        return m_histogram[index];
    }
};

//------------------------------------------------------------------------
// LikelyClassMethodHistogram::LikelyClassMethodHistogram: construct a new histogram
//
// Arguments:
//    histogramEntries - pointer to the table portion of a ClassProfile* object (see corjit.h)
//    entryCount - number of entries in the table to examine
//
LikelyClassMethodHistogram::LikelyClassMethodHistogram(INT_PTR* histogramEntries, unsigned entryCount)
{
    m_unknownHandles               = 0;
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
            if (m_histogram[h].m_handle == currentEntry)
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
            LikelyClassMethodHistogramEntry newEntry;
            newEntry.m_handle                     = currentEntry;
            newEntry.m_count                      = 1;
            m_histogram[countHistogramElements++] = newEntry;
        }
    }
}

//------------------------------------------------------------------------
// getLikelyClassesOrMethods:
//   Find class/method profile data for an IL offset, and return the most
//   likely classes/methods.
//
//   This is a common entrypoint for getLikelyClasses and getLikelyMethods.
//   See documentation for those for more information.
//
static unsigned getLikelyClassesOrMethods(LikelyClassMethodRecord*               pLikelyEntries,
                                          UINT32                                 maxLikelyClasses,
                                          ICorJitInfo::PgoInstrumentationSchema* schema,
                                          UINT32                                 countSchemaItems,
                                          BYTE*                                  pInstrumentationData,
                                          int32_t                                ilOffset,
                                          bool                                   types)
{
    ICorJitInfo::PgoInstrumentationKind histogramKind =
        types ? ICorJitInfo::PgoInstrumentationKind::HandleHistogramTypes
              : ICorJitInfo::PgoInstrumentationKind::HandleHistogramMethods;
    ICorJitInfo::PgoInstrumentationKind compressedKind = types ? ICorJitInfo::PgoInstrumentationKind::GetLikelyClass
                                                               : ICorJitInfo::PgoInstrumentationKind::GetLikelyMethod;

    memset(pLikelyEntries, 0, maxLikelyClasses * sizeof(*pLikelyEntries));

    if (schema == nullptr)
    {
        return 0;
    }

    for (COUNT_T i = 0; i < countSchemaItems; i++)
    {
        if (schema[i].ILOffset != ilOffset)
            continue;

        if ((schema[i].InstrumentationKind == compressedKind) && (schema[i].Count == 1))
        {
            intptr_t result = *(intptr_t*)(pInstrumentationData + schema[i].Offset);
            if (ICorJitInfo::IsUnknownHandle(result))
            {
                return 0;
            }
            assert(result != 0); // we don't expect zero in GetLikelyClass/GetLikelyMethod
            pLikelyEntries[0].likelihood = (UINT32)(schema[i].Other & 0xFF);
            pLikelyEntries[0].handle     = result;
            return 1;
        }

        const bool isHistogramCount =
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramIntCount) ||
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::HandleHistogramLongCount);

        if (isHistogramCount && (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == histogramKind))
        {
            // Form a histogram
            //
            LikelyClassMethodHistogram h((INT_PTR*)(pInstrumentationData + schema[i + 1].Offset), schema[i + 1].Count);

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
                    LikelyClassMethodHistogramEntry const hist0 = h.HistogramEntryAt(0);
                    // Fast path for monomorphic cases
                    if (ICorJitInfo::IsUnknownHandle(hist0.m_handle))
                    {
                        return 0;
                    }
                    pLikelyEntries[0].likelihood = 100;
                    pLikelyEntries[0].handle     = hist0.m_handle;
                    return 1;
                }

                case 2:
                {
                    // Fast path for two classes
                    LikelyClassMethodHistogramEntry const hist0 = h.HistogramEntryAt(0);
                    LikelyClassMethodHistogramEntry const hist1 = h.HistogramEntryAt(1);
                    if ((hist0.m_count >= hist1.m_count) && !ICorJitInfo::IsUnknownHandle(hist0.m_handle))
                    {
                        pLikelyEntries[0].likelihood = (100 * hist0.m_count) / h.m_totalCount;
                        pLikelyEntries[0].handle     = hist0.m_handle;

                        if ((maxLikelyClasses > 1) && !ICorJitInfo::IsUnknownHandle(hist1.m_handle))
                        {
                            pLikelyEntries[1].likelihood = (100 * hist1.m_count) / h.m_totalCount;
                            pLikelyEntries[1].handle     = hist1.m_handle;
                            return 2;
                        }
                        return 1;
                    }

                    if (!ICorJitInfo::IsUnknownHandle(hist1.m_handle))
                    {
                        pLikelyEntries[0].likelihood = (100 * hist1.m_count) / h.m_totalCount;
                        pLikelyEntries[0].handle     = hist1.m_handle;

                        if ((maxLikelyClasses > 1) && !ICorJitInfo::IsUnknownHandle(hist0.m_handle))
                        {
                            pLikelyEntries[1].likelihood = (100 * hist0.m_count) / h.m_totalCount;
                            pLikelyEntries[1].handle     = hist0.m_handle;
                            return 2;
                        }
                        return 1;
                    }
                    return 0;
                }

                default:
                {
                    LikelyClassMethodHistogramEntry sortedEntries[HISTOGRAM_MAX_SIZE_COUNT];

                    // Since this method can be invoked without a jit instance we can't use any existing allocators
                    unsigned knownHandles = 0;
                    for (unsigned m = 0; m < h.countHistogramElements; m++)
                    {
                        LikelyClassMethodHistogramEntry const hist = h.HistogramEntryAt(m);
                        if (!ICorJitInfo::IsUnknownHandle(hist.m_handle))
                        {
                            sortedEntries[knownHandles++] = hist;
                        }
                    }

                    // sort by m_count (descending)
                    jitstd::sort(sortedEntries, sortedEntries + knownHandles,
                                 [](const LikelyClassMethodHistogramEntry& h1,
                                    const LikelyClassMethodHistogramEntry& h2) -> bool {
                                     return h1.m_count > h2.m_count;
                                 });

                    const UINT32 numberOfClasses = min(knownHandles, maxLikelyClasses);

                    for (size_t hIdx = 0; hIdx < numberOfClasses; hIdx++)
                    {
                        LikelyClassMethodHistogramEntry const hc = sortedEntries[hIdx];
                        pLikelyEntries[hIdx].handle              = hc.m_handle;
                        pLikelyEntries[hIdx].likelihood          = hc.m_count * 100 / h.m_totalCount;
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
extern "C" DLLEXPORT UINT32 WINAPI getLikelyClasses(LikelyClassMethodRecord*               pLikelyClasses,
                                                    UINT32                                 maxLikelyClasses,
                                                    ICorJitInfo::PgoInstrumentationSchema* schema,
                                                    UINT32                                 countSchemaItems,
                                                    BYTE*                                  pInstrumentationData,
                                                    int32_t                                ilOffset)
{
    return getLikelyClassesOrMethods(pLikelyClasses, maxLikelyClasses, schema, countSchemaItems, pInstrumentationData,
                                     ilOffset, true);
}

//------------------------------------------------------------------------
// getLikelyMethods: find method profile data for an IL offset, and return the most likely methods
//
// See documentation on getLikelyClasses above.
//
extern "C" DLLEXPORT UINT32 WINAPI getLikelyMethods(LikelyClassMethodRecord*               pLikelyMethods,
                                                    UINT32                                 maxLikelyMethods,
                                                    ICorJitInfo::PgoInstrumentationSchema* schema,
                                                    UINT32                                 countSchemaItems,
                                                    BYTE*                                  pInstrumentationData,
                                                    int32_t                                ilOffset)
{
    return getLikelyClassesOrMethods(pLikelyMethods, maxLikelyMethods, schema, countSchemaItems, pInstrumentationData,
                                     ilOffset, false);
}
