// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "log.h"
#include "pgo.h"

#ifdef FEATURE_PGO

ICorJitInfo::BlockCounts* PgoManager::s_PgoData;
unsigned                  PgoManager::s_PgoIndex;
const char* const         PgoManager::s_FileHeaderString  = "*** START PGO Data, max index = %u ***\n";
const char* const         PgoManager::s_FileTrailerString = "*** END PGO Data ***\n";
const char* const         PgoManager::s_MethodHeaderString = "@@@ token 0x%08X hash 0x%08X ilSize 0x%08X records 0x%08X index %u\n";
const char* const         PgoManager::s_RecordString = "ilOffs %u count %u\n";

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

        ICorJitInfo::BlockCounts* records     = &s_PgoData[index];
        unsigned                  recordCount = header->recordCount - 2;
        unsigned                  lastOffset  = 0;
        for (unsigned i = 0; i < recordCount; i++)
        {
            const unsigned thisOffset = records[i].ILOffset;
            assert((thisOffset > lastOffset) || (lastOffset == 0));
            lastOffset = thisOffset;
            fprintf(pgoDataFile, s_RecordString, records[i].ILOffset, records[i].ExecutionCount);
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

        assert(index == rIndex);
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
                failed = true;
                break;
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

#endif // FEATURE_PGO
