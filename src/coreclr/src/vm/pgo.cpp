// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "log.h"
#include "pgo.h"

#ifdef FEATURE_PGO

ICorJitInfo::BlockCounts* PgoManager::s_PgoData;
unsigned volatile         PgoManager::s_PgoIndex;
const char* const         PgoManager::s_FileHeaderString  = "*** START PGO Data, max index = %u ***\n";
const char* const         PgoManager::s_FileTrailerString = "*** END PGO Data ***\n";
const char* const         PgoManager::s_MethodHeaderString = "@@@ token 0x%08X hash 0x%08X ilSize 0x%08X records 0x%08X index %u\n";
const char* const         PgoManager::s_RecordString = "ilOffs %u count %u\n";
const char* const         PgoManager::s_ClassProfileHeader = "classProfile iloffs %u samples %u entries %u totalCount %u %s\n";
const char* const         PgoManager::s_ClassProfileEntry = "class %p (%s) count %u\n";

// Data item in class profile histogram
//
struct HistogramEntry
{
    // Class that was observed at runtime
    CORINFO_CLASS_HANDLE m_mt;
    // Number of observations in the table
    unsigned             m_count;
};

// Summarizes a ClassProfile table by forming a Histogram
//
struct Histogram
{
    Histogram(const ICorJitInfo::ClassProfile* classProfile);

    // Number of nonzero entries in the histogram
    unsigned m_count;
    // Sum of counts from all entries in the histogram
    unsigned m_totalCount;
    // Histogram entries, in no particular order.
    // The first m_count of these will be valid.
    HistogramEntry m_histogram[ICorJitInfo::ClassProfile::SIZE];
};

Histogram::Histogram(const ICorJitInfo::ClassProfile* classProfile)
{
    m_count = 0;
    m_totalCount = 0;

    for (unsigned k = 0; k < ICorJitInfo::ClassProfile::SIZE; k++)
    {
        CORINFO_CLASS_HANDLE currentEntry = classProfile->ClassTable[k];
        
        if (currentEntry == NULL)
        {
            continue;
        }
        
        m_totalCount++;
        
        bool found = false;
        unsigned h = 0;
        for(; h < m_count; h++)
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
            m_histogram[h].m_mt = currentEntry;
            m_histogram[h].m_count = 1;
            m_count++;
        }
    }

    // Zero the remainder
    for (unsigned k = m_count; k < ICorJitInfo::ClassProfile::SIZE; k++)
    {
        m_histogram[k].m_mt = 0;
        m_histogram[k].m_count = 0;
    }
}

void PgoManager::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    // If any PGO mode is active, allocate the slab
    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ReadPGOData) > 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WritePGOData) > 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TieredPGO) > 0))
    {
        s_PgoData = new ICorJitInfo::BlockCounts[BUFFER_SIZE];
        s_PgoIndex = 0;
    }

    // If we're reading in counts, do that now
    ReadPgoData();
}

void PgoManager::Shutdown()
{
    WritePgoData();
}

void PgoManager::VerifyAddress(void* address)
{
    _ASSERTE(address > s_PgoData);
    _ASSERTE(address <= s_PgoData + BUFFER_SIZE);
}

void PgoManager::WritePgoData()
{
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WritePGOData) == 0)
    {
        return;
    }

    if (s_PgoData == NULL)
    {
        return;
    }
    CLRConfigStringHolder fileName(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PGODataPath));

    if (fileName == NULL)
    {
        return;
    }

    FILE* const pgoDataFile = _wfopen(fileName, W("w"));

    if (pgoDataFile == NULL)
    {
        return;
    }

    fprintf(pgoDataFile, s_FileHeaderString, s_PgoIndex);
    unsigned       index    = 0;
    const unsigned maxIndex = s_PgoIndex;

    while (index < maxIndex)
    {
        const Header* const header = (Header*)&s_PgoData[index];

        if ((header->recordCount < MIN_RECORD_COUNT) || (header->recordCount > MAX_RECORD_COUNT))
        {
            fprintf(pgoDataFile, "Unreasonable record count %u at index %u\n", header->recordCount, index);
            break;
        }

        fprintf(pgoDataFile, s_MethodHeaderString, header->token, header->hash, header->ilSize, header->recordCount, index);

        index += 2;

        ICorJitInfo::BlockCounts* records         = &s_PgoData[index];
        unsigned                  recordCount     = header->recordCount - 2;
        unsigned                  lastOffset      = 0;
        bool                      hasClassProfile = false;
        unsigned                  i               = 0;

        while (i < recordCount)
        {
            const unsigned thisOffset = records[i].ILOffset;


            if ((thisOffset & ICorJitInfo::ClassProfile::CLASS_FLAG) != 0)
            {
                // remainder must be class probe data
                hasClassProfile = true;
                break;
            }

            lastOffset = thisOffset;
            fprintf(pgoDataFile, s_RecordString, records[i].ILOffset, records[i].ExecutionCount);
            i++;
        }

        if (hasClassProfile)
        {
            fflush(pgoDataFile);

            // Write out histogram of each probe's data.
            // We currently don't expect to be able to read this back in.
            // 
            while (i < recordCount)
            {
                // Should be enough room left for a class profile.
                _ASSERTE(i + sizeof(ICorJitInfo::ClassProfile) / sizeof(ICorJitInfo::BlockCounts) <= recordCount);

                const ICorJitInfo::ClassProfile* classProfile = (ICorJitInfo::ClassProfile*)&s_PgoData[i + index];

                // Form a histogram...
                //
                Histogram h(classProfile);

                // And display...
                //
                // Figure out if this is a virtual or interface probe.
                //
                const char* profileType = "virtual";

                if ((classProfile->ILOffset & ICorJitInfo::ClassProfile::INTERFACE_FLAG) != 0)
                {
                    profileType = "interface";
                }

                // "classProfile iloffs %u samples %u entries %u totalCount %u %s\n";
                //
                fprintf(pgoDataFile, s_ClassProfileHeader, (classProfile->ILOffset & ICorJitInfo::ClassProfile::OFFSET_MASK),
                    classProfile->Count, h.m_count, h.m_totalCount, profileType);

                for (unsigned j = 0; j < h.m_count; j++)
                {
                    CORINFO_CLASS_HANDLE clsHnd = h.m_histogram[j].m_mt;
                    const char* className = "n/a";
#ifdef _DEBUG
                    TypeHandle typeHnd(clsHnd);
                    MethodTable* pMT = typeHnd.AsMethodTable();
                    className = pMT->GetDebugClassName();
#endif
                    fprintf(pgoDataFile, s_ClassProfileEntry, clsHnd, className, h.m_histogram[j].m_count);
                }

                // Advance to next entry.
                //
                i += sizeof(ICorJitInfo::ClassProfile) / sizeof(ICorJitInfo::BlockCounts);
            }
        }

        index += recordCount;
    }

    fprintf(pgoDataFile, s_FileTrailerString);
    fclose(pgoDataFile);
}

void PgoManager::ReadPgoData()
{
    // Skip, if we're not reading, or we're writing profile data, or doing tiered pgo
    //
    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WritePGOData) > 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TieredPGO) > 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ReadPGOData) == 0))
    {
        return;
    }

    // PGO data slab should already be set up, if not, just bail
    //
    if (s_PgoData == NULL)
    {
        return;
    }

    CLRConfigStringHolder fileName(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PGODataPath));

    if (fileName == NULL)
    {
        return;
    }

    FILE* const pgoDataFile = _wfopen(fileName, W("r"));

    if (pgoDataFile == NULL)
    {
        return;
    }

    char     buffer[256];
    unsigned maxIndex = 0;

    // Header must be first line
    //
    if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
    {
        return;
    }

    if (sscanf_s(buffer, s_FileHeaderString, &maxIndex) != 1)
    {
        return;
    }

    // Sanity check data will fit into the slab
    //
    if ((maxIndex == 0) || (maxIndex >= MAX_RECORD_COUNT))
    {
        return;
    }

    // Fill in the data
    //
    unsigned index   = 0;
    unsigned methods = 0;
    unsigned probes = 0;

    bool failed = false;
    while (!failed)
    {
        if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
        {
            break;
        }

        // Find the next method entry line
        //
        unsigned recordCount = 0;
        unsigned token       = 0;
        unsigned hash        = 0;
        unsigned ilSize      = 0;
        unsigned rIndex      = 0;

        if (sscanf_s(buffer, s_MethodHeaderString, &token, &hash, &ilSize, &recordCount, &rIndex) != 5)
        {
            continue;
        }

        _ASSERTE(index == rIndex);
        methods++;

        // If there's not enough room left, bail
        if ((index + recordCount) > maxIndex)
        {
            failed = true;
            break;
        }

        Header* const header = (Header*)&s_PgoData[index];

        header->recordCount = recordCount;
        header->token       = token;
        header->hash        = hash;
        header->ilSize      = ilSize;

        // Sanity check
        //
        if ((recordCount < MIN_RECORD_COUNT) || (recordCount > MAX_RECORD_COUNT))
        {
            failed = true;
            break;
        }

        index += 2;

        // Read il data
        //
        for (unsigned i = 0; i < recordCount - 2; i++)
        {
            if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
            {
                failed = true;
                break;
            }

            if (sscanf_s(buffer, s_RecordString, &s_PgoData[index].ILOffset, &s_PgoData[index].ExecutionCount) != 2)
            {
                // This might be class profile data; if so just skip it.
                //
                if (strstr(buffer, "class") != buffer)
                {
                    failed = true;
                    break;
                }
            }

            index++;
        }

        probes += recordCount - 2;
    }

    s_PgoIndex = maxIndex;
}

HRESULT PgoManager::allocMethodBlockCounts(MethodDesc* pMD, UINT32 count,
    ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize)
{
    // Initialize our out param
    *pBlockCounts = NULL;

    if (s_PgoData == nullptr)
    {
        return E_NOTIMPL;
    }

    unsigned methodIndex = 0;
    unsigned recordCount = count + 2;

    // Look for space in the profile buffer for this method.
    // Note other jit invocations may be vying for space concurrently.
    //
    while (true)
    {
        const unsigned oldIndex = s_PgoIndex;
        const unsigned newIndex = oldIndex + recordCount;

        // If there is no room left for this method,
        // that's ok, we just won't profile this method.
        //
        if (newIndex >= BUFFER_SIZE)
        {
            return E_NOTIMPL;
        }

        const unsigned updatedIndex = InterlockedCompareExchangeT(&s_PgoIndex, newIndex, oldIndex);

        if (updatedIndex == oldIndex)
        {
            // Found space
            methodIndex = oldIndex;
            break;
        }
    }

    // Fill in the header
    Header* const header = (Header*)&s_PgoData[methodIndex];
    header->recordCount = recordCount;
    header->token = pMD->IsDynamicMethod() ? 0 : pMD->GetMemberDef();
    header->hash = pMD->GetStableHash();
    header->ilSize = ilSize;

    // Return pointer to start of count records
    *pBlockCounts = &s_PgoData[methodIndex + 2];
    return S_OK;
}

HRESULT PgoManager::getMethodBlockCounts(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
    ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns)
{
    // Initialize our out params
    *pCount = 0;
    *pBlockCounts = NULL;
    *pNumRuns = 0;

   // Bail if there's no profile data.
    //
    if (s_PgoData == NULL)
    {
        return E_NOTIMPL;
    }

    // See if we can find counts for this method in the profile buffer.
    //
    const unsigned maxIndex = s_PgoIndex;
    const unsigned token    = pMD->IsDynamicMethod() ? 0 : pMD->GetMemberDef();
    const unsigned hash     = pMD->GetStableHash();


    unsigned index = 0;
    unsigned methodsChecked = 0;

    while (index < maxIndex)
    {
        // The first two "records" of each entry are actually header data
        // to identify the method.
        //
        Header* const header = (Header*)&s_PgoData[index];

        // Sanity check that header data looks reasonable. If not, just
        // fail the lookup.
        //
        if ((header->recordCount < MIN_RECORD_COUNT) || (header->recordCount > MAX_RECORD_COUNT))
        {
            break;
        }

        // See if the header info matches the current method.
        //
        if ((header->token == token) && (header->hash == hash) && (header->ilSize == ilSize))
        {
            // Yep, found data.
            //
            *pBlockCounts = &s_PgoData[index + 2];
            *pCount       = header->recordCount - 2;
            *pNumRuns     = 1;
            return S_OK;
        }

        index += header->recordCount;
        methodsChecked++;
    }

    return E_NOTIMPL;
}

// See if there is a class profile for this method at the indicated il Offset.
// If so, return the most frequently seen class, along with the likelihood that
// it was the class seen, and the total number of classes seen.
//
// Return NULL if there is no profile data to be found.
//
CORINFO_CLASS_HANDLE PgoManager::getLikelyClass(MethodDesc* pMD, unsigned ilSize, unsigned ilOffset, UINT32* pLikelihood, UINT32* pNumberOfClasses)
{
    *pLikelihood = 0;
    *pNumberOfClasses = 0;

    // Bail if there's no profile data.
    //
    if (s_PgoData == NULL)
    {
        return NULL;
    }

    // See if we can find profile data for this method in the profile buffer.
    //
    const unsigned maxIndex = s_PgoIndex;
    const unsigned token    = pMD->IsDynamicMethod() ? 0 : pMD->GetMemberDef();
    const unsigned hash     = pMD->GetStableHash();

    unsigned index = 0;
    unsigned methodsChecked = 0;

    while (index < maxIndex)
    {
        // The first two "records" of each entry are actually header data
        // to identify the method.
        //
        Header* const header = (Header*)&s_PgoData[index];

        // Sanity check that header data looks reasonable. If not, just
        // fail the lookup.
        //
        if ((header->recordCount < MIN_RECORD_COUNT) || (header->recordCount > MAX_RECORD_COUNT))
        {
            break;
        }

        // See if the header info matches the current method.
        //
        if ((header->token == token) && (header->hash == hash) && (header->ilSize == ilSize))
        {
            // Yep, found data. See if there is a suitable class profile.
            //
            // This bit is currently somewhat hacky ... we scan the records, the count records come
            // first and are in increasing IL offset order. Class profiles have inverted IL offsets
            // so when we find an offset with high bit set, it's going to be an class profile.
            //
            unsigned countILOffset = 0;
            unsigned j = 2;

            // Skip past all the count entries
            //
            while (j < header->recordCount)
            {
                if ((s_PgoData[index + j].ILOffset & ICorJitInfo::ClassProfile::CLASS_FLAG) != 0)
                {
                    break;
                }

                countILOffset = s_PgoData[index + j].ILOffset;
                j++;
            }

            // Now we're in the "class profile" portion of the slab for this method.
            // Look for the one that has the right IL offset.
            //
            while (j < header->recordCount)
            {
                const ICorJitInfo::ClassProfile* const classProfile = (ICorJitInfo::ClassProfile*)&s_PgoData[index + j];

                if ((classProfile->ILOffset & ICorJitInfo::ClassProfile::OFFSET_MASK) != ilOffset)
                {
                    // Need to make sure this is even divisor
                    //
                    j += sizeof(ICorJitInfo::ClassProfile) / sizeof(ICorJitInfo::BlockCounts);
                    continue;
                }

                // Form a histogram
                //
                Histogram h(classProfile);

                // Use histogram count as number of classes estimate
                //
                *pNumberOfClasses = h.m_count;

                // Report back what we've learned
                // (perhaps, use count to augment likelihood?)
                // 
                switch (h.m_count)
                {
                    case 0:
                    {
                        return NULL;
                    }
                    break;

                    case 1:
                    {
                        *pLikelihood = 100;
                        return h.m_histogram[0].m_mt;
                    }
                    break;

                    case 2:
                    {
                        if (h.m_histogram[0].m_count >= h.m_histogram[1].m_count)
                        {
                            *pLikelihood = (100 * h.m_histogram[0].m_count) / h.m_totalCount;
                            return h.m_histogram[0].m_mt;
                        }
                        else
                        {
                            *pLikelihood = (100 * h.m_histogram[1].m_count) / h.m_totalCount;
                            return h.m_histogram[1].m_mt;
                        }
                    }
                    break;

                    default:
                    {
                        // Find maximum entry and return it
                        //
                        unsigned maxIndex = 0;
                        unsigned maxCount = 0;

                        for (unsigned m = 0; m < h.m_count; m++)
                        {
                            if (h.m_histogram[m].m_count > maxCount)
                            {
                                maxIndex = m;
                                maxCount = h.m_histogram[m].m_count;
                            }
                        }

                        if (maxCount > 0)
                        {
                            *pLikelihood = (100 * maxCount) / h.m_totalCount;
                            return h.m_histogram[maxIndex].m_mt;
                        }

                        return NULL;
                    }
                    break;
                }
            }

            // Failed to find a class profile entry
            //
            return NULL;
        }

        index += header->recordCount;
        methodsChecked++;
    }

    // Failed to find any sort of profile data for this method
    //
    return NULL;
}

#else

// Stub version for !FEATURE_PGO builds
//
HRESULT PgoManager::allocMethodBlockCounts(MethodDesc* pMD, UINT32 count,
    ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize)
{
    pBlockCounts = NULL;
    return E_NOTIMPL;
}

// Stub version for !FEATURE_PGO builds
//
HRESULT PgoManager::getMethodBlockCounts(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
    ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns)
{
    pBlockCounts = NULL;
    pCount = 0;
    pNumRuns = 0;
    return E_NOTIMPL;
}

// Stub version for !FEATURE_PGO builds
//
CORINFO_CLASS_HANDLE PgoManager::getLikelyClass(MethodDesc* pMD, unsigned ilSize, unsigned ilOffset)
{
    return NULL;
}

#endif // FEATURE_PGO
