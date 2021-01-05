// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "log.h"
#include "pgo.h"
#include "versionresilienthashcode.h"

#ifdef FEATURE_PGO

// Data structure for holding pgo data
// Need to be walkable at process shutdown without taking meaningful locks
//  Need to have an associated MethodDesc for emission
//
//  Need to support lookup by Exact method, and at the non-generic level as well
//   In addition, lookup by some form of stable hash would be really nice for both R2R multi-module scenarios
//    as well as the existing text format approach
//
// In the current implementation, the method stable hash code isn't a good replacement for "token" as it doesn't
// carry any detail about signatures, and is probably quite slow to compute
// The plan is to swap over to the typenamehash 

// Goals
// 1. Need to be able to walk at any time.
// 2. Need to be able to lookup by MethodDesc
// 3. Need to be able to lookup by Hash!

// Solution:

// Lookup patterns for use by JIT
// 1. For Current Runtime generated lookups, there is a SHash in each LoaderAllocator, using the MethodDesc as
//    key for non-dynamic methods, and a field in the DynamicMethodDesc for the dynamic methods.
// 2. For R2R lookups, lookup via IL token exact match, as well as a hash based lookup.
// 3. For text based lookups, lookup by hash (only enabled if the ReadPGOData COMPlus is set).

// For emission into output, we will use an approach that relies on walking linked lists
// 1. InstrumentationDataHeader shall be placed before any instrumentation data. It will be part of a linked
//    list of instrumentation data that has the same lifetime.
// 2. InstrumentationDataWithEqualLifetimeHeader shall be part of a doubly linked list. This list shall be protected
//    by a lock, and serves to point at the various singly linked lists of InstrumentationData.

const char* const         PgoManager::s_FileHeaderString  = "*** START PGO Data, max index = %u ***\n";
const char* const         PgoManager::s_FileTrailerString = "*** END PGO Data ***\n";
const char* const         PgoManager::s_MethodHeaderString = "@@@ codehash 0x%08X methodhash 0x%08X ilSize 0x%08X records 0x%08X\n";
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

PtrSHash<PgoManager::Header, PgoManager::CodeAndMethodHash> PgoManager::s_textFormatPgoData;
CrstStatic PgoManager::s_pgoMgrLock;
PgoManager PgoManager::s_InitialPgoManager;

void PgoManager::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_pgoMgrLock.Init(CrstLeafLock, CRST_DEFAULT);

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

    int pgoDataCount = 0;
    EnumeratePGOHeaders([&pgoDataCount](HeaderList *pgoData)
    {
        pgoDataCount++;
        return true;
    });

    if (pgoDataCount == 0)
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

    fprintf(pgoDataFile, s_FileHeaderString, pgoDataCount);

    EnumeratePGOHeaders([pgoDataFile](HeaderList *pgoData)
    {
        fprintf(pgoDataFile, s_MethodHeaderString, pgoData->header.codehash, pgoData->header.methodhash, pgoData->header.ilSize, pgoData->header.recordCount);

        SString tClass, tMethodName, tMethodSignature;
        pgoData->header.method->GetMethodInfo(tClass, tMethodName, tMethodSignature);

        StackScratchBuffer nameBuffer;
        StackScratchBuffer nameBuffer2;
        fprintf(pgoDataFile, "MethodName: %s.%s\n", tClass.GetUTF8(nameBuffer), tMethodName.GetUTF8(nameBuffer2));
        fprintf(pgoDataFile, "Signature: %s\n", tMethodSignature.GetUTF8(nameBuffer));

        ICorJitInfo::BlockCounts* records = pgoData->header.GetData();
        unsigned                  lastOffset  = 0;
        for (unsigned i = 0; i < pgoData->header.recordCount; i++)
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

        return true;
    });

    fprintf(pgoDataFile, s_FileTrailerString);
    fclose(pgoDataFile);
}

void ReadLineAndDiscard(FILE* file)
{
    char buffer[255];
    while (fgets(buffer, sizeof(buffer), file) != NULL)
    {
        auto stringLen = strlen(buffer);
        if (stringLen == 0)
            return;
        
        if (buffer[stringLen - 1] == '\n')
        {
            return;
        }
    }
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


    // Fill in the data
    //
    unsigned methods = 0;
    unsigned probes = 0;

    bool failed = false;

    while (!failed)
    {
        if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
        {
            break;
        }

        // Discard the next two lines that hold the string name of the method
        ReadLineAndDiscard(pgoDataFile);
        ReadLineAndDiscard(pgoDataFile);

        // Find the next method entry line
        //
        unsigned recordCount = 0;
        unsigned codehash    = 0;
        unsigned methodhash  = 0;
        unsigned ilSize      = 0;

        if (sscanf_s(buffer, s_MethodHeaderString, &codehash, &methodhash, &ilSize, &recordCount) != 4)
        {
            continue;
        }

        methods++;

        S_SIZE_T allocationSize = S_SIZE_T(sizeof(Header)) + S_SIZE_T(sizeof(ICorJitInfo::BlockCounts)) * S_SIZE_T(recordCount);
        if (allocationSize.IsOverflow())
            return;

        Header* methodData = (Header*)malloc(allocationSize.Value());
        methodData->HashInit(methodhash, codehash, ilSize, recordCount);
        ICorJitInfo::BlockCounts* blockCounts = methodData->GetData();
        // Read il data
        //
        for (unsigned i = 0; i < recordCount; i++)
        {
            if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
            {
                failed = true;
                break;
            }

            if (sscanf_s(buffer, s_RecordString, &blockCounts[i].ILOffset, &blockCounts[i].ExecutionCount) != 2)
            {
                // This might be class profile data; if so just skip it.
                //
                if (strstr(buffer, "class") != buffer)
                {
                    failed = true;
                    break;
                }
            }
        }

        s_textFormatPgoData.Add(methodData);
        probes += recordCount;
    }
}

void PgoManager::CreatePgoManager(PgoManager* volatile* ppMgr, bool loaderAllocator)
{
    CrstHolder lock(&s_pgoMgrLock);
    if (*ppMgr != NULL)
        return;

    PgoManager* newManager;
    if (loaderAllocator)
        newManager = new LoaderAllocatorPgoManager();
    else
        newManager = new PgoManager();

    VolatileStore((PgoManager**)ppMgr, newManager);
}

void PgoManager::Header::Init(MethodDesc *pMD, unsigned codehash, unsigned ilSize, unsigned recordCount)
{
    this->codehash = codehash;
    this->methodhash = pMD->GetStableHash();
    this->ilSize = ilSize;
    this->method = pMD;
    this->recordCount = recordCount;
}

HRESULT PgoManager::allocMethodBlockCounts(MethodDesc* pMD, UINT32 count,
    ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize)
{
    STANDARD_VM_CONTRACT;

    PgoManager* mgr;
    if (!pMD->IsDynamicMethod())
    {
        mgr = pMD->GetLoaderAllocator()->GetOrCreatePgoManager();
    }
    else
    {
        PgoManager* volatile* ppMgr = pMD->AsDynamicMethodDesc()->GetResolver()->GetDynamicPgoManagerPointer();
        if (ppMgr == NULL)
        {
            return E_NOTIMPL;
        }

        CreatePgoManager(ppMgr, false);
        mgr = *ppMgr;
    }

    if (mgr == NULL)
    {
        return E_NOTIMPL;
    }

    return mgr->allocMethodBlockCountsInstance(pMD, count, pBlockCounts, ilSize);
}

HRESULT PgoManager::allocMethodBlockCountsInstance(MethodDesc* pMD, UINT32 count,
    ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize)
{
    // Initialize our out param
    *pBlockCounts = NULL;
    int codehash;
    if (!GetVersionResilientILCodeHashCode(pMD, &codehash))
    {
        return E_NOTIMPL;
    }

    S_SIZE_T allocationSize = S_SIZE_T(sizeof(HeaderList)) + S_SIZE_T(sizeof(ICorJitInfo::BlockCounts)) * (S_SIZE_T)(count);
    if (allocationSize.IsOverflow())
    {
        return E_NOTIMPL;
    }
    size_t unsafeAllocationSize = allocationSize.Value();
    HeaderList* pHeaderList = NULL;

    if (pMD->IsDynamicMethod())
    {
        HeaderList *currentHeaderList = m_pgoHeaders;
        if (currentHeaderList != NULL)
        {
            if (currentHeaderList->header.recordCount != count)
            {
                return E_NOTIMPL;
            }
            _ASSERTE(currentHeaderList->header.method == pMD);
            *pBlockCounts = currentHeaderList->header.GetData();
            return S_OK;
        }

        pHeaderList = (HeaderList*)pMD->AsDynamicMethodDesc()->GetResolver()->GetJitMetaHeap()->New(unsafeAllocationSize);

        memset(pHeaderList, 0, unsafeAllocationSize);
        pHeaderList->header.Init(pMD, codehash, ilSize, count);
        *pBlockCounts = pHeaderList->header.GetData();
        m_pgoHeaders = pHeaderList;
        return S_OK;
    }
    else
    {
        LoaderAllocatorPgoManager *laPgoManagerThis = (LoaderAllocatorPgoManager *)this;
        CrstHolder (&laPgoManagerThis->m_lock);

        HeaderList* existingData = laPgoManagerThis->m_pgoDataLookup.Lookup(pMD);
        if (existingData != NULL)
        {
            if (existingData->header.recordCount != count)
            {
                return E_NOTIMPL;
            }
            *pBlockCounts = existingData->header.GetData();
            return S_OK;
        }

        AllocMemTracker loaderHeapAllocation;
        pHeaderList = (HeaderList*)loaderHeapAllocation.Track(pMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(allocationSize));
        memset(pHeaderList, 0, unsafeAllocationSize);
        pHeaderList->header.Init(pMD, codehash, ilSize, count);
        pHeaderList->next = m_pgoHeaders;
        *pBlockCounts = pHeaderList->header.GetData();
        laPgoManagerThis->m_pgoDataLookup.Add(pHeaderList);
        loaderHeapAllocation.SuppressRelease();
        m_pgoHeaders = pHeaderList;
        return S_OK;
    }
}

HRESULT PgoManager::getMethodBlockCounts(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
    ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns)
{
    // Initialize our out params
    *pCount = 0;
    *pBlockCounts = NULL;
    *pNumRuns = 0;

    PgoManager *mgr;
    if (!pMD->IsDynamicMethod())
    {
        mgr = pMD->GetLoaderAllocator()->GetPgoManager();
    }
    else
    {
        mgr = pMD->AsDynamicMethodDesc()->GetResolver()->GetDynamicPgoManager();
    }

    HRESULT hr = E_NOTIMPL;
    if (mgr != NULL)
    {
        hr = mgr->getMethodBlockCountsInstance(pMD, ilSize, pCount, pBlockCounts, pNumRuns);
    }

    // If not found in the data from the current run, look in the data from the text file
    if (FAILED(hr) && s_textFormatPgoData.GetCount() > 0)
    {
        COUNT_T methodhash = pMD->GetStableHash();
        int codehash;
        if (GetVersionResilientILCodeHashCode(pMD, &codehash))
        {
            Header *found = s_textFormatPgoData.Lookup(CodeAndMethodHash(codehash, methodhash));
            if (found != NULL)
            {
                *pNumRuns = 1;
                *pCount = found->recordCount;
                *pBlockCounts = found->GetData();
                hr = S_OK;
            }
        }
    }

    return hr;
}

HRESULT PgoManager::getMethodBlockCountsInstance(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
    ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns)
{
    // Initialize our out params
    *pCount = 0;
    *pBlockCounts = NULL;
    *pNumRuns = 0;

    HeaderList *found;

    if (pMD->IsDynamicMethod())
    {
        found = m_pgoHeaders;
    }
    else
    {
        LoaderAllocatorPgoManager *laPgoManagerThis = (LoaderAllocatorPgoManager *)this;
        CrstHolder (&laPgoManagerThis->m_lock);
        found = laPgoManagerThis->m_pgoDataLookup.Lookup(pMD);
    }

    if (found == NULL)
    {
        return E_NOTIMPL;
    }

    *pBlockCounts = found->header.GetData();
    *pNumRuns = 1;
    *pCount = found->header.recordCount;

    return S_OK;
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

void PgoManager::CreatePgoManager(PgoManager** ppMgr, bool loaderAllocator)
{
    *ppMgr = NULL;
}

#endif // FEATURE_PGO
