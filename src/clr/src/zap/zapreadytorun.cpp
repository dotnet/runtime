//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// ZapReadyToRun.cpp
//

//
// Zapping of ready-to-run specific structures
// 
// ======================================================================================

#include "common.h"

#include "zapreadytorun.h"

#include "zapimport.h"

#include "nativeformatwriter.h"

#include "nibblestream.h"

using namespace NativeFormat;

void ZapReadyToRunHeader::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    READYTORUN_HEADER readyToRunHeader;

    ZeroMemory(&readyToRunHeader, sizeof(readyToRunHeader));

    readyToRunHeader.Signature = READYTORUN_SIGNATURE;
    readyToRunHeader.MajorVersion = READYTORUN_MAJOR_VERSION;
    readyToRunHeader.MinorVersion = READYTORUN_MINOR_VERSION;

    if (pImage->m_ModuleDecoder.IsPlatformNeutral())
        readyToRunHeader.Flags |= READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE;

    // If all types loaded succesfully, set a flag to skip type loading sanity checks at runtime
    if (pImage->GetCompileInfo()->AreAllClassesFullyLoaded(pImage->GetModuleHandle()))
        readyToRunHeader.Flags |= READYTORUN_FLAG_SKIP_TYPE_VALIDATION;

    readyToRunHeader.NumberOfSections = m_Sections.GetCount();

    pZapWriter->Write(&readyToRunHeader, sizeof(readyToRunHeader));

    qsort(&m_Sections[0], m_Sections.GetCount(), sizeof(Section), SectionCmp);

    for(COUNT_T i = 0; i < m_Sections.GetCount(); i++)
    {
        READYTORUN_SECTION section;
        section.Type = m_Sections[i].type;
        ZapWriter::SetDirectoryData(&section.Section, m_Sections[i].pSection);
        pZapWriter->Write(&section, sizeof(section));
    }
}

class BlobVertex : public NativeFormat::Vertex
{
    int m_cbSize;

public:
    BlobVertex(int cbSize)
        : m_cbSize(cbSize)
    {
    }

    void * GetData()
    {
        return this + 1;
    }

    int GetSize()
    {
        return m_cbSize;
    }

    virtual void Save(NativeWriter * pWriter)
    {
        byte * pData = (byte *)GetData();
        for (int i = 0; i < m_cbSize; i++)
            pWriter->WriteByte(pData[i]);
    }
};

class BlobVertexKey
{
    PVOID   _pData;
    int     _cbSize;

public:
    BlobVertexKey(PVOID pData, int cbSize)
        : _pData(pData), _cbSize(cbSize)
    {
    }

    void * GetData()
    {
        return _pData;
    }

    int GetSize()
    {
        return _cbSize;
    }
};

class BlobVertexSHashTraits : public DefaultSHashTraits<BlobVertex *>
{
public:
    typedef BlobVertexKey key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return key_t(e->GetData(), e->GetSize());
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        if (k1.GetSize() != k2.GetSize())
            return FALSE;
        return memcmp(k1.GetData(), k2.GetData(), k1.GetSize()) == 0;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        count_t hash = 5381 + (count_t)(k.GetSize() << 7);

        PBYTE pbData = (PBYTE)k.GetData();
        PBYTE pbDataEnd = pbData + k.GetSize();

        for (/**/ ; pbData < pbDataEnd; pbData++)
        {
            hash = ((hash << 5) + hash) ^ *pbData;
        }
        return hash;
    }
};


class EntryPointVertex : public NativeFormat::Vertex
{
    DWORD m_methodIndex;
    BlobVertex * m_pFixups;

public:
    EntryPointVertex(DWORD methodIndex, BlobVertex * pFixups)
        : m_methodIndex(methodIndex), m_pFixups(pFixups)
    {
    }

    virtual void Save(NativeWriter * pWriter)
    {
        if (m_pFixups != NULL)
        {
            int existingOffset = pWriter->GetCurrentOffset(m_pFixups);
            if (existingOffset != -1)
            {
                pWriter->WriteUnsigned((m_methodIndex << 2) | 3);
                pWriter->WriteUnsigned(pWriter->GetCurrentOffset() - existingOffset);
            }
            else
            {
                pWriter->WriteUnsigned((m_methodIndex << 2) | 1);
                pWriter->SetCurrentOffset(m_pFixups);
                m_pFixups->Save(pWriter);
            }
        }
        else
        {
            pWriter->WriteUnsigned(m_methodIndex << 1);
        }
    }
};

void ZapImage::OutputEntrypointsTableForReadyToRun()
{
    BeginRegion(CORINFO_REGION_COLD);

    NativeWriter writer;

    NativeSection * pSection = writer.NewSection();

    VertexArray vertexArray(pSection);
    pSection->Place(&vertexArray);

    bool fEmpty = true;

    SHash< NoRemoveSHashTraits < BlobVertexSHashTraits > > fixupBlobs;

    COUNT_T nCount = m_MethodCompilationOrder.GetCount();
    for (COUNT_T i = 0; i < nCount; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

        mdMethodDef token = GetJitInfo()->getMethodDefFromMethod(pMethod->GetHandle());

        int rid = RidFromToken(token);
        _ASSERTE(rid != 0);

        BlobVertex * pFixupBlob = NULL;

        if (pMethod->m_pFixupList != NULL)
        {
            NibbleWriter writer;
            m_pImportTable->PlaceFixups(pMethod->m_pFixupList, writer);

            DWORD cbBlob;
            PVOID pBlob = writer.GetBlob(&cbBlob);

            pFixupBlob = fixupBlobs.Lookup(BlobVertexKey(pBlob, cbBlob));
            if (pFixupBlob == NULL)
            {
                void * pMemory = new (GetHeap()) BYTE[sizeof(BlobVertex) + cbBlob];
                pFixupBlob = new (pMemory) BlobVertex(cbBlob);
                memcpy(pFixupBlob->GetData(), pBlob, cbBlob);

                fixupBlobs.Add(pFixupBlob);
            }
        }

        vertexArray.Set(rid - 1, new (GetHeap()) EntryPointVertex(pMethod->GetMethodIndex(), pFixupBlob));

        fEmpty = false;
    }

    if (fEmpty)
        return;

    vertexArray.ExpandLayout();

    vector<byte>& blob = writer.Save();

    ZapNode * pBlob = ZapBlob::NewBlob(this, &blob[0], blob.size());
    m_pCodeMethodDescsSection->Place(pBlob);

    ZapReadyToRunHeader * pReadyToRunHeader = GetReadyToRunHeader();
    pReadyToRunHeader->RegisterSection(READYTORUN_SECTION_METHODDEF_ENTRYPOINTS, pBlob);
    pReadyToRunHeader->RegisterSection(READYTORUN_SECTION_RUNTIME_FUNCTIONS, m_pRuntimeFunctionSection);

    if (m_pImportSectionsTable->GetSize() != 0)
        pReadyToRunHeader->RegisterSection(READYTORUN_SECTION_IMPORT_SECTIONS, m_pImportSectionsTable);

    if (m_pLazyMethodCallHelperSection->GetNodeCount() != 0)
        pReadyToRunHeader->RegisterSection(READYTORUN_SECTION_DELAYLOAD_METHODCALL_THUNKS, m_pLazyMethodCallHelperSection);

    if (m_pExceptionInfoLookupTable->GetSize() != 0)
        pReadyToRunHeader->RegisterSection(READYTORUN_SECTION_EXCEPTION_INFO, m_pExceptionInfoLookupTable);

    EndRegion(CORINFO_REGION_COLD);
}

class DebugInfoVertex : public NativeFormat::Vertex
{
    BlobVertex * m_pDebugInfo;

public:
    DebugInfoVertex(BlobVertex * pDebugInfo)
        : m_pDebugInfo(pDebugInfo)
    {
    }

    virtual void Save(NativeWriter * pWriter)
    {
        int existingOffset = pWriter->GetCurrentOffset(m_pDebugInfo);
        if (existingOffset != -1)
        {
            _ASSERTE(pWriter->GetCurrentOffset() > existingOffset);
            pWriter->WriteUnsigned(pWriter->GetCurrentOffset() - existingOffset);
        }
        else
        {
            pWriter->WriteUnsigned(0);
            pWriter->SetCurrentOffset(m_pDebugInfo);
            m_pDebugInfo->Save(pWriter);
        }
    }
};

void ZapImage::OutputDebugInfoForReadyToRun()
{
    NativeWriter writer;

    NativeSection * pSection = writer.NewSection();

    VertexArray vertexArray(pSection);
    pSection->Place(&vertexArray);

    bool fEmpty = true;

    SHash< NoRemoveSHashTraits < BlobVertexSHashTraits > > debugInfoBlobs;

    COUNT_T nCount = m_MethodCompilationOrder.GetCount();
    for (COUNT_T i = 0; i < nCount; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

        ZapBlob * pDebugInfo = pMethod->GetDebugInfo();
        if (pDebugInfo == NULL)
            continue;

        DWORD cbBlob = pDebugInfo->GetBlobSize();
        PVOID pBlob = pDebugInfo->GetData();

        BlobVertex * pDebugInfoBlob = debugInfoBlobs.Lookup(BlobVertexKey(pBlob, cbBlob));
        if (pDebugInfoBlob == NULL)
        {
            void * pMemory = new (GetHeap()) BYTE[sizeof(BlobVertex) + cbBlob];
            pDebugInfoBlob = new (pMemory) BlobVertex(cbBlob);
            memcpy(pDebugInfoBlob->GetData(), pBlob, cbBlob);

            debugInfoBlobs.Add(pDebugInfoBlob);
        }

        vertexArray.Set(pMethod->GetMethodIndex(), new (GetHeap()) DebugInfoVertex(pDebugInfoBlob));

        fEmpty = false;
    }

    if (fEmpty)
        return;

    vertexArray.ExpandLayout();

    vector<byte>& blob = writer.Save();

    ZapNode * pBlob = ZapBlob::NewBlob(this, &blob[0], blob.size());
    m_pDebugSection->Place(pBlob);

    GetReadyToRunHeader()->RegisterSection(READYTORUN_SECTION_DEBUG_INFO, pBlob);
}


//
// Verify that data structures and flags shared between NGen and ReadyToRun are in sync
//

//
// READYTORUN_IMPORT_SECTION
//
static_assert_no_msg(sizeof(READYTORUN_IMPORT_SECTION)          == sizeof(CORCOMPILE_IMPORT_SECTION));

static_assert_no_msg(READYTORUN_IMPORT_SECTION_TYPE_UNKNOWN     == CORCOMPILE_IMPORT_TYPE_UNKNOWN);

static_assert_no_msg(READYTORUN_IMPORT_SECTION_FLAGS_EAGER      == CORCOMPILE_IMPORT_FLAGS_EAGER);

//
// READYTORUN_METHOD_SIG
//
static_assert_no_msg(READYTORUN_METHOD_SIG_UnboxingStub         == ENCODE_METHOD_SIG_UnboxingStub);
static_assert_no_msg(READYTORUN_METHOD_SIG_InstantiatingStub    == ENCODE_METHOD_SIG_InstantiatingStub);
static_assert_no_msg(READYTORUN_METHOD_SIG_MethodInstantiation  == ENCODE_METHOD_SIG_MethodInstantiation);
static_assert_no_msg(READYTORUN_METHOD_SIG_SlotInsteadOfToken   == ENCODE_METHOD_SIG_SlotInsteadOfToken);
static_assert_no_msg(READYTORUN_METHOD_SIG_MemberRefToken       == ENCODE_METHOD_SIG_MemberRefToken);
static_assert_no_msg(READYTORUN_METHOD_SIG_Constrained          == ENCODE_METHOD_SIG_Constrained);
static_assert_no_msg(READYTORUN_METHOD_SIG_OwnerType            == ENCODE_METHOD_SIG_OwnerType);

//
// READYTORUN_FIELD_SIG
//
static_assert_no_msg(READYTORUN_FIELD_SIG_IndexInsteadOfToken   == ENCODE_FIELD_SIG_IndexInsteadOfToken);
static_assert_no_msg(READYTORUN_FIELD_SIG_MemberRefToken        == ENCODE_FIELD_SIG_MemberRefToken);
static_assert_no_msg(READYTORUN_FIELD_SIG_OwnerType             == ENCODE_FIELD_SIG_OwnerType);

//
// READYTORUN_FIXUP
//
static_assert_no_msg(READYTORUN_FIXUP_TypeHandle                == ENCODE_TYPE_HANDLE);
static_assert_no_msg(READYTORUN_FIXUP_MethodHandle              == ENCODE_METHOD_HANDLE);
static_assert_no_msg(READYTORUN_FIXUP_FieldHandle               == ENCODE_FIELD_HANDLE);

static_assert_no_msg(READYTORUN_FIXUP_MethodEntry               == ENCODE_METHOD_ENTRY);
static_assert_no_msg(READYTORUN_FIXUP_MethodEntry_DefToken      == ENCODE_METHOD_ENTRY_DEF_TOKEN);
static_assert_no_msg(READYTORUN_FIXUP_MethodEntry_RefToken      == ENCODE_METHOD_ENTRY_REF_TOKEN);

static_assert_no_msg(READYTORUN_FIXUP_VirtualEntry              == ENCODE_VIRTUAL_ENTRY);
static_assert_no_msg(READYTORUN_FIXUP_VirtualEntry_DefToken     == ENCODE_VIRTUAL_ENTRY_DEF_TOKEN);
static_assert_no_msg(READYTORUN_FIXUP_VirtualEntry_RefToken     == ENCODE_VIRTUAL_ENTRY_REF_TOKEN);
static_assert_no_msg(READYTORUN_FIXUP_VirtualEntry_Slot         == ENCODE_VIRTUAL_ENTRY_SLOT);

static_assert_no_msg(READYTORUN_FIXUP_Helper                    == ENCODE_READYTORUN_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_StringHandle              == ENCODE_STRING_HANDLE);

static_assert_no_msg(READYTORUN_FIXUP_NewObject                  == ENCODE_NEW_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_NewArray                   == ENCODE_NEW_ARRAY_HELPER);

static_assert_no_msg(READYTORUN_FIXUP_IsInstanceOf               == ENCODE_ISINSTANCEOF_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_ChkCast                    == ENCODE_CHKCAST_HELPER);

static_assert_no_msg(READYTORUN_FIXUP_FieldAddress               == ENCODE_FIELD_ADDRESS);
static_assert_no_msg(READYTORUN_FIXUP_CctorTrigger               == ENCODE_CCTOR_TRIGGER);

static_assert_no_msg(READYTORUN_FIXUP_StaticBaseNonGC            == ENCODE_STATIC_BASE_NONGC_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_StaticBaseGC               == ENCODE_STATIC_BASE_GC_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_ThreadStaticBaseNonGC      == ENCODE_THREAD_STATIC_BASE_NONGC_HELPER);
static_assert_no_msg(READYTORUN_FIXUP_ThreadStaticBaseGC         == ENCODE_THREAD_STATIC_BASE_GC_HELPER);

static_assert_no_msg(READYTORUN_FIXUP_FieldBaseOffset            == ENCODE_FIELD_BASE_OFFSET);
static_assert_no_msg(READYTORUN_FIXUP_FieldOffset                == ENCODE_FIELD_OFFSET);

static_assert_no_msg(READYTORUN_FIXUP_TypeDictionary             == ENCODE_TYPE_DICTIONARY);
static_assert_no_msg(READYTORUN_FIXUP_MethodDictionary           == ENCODE_METHOD_DICTIONARY);

static_assert_no_msg(READYTORUN_FIXUP_Check_TypeLayout           == ENCODE_CHECK_TYPE_LAYOUT);
static_assert_no_msg(READYTORUN_FIXUP_Check_FieldOffset          == ENCODE_CHECK_FIELD_OFFSET);

static_assert_no_msg(READYTORUN_FIXUP_DelegateCtor               == ENCODE_DELEGATE_CTOR);

//
// READYTORUN_EXCEPTION
//
static_assert_no_msg(sizeof(READYTORUN_EXCEPTION_LOOKUP_TABLE_ENTRY) == sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
static_assert_no_msg(sizeof(READYTORUN_EXCEPTION_CLAUSE) == sizeof(CORCOMPILE_EXCEPTION_CLAUSE));
