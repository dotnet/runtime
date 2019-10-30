// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
                m_module->GetFile()->GetMVID(&thisGuid);
            }

            if (other.m_module == NULL)
            {
                memset(&otherGuid, 0, sizeof(GUID));
            }
            else
            {
                other.m_module->GetFile()->GetMVID(&otherGuid);
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

void ZapInlineeRecord::InitForNGen(RID rid, LPCUTF8 simpleName)
{
    LIMITED_METHOD_CONTRACT;
    //XOR of up to first 24 bytes in module name
    DWORD hash = 0;
    for (int i = 0; simpleName[i] && i < 24; i++)
        hash ^= (BYTE)simpleName[i];

    // This key contains 24 bits of RID and 8 bits from module name.
    // Since RID can't be longer than 24 bits, we can't have method RID collistions,
    // that's why PersistentInlineTrackingMap::GetInliners only deals with module collisions.
    m_key = (hash << 24) | rid;
}


COUNT_T PersistentInlineTrackingMapNGen::GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(inlineeOwnerMod);
    _ASSERTE(inliners);

    if (incompleteData)
    {
        *incompleteData = FALSE;
    }
    if (m_inlineeIndex == NULL || m_inlinersBuffer == NULL)
    {
        //No inlines saved in this image.
        return 0;
    }

    // Binary search to find all records matching (inlineeTkn/inlineeOwnerMod)
    ZapInlineeRecord probeRecord;
    probeRecord.InitForNGen(RidFromToken(inlineeTkn), inlineeOwnerMod->GetSimpleName());
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

        DWORD inlineeModuleZapIndex = stream.ReadEncodedU32();
        Module *decodedInlineeModule = GetModuleByIndex(inlineeModuleZapIndex);

        // Check if this is just token/method name hash collision
        if (decodedInlineeModule == inlineeOwnerMod)
        {
            // We found the token and the module we were looking for!
            DWORD inlinerModuleZapIndex = stream.ReadEncodedU32(); //read inliner module, it is same for all inliners
            Module *inlinerModule = GetModuleByIndex(inlinerModuleZapIndex);

            if (inlinerModule != NULL) 
            {
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
            else
            {
                // We can't find module for this inlineeModuleZapIndex, it means it hasn't been loaded yet
                // (maybe it never will be), we just report it to the profiler.
                // Profiler might want to try later when more modules are loaded.
                if (incompleteData)
                {
                    *incompleteData = TRUE;
                }
            }
        }
    }

    return result;
}



Module *PersistentInlineTrackingMapNGen::GetModuleByIndex(DWORD index)
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
    // This is useful when ngen image was compiler against a different assembly version than the one loaded now. 
    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare);

    return m_module->GetModuleFromIndexIfLoaded(index);
}



#ifndef DACCESS_COMPILE
#ifdef FEATURE_NATIVE_IMAGE_GENERATION

// This is a shared serialization routine used for both NGEN and R2R formats. If image != NULL the NGEN format is generated, otherwise the R2R format
void SerializeInlineTrackingEntry(DataImage* image, SBuffer *inlinersBuffer, SArray<ZapInlineeRecord> *inlineeIndex, InlineTrackingEntry *entry)
{
    STANDARD_VM_CONTRACT;
    // This call removes duplicates from inliners and makes sure they are sorted by module
    entry->SortAndDeduplicate();
    MethodInModule inlinee = entry->m_inlinee;
    DWORD inlineeModuleZapIndex = 0;
    if (image != NULL)
    {
        inlineeModuleZapIndex = image->GetModuleImportIndex(inlinee.m_module);
    }
    InlineSArray<MethodInModule, 3> &inliners = entry->m_inliners;
    COUNT_T totalInlinersCount = inliners.GetCount();
    _ASSERTE(totalInlinersCount > 0);

    COUNT_T sameModuleCount;
    // Going through all inliners and grouping them by their module, for each module we'll create
    // an ZapInlineeRecord and encode inliners as bytes in inlinersBuffer.
    for (COUNT_T thisModuleBegin = 0; thisModuleBegin < totalInlinersCount; thisModuleBegin += sameModuleCount)
    {
        Module *lastInlinerModule = inliners[thisModuleBegin].m_module;
        DWORD lastInlinerModuleZapIndex = 0;
        if (image != NULL)
        {
            lastInlinerModuleZapIndex = image->GetModuleImportIndex(lastInlinerModule);
        }

        // Counting how many inliners belong to this module
        sameModuleCount = 1;
        while (thisModuleBegin + sameModuleCount < totalInlinersCount &&
            inliners[thisModuleBegin + sameModuleCount].m_module == lastInlinerModule)
        {
            sameModuleCount++;
        }

        // Saving module indexes and number of inliners
        NibbleWriter inlinersStream;
        if (image != NULL)
        {
            inlinersStream.WriteEncodedU32(inlineeModuleZapIndex);
            inlinersStream.WriteEncodedU32(lastInlinerModuleZapIndex);
        }
        inlinersStream.WriteEncodedU32(sameModuleCount);

        // Saving inliners RIDs, each new RID is represented as an adjustment (diff) to the previous one
        RID prevMethodRid = 0;
        for (COUNT_T i = thisModuleBegin; i < thisModuleBegin + sameModuleCount; i++)
        {
            RID methodRid = RidFromToken(inliners[i].m_methodDef);
            _ASSERTE(methodRid >= prevMethodRid);
            inlinersStream.WriteEncodedU32(methodRid - prevMethodRid);
            prevMethodRid = methodRid;
        }
        inlinersStream.Flush();
        
        // Copy output of NibbleWriter into a big buffer (inlinersBuffer) for inliners from the same module
        // and create an InlineeRecord with correct offset
        DWORD inlinersStreamSize;
        const BYTE *inlinersStreamPtr = (const BYTE *)inlinersStream.GetBlob(&inlinersStreamSize);
        ZapInlineeRecord record;
        if (image != NULL)
        {
            record.InitForNGen(RidFromToken(inlinee.m_methodDef), inlinee.m_module->GetSimpleName());
        }
        else
        {
            record.InitForR2R(RidFromToken(inlinee.m_methodDef));
        }
        record.m_offset = inlinersBuffer->GetSize();
        inlinersBuffer->Insert(inlinersBuffer->End(), SBuffer(SBuffer::Immutable, inlinersStreamPtr, inlinersStreamSize));
        inlineeIndex->Append(record);
    }
}

bool compare_entry(const InlineTrackingEntry* first, const InlineTrackingEntry* second)
{
    return first->m_inlinee < second->m_inlinee;
}

// This is a shared serialization routine used for both NGEN and R2R formats. If image != NULL the NGEN format is generated, otherwise the R2R format
void SerializeTrackingMapBuffers(ZapHeap* heap, DataImage *image, SBuffer *inlinersBuffer, SArray<ZapInlineeRecord> *inlineeIndex, InlineTrackingMap* runtimeMap)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(runtimeMap != NULL);

    // Sort records from runtimeMap, because we need to make sure 
    // we save everything in deterministic order. Hashtable iteration is not deterministic.
    COUNT_T runtimeMapCount = runtimeMap->GetCount();
    InlineTrackingEntry **inlinees = new (heap) InlineTrackingEntry *[runtimeMapCount];
    int index = 0;
    for (auto iter = runtimeMap->Begin(), end = runtimeMap->End(); iter != end; ++iter)
    {
        inlinees[index++] = const_cast<InlineTrackingEntry *>(&*iter);
    }
    util::sort(inlinees, inlinees + runtimeMapCount, compare_entry);


    // Iterate throught each inlinee record from the InlineTrackingMap
    // and write corresponding records into inlineeIndex and inlinersBuffer
    for (COUNT_T i = 0; i < runtimeMapCount; i++)
    {
        SerializeInlineTrackingEntry(image, inlinersBuffer, inlineeIndex, inlinees[i]);
    }
}



void PersistentInlineTrackingMapNGen::Save(DataImage *image, InlineTrackingMap* runtimeMap)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(image != NULL);
    _ASSERTE(runtimeMap != NULL);

    SArray<ZapInlineeRecord> inlineeIndex;
    SBuffer inlinersBuffer;

    SerializeTrackingMapBuffers(image->GetHeap(), image, &inlinersBuffer, &inlineeIndex, runtimeMap);

    m_inlineeIndexSize = inlineeIndex.GetCount();
    m_inlinersBufferSize = inlinersBuffer.GetSize();
    _ASSERTE((m_inlineeIndexSize == 0) == (m_inlinersBufferSize == 0)); 

    if (m_inlineeIndexSize != 0 && m_inlinersBufferSize != 0)
    {
        // Copy everything to the class fields, we didn't use the class fields for addition
        // because we want to make sure we don't waste memory for buffer's amortized growth
        m_inlineeIndex = new (image->GetHeap()) ZapInlineeRecord[m_inlineeIndexSize];
        inlineeIndex.Copy(m_inlineeIndex, inlineeIndex.Begin(), m_inlineeIndexSize);

        m_inlinersBuffer = new (image->GetHeap()) BYTE[m_inlinersBufferSize];
        inlinersBuffer.Copy(m_inlinersBuffer, inlinersBuffer.Begin(), m_inlinersBufferSize);

        //Sort m_inlineeIndex so we can later use binary search 
        util::sort(m_inlineeIndex, m_inlineeIndex + m_inlineeIndexSize);

        //Making sure all this memory actually gets saved into NGEN image
        image->StoreStructure(m_inlineeIndex, m_inlineeIndexSize * sizeof(m_inlineeIndex[0]), DataImage::ITEM_INLINING_DATA);
        image->StoreStructure(m_inlinersBuffer, m_inlinersBufferSize, DataImage::ITEM_INLINING_DATA);
    }

    image->StoreStructure(this, sizeof(*this), DataImage::ITEM_INLINING_DATA);
    LOG((LF_ZAP, LL_INFO100000,
        "PersistentInlineTrackingMap saved. InlineeIndexSize: %d bytes, InlinersBufferSize: %d bytes\n",
        m_inlineeIndexSize * sizeof(m_inlineeIndex[0]), m_inlinersBufferSize));
}

void PersistentInlineTrackingMapNGen::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMapNGen, m_module));
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMapNGen, m_inlineeIndex));
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMapNGen, m_inlinersBuffer));
}

#endif //FEATURE_NATIVE_IMAGE_GENERATION
#endif //!DACCESS_COMPILE

#ifdef FEATURE_READYTORUN

struct InliningHeader
{
    int SizeOfInlineeIndex;
};

#ifndef DACCESS_COMPILE
#ifdef FEATURE_NATIVE_IMAGE_GENERATION



void PersistentInlineTrackingMapR2R::Save(ZapHeap* pHeap, SBuffer* pSaveTarget, InlineTrackingMap* runtimeMap)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pSaveTarget != NULL);
    _ASSERTE(runtimeMap != NULL);

    SArray<ZapInlineeRecord> inlineeIndex;
    SBuffer inlinersBuffer;

    SerializeTrackingMapBuffers(pHeap, NULL, &inlinersBuffer, &inlineeIndex, runtimeMap);

    InliningHeader header;
    header.SizeOfInlineeIndex = inlineeIndex.GetCount() * sizeof(ZapInlineeRecord);
    
    pSaveTarget->Insert(pSaveTarget->End(), SBuffer(SBuffer::Immutable, (const BYTE*) &header, sizeof(header)));
    DWORD unused = 0;
    pSaveTarget->Insert(pSaveTarget->End(), SBuffer(SBuffer::Immutable, (const BYTE*) inlineeIndex.GetElements(), header.SizeOfInlineeIndex));
    pSaveTarget->Insert(pSaveTarget->End(), SBuffer(SBuffer::Immutable, (const BYTE*) inlinersBuffer, inlinersBuffer.GetSize()));

    LOG((LF_ZAP, LL_INFO100000,
        "PersistentInlineTrackingMap saved. InlineeIndexSize: %d bytes, InlinersBufferSize: %d bytes\n",
        header.SizeOfInlineeIndex, inlinersBuffer.GetSize()));
}

#endif //FEATURE_NATIVE_IMAGE_GENERATION

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
    _ASSERTE(inliners);

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

#endif //FEATURE_READYTORUN


#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
JITInlineTrackingMap::JITInlineTrackingMap(LoaderAllocator *pAssociatedLoaderAllocator) :
    m_mapCrst(CrstJitInlineTrackingMap),
    m_map()
{
    LIMITED_METHOD_CONTRACT;

    m_map.Init(pAssociatedLoaderAllocator);
}

BOOL JITInlineTrackingMap::InliningExistsDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_mapCrst.OwnedByCurrentThread());
    _ASSERTE(inliner != NULL);
    _ASSERTE(inlinee != NULL);

    BOOL found = FALSE;
    auto lambda = [&](OBJECTREF obj, MethodDesc *lambdaInlinee, MethodDesc *lambdaInliner)
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

    CrstHolder holder(&m_mapCrst);
    AddInliningDontTakeLock(inliner, inlinee);
}

void JITInlineTrackingMap::AddInliningDontTakeLock(MethodDesc *inliner, MethodDesc *inlinee)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_mapCrst.OwnedByCurrentThread());
    _ASSERTE(inliner != NULL);
    _ASSERTE(inlinee != NULL);

    GCX_COOP();

    if (!InliningExistsDontTakeLock(inliner, inlinee))
    {
        LoaderAllocator *loaderAllocatorOfInliner = inliner->GetLoaderAllocator();
        m_map.Add(inlinee, inliner, loaderAllocatorOfInliner);
    }
}

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
