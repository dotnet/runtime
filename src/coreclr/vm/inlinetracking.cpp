// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// =============================================================================================
// Code for tracking method inlinings in NGen and R2R images.
// The only information stored is "who" got inlined "where", no offsets or inlining depth tracking.
// (No good for debugger yet.)
// This information is later exposed to profilers and can be useful for ReJIT.
// Runtime inlining is not being tracked because profilers can deduce it via callbacks anyway.
// =============================================================================================
#include "common.h"
#include "inlinetracking.h"
#include "ceeload.h"
#include "versionresilienthashcode.h"

using namespace NativeFormat;

#ifndef DACCESS_COMPILE

bool MethodInModule::operator <(const MethodInModule& other) const
{
    STANDARD_VM_CONTRACT;
    if (m_module == other.m_module)
    {
        return m_methodDef < other.m_methodDef;
    }
    else
    {
        // Since NGen images are supposed to be determenistic,
        // we need stable sort order that isn't changing between different runs
        // That's why we use names and GUIDs instead of just doing m_module < other.m_module

        // First we try to compare simple names (should be fast enough)
        LPCUTF8 simpleName = m_module ? m_module->GetSimpleName() : "";
        LPCUTF8 otherSimpleName = other.m_module ? other.m_module->GetSimpleName() : "";
        int nameCmpResult = strcmp(simpleName, otherSimpleName);

        if (nameCmpResult == 0)
        {
            // Names are equal but module addresses aren't, it's suspicious
            // falling back to module GUIDs
            GUID thisGuid, otherGuid;
            if (m_module == NULL)
            {
                memset(&thisGuid, 0, sizeof(GUID));
            }
            else
            {
                m_module->GetPEAssembly()->GetMVID(&thisGuid);
            }

            if (other.m_module == NULL)
            {
                memset(&otherGuid, 0, sizeof(GUID));
            }
            else
            {
                other.m_module->GetPEAssembly()->GetMVID(&otherGuid);
            }

            return memcmp(&thisGuid, &otherGuid, sizeof(GUID)) < 0;
        }
        else
        {
            return nameCmpResult < 0;
        }
    }
}

bool MethodInModule::operator ==(const MethodInModule& other) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_methodDef == other.m_methodDef &&
           m_module == other.m_module;
}

bool MethodInModule::operator !=(const MethodInModule& other) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_methodDef != other.m_methodDef ||
           m_module != other.m_module;
}


void InlineTrackingEntry::SortAndDeduplicate()
{
    STANDARD_VM_CONTRACT;

    //Sort
    MethodInModule *begin = &m_inliners[0];
    MethodInModule *end = begin + m_inliners.GetCount();
    util::sort(begin, end);

    //Deduplicate
    MethodInModule *left = begin;
    MethodInModule *right = left + 1;
    while (right < end)
    {
        auto rvalue = *right;
        if (*left != rvalue)
        {
            left++;
            if (left != right)
            {
                *left = rvalue;
            }
        }
        right++;
    }

    //Shrink
    int newCount = (int)(left - begin + 1);
    m_inliners.SetCount(newCount);
}

InlineTrackingEntry::InlineTrackingEntry(const InlineTrackingEntry& other)
    :m_inlinee(other.m_inlinee)
{
    STANDARD_VM_CONTRACT;
    m_inliners.Set(other.m_inliners);
}

InlineTrackingEntry & InlineTrackingEntry::operator = (const InlineTrackingEntry &other)
{
    STANDARD_VM_CONTRACT;
    m_inlinee = other.m_inlinee;
    m_inliners.Set(other.m_inliners);
    return *this;
}

void InlineTrackingEntry::Add(PTR_MethodDesc inliner)
{
    STANDARD_VM_CONTRACT;

    MethodInModule method(inliner->GetModule(), inliner->GetMemberDef());

    // Going through last 10 inliners to check if a given inliner has recently been registered.
    // It allows to filter out most duplicates without having to scan through hundreds of inliners
    // for methods like Object.ctor or Monitor.Enter.
    // We are OK to keep occasional duplicates in m_inliners, we'll get rid of them
    // in SortAndDeduplicate() anyway.
    int count = static_cast<int>(m_inliners.GetCount());
    int start = max(0, count - 10);
    for (int i = count - 1; i >= start; i--)
    {
        if (m_inliners[i] == method)
            return;
    }

    //look like we see this inliner for the first time, add it to the collection
    m_inliners.Append(method);
}

InlineTrackingMap::InlineTrackingMap()
    : m_mapCrst(CrstInlineTrackingMap)
{
    STANDARD_VM_CONTRACT;
}

void InlineTrackingMap::AddInlining(MethodDesc *inliner, MethodDesc *inlinee)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(inliner != NULL);
    _ASSERTE(inlinee != NULL);

    MethodInModule inlineeMnM(inlinee->GetModule(), inlinee->GetMemberDef());

    if (RidFromToken(inlineeMnM.m_methodDef) == 0 || RidFromToken(inliner->GetMemberDef()) == 0)
    {
        // Sometimes we do see methods that don't have valid tokens (stubs etc)
        // we just ignore them.
        return;
    }

    CrstHolder lock(&m_mapCrst);
    InlineTrackingEntry *existingEntry = const_cast<InlineTrackingEntry *>(LookupPtr(inlineeMnM));
    if (existingEntry)
    {
        // We saw this inlinee before, just add one more inliner
        existingEntry->Add(inliner);
    }
    else
    {
        // We haven't seen this inlinee before, create a new record in the hashtable
        // and add a first inliner to it.
        InlineTrackingEntry newEntry;
        newEntry.m_inlinee = inlineeMnM;
        newEntry.Add(inliner);
        Add(newEntry);
    }
}

#endif //!DACCESS_COMPILE

#ifdef FEATURE_READYTORUN

struct InliningHeader
{
    int SizeOfInlineeIndex;
};

#ifndef DACCESS_COMPILE

BOOL PersistentInlineTrackingMapR2R::TryLoad(Module* pModule, const BYTE* pBuffer, DWORD cbBuffer,
	                                         AllocMemTracker *pamTracker, PersistentInlineTrackingMapR2R** ppLoadedMap)
{
    InliningHeader* pHeader = (InliningHeader*)pBuffer;
    if (pHeader->SizeOfInlineeIndex > (int)(cbBuffer - sizeof(InliningHeader)))
    {
		//invalid serialized data, the index can't be larger the entire block
		_ASSERTE(!"R2R image is invalid or there is a bug in the R2R parser");
        return FALSE;
    }

	//NOTE: Error checking on the format is very limited at this point.
	//We trust the image format is valid and this initial check is a cheap
	//verification that may help catch simple bugs. It does not secure against
	//a deliberately maliciously formed binary.

	LoaderHeap *pHeap = pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
	void * pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(PersistentInlineTrackingMapR2R)));
	PersistentInlineTrackingMapR2R* pMap = new (pMemory) PersistentInlineTrackingMapR2R();

    pMap->m_module = pModule;
	pMap->m_inlineeIndex = (PTR_ZapInlineeRecord)(pHeader + 1);
	pMap->m_inlineeIndexSize = pHeader->SizeOfInlineeIndex / sizeof(ZapInlineeRecord);
	pMap->m_inlinersBuffer = ((PTR_BYTE)(pHeader+1)) + pHeader->SizeOfInlineeIndex;
	pMap->m_inlinersBufferSize = cbBuffer - sizeof(InliningHeader) - pMap->m_inlineeIndexSize;
	*ppLoadedMap = pMap;
    return TRUE;
}

#endif //!DACCESS_COMPILE

COUNT_T PersistentInlineTrackingMapR2R::GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(inlineeOwnerMod);
    _ASSERTE(inliners != NULL || inlinersSize == 0);

    if (incompleteData)
    {
        *incompleteData = FALSE;
    }
    if (m_inlineeIndex == NULL || m_inlinersBuffer == NULL)
    {
        //No inlines saved in this image.
        return 0;
    }
    if(inlineeOwnerMod != m_module)
    {
        // no cross module inlining (yet?)
        return 0;
    }

    // Binary search to find all records matching (inlineeTkn)
    ZapInlineeRecord probeRecord;
    probeRecord.InitForR2R(RidFromToken(inlineeTkn));
    ZapInlineeRecord *begin = m_inlineeIndex;
    ZapInlineeRecord *end = m_inlineeIndex + m_inlineeIndexSize;
    ZapInlineeRecord *foundRecord = util::lower_bound(begin, end, probeRecord);
    DWORD result = 0;
    DWORD outputIndex = 0;

    // Go through all matching records
    for (; foundRecord < end && *foundRecord == probeRecord; foundRecord++)
    {
        DWORD offset = foundRecord->m_offset;
        NibbleReader stream(m_inlinersBuffer + offset, m_inlinersBufferSize - offset);
        Module *inlinerModule = m_module;

        DWORD inlinersCount = stream.ReadEncodedU32();
        _ASSERTE(inlinersCount > 0);

        RID inlinerRid = 0;
        // Reading inliner RIDs one by one, each RID is represented as an adjustment (diff) to the previous one.
        // Adding inliners module and coping to the output buffer
        for (DWORD i = 0; i < inlinersCount && outputIndex < inlinersSize; i++)
        {
            inlinerRid += stream.ReadEncodedU32();
            mdMethodDef inlinerTkn = TokenFromRid(inlinerRid, mdtMethodDef);
            inliners[outputIndex++] = MethodInModule(inlinerModule, inlinerTkn);
        }
        result += inlinersCount;
    }

    return result;
}

#ifndef DACCESS_COMPILE
BOOL PersistentInlineTrackingMapR2R2::TryLoad(Module* pModule, const BYTE* pBuffer, DWORD cbBuffer,
    AllocMemTracker* pamTracker, PersistentInlineTrackingMapR2R2** ppLoadedMap)
{
    LoaderHeap* pHeap = pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
    void* pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(PersistentInlineTrackingMapR2R2)));
    PersistentInlineTrackingMapR2R2* pMap = new (pMemory) PersistentInlineTrackingMapR2R2();

    pMap->m_module = pModule;

    pMap->m_reader = NativeReader(pBuffer, cbBuffer);
    NativeParser parser = NativeParser(&pMap->m_reader, 0);
    pMap->m_hashtable = NativeHashtable(parser);
    *ppLoadedMap = pMap;
    return TRUE;
}

COUNT_T PersistentInlineTrackingMapR2R2::GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL* incompleteData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(inlineeOwnerMod);
    _ASSERTE(inliners != NULL || inlinersSize == 0);

    if (incompleteData)
    {
        *incompleteData = FALSE;
    }
    DWORD result = 0;

    int hashCode = GetVersionResilientModuleHashCode(inlineeOwnerMod);
    hashCode ^= inlineeTkn;

    NativeHashtable::Enumerator lookup = m_hashtable.Lookup(hashCode);
    NativeParser entryParser;
    while (lookup.GetNext(entryParser))
    {
        DWORD streamSize = entryParser.GetUnsigned();
        _ASSERTE(streamSize > 1);

        // First make sure this is the right inlinee and not just a hash collision

        DWORD inlineeRidAndFlag = entryParser.GetUnsigned();
        streamSize--;
        mdMethodDef inlineeToken = TokenFromRid(inlineeRidAndFlag >> 1, mdtMethodDef);
        if (inlineeToken != inlineeTkn)
        {
            continue;
        }

        Module* inlineeModule;
        if ((inlineeRidAndFlag & 1) != 0)
        {
            inlineeModule = GetModuleByIndex(entryParser.GetUnsigned());
            streamSize--;
            _ASSERTE(streamSize > 0);
        }
        else
        {
            inlineeModule = m_module;
        }

        if (inlineeModule != inlineeOwnerMod)
        {
            continue;
        }

        // We have the right inlinee, let's look at the inliners

        DWORD currentInlinerRid = 0;
        do
        {
            DWORD inlinerRidDeltaAndFlag = entryParser.GetUnsigned();
            streamSize--;

            currentInlinerRid += inlinerRidDeltaAndFlag >> 1;

            Module* inlinerModule;
            if ((inlinerRidDeltaAndFlag & 1) != 0)
            {
                _ASSERTE(streamSize > 0);
                inlinerModule = GetModuleByIndex(entryParser.GetUnsigned());
                streamSize--;
                if (inlinerModule == nullptr && incompleteData)
                {
                    // We can't find module for this inlineeModuleZapIndex, it means it hasn't been loaded yet
                    // (maybe it never will be), we just report it to the profiler.
                    // Profiler might want to try later when more modules are loaded.
                    *incompleteData = TRUE;
                    continue;
                }
            }
            else
            {
                inlinerModule = m_module;
            }

            if (result < inlinersSize)
            {
                inliners[result].m_methodDef = TokenFromRid(currentInlinerRid, mdtMethodDef);
                inliners[result].m_module = inlinerModule;
            }

            result++;
        } while (streamSize > 0);
    }

    return result;
}

Module* PersistentInlineTrackingMapR2R2::GetModuleByIndex(DWORD index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This "black magic spell" has in fact nothing to do with GenericInstantiationCompare per se, but just sets a thread flag
    // that later activates more thorough search inside Module::GetAssemblyIfLoaded, which is indirectly called from GetModuleFromIndexIfLoaded.
    // This is useful when a R2R image was compiled against a different assembly version than the one loaded now.
    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare);

    _ASSERTE(m_module->GetModuleFromIndexIfLoaded(index)->IsFullModule());
    return static_cast<Module*>(m_module->GetModuleFromIndexIfLoaded(index));
}

BOOL CrossModulePersistentInlineTrackingMapR2R::TryLoad(Module* pModule, LoaderAllocator* pLoaderAllocator, const BYTE* pBuffer, DWORD cbBuffer,
    AllocMemTracker* pamTracker, CrossModulePersistentInlineTrackingMapR2R** ppLoadedMap)
{
    LoaderHeap* pHeap = pLoaderAllocator->GetHighFrequencyHeap();
    void* pMemory = pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(CrossModulePersistentInlineTrackingMapR2R)));
    CrossModulePersistentInlineTrackingMapR2R* pMap = new (pMemory) CrossModulePersistentInlineTrackingMapR2R();

    pMap->m_module = pModule;

    pMap->m_reader = NativeReader(pBuffer, cbBuffer);
    NativeParser parser = NativeParser(&pMap->m_reader, 0);
    pMap->m_hashtable = NativeHashtable(parser);
    *ppLoadedMap = pMap;
    return TRUE;
}

void CrossModulePersistentInlineTrackingMapR2R::GetILBodySection(MethodDesc*** pppMethods, COUNT_T* pcMethods)
{
    COUNT_T countImportSections;
    auto pImportSections = m_module->GetImportSections(&countImportSections);

    *pppMethods = NULL;
    *pcMethods = 0;

    for (COUNT_T iSection = 0; iSection < countImportSections; iSection++)
    {
        if (pImportSections[iSection].Type == ReadyToRunImportSectionType::ILBodyFixups)
        {
            COUNT_T cbData;
            *pppMethods = (MethodDesc**)(m_module->GetReadyToRunImage()->GetDirectoryData(&pImportSections[iSection].Section, &cbData));
            *pcMethods = cbData / sizeof(TADDR);
            return;
        }
    }
}

static void GetILBodyTokenInfo(uint32_t ilBodyIndex, MethodDesc** ppMethods, COUNT_T cMethods,  Module **readModule, mdMethodDef *readToken)
{
    *readModule = NULL;
    *readToken = 0;
    if (ilBodyIndex < cMethods)
    {
        PTR_MethodDesc ilBody = VolatileLoad(ppMethods + ilBodyIndex);
        if (ilBody != NULL)
        {
            *readModule = ilBody->GetModule();
            *readToken = ilBody->GetMemberDef();
        }
    }
}

static void AddInliner(uint32_t inlinersSize, MethodInModule inliners[], uint32_t *pInlinerCount, Module* inlinerModule, mdMethodDef inlinerToken, BOOL *incompleteData)
{
    if (inlinerModule == nullptr && incompleteData)
    {
        // We can't find module for this inlineeModuleZapIndex, it means it hasn't been loaded yet
        // (maybe it never will be), we just report it to the profiler.
        // Profiler might want to try later when more modules are loaded.
        *incompleteData = TRUE;
        return;
    }

    if ((*pInlinerCount) < inlinersSize)
    {
        inliners[*pInlinerCount].m_methodDef = inlinerToken;
        inliners[*pInlinerCount].m_module = inlinerModule;
    }
    (*pInlinerCount)++;
}

COUNT_T CrossModulePersistentInlineTrackingMapR2R::GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL* incompleteData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(inlineeOwnerMod);
    _ASSERTE(inliners != NULL || inlinersSize == 0);

    // This api while it is capable of declaring its results incomplete, does not provide a reasonable
    // way for the customer to detect additional inlines made visible by the resolution of ILBody fixups.
    // Thus, once we have queried the set of inliners, it must not increase by the resolution of additional ILBody fixups.
    m_module->GetReadyToRunInfo()->ForbidProcessMoreILBodyFixups();

    if (incompleteData)
    {
        *incompleteData = FALSE;
    }
    uint32_t result = 0;

    int hashCode;
    if (!GetVersionResilientMethodDefHashCode(inlineeOwnerMod->GetMDImport(), inlineeTkn, &hashCode))
    {
        return result;
    }

    bool multiModuleFormat = m_module->GetReadyToRunInfo()->MultiModuleVersionBubble();

    MethodDesc** ppMethods;
    COUNT_T cMethods;
    GetILBodySection(&ppMethods, &cMethods);

    NativeHashtable::Enumerator lookup = m_hashtable.Lookup(hashCode);
    NativeParser entryParser;
    while (lookup.GetNext(entryParser))
    {
        uint32_t streamSize = entryParser.GetUnsigned();
        _ASSERTE(streamSize > 1);

        // First make sure this is the right inlinee and not just a hash collision
        Module* inlineeModule;

        uint32_t inlineeIndexAndFlags = entryParser.GetUnsigned();
        streamSize--;

        uint32_t inlineeIndex = inlineeIndexAndFlags >> (int)ReadyToRunCrossModuleInlineFlags::CrossModuleInlinerIndexShift;
        bool hasCrossModuleInliners = ((ReadyToRunCrossModuleInlineFlags)inlineeIndexAndFlags & ReadyToRunCrossModuleInlineFlags::HasCrossModuleInliners) == ReadyToRunCrossModuleInlineFlags::HasCrossModuleInliners;
        bool crossModuleInlinee = ((ReadyToRunCrossModuleInlineFlags)inlineeIndexAndFlags & ReadyToRunCrossModuleInlineFlags::CrossModuleInlinee) == ReadyToRunCrossModuleInlineFlags::CrossModuleInlinee;

        if (crossModuleInlinee)
        {
            mdMethodDef inlineeToken;
            GetILBodyTokenInfo(inlineeIndex, ppMethods, cMethods, &inlineeModule, &inlineeToken);
            if (inlineeToken != inlineeTkn)
                continue;
        }
        else
        {
            mdMethodDef inlineeToken = TokenFromRid(inlineeIndex, mdtMethodDef);
            if (inlineeTkn != inlineeToken)
                continue;

            if (multiModuleFormat)
            {
                _ASSERTE(streamSize > 0);
                uint32_t moduleIndex = entryParser.GetUnsigned();
                streamSize--;
                inlineeModule = GetModuleByIndex(moduleIndex);
            }
            else
            {
                inlineeModule = m_module;
            }
        }

        if (inlineeModule != inlineeOwnerMod)
        {
            continue;
        }

        // We have the right inlinee, let's look at the inliners

        // First chack for cross module inliners
        if (hasCrossModuleInliners)
        {
            _ASSERTE(streamSize > 0);
            uint32_t crossModuleInlinerCount = entryParser.GetUnsigned();
            streamSize--;

            for (uint32_t iInliner = 0; iInliner < crossModuleInlinerCount; iInliner++)
            {
                _ASSERTE(streamSize > 0);
                uint32_t inlinerIndex = entryParser.GetUnsigned();
                streamSize--;

                Module* inlinerModule = NULL;
                mdMethodDef inlinerToken = 0;
                GetILBodyTokenInfo(inlinerIndex, ppMethods, cMethods, &inlinerModule, &inlinerToken);
                AddInliner(inlinersSize, inliners, &result, inlinerModule, inlinerToken, incompleteData);
            }
        }

        // Then handle the non-cross module ones
        DWORD currentInlinerIndex = 0;
        while (streamSize > 0)
        {
            uint32_t inlinerIndexDeltaAndFlag = entryParser.GetUnsigned();
            streamSize--;

            Module* inlinerModule = inlineeModule;

            if (multiModuleFormat)
            {
                currentInlinerIndex += inlinerIndexDeltaAndFlag >> (int)ReadyToRunCrossModuleInlineFlags::InlinerRidShift;
                if ((((ReadyToRunCrossModuleInlineFlags)inlinerIndexDeltaAndFlag) & ReadyToRunCrossModuleInlineFlags::InlinerRidHasModule) == ReadyToRunCrossModuleInlineFlags::InlinerRidHasModule)
                {
                    _ASSERTE(streamSize >= 0);
                    uint32_t moduleIndex = entryParser.GetUnsigned();
                    streamSize--;
                    inlinerModule = GetModuleByIndex(moduleIndex);
                }
            }
            mdMethodDef inlinerToken = TokenFromRid(currentInlinerIndex, mdtMethodDef);


            AddInliner(inlinersSize, inliners, &result, inlinerModule, inlinerToken, incompleteData);
        }
    }

    return result;
}

Module* CrossModulePersistentInlineTrackingMapR2R::GetModuleByIndex(DWORD index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This "black magic spell" has in fact nothing to do with GenericInstantiationCompare per se, but just sets a thread flag
    // that later activates more thorough search inside Module::GetAssemblyIfLoaded, which is indirectly called from GetModuleFromIndexIfLoaded.
    // This is useful when a R2R image was compiled against a different assembly version than the one loaded now.
    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare);

    _ASSERTE(m_module->GetModuleFromIndexIfLoaded(index)->IsFullModule());
    return static_cast<Module*>(m_module->GetModuleFromIndexIfLoaded(index));
}
#endif //!DACCESS_COMPILE

#endif //FEATURE_READYTORUN


#if !defined(DACCESS_COMPILE)
CrstStatic JITInlineTrackingMap::s_mapCrst;

JITInlineTrackingMap::JITInlineTrackingMap(LoaderAllocator *pAssociatedLoaderAllocator) : m_map()
{
    LIMITED_METHOD_CONTRACT;

    m_map.Init(pAssociatedLoaderAllocator);
}

BOOL JITInlineTrackingMap::InliningExistsDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(s_mapCrst.OwnedByCurrentThread());
    _ASSERTE(inliner != NULL);
    _ASSERTE(inlinee != NULL);

    BOOL found = FALSE;
    auto lambda = [&](LoaderAllocator *loaderAllocatorOfInliner, MethodDesc *lambdaInlinee, MethodDesc *lambdaInliner)
    {
        _ASSERTE(inlinee == lambdaInlinee);

        if (lambdaInliner == inliner)
        {
            found = TRUE;
            return false;
        }

        return true;
    };

    m_map.VisitValuesOfKey(inlinee, lambda);

    return found;
}

void JITInlineTrackingMap::AddInlining(MethodDesc *inliner, MethodDesc *inlinee)
{
    LIMITED_METHOD_CONTRACT;

    inlinee = inlinee->LoadTypicalMethodDefinition();

    CrstHolder holder(&s_mapCrst);
    AddInliningDontTakeLock(inliner, inlinee);
}

void JITInlineTrackingMap::AddInliningDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(s_mapCrst.OwnedByCurrentThread());
    _ASSERTE(inliner != NULL);
    _ASSERTE(inlinee != NULL);

    if (!InliningExistsDontTakeLock(inliner, inlinee))
    {
        LoaderAllocator *loaderAllocatorOfInliner = inliner->GetLoaderAllocator();
        m_map.Add(inlinee, inliner, loaderAllocatorOfInliner);
    }
}

#endif // !defined(DACCESS_COMPILE)
