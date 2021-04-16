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
    LikelyClassHistogram(uint32_t histogramCount, INT_PTR* histogramEntries, unsigned entryCount);

    // Sum of counts from all entries in the histogram. This includes "unknown" entries which are not captured in
    // m_histogram
    unsigned m_totalCount;
    // Rough guess at count of unknown types
    unsigned m_unknownTypes;
    // Histogram entries, in no particular order.
    LikelyClassHistogramEntry m_histogram[64];
    UINT32                    countHistogramElements = 0;

    LikelyClassHistogramEntry HistogramEntryAt(unsigned index)
    {
        return m_histogram[index];
    }
};

LikelyClassHistogram::LikelyClassHistogram(uint32_t histogramCount, INT_PTR* histogramEntries, unsigned entryCount)
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
            if (countHistogramElements >= _countof(m_histogram))
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

// This is used by the devirtualization logic below, and by crossgen2 when producing the R2R image (to reduce the size
// cost of carrying the type histogram)
extern "C" DLLEXPORT CORINFO_CLASS_HANDLE WINAPI getLikelyClass(ICorJitInfo::PgoInstrumentationSchema* schema,
                                                                UINT32                                 countSchemaItems,
                                                                BYTE*   pInstrumentationData,
                                                                int32_t ilOffset,
                                                                UINT32* pLikelihood,
                                                                UINT32* pNumberOfClasses)
{
    *pLikelihood      = 0;
    *pNumberOfClasses = 0;

    if (schema == NULL)
        return NULL;

    for (COUNT_T i = 0; i < countSchemaItems; i++)
    {
        if (schema[i].ILOffset != (int32_t)ilOffset)
            continue;

        if ((schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::GetLikelyClass) &&
            (schema[i].Count == 1))
        {
            *pNumberOfClasses = (UINT32)schema[i].Other >> 8;
            *pLikelihood      = (UINT32)(schema[i].Other && 0xFF);
            INT_PTR result    = *(INT_PTR*)(pInstrumentationData + schema[i + 1].Offset);
            if (ICorJitInfo::IsUnknownTypeHandle(result))
                return NULL;
            else
                return (CORINFO_CLASS_HANDLE)result;
        }

        if ((schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramCount) &&
            (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramTypeHandle))
        {
            // Form a histogram
            //
            LikelyClassHistogram h(*(uint32_t*)(pInstrumentationData + schema[i].Offset),
                                   (INT_PTR*)(pInstrumentationData + schema[i + 1].Offset), schema[i + 1].Count);

            // Use histogram count as number of classes estimate
            //
            *pNumberOfClasses = (uint32_t)h.countHistogramElements + h.m_unknownTypes;

            // Report back what we've learned
            // (perhaps, use count to augment likelihood?)
            //
            switch (*pNumberOfClasses)
            {
                case 0:
                {
                    return NULL;
                }
                break;

                case 1:
                {
                    if (ICorJitInfo::IsUnknownTypeHandle(h.HistogramEntryAt(0).m_mt))
                    {
                        return NULL;
                    }
                    *pLikelihood = 100;
                    return (CORINFO_CLASS_HANDLE)h.HistogramEntryAt(0).m_mt;
                }
                break;

                case 2:
                {
                    if ((h.HistogramEntryAt(0).m_count >= h.HistogramEntryAt(1).m_count) &&
                        !ICorJitInfo::IsUnknownTypeHandle(h.HistogramEntryAt(0).m_mt))
                    {
                        *pLikelihood = (100 * h.HistogramEntryAt(0).m_count) / h.m_totalCount;
                        return (CORINFO_CLASS_HANDLE)h.HistogramEntryAt(0).m_mt;
                    }
                    else if (!ICorJitInfo::IsUnknownTypeHandle(h.HistogramEntryAt(1).m_mt))
                    {
                        *pLikelihood = (100 * h.HistogramEntryAt(1).m_count) / h.m_totalCount;
                        return (CORINFO_CLASS_HANDLE)h.HistogramEntryAt(1).m_mt;
                    }
                    else
                    {
                        return NULL;
                    }
                }
                break;

                default:
                {
                    // Find maximum entry and return it
                    //
                    unsigned maxKnownIndex = 0;
                    unsigned maxKnownCount = 0;

                    for (unsigned m = 0; m < h.countHistogramElements; m++)
                    {
                        if ((h.HistogramEntryAt(m).m_count > maxKnownCount) &&
                            !ICorJitInfo::IsUnknownTypeHandle(h.HistogramEntryAt(m).m_mt))
                        {
                            maxKnownIndex = m;
                            maxKnownCount = h.HistogramEntryAt(m).m_count;
                        }
                    }

                    if (maxKnownCount > 0)
                    {
                        *pLikelihood = (100 * maxKnownCount) / h.m_totalCount;
                        ;
                        return (CORINFO_CLASS_HANDLE)h.HistogramEntryAt(maxKnownIndex).m_mt;
                    }

                    return NULL;
                }
                break;
            }
        }
    }

    // Failed to find histogram data for this method
    //
    return NULL;
}
