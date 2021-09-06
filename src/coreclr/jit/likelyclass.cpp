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
    LikelyClassHistogram(INT_PTR* histogramEntries, unsigned entryCount);

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

//------------------------------------------------------------------------
// getLikelyClass: find class profile data for an IL offset, and return the most likely class
//
// Arguments:
//    schema - profile schema
//    countSchemaItems - number of items in the schema
//    pInstrumentationData - associated data
//    ilOffset - il offset of the callvirt
//    pLikelihood - [OUT] likelihood of observing that entry [0...100]
//    pNumberOfClasses - [OUT] estimated number of classes seen at runtime
//
// Returns:
//    Class handle for the most likely class, or nullptr
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
            *pLikelihood      = (UINT32)(schema[i].Other & 0xFF);
            INT_PTR result    = *(INT_PTR*)(pInstrumentationData + schema[i].Offset);
            if (ICorJitInfo::IsUnknownTypeHandle(result))
                return NULL;
            else
                return (CORINFO_CLASS_HANDLE)result;
        }

        bool isHistogramCount =
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramIntCount) ||
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramLongCount);

        if (isHistogramCount && (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramTypeHandle))
        {
            // Form a histogram
            //
            LikelyClassHistogram h((INT_PTR*)(pInstrumentationData + schema[i + 1].Offset), schema[i + 1].Count);

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
            if (ICorJitInfo::IsUnknownTypeHandle(result))
            {
                return NO_CLASS_HANDLE;
            }
            else
            {
                return (CORINFO_CLASS_HANDLE)result;
            }
        }

        bool isHistogramCount =
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramIntCount) ||
            (schema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramLongCount);

        if (isHistogramCount && (schema[i].Count == 1) && ((i + 1) < countSchemaItems) &&
            (schema[i + 1].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramTypeHandle))
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

            if (ICorJitInfo::IsUnknownTypeHandle(randomEntry.m_mt))
            {
                return NO_CLASS_HANDLE;
            }

            return (CORINFO_CLASS_HANDLE)randomEntry.m_mt;
        }
    }

    return NO_CLASS_HANDLE;
}
