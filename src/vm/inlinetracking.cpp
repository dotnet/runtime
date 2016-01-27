// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// =============================================================================================
// Code for tracking method inlinings in NGen images.
// The only information stored is "who" got inlined "where", no offsets or inlining depth tracking. 
// (No good for debugger yet.)
// This information is later exposed to profilers and can be useful for ReJIT.
// Runtime inlining is not being tracked because profilers can deduce it via callbacks anyway.
// =============================================================================================
#include "common.h"
#include "inlinetracking.h"
#include "ceeload.h"

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


#ifndef DACCESS_COMPILE
COUNT_T PersistentInlineTrackingMap::GetInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData)
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
    InlineeRecord probeRecord(RidFromToken(inlineeTkn), inlineeOwnerMod->GetSimpleName());
    InlineeRecord *begin = m_inlineeIndex;
    InlineeRecord *end = m_inlineeIndex + m_inlineeIndexSize;
    InlineeRecord *foundRecord = util::lower_bound(begin, end, probeRecord);
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

Module *PersistentInlineTrackingMap::GetModuleByIndex(DWORD index)
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

PersistentInlineTrackingMap::InlineeRecord::InlineeRecord(RID rid, LPCUTF8 simpleName)
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

void PersistentInlineTrackingMap::ProcessInlineTrackingEntry(DataImage *image, SBuffer *inlinersBuffer, SArray<InlineeRecord> *inlineeIndex, InlineTrackingEntry *entry)
{
    STANDARD_VM_CONTRACT;
    // This call removes duplicates from inliners and makes sure they are sorted by module
    entry->SortAndDeduplicate();
    MethodInModule inlinee = entry->m_inlinee;
    DWORD inlineeModuleZapIndex = image->GetModuleImportIndex(inlinee.m_module);
    InlineSArray<MethodInModule, 3> &inliners = entry->m_inliners;
    COUNT_T tatalInlinersCount = inliners.GetCount();
    _ASSERTE(tatalInlinersCount > 0);

    COUNT_T sameModuleCount;
    // Going through all inliners and grouping them by their module, for each module we'll create
    // InlineeRecord and encode inliners as bytes in inlinersBuffer.
    for (COUNT_T thisModuleBegin = 0; thisModuleBegin < tatalInlinersCount; thisModuleBegin += sameModuleCount)
    {
        Module *lastInlinerModule = inliners[thisModuleBegin].m_module;
        DWORD lastInlinerModuleZapIndex = image->GetModuleImportIndex(lastInlinerModule);
        
        // Counting how many inliners belong to this module
        sameModuleCount = 1;
        while (thisModuleBegin + sameModuleCount < tatalInlinersCount &&
            inliners[thisModuleBegin + sameModuleCount].m_module == lastInlinerModule)
        {
            sameModuleCount++;
        }

        // Saving module indexes and number of inliners
        NibbleWriter inlinersStream;
        inlinersStream.WriteEncodedU32(inlineeModuleZapIndex);
        inlinersStream.WriteEncodedU32(lastInlinerModuleZapIndex);
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
        InlineeRecord record(RidFromToken(inlinee.m_methodDef), inlinee.m_module->GetSimpleName());
        DWORD inlinersStreamSize;
        const BYTE *inlinersStreamPtr = (const BYTE *)inlinersStream.GetBlob(&inlinersStreamSize);
        record.m_offset = inlinersBuffer->GetSize();
        inlinersBuffer->Insert(inlinersBuffer->End(), SBuffer(SBuffer::Immutable, inlinersStreamPtr, inlinersStreamSize));

        inlineeIndex->Append(record);
    }
}

bool compare_entry(const InlineTrackingEntry* first, const InlineTrackingEntry* second)
{
    return first->m_inlinee < second->m_inlinee;
}

void PersistentInlineTrackingMap::Save(DataImage *image, InlineTrackingMap* runtimeMap)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(image != NULL);
    _ASSERTE(runtimeMap != NULL);

    SArray<InlineeRecord> inlineeIndex;
    SBuffer inlinersBuffer;

    // Sort records from runtimeMap, because we need to make sure 
    // we save everything in deterministic order. Hashtable iteration is not deterministic.
    COUNT_T runtimeMapCount = runtimeMap->GetCount();
    InlineTrackingEntry **inlinees = new InlineTrackingEntry *[runtimeMapCount];
    NewArrayHolder<InlineTrackingEntry *>inlineesHolder(inlinees);
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
        ProcessInlineTrackingEntry(image, &inlinersBuffer, &inlineeIndex, inlinees[i]);
    }

    m_inlineeIndexSize = inlineeIndex.GetCount();
    m_inlinersBufferSize = inlinersBuffer.GetSize();
    _ASSERTE((m_inlineeIndexSize == 0) == (m_inlinersBufferSize == 0)); 

    if (m_inlineeIndexSize != 0 && m_inlinersBufferSize != 0)
    {
        // Copy everything to the class fields, we didn't use the class fields for addition
        // because we want to make sure we don't waste memory for buffer's amortized growth
        m_inlineeIndex = new (image->GetHeap()) InlineeRecord[m_inlineeIndexSize];
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

void PersistentInlineTrackingMap::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMap, m_module));
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMap, m_inlineeIndex));
    image->FixupPointerField(this, offsetof(PersistentInlineTrackingMap, m_inlinersBuffer));
}
#endif //FEATURE_NATIVE_IMAGE_GENERATION
#endif //!DACCESS_COMPILE
