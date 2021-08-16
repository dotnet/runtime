// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapImport.cpp
//

//
// Zapping of soft bound references to elements outside the current module
//
// ======================================================================================

#include "common.h"

#include "zapimport.h"

#include "nibblestream.h"
#include "sigbuilder.h"

#if defined(FEATURE_READYTORUN_COMPILER)
// A flag to indicate that a helper call uses VSD
const DWORD READYTORUN_HELPER_FLAG_VSD = 0x10000000;
#endif

//
// ZapImportTable
//

ZapImportTable::ModuleReferenceEntry * ZapImportTable::GetModuleReference(CORINFO_MODULE_HANDLE handle)
{
    ModuleReferenceEntry * pEntry = m_moduleReferences.Lookup(handle);

    if (pEntry != NULL)
        return pEntry;

    if (!GetCompileInfo()->IsInCurrentVersionBubble(handle))
    {
        // FUTURE TODO: Version resilience
        _ASSERTE(!"Invalid reference to module outside of current version bubble");
        ThrowHR(E_FAIL);
    }

    pEntry = new (m_pImage->GetHeap()) ModuleReferenceEntry();
    pEntry->m_module = handle;

    GetCompileInfo()->EncodeModuleAsIndex(m_pImage->GetModuleHandle(), handle,
                                                        &pEntry->m_index,
                                                        m_pImage->GetAssemblyEmit());

    m_moduleReferences.Add(pEntry);

    return pEntry;
}

ZapBlob * ZapImportTable::GetBlob(SigBuilder * pSigBuilder, BOOL fEager)
{
    DWORD cbBlob;
    PVOID pSignature = pSigBuilder->GetSignature(&cbBlob);

    if (fEager)
    {
        // Use dedicated section for blobs of eager fixups
        return ZapBlob::NewBlob(m_pImage, pSignature, cbBlob);
    }

    ZapBlob * pBlob = m_blobs.Lookup(ZapBlob::SHashKey(pSignature, cbBlob));

    if (pBlob == NULL)
    {
        pBlob = ZapBlob::NewBlob(m_pImage, pSignature, cbBlob);

        m_blobs.Add(pBlob);
    }

    return pBlob;
}

ZapBlob * ZapImportTable::PlaceImportBlob(ZapImport * pImport, BOOL fEager)
{
    ZapBlob * pBlob;
    if (pImport->HasBlob())
    {
        pBlob = pImport->GetBlob();
    }
    else
    {
        SigBuilder sigBuilder;
        pImport->EncodeSignature(this, &sigBuilder);

        pBlob = GetBlob(&sigBuilder, fEager);

        pImport->SetBlob(pBlob);
    }

    if (!pBlob->IsPlaced())
        PlaceBlob(pBlob, fEager);

    return pBlob;
}

static const struct ImportSectionProperties
{
    BYTE                    Type;
    BYTE                    EntrySize;
    WORD                    Flags;
}
c_ImportSectionProperties[ZapImportSectionType_Count] =
{
    { /* ZapImportSectionType_Handle,       */ CORCOMPILE_IMPORT_TYPE_UNKNOWN,         0,                    0                               },
    { /* ZapImportSectionType_TypeHandle,   */ CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE,     TARGET_POINTER_SIZE,  0                               },
    { /* ZapImportSectionType_MethodHandle, */ CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE,   TARGET_POINTER_SIZE,  0                               },
#ifdef TARGET_ARM
    { /* ZapImportSectionType_PCode,        */ CORCOMPILE_IMPORT_TYPE_UNKNOWN,         0,                    CORCOMPILE_IMPORT_FLAGS_PCODE   },
#endif
    { /* ZapImportSectionType_StringHandle, */ CORCOMPILE_IMPORT_TYPE_STRING_HANDLE,   TARGET_POINTER_SIZE,  0                               },
};

void ZapImportTable::PlaceImport(ZapImport * pImport)
{
    BOOL fIsEager, fNeedsSignature;
    ZapImportSectionType table = pImport->ComputePlacement(m_pImage, &fIsEager, &fNeedsSignature);

    if (fIsEager)
    {
        table = ZapImportSectionType_Eager;
    }
    else
    if (!m_pImage->IsCurrentCodeRegionHot())
    {
        table = (ZapImportSectionType)(table + ZapImportSectionType_Cold);
    }

    _ASSERTE(table < ZapImportSectionType_Total);


    if (fNeedsSignature)
    {
        PlaceImportBlob(pImport, fIsEager);
    }

    ZapVirtualSection * pVirtualSection = m_pImage->m_pDelayLoadInfoTableSection[table];

    if (m_nImportSectionSizes[table] == 0)
    {
        const ImportSectionProperties  * pProps = &c_ImportSectionProperties[table % ZapImportSectionType_Count];

        WORD flags = pProps->Flags;

        if (fIsEager)
            flags |= CORCOMPILE_IMPORT_FLAGS_EAGER;

        m_nImportSectionIndices[table] = m_pImage->GetImportSectionsTable()->Append(pProps->Type, flags, pProps->EntrySize,
            pVirtualSection, m_pImage->m_pDelayLoadInfoDataTable[table]);
    }

    pImport->SetSectionIndexAndOffset(m_nImportSectionIndices[table], m_nImportSectionSizes[table]);

    pVirtualSection->Place(pImport);

    m_nImportSectionSizes[table] += pImport->GetSize();
}

// Sort ZapImport* by CorCompileTokenTable as primary key and offset within the table as secondary key
static int __cdecl fixupCmp(const void* a_, const void* b_)
{
    ZapImport *a = *(ZapImport **)a_;
    ZapImport *b = *(ZapImport **)b_;

    int tableDiff = a->GetSectionIndex() - b->GetSectionIndex();
    if (tableDiff != 0)
        return tableDiff;

    // Sort by offset within the table
    return (a->GetOffset() - b->GetOffset());
}

void ZapImportTable::PlaceFixups(ZapImport ** pImports, NibbleWriter& writer)
{
    COUNT_T nImports = 0;

    for (;;)
    {
        ZapImport * pImport = pImports[nImports];
        if (pImport == NULL) // end of the list
            break;
        if (!pImport->IsPlaced())
            PlaceImport(pImport);
        nImports++;
    }

    qsort(pImports, nImports, sizeof(ZapImport *), fixupCmp);

    //
    // Build the encoded fixup list
    //

    int curTableIndex = -1;
    DWORD curOffset = 0;

    for (COUNT_T iImport = 0; iImport < nImports; iImport++)
    {
        ZapImport * pImport = pImports[iImport];

        int tableIndex = pImport->GetSectionIndex();
        unsigned offset = pImport->GetOffset();

        _ASSERTE(offset % TARGET_POINTER_SIZE == 0);
        offset /= TARGET_POINTER_SIZE;

        if (tableIndex != curTableIndex)
        {
            // Write delta relative to the previous table index
            _ASSERTE(tableIndex > curTableIndex);
            if (curTableIndex != -1)
            {
                writer.WriteEncodedU32(0); // table separator, so add except for the first entry
                writer.WriteEncodedU32(tableIndex - curTableIndex); // add table index delta
            }
            else
            {
                writer.WriteEncodedU32(tableIndex);
            }
            curTableIndex = tableIndex;

            // This is the first fixup in the current table.
            // We will write it out completely (without delta-encoding)
            writer.WriteEncodedU32(offset);
        }
        else
        {
            // This is not the first entry in the current table.
            // We will write out the delta relative to the previous fixup value
            int delta = offset - curOffset;
            _ASSERTE(delta > 0);
            writer.WriteEncodedU32(delta);
        }

        // future entries for this table would be relative to this rva
        curOffset = offset;
    }

    writer.WriteEncodedU32(0); // table separator
    writer.WriteEncodedU32(0); // fixup list ends

    writer.Flush();
}

ZapFixupInfo * ZapImportTable::PlaceFixups(ZapImport ** pImports)
{
    NibbleWriter writer;

    PlaceFixups(pImports, writer);

    DWORD cbBlob;
    PVOID pBlob = writer.GetBlob(&cbBlob);

    //
    // Intern the fixup info
    //

    ZapFixupInfo * pFixupInfo = m_blobs.Lookup(ZapBlob::SHashKey(pBlob, cbBlob));

    if (pFixupInfo == NULL)
    {
        // Fixup infos are mixed with other blobs
        pFixupInfo = ZapBlob::NewBlob(m_pImage, pBlob, cbBlob);
        m_blobs.Add(pFixupInfo);
    }

    if (!pFixupInfo->IsPlaced())
        PlaceBlob(pFixupInfo);

    return pFixupInfo;
}

void ZapImportTable::PlaceBlob(ZapBlob * pBlob, BOOL fEager)
{
    ZapVirtualSection * pSection;
    if (fEager)
        pSection = m_pImage->m_pDelayLoadInfoDelayListSectionEager;
    else
    if (m_pImage->IsCurrentCodeRegionHot())
        pSection = m_pImage->m_pDelayLoadInfoDelayListSectionHot;
    else
        pSection = m_pImage->m_pDelayLoadInfoDelayListSectionCold;
    pSection->Place(pBlob);
}

// ======================================================================================
//
// Generic signatures
//
ZapGenericSignature * ZapImportTable::GetGenericSignature(PVOID signature, BOOL fMethod)
{
#ifdef  REDHAWK
    _ASSERTE(!"NYI");
    return NULL;
#else
    SigBuilder sigBuilder;
    GetCompileInfo()->EncodeGenericSignature(signature, fMethod, &sigBuilder, this, EncodeModuleHelper);

    DWORD cbSig;
    PVOID pSig = sigBuilder.GetSignature(&cbSig);

    ZapGenericSignature * pGenericSignature = (ZapGenericSignature *)m_genericSignatures.Lookup(ZapBlob::SHashKey(pSig, cbSig));

    if (pGenericSignature != NULL)
        return pGenericSignature;

    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapGenericSignature)) + S_SIZE_T(cbSig);

    if (cbAllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    void * pMemory = new (m_pImage->GetHeap()) BYTE[cbAllocSize.Value()];

    pGenericSignature = new (pMemory) ZapGenericSignature(cbSig);
    memcpy((void *)(pGenericSignature + 1), pSig, cbSig);

    m_genericSignatures.Add(pGenericSignature);

    return pGenericSignature;
#endif // REDHAWK
}

// At ngen time Zapper::CompileModule PlaceFixups called from
//     code:ZapSig.GetSignatureForTypeHandle
//
/*static*/ DWORD ZapImportTable::EncodeModuleHelper( LPVOID compileContext,
                                                CORINFO_MODULE_HANDLE referencedModule)
{
    _ASSERTE(!IsReadyToRunCompilation() || IsLargeVersionBubbleEnabled());
    ZapImportTable * pTable = (ZapImportTable *)compileContext;
    return pTable->GetIndexOfModule(referencedModule);
}

void ZapImport::Save(ZapWriter * pZapWriter)
{
    if (IsReadyToRunCompilation())
    {
        TARGET_POINTER_TYPE value = 0;
        pZapWriter->Write(&value, sizeof(value));
        return;
    }

    TARGET_POINTER_TYPE token = CORCOMPILE_TAG_TOKEN(GetBlob()->GetRVA());
    pZapWriter->Write(&token, sizeof(token));
}

//
// CORCOMPILE_CODE_IMPORT_SECTION
//

COUNT_T ZapImportSectionsTable::Append(BYTE Type, USHORT Flags, BYTE EntrySize, ZapVirtualSection * pSection, ZapNode * pSignatures, ZapNode * pAuxiliaryData)
{
    ImportSection entry;

    entry.m_pSection = pSection;
    entry.m_pSignatures = pSignatures;
    entry.m_pAuxiliaryData = pAuxiliaryData;
    entry.m_Flags = Flags;
    entry.m_Type = Type;
    entry.m_EntrySize = EntrySize;

    m_ImportSectionsTable.Append(entry);

    return m_ImportSectionsTable.GetCount() - 1;
}

DWORD ZapImportSectionsTable::GetSize()
{
    return m_ImportSectionsTable.GetCount() * sizeof(CORCOMPILE_IMPORT_SECTION);
}

void ZapImportSectionsTable::Save(ZapWriter * pZapWriter)
{
    COUNT_T nSections = m_ImportSectionsTable.GetCount();
    for (COUNT_T iSection = 0; iSection < nSections; iSection++)
    {
        ImportSection * p = &m_ImportSectionsTable[iSection];

        CORCOMPILE_IMPORT_SECTION entry;

        ZapWriter::SetDirectoryData(&entry.Section, p->m_pSection);

        entry.Flags = p->m_Flags;
        entry.Type = p->m_Type;
        entry.EntrySize = p->m_EntrySize;

        entry.Signatures = (p->m_pSignatures != NULL) ? p->m_pSignatures->GetRVA() : NULL;
        entry.AuxiliaryData = (p->m_pAuxiliaryData != NULL) ? p->m_pAuxiliaryData->GetRVA() : NULL;

        pZapWriter->Write(&entry, sizeof(entry));
    }
}


ZapImportSectionSignatures::ZapImportSectionSignatures(ZapImage * pImage, ZapVirtualSection * pImportSection, ZapVirtualSection * pGCSection)
    : m_pImportSection(pImportSection), m_pImage(pImage)
{
    if (pGCSection != NULL)
    {
        m_pGCRefMapTable = new (pImage->GetHeap()) ZapGCRefMapTable(pImage);
        pGCSection->Place(m_pGCRefMapTable);
    }
}

ZapImportSectionSignatures::~ZapImportSectionSignatures()
{
    if (m_pGCRefMapTable != NULL)
        m_pGCRefMapTable->~ZapGCRefMapTable();
}

DWORD ZapImportSectionSignatures::GetSize()
{
    return m_pImportSection->GetNodeCount() * sizeof(DWORD);
}

void ZapImportSectionSignatures::Save(ZapWriter * pZapWriter)
{
    COUNT_T nCount = m_pImportSection->GetNodeCount();
    for (COUNT_T i = 0; i < nCount; i++)
    {
        ZapNode * pNode = m_pImportSection->GetNode(i);
        DWORD dwRVA = ((ZapImport *)pNode)->GetBlob()->GetRVA();
        pZapWriter->Write(&dwRVA, sizeof(dwRVA));
    }
}

// ======================================================================================
//
// Special lazy imports for lazily resolved method calls
//

//
// External method thunk is a patchable thunk used for cross-module direct calls
//
class ZapExternalMethodThunk : public ZapImport
{
public:
    ZapExternalMethodThunk()
    {
    }

    CORINFO_METHOD_HANDLE GetMethod()
    {
        return (CORINFO_METHOD_HANDLE)GetHandle();
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_EXTERNAL_METHOD_THUNK);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ExternalMethodThunk;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)GetHandle();

        CORINFO_MODULE_HANDLE referencingModule;
        mdToken token = pTable->GetCompileInfo()->TryEncodeMethodAsToken(handle, NULL, &referencingModule);
        if (token != mdTokenNil)
        {
            _ASSERTE(TypeFromToken(token) == mdtMethodDef || TypeFromToken(token) == mdtMemberRef);

            pTable->EncodeModule(
                (TypeFromToken(token) == mdtMethodDef) ? ENCODE_METHOD_ENTRY_DEF_TOKEN : ENCODE_METHOD_ENTRY_REF_TOKEN,
                referencingModule, pSigBuilder);

            pSigBuilder->AppendData(RidFromToken(token));
        }
        else
        {
            pTable->EncodeMethod(ENCODE_METHOD_ENTRY, handle, pSigBuilder);
        }
    }

    virtual void Save(ZapWriter * pZapWriter);
};

void ZapExternalMethodThunk::Save(ZapWriter * pZapWriter)
{
    ZapImage *             pImage  = ZapImage::GetImage(pZapWriter);
    ZapNode *              helper  = pImage->GetHelperThunk(CORINFO_HELP_EE_EXTERNAL_FIXUP);

    CORCOMPILE_EXTERNAL_METHOD_THUNK thunk;
    memset(&thunk, DEFAULT_CODE_BUFFER_INIT, sizeof(thunk));
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    thunk.callJmp[0]  = 0xE8;  // call rel32
    pImage->WriteReloc(&thunk, 1, helper, 0, IMAGE_REL_BASED_REL32);
    thunk.precodeType = _PRECODE_EXTERNAL_METHOD_THUNK;
#elif defined(TARGET_ARM)
    // Setup the call to ExternalMethodFixupStub
    //
    // mov r12, pc
    //
    // Per ARM architecture reference manual section A2.3,
    // reading the value of PC register will read the address
    // of the current instruction plus 4. In this case,
    // R12 will containing the address of "F004" below once
    // the "mov" is executed.
    //
    // Since this is 4 bytes ahead of the start of the thunk,
    // the assembly helper we will call into will adjust this
    // so that we point to the start of the thunk correctly.
    thunk.m_rgCode[0] = 0x46fc;

    // ldr pc, [pc, #4]
    thunk.m_rgCode[1] = 0xf8df;
    thunk.m_rgCode[2] = 0xf004;

    // Setup the initial target to be our assembly helper.
    pImage->WriteReloc(&thunk, offsetof(CORCOMPILE_EXTERNAL_METHOD_THUNK, m_pTarget), helper, 0, IMAGE_REL_BASED_PTR);
#elif defined(TARGET_ARM64)

    thunk.m_rgCode[0] = 0x1000000C; //adr       x12, #0
    thunk.m_rgCode[1] = 0xF940098A; //ldr       x10, [x12, #16]
    thunk.m_rgCode[2] = 0xD61F0140; //br        x10

    pImage->WriteReloc(&thunk, offsetof(CORCOMPILE_EXTERNAL_METHOD_THUNK, m_pTarget), helper, 0, IMAGE_REL_BASED_PTR);
#else
    PORTABILITY_ASSERT("ZapExternalMethodThunk::Save");

#endif

    pZapWriter->Write(&thunk,  sizeof(thunk));
    _ASSERTE(sizeof(thunk) == GetSize());
}

void ZapImportSectionSignatures::PlaceExternalMethodThunk(ZapImport * pImport)
{
    ZapExternalMethodThunk * pThunk = (ZapExternalMethodThunk *)pImport;

    if (m_pImportSection->GetNodeCount() == 0)
    {
        m_dwIndex = m_pImage->GetImportSectionsTable()->Append(CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD, CORCOMPILE_IMPORT_FLAGS_CODE,
            sizeof(CORCOMPILE_EXTERNAL_METHOD_THUNK), m_pImportSection, this, m_pGCRefMapTable);

        // Make sure the helper created
        m_pImage->GetHelperThunk(CORINFO_HELP_EE_EXTERNAL_FIXUP);
    }

    // Add entry to both the the cell and data sections
    m_pImportSection->Place(pThunk);

    m_pImage->GetImportTable()->PlaceImportBlob(pThunk);

    m_pGCRefMapTable->Append(pThunk->GetMethod());
}

ZapImport * ZapImportTable::GetExternalMethodThunk(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapExternalMethodThunk, ZapNodeType_ExternalMethodThunk>((PVOID)handle);
}

//
// Stub dispatch cell is lazily initialized indirection used for virtual stub dispatch
//
class ZapStubDispatchCell : public ZapImport
{
    ZapNode * m_pDelayLoadHelper;

public:
    void SetDelayLoadHelper(ZapNode * pDelayLoadHelper)
    {
        _ASSERTE(m_pDelayLoadHelper == NULL);
        m_pDelayLoadHelper = pDelayLoadHelper;
    }

    CORINFO_METHOD_HANDLE GetMethod()
    {
        return (CORINFO_METHOD_HANDLE)GetHandle();
    }

    CORINFO_CLASS_HANDLE GetClass()
    {
        return (CORINFO_CLASS_HANDLE)GetHandle2();
    }

    virtual DWORD GetSize()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_StubDispatchCell;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        CORINFO_MODULE_HANDLE referencingModule = pTable->GetJitInfo()->getClassModule(GetClass());
        referencingModule = pTable->TryEncodeModule(ENCODE_VIRTUAL_ENTRY_SLOT, referencingModule, pSigBuilder);

        DWORD slot = pTable->GetCompileInfo()->TryEncodeMethodSlot(GetMethod());

        // We expect the encoding to always succeed
        _ASSERTE(slot != (DWORD)-1);

        pSigBuilder->AppendData(slot);

        pTable->EncodeClassInContext(referencingModule, GetClass(), pSigBuilder);
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        TARGET_POINTER_TYPE cell;
        pImage->WriteReloc(&cell, 0, m_pDelayLoadHelper, 0, IMAGE_REL_BASED_PTR);
        pZapWriter->Write(&cell, sizeof(cell));
    }
};

ZapImport * ZapImportTable::GetStubDispatchCell(CORINFO_CLASS_HANDLE typeHnd, CORINFO_METHOD_HANDLE methHnd)
{
    // Do not intern stub dispatch imports. Each callsite should get own cell.
    ZapImport * pImport = new (m_pImage->GetHeap()) ZapStubDispatchCell();
    pImport->SetHandle(methHnd);
    pImport->SetHandle2(typeHnd);
    return pImport;
}

void ZapImportSectionSignatures::PlaceStubDispatchCell(ZapImport * pImport)
{
    ZapStubDispatchCell * pCell = (ZapStubDispatchCell *)pImport;

    if (m_pImportSection->GetNodeCount() == 0)
    {
        m_dwIndex = m_pImage->GetImportSectionsTable()->Append(CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH, CORCOMPILE_IMPORT_FLAGS_PCODE,
            TARGET_POINTER_SIZE, m_pImportSection, this, m_pGCRefMapTable);
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        // Create the delay load helper
        ReadyToRunHelper helper = (ReadyToRunHelper)(READYTORUN_HELPER_DelayLoad_MethodCall | READYTORUN_HELPER_FLAG_VSD);
        ZapNode * pDelayLoadHelper = m_pImage->GetImportTable()->GetPlacedIndirectHelperThunk(helper, (PVOID)(SIZE_T)m_dwIndex);
        pCell->SetDelayLoadHelper(pDelayLoadHelper);
    }
    else
#endif
    {
        pCell->SetDelayLoadHelper(m_pImage->GetHelperThunk(CORINFO_HELP_EE_VSD_FIXUP));
    }

    // Add entry to both the cell and data sections
    m_pImportSection->Place(pCell);

    m_pImage->GetImportTable()->PlaceImportBlob(pCell);

    m_pGCRefMapTable->Append(pCell->GetMethod(), true);
}

//
// External method cell is lazily initialized indirection used for method calls
//
class ZapExternalMethodCell : public ZapImport
{
    ZapNode * m_pDelayLoadHelper;

public:
    void SetDelayLoadHelper(ZapNode * pDelayLoadHelper)
    {
        _ASSERTE(m_pDelayLoadHelper == NULL);
        m_pDelayLoadHelper = pDelayLoadHelper;
    }
    CORINFO_METHOD_HANDLE GetMethod()
    {
        return (CORINFO_METHOD_HANDLE)GetHandle();
    }

    virtual DWORD GetSize()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ExternalMethodCell;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)GetHandle();

        CORINFO_MODULE_HANDLE referencingModule;
        mdToken token = pTable->GetCompileInfo()->TryEncodeMethodAsToken(handle, NULL, &referencingModule);
        if (token != mdTokenNil)
        {
            _ASSERTE(TypeFromToken(token) == mdtMethodDef || TypeFromToken(token) == mdtMemberRef);

            pTable->EncodeModule(
                (TypeFromToken(token) == mdtMethodDef) ? ENCODE_METHOD_ENTRY_DEF_TOKEN : ENCODE_METHOD_ENTRY_REF_TOKEN,
                referencingModule, pSigBuilder);

            pSigBuilder->AppendData(RidFromToken(token));
        }
        else
        {
            pTable->EncodeMethod(ENCODE_METHOD_ENTRY, handle, pSigBuilder);
        }
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        TARGET_POINTER_TYPE cell;
        pImage->WriteReloc(&cell, 0, m_pDelayLoadHelper, 0, IMAGE_REL_BASED_PTR);
        pZapWriter->Write(&cell, sizeof(cell));
    }
};

ZapImport * ZapImportTable::GetExternalMethodCell(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapExternalMethodCell, ZapNodeType_ExternalMethodCell>((PVOID)handle);
}

void ZapImportSectionSignatures::PlaceExternalMethodCell(ZapImport * pImport)
{
    ZapExternalMethodCell * pCell = (ZapExternalMethodCell *)pImport;

    if (m_pImportSection->GetNodeCount() == 0)
    {
        m_dwIndex = m_pImage->GetImportSectionsTable()->Append(CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH, CORCOMPILE_IMPORT_FLAGS_PCODE,
            TARGET_POINTER_SIZE, m_pImportSection, this, m_pGCRefMapTable);
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        // Create the delay load helper
        ZapNode * pDelayLoadHelper = m_pImage->GetImportTable()->GetPlacedIndirectHelperThunk(READYTORUN_HELPER_DelayLoad_MethodCall, (PVOID)(SIZE_T)m_dwIndex);
        pCell->SetDelayLoadHelper(pDelayLoadHelper);
    }
    else
#endif
    {
        pCell->SetDelayLoadHelper(m_pImage->GetHelperThunk(CORINFO_HELP_EE_EXTERNAL_FIXUP));
    }

    // Add entry to both the cell and data sections
    m_pImportSection->Place(pCell);

    m_pImage->GetImportTable()->PlaceImportBlob(pCell);

    m_pGCRefMapTable->Append(pCell->GetMethod());
}

//
// Virtual import thunk is a patchable thunk used for cross-module virtual calls.
//
class ZapVirtualMethodThunk : public ZapImport
{
public:
    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_VIRTUAL_IMPORT_THUNK);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_VirtualMethodThunk;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        // Virtual import thunks do not have signatures
        _ASSERTE(false);
    }

    virtual void Save(ZapWriter * pZapWriter);
};

void ZapVirtualMethodThunk::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    CORCOMPILE_VIRTUAL_IMPORT_THUNK thunk;
    memset(&thunk, DEFAULT_CODE_BUFFER_INIT, sizeof(thunk));

    // On ARM, the helper would already have the thumb-bit set. Refer to
    // GetHelperThunk implementation.
    ZapNode *  helper  = pImage->GetHelperThunk(CORINFO_HELP_EE_VTABLE_FIXUP);
    _ASSERTE(FitsIn<UINT16>((SIZE_T)GetHandle2() - 1));
    USHORT     slotNum = (USHORT)((SIZE_T)GetHandle2() - 1);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    thunk.callJmp[0] = 0xE8;        // call rel32
    pImage->WriteReloc(&thunk, 1, helper, 0, IMAGE_REL_BASED_REL32);

    // Mark this as a Virtual Import Thunk
    thunk.precodeType = _PRECODE_VIRTUAL_IMPORT_THUNK;
#elif defined(TARGET_ARM)
    // Setup the call to VirtualMethodFixupStub
    //
    // mov r12, pc
    //
    // Per ARM architecture reference manual section A2.3,
    // reading the value of PC register will read the address
    // of the current instruction plus 4. In this case,
    // R12 will containing the address of "F004" below once
    // the "mov" is executed.
    //
    // Since this is 4 bytes ahead of the start of the thunk,
    // the assembly helper we will call into will adjust this
    // so that we point to the start of the thunk correctly.
    thunk.m_rgCode[0] = 0x46fc;

    // ldr pc, [pc, #4]
    thunk.m_rgCode[1] = 0xf8df;
    thunk.m_rgCode[2] = 0xf004;

    // Slot ID is setup below, so now setup the initial target
    // to be our assembly helper.
    pImage->WriteReloc(&thunk, offsetof(CORCOMPILE_VIRTUAL_IMPORT_THUNK, m_pTarget), helper, 0, IMAGE_REL_BASED_PTR);
 #elif defined(TARGET_ARM64)

    thunk.m_rgCode[0] = 0x1000000C; //adr       x12, #0
    thunk.m_rgCode[1] = 0xF940098A; //ldr       x10, [x12, #16]
    thunk.m_rgCode[2] = 0xD61F0140; //br        x10

    // Slot ID is setup below, so now setup the initial target
    // to be our assembly helper.
    pImage->WriteReloc(&thunk, offsetof(CORCOMPILE_VIRTUAL_IMPORT_THUNK, m_pTarget), helper, 0, IMAGE_REL_BASED_PTR);
#else
    PORTABILITY_ASSERT("ZapVirtualMethodThunk::Save");
#endif

    thunk.slotNum = slotNum;

    pZapWriter->Write(&thunk, sizeof(thunk));
}

void ZapImportTable::PlaceVirtualImportThunk(ZapImport * pVirtualImportThunk)
{
    if (m_pImage->m_pVirtualImportThunkSection->GetNodeCount() == 0)
    {
        m_pImage->GetImportSectionsTable()->Append(CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD, CORCOMPILE_IMPORT_FLAGS_CODE,
            sizeof(CORCOMPILE_VIRTUAL_IMPORT_THUNK), m_pImage->m_pVirtualImportThunkSection);

        // Make sure the helper created
        m_pImage->GetHelperThunk(CORINFO_HELP_EE_VTABLE_FIXUP);
    }

    m_pImage->m_pVirtualImportThunkSection->Place(pVirtualImportThunk);
}

ZapImport * ZapImportTable::GetVirtualImportThunk(CORINFO_METHOD_HANDLE handle, int slot)
{
    return GetImport<ZapVirtualMethodThunk, ZapNodeType_VirtualMethodThunk>(handle, (PVOID)(SIZE_T)(slot+1));
}

// ======================================================================================
//
// GCRefMapTable is used to encode for GC references locations for lazily resolved calls
//

void ZapGCRefMapTable::Append(CORINFO_METHOD_HANDLE handle, bool isDispatchCell)
{
    m_pImage->GetCompileInfo()->GetCallRefMap(handle, &m_GCRefMapBuilder, isDispatchCell);
    m_nCount++;
}

DWORD ZapGCRefMapTable::GetSize()
{
    if (m_nCount == 0) return 0;

    COUNT_T nLookupEntries = (1 + m_nCount / GCREFMAP_LOOKUP_STRIDE);

    return (nLookupEntries * sizeof(DWORD)) + m_GCRefMapBuilder.GetBlobLength();
}

void ZapGCRefMapTable::Save(ZapWriter * pZapWriter)
{
    if (m_nCount == 0) return;

    COUNT_T nLookupEntries = (1 + m_nCount / GCREFMAP_LOOKUP_STRIDE);

    DWORD dwBlobLength;
    BYTE * pBlob = (BYTE *)m_GCRefMapBuilder.GetBlob(&dwBlobLength);

    DWORD pos = 0;
    COUNT_T iLookupEntry = 0;
    for (;;)
    {
        DWORD relOfs = (nLookupEntries * sizeof(DWORD)) + pos;
        pZapWriter->Write(&relOfs, sizeof(relOfs));
        iLookupEntry++;

        if (iLookupEntry >= nLookupEntries)
            break;

        for (int i = 0; i < GCREFMAP_LOOKUP_STRIDE; i++)
        {
            while ((*(pBlob + pos) & 0x80) != 0)
                pos++;
            pos++;

            _ASSERTE(pos <= dwBlobLength);
        }
    }

    pZapWriter->Write(pBlob, dwBlobLength);
}

// ======================================================================================
//
// Getters for existing imports.
//

ZapImport * ZapImportTable::GetExistingClassHandleImport(CORINFO_CLASS_HANDLE handle)
{
    return GetExistingImport(ZapNodeType_Import_ClassHandle, handle);
}

ZapImport * ZapImportTable::GetExistingFieldHandleImport(CORINFO_FIELD_HANDLE handle)
{
    return GetExistingImport(ZapNodeType_Import_FieldHandle, handle);
}

ZapImport * ZapImportTable::GetExistingMethodHandleImport(CORINFO_METHOD_HANDLE handle)
{
    return GetExistingImport(ZapNodeType_Import_MethodHandle, handle);
}

CORINFO_MODULE_HANDLE ZapImportTable::TryEncodeModule(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_MODULE_HANDLE module, SigBuilder * pSigBuilder)
{
    if (!GetCompileInfo()->IsInCurrentVersionBubble(module))
        module = GetImage()->GetModuleHandle();

    EncodeModule(kind, module, pSigBuilder);
    return module;
}

void ZapImportTable::EncodeModule(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_MODULE_HANDLE module, SigBuilder * pSigBuilder)
{
    if (module != GetImage()->GetModuleHandle())
    {
        _ASSERTE(!IsReadyToRunCompilation() || IsLargeVersionBubbleEnabled());
        pSigBuilder->AppendByte(kind | ENCODE_MODULE_OVERRIDE);
        pSigBuilder->AppendData(GetIndexOfModule(module));
    }
    else
    {
        pSigBuilder->AppendByte(kind);
    }
}

void ZapImportTable::EncodeClass(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_CLASS_HANDLE handle, SigBuilder * pSigBuilder)
{
    CORINFO_MODULE_HANDLE referencingModule = GetJitInfo()->getClassModule(handle);
    referencingModule = TryEncodeModule(kind, referencingModule, pSigBuilder);
    GetCompileInfo()->EncodeClass(referencingModule, handle, pSigBuilder, this, EncodeModuleHelper);
}

void ZapImportTable::EncodeClassInContext(CORINFO_MODULE_HANDLE context, CORINFO_CLASS_HANDLE handle, SigBuilder * pSigBuilder)
{
    GetCompileInfo()->EncodeClass(context, handle, pSigBuilder, this, EncodeModuleHelper);
}

void ZapImportTable::EncodeField(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, SigBuilder * pSigBuilder,
        CORINFO_RESOLVED_TOKEN * pResolvedToken, BOOL fEncodeUsingResolvedTokenSpecStreams)
{
    CORINFO_CLASS_HANDLE clsHandle = GetJitInfo()->getFieldClass(handle);
    CORINFO_MODULE_HANDLE referencingModule = GetJitInfo()->getClassModule(clsHandle);
    referencingModule = TryEncodeModule(kind, referencingModule, pSigBuilder);
    GetCompileInfo()->EncodeField(referencingModule, handle, pSigBuilder, this, EncodeModuleHelper,
        pResolvedToken, fEncodeUsingResolvedTokenSpecStreams);
}

void ZapImportTable::EncodeMethod(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle, SigBuilder * pSigBuilder,
        CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken, BOOL fEncodeUsingResolvedTokenSpecStreams)
{
    CORINFO_CLASS_HANDLE clsHandle = GetJitInfo()->getMethodClass(handle);
    CORINFO_MODULE_HANDLE referencingModule = GetJitInfo()->getClassModule(clsHandle);
    referencingModule = TryEncodeModule(kind, referencingModule, pSigBuilder);
    GetCompileInfo()->EncodeMethod(referencingModule, handle, pSigBuilder, this, EncodeModuleHelper,
        pResolvedToken, pConstrainedResolvedToken, fEncodeUsingResolvedTokenSpecStreams);
}

// ======================================================================================
//
// Actual imports
//

class ZapModuleHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ModuleHandle;
    }

    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        ZapImport::ComputePlacement(pImage, pfIsEager, pfNeedsSignature);

         CORINFO_MODULE_HANDLE handle = (CORINFO_MODULE_HANDLE)GetHandle();
         if (pImage->m_pPreloader->CanEmbedModuleHandle(handle))
         {
            *pfIsEager = TRUE;
         }

         return ZapImportSectionType_Handle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeModule(ENCODE_MODULE_HANDLE, (CORINFO_MODULE_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetModuleHandleImport(CORINFO_MODULE_HANDLE handle)
{
    return GetImport<ZapModuleHandleImport, ZapNodeType_Import_ModuleHandle>(handle);
}

class ZapClassHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ClassHandle;
    }

    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        ZapImport::ComputePlacement(pImage, pfIsEager, pfNeedsSignature);

        if (IsReadyToRunCompilation())
            return ZapImportSectionType_Handle;

        CORINFO_CLASS_HANDLE handle = (CORINFO_CLASS_HANDLE)GetHandle();
        if (pImage->m_pPreloader->CanEmbedClassHandle(handle))
        {
            // We may have entries pointing to our module that exist in the handle table to trigger restore.
            if (pImage->GetCompileInfo()->GetLoaderModuleForEmbeddableType(handle) == pImage->GetModuleHandle())
            {
                *pfNeedsSignature = FALSE;
            }
            else
            {
                *pfIsEager = TRUE;
            }
         }

        return ZapImportSectionType_TypeHandle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeClass(ENCODE_TYPE_HANDLE, (CORINFO_CLASS_HANDLE)GetHandle(), pSigBuilder);
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        // We may have entries pointing to our module that exist in the handle table to trigger restore.
        if (!HasBlob())
        {
            PVOID cell;
            ZapNode handle(pImage->m_pPreloader->MapClassHandle((CORINFO_CLASS_HANDLE)GetHandle()));
            pImage->WriteReloc(&cell, 0, &handle, 0, IMAGE_REL_BASED_PTR);
            pZapWriter->Write(&cell, sizeof(cell));
        }
        else
        {
            ZapImport::Save(pZapWriter);
        }
    }
};

ZapImport * ZapImportTable::GetClassHandleImport(CORINFO_CLASS_HANDLE handle, PVOID pUniqueId)
{
    // pUniqueId is workaround for loading of generic parent method table. It would be nice to clean it up.
    if (pUniqueId != NULL)
    {
        return GetImport<ZapClassHandleImport, ZapNodeType_Import_ClassHandle>(handle, pUniqueId);
    }

    ZapImport * pImport = GetImport<ZapClassHandleImport, ZapNodeType_Import_ClassHandle>(handle);

    if (IsReadyToRunCompilation() && !pImport->HasBlob())
    {
        SigBuilder sigBuilder;

        EncodeClass(ENCODE_TYPE_HANDLE, handle, &sigBuilder);

        pImport->SetBlob(GetBlob(&sigBuilder));
    }

    return pImport;
}

class ZapFieldHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_FieldHandle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeField(ENCODE_FIELD_HANDLE, (CORINFO_FIELD_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetFieldHandleImport(CORINFO_FIELD_HANDLE handle)
{
    return GetImport<ZapFieldHandleImport, ZapNodeType_Import_FieldHandle>(handle);
}

class ZapMethodHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_MethodHandle;
    }

    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        ZapImport::ComputePlacement(pImage, pfIsEager, pfNeedsSignature);

        if (IsReadyToRunCompilation())
            return ZapImportSectionType_Handle;

        CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)GetHandle();
        if (pImage->m_pPreloader->CanEmbedMethodHandle(handle))
        {
            // We may have entries pointing to our module that exist in the handle table to trigger restore.
            if (pImage->GetCompileInfo()->GetLoaderModuleForEmbeddableMethod(handle) == pImage->GetModuleHandle())
            {
                *pfNeedsSignature = FALSE;
            }
        }

        return ZapImportSectionType_MethodHandle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeMethod(ENCODE_METHOD_HANDLE, (CORINFO_METHOD_HANDLE)GetHandle(), pSigBuilder);
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        // We may have entries pointing to our module that exist in the handle table to trigger restore.
        if (!HasBlob())
        {
            PVOID cell;
            ZapNode handle(pImage->m_pPreloader->MapMethodHandle((CORINFO_METHOD_HANDLE)GetHandle()));
            pImage->WriteReloc(&cell, 0, &handle, 0, IMAGE_REL_BASED_PTR);
            pZapWriter->Write(&cell, sizeof(cell));
        }
        else
        {
            ZapImport::Save(pZapWriter);
        }
    }
};

ZapImport * ZapImportTable::GetMethodHandleImport(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapMethodHandleImport, ZapNodeType_Import_MethodHandle>(handle);
}

class ZapStringHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_StringHandle;
    }

    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        ZapImport::ComputePlacement(pImage, pfIsEager, pfNeedsSignature);

        // Empty string
        if ((mdString)(size_t)GetHandle2() == mdtString)
        {
            *pfIsEager = TRUE;
            return ZapImportSectionType_Handle;
        }

        return ZapImportSectionType_StringHandle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        CORINFO_MODULE_HANDLE referencingModule = (CORINFO_MODULE_HANDLE)GetHandle();

        pTable->EncodeModule(ENCODE_STRING_HANDLE, referencingModule, pSigBuilder);

        mdString token = (mdString)(size_t)GetHandle2();
        pSigBuilder->AppendData(RidFromToken(token));
    }
};

ZapImport * ZapImportTable::GetStringHandleImport(CORINFO_MODULE_HANDLE tokenScope, mdString metaTok)
{
    return GetImport<ZapStringHandleImport, ZapNodeType_Import_StringHandle>(tokenScope, (PVOID)(size_t)metaTok);
}

class ZapFunctionEntryImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_FunctionEntry;
    }

#ifdef TARGET_ARM
    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        ZapImport::ComputePlacement(pImage, pfIsEager, pfNeedsSignature);
        return ZapImportSectionType_PCode;
    }
#endif

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)GetHandle();

        CORINFO_MODULE_HANDLE referencingModule;
        mdToken token = pTable->GetCompileInfo()->TryEncodeMethodAsToken(handle, NULL, &referencingModule);
        if (token != mdTokenNil)
        {
            _ASSERTE(TypeFromToken(token) == mdtMethodDef || TypeFromToken(token) == mdtMemberRef);
            _ASSERTE(!pTable->GetCompileInfo()->IsUnmanagedCallersOnlyMethod(handle));

            pTable->EncodeModule(
                (TypeFromToken(token) == mdtMethodDef) ? ENCODE_METHOD_ENTRY_DEF_TOKEN : ENCODE_METHOD_ENTRY_REF_TOKEN,
                referencingModule, pSigBuilder);

            pSigBuilder->AppendData(RidFromToken(token));
        }
        else
        {
            pTable->EncodeMethod(ENCODE_METHOD_ENTRY, handle, pSigBuilder);
        }
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        SIZE_T token = CORCOMPILE_TAG_PCODE(GetBlob()->GetRVA());
        pZapWriter->Write(&token, sizeof(token));
    }
};

ZapImport * ZapImportTable::GetFunctionEntryImport(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapFunctionEntryImport, ZapNodeType_Import_FunctionEntry>(handle);
}

class ZapStaticFieldAddressImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_StaticFieldAddress;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeField(ENCODE_STATIC_FIELD_ADDRESS, (CORINFO_FIELD_HANDLE)GetHandle(), pSigBuilder);
    }

    virtual DWORD GetSize()
    {
        return 2 * TARGET_POINTER_SIZE;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImport::Save(pZapWriter);

        TADDR value = 0;
        pZapWriter->Write(&value, sizeof(value));
    }
};

ZapImport * ZapImportTable::GetStaticFieldAddressImport(CORINFO_FIELD_HANDLE handle)
{
    return GetImport<ZapStaticFieldAddressImport, ZapNodeType_Import_StaticFieldAddress>(handle);
}

class ZapClassDomainIdImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ClassDomainId;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeClass(ENCODE_CLASS_ID_FOR_STATICS, (CORINFO_CLASS_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetClassDomainIdImport(CORINFO_CLASS_HANDLE handle)
{
    return GetImport<ZapClassDomainIdImport, ZapNodeType_Import_ClassDomainId>(handle);
}

class ZapModuleDomainIdImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ModuleDomainId;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        if (GetHandle() != NULL)
        {
            pTable->EncodeModule(ENCODE_MODULE_ID_FOR_STATICS, (CORINFO_MODULE_HANDLE)GetHandle(), pSigBuilder);
        }
        else
        {
            _ASSERTE(GetHandle2() != NULL);

            pTable->EncodeClass(ENCODE_MODULE_ID_FOR_GENERIC_STATICS, (CORINFO_CLASS_HANDLE)GetHandle2(), pSigBuilder);
        }
    }
};

ZapImport * ZapImportTable::GetModuleDomainIdImport(CORINFO_MODULE_HANDLE handleToModule, CORINFO_CLASS_HANDLE handleToClass)
{
    _ASSERTE(((void *)handleToModule != (void *)handleToClass) && ((handleToModule == NULL) || (handleToClass == NULL)));

    return GetImport<ZapModuleDomainIdImport, ZapNodeType_Import_ModuleDomainId>(handleToModule, handleToClass);
}

class ZapSyncLockImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_SyncLock;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeClass(ENCODE_SYNC_LOCK, (CORINFO_CLASS_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetSyncLockImport(CORINFO_CLASS_HANDLE handle)
{
    return GetImport<ZapSyncLockImport, ZapNodeType_Import_SyncLock>(handle);
}

class ZapIndirectPInvokeTargetImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_IndirectPInvokeTarget;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeMethod(ENCODE_INDIRECT_PINVOKE_TARGET, (CORINFO_METHOD_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetIndirectPInvokeTargetImport(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapIndirectPInvokeTargetImport, ZapNodeType_Import_IndirectPInvokeTarget>(handle);
}

class ZapPInvokeTargetImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_PInvokeTarget;
    }

    virtual void EncodeSignature(ZapImportTable* pTable, SigBuilder* pSigBuilder)
    {
        pTable->EncodeMethod(ENCODE_PINVOKE_TARGET, (CORINFO_METHOD_HANDLE)GetHandle(), pSigBuilder);
    }
};

ZapImport * ZapImportTable::GetPInvokeTargetImport(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapPInvokeTargetImport, ZapNodeType_Import_PInvokeTarget>(handle);
}

class ZapProfilingHandleImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ProfilingHandle;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeMethod(ENCODE_PROFILING_HANDLE, (CORINFO_METHOD_HANDLE)GetHandle(), pSigBuilder);
    }

    virtual DWORD GetSize()
    {
        // fixup cell, 3 pointers to interception method (Enter/Leave/Tailcall) and opaque handle
        return kZapProfilingHandleImportValueIndexCount * TARGET_POINTER_SIZE;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        // Save fixup cell
        ZapImport::Save(pZapWriter);

        // Save zeroes for the rest of the entries

        // If this assert fires, someone changed the
        // kZapProfilingHandleImportValueIndex... enum values without updating me!
        _ASSERTE(kZapProfilingHandleImportValueIndexCount == 5);

        TADDR value = 0;
        pZapWriter->Write(&value, sizeof(value));
        pZapWriter->Write(&value, sizeof(value));
        pZapWriter->Write(&value, sizeof(value));
        pZapWriter->Write(&value, sizeof(value));
    }
};

ZapImport * ZapImportTable::GetProfilingHandleImport(CORINFO_METHOD_HANDLE handle)
{
    return GetImport<ZapProfilingHandleImport, ZapNodeType_Import_ProfilingHandle>(handle);
}

class ZapVarArgImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_VarArg;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        _ASSERTE((CORINFO_MODULE_HANDLE)GetHandle() == pTable->GetImage()->GetModuleHandle());

        mdToken token = (mdToken)(size_t)GetHandle2();
        switch (TypeFromToken(token))
        {
        case mdtSignature:
            pSigBuilder->AppendByte(ENCODE_VARARGS_SIG);
            break;

        case mdtMemberRef:
            pSigBuilder->AppendByte(ENCODE_VARARGS_METHODREF);
            break;

        case mdtMethodDef:
            pSigBuilder->AppendByte(ENCODE_VARARGS_METHODDEF);
            break;

        default:
            _ASSERTE(!"Bogus token for signature");
        }

        pSigBuilder->AppendData(RidFromToken(token));
    }
};

ZapImport * ZapImportTable::GetVarArgImport(CORINFO_MODULE_HANDLE handle, mdToken sigOrMemberRefOrDef)
{
    return GetImport<ZapVarArgImport, ZapNodeType_Import_VarArg>(handle, (PVOID)(size_t)sigOrMemberRefOrDef);
}

class ZapActiveDependencyImport : public ZapImport
{
public:
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_ActiveDependency;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        pTable->EncodeModule(ENCODE_ACTIVE_DEPENDENCY, (CORINFO_MODULE_HANDLE)GetHandle(), pSigBuilder);
        pSigBuilder->AppendData(pTable->GetIndexOfModule((CORINFO_MODULE_HANDLE)GetHandle2()));
    }
};

ZapImport * ZapImportTable::GetActiveDependencyImport(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo)
{
    return GetImport<ZapActiveDependencyImport, ZapNodeType_Import_ActiveDependency>(moduleFrom, moduleTo);
}

ZapImport * ZapImportTable::GetClassImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CORINFO_CLASS_HANDLE handle = (CORINFO_CLASS_HANDLE) pResolvedToken->hClass;

    ZapImport * pImport = GetImport<ZapClassHandleImport, ZapNodeType_Import_ClassHandle>(handle, (PVOID)kind);

    if (!pImport->HasBlob())
    {
        SigBuilder sigBuilder;

        EncodeClass(kind, handle, &sigBuilder);

        pImport->SetBlob(GetBlob(&sigBuilder));
    }

    return pImport;
}

ZapImport * ZapImportTable::GetMethodImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle,
    CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken /*=NULL*/)
{
    SigBuilder sigBuilder;
    EncodeMethod(kind, handle, &sigBuilder, pResolvedToken, pConstrainedResolvedToken);

    return GetImportForSignature<ZapMethodHandleImport, ZapNodeType_Import_MethodHandle>((PVOID)handle, &sigBuilder);
}

ZapImport * ZapImportTable::GetFieldImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    SigBuilder sigBuilder;
    EncodeField(kind, handle, &sigBuilder, pResolvedToken);

    return GetImportForSignature<ZapFieldHandleImport, ZapNodeType_Import_FieldHandle>((PVOID)handle, &sigBuilder);
}

#ifdef FEATURE_READYTORUN_COMPILER
ZapImport * ZapImportTable::GetCheckTypeLayoutImport(CORINFO_CLASS_HANDLE handle)
{
    ZapImport * pImport = GetImport<ZapClassHandleImport, ZapNodeType_Import_ClassHandle>(handle, (PVOID)ENCODE_CHECK_TYPE_LAYOUT);

    if (!pImport->HasBlob())
    {
        SigBuilder sigBuilder;

        sigBuilder.AppendData(ENCODE_CHECK_TYPE_LAYOUT);

        GetCompileInfo()->EncodeClass(m_pImage->GetModuleHandle(), handle, &sigBuilder, NULL, NULL);

        GetCompileInfo()->EncodeTypeLayout(handle, &sigBuilder);

        pImport->SetBlob(GetBlob(&sigBuilder));
    }

    return pImport;
}

ZapImport * ZapImportTable::GetCheckFieldOffsetImport(CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, DWORD offset)
{
    SigBuilder sigBuilder;

    sigBuilder.AppendData(ENCODE_CHECK_FIELD_OFFSET);

    sigBuilder.AppendData(offset);

    GetCompileInfo()->EncodeField(m_pImage->GetModuleHandle(), handle, &sigBuilder, NULL, NULL, pResolvedToken);

    EncodeField(ENCODE_CHECK_FIELD_OFFSET, handle, &sigBuilder, pResolvedToken);

    return GetImportForSignature<ZapFieldHandleImport, ZapNodeType_Import_FieldHandle>((PVOID)handle, &sigBuilder);
}
#endif // FEATURE_READYTORUN_COMPILER

ZapImport * ZapImportTable::GetStubDispatchCell(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CORINFO_METHOD_HANDLE handle = pResolvedToken->hMethod;

    SigBuilder sigBuilder;

    DWORD slot = GetCompileInfo()->TryEncodeMethodSlot(handle);
    if (slot != (DWORD) -1)
    {
        CORINFO_CLASS_HANDLE clsHandle = pResolvedToken->hClass;

        CORINFO_MODULE_HANDLE referencingModule = GetJitInfo()->getClassModule(clsHandle);

        referencingModule = TryEncodeModule(ENCODE_VIRTUAL_ENTRY_SLOT, referencingModule, &sigBuilder);

        sigBuilder.AppendData(slot);

        EncodeClassInContext(referencingModule, clsHandle, &sigBuilder);
    }
    else
    {
        CORINFO_MODULE_HANDLE referencingModule;
        mdToken token = GetCompileInfo()->TryEncodeMethodAsToken(handle, pResolvedToken, &referencingModule);
        if (token != mdTokenNil)
        {
            _ASSERTE(TypeFromToken(token) == mdtMethodDef || TypeFromToken(token) == mdtMemberRef);

            EncodeModule(
                (TypeFromToken(token) == mdtMethodDef) ? ENCODE_VIRTUAL_ENTRY_DEF_TOKEN : ENCODE_VIRTUAL_ENTRY_REF_TOKEN,
                referencingModule, &sigBuilder);

            sigBuilder.AppendData(RidFromToken(token));
        }
        else
        {
            EncodeMethod(ENCODE_VIRTUAL_ENTRY, handle, &sigBuilder, pResolvedToken);
        }
    }

    // For now, always optimize ready to run for size and startup performance - share cells between callsites
    return GetImportForSignature<ZapStubDispatchCell, ZapNodeType_StubDispatchCell>((PVOID)handle, &sigBuilder);
}

ZapImport * ZapImportTable::GetExternalMethodCell(CORINFO_METHOD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken)
{
    SigBuilder sigBuilder;

    CORINFO_MODULE_HANDLE referencingModule;
    mdToken token = GetCompileInfo()->TryEncodeMethodAsToken(handle, pResolvedToken, &referencingModule);
    if (token != mdTokenNil)
    {
        _ASSERTE(TypeFromToken(token) == mdtMethodDef || TypeFromToken(token) == mdtMemberRef);

        EncodeModule(
            (TypeFromToken(token) == mdtMethodDef) ? ENCODE_METHOD_ENTRY_DEF_TOKEN : ENCODE_METHOD_ENTRY_REF_TOKEN,
            referencingModule, &sigBuilder);

        sigBuilder.AppendData(RidFromToken(token));
    }
    else
    {
        EncodeMethod(ENCODE_METHOD_ENTRY, handle, &sigBuilder, pResolvedToken, pConstrainedResolvedToken, false);
    }

    return GetImportForSignature<ZapExternalMethodCell, ZapNodeType_ExternalMethodCell>((PVOID)handle, &sigBuilder);
}


#ifdef FEATURE_READYTORUN_COMPILER

class ZapDynamicHelperCell : public ZapImport
{
    ZapNode * m_pDelayLoadHelper;

public:
    void SetDelayLoadHelper(ZapNode * pDelayLoadHelper)
    {
        _ASSERTE(m_pDelayLoadHelper == NULL);
        m_pDelayLoadHelper = pDelayLoadHelper;
    }

    virtual DWORD GetSize()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_DynamicHelperCell;
    }

    CORCOMPILE_FIXUP_BLOB_KIND GetKind()
    {
        int kind = (int)(SIZE_T)GetHandle();

        if ((kind & 1) == 1)
        {
            return (CORCOMPILE_FIXUP_BLOB_KIND)(kind >> 1);
        }
        else
        {
            _ASSERTE(
                (GetBlob()->GetSize() > 0) && (
                    GetBlob()->GetData()[0] == ENCODE_DICTIONARY_LOOKUP_THISOBJ ||
                    GetBlob()->GetData()[0] == ENCODE_DICTIONARY_LOOKUP_METHOD ||
                    GetBlob()->GetData()[0] == ENCODE_DICTIONARY_LOOKUP_TYPE));

            return (CORCOMPILE_FIXUP_BLOB_KIND)GetBlob()->GetData()[0];
        }
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        // Encode should be unreachable for ready-to-run cells
        _ASSERTE(false);
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        TARGET_POINTER_TYPE cell;
        pImage->WriteReloc(&cell, 0, m_pDelayLoadHelper, 0, IMAGE_REL_BASED_PTR);
        pZapWriter->Write(&cell, sizeof(cell));
    }
};

static ReadyToRunHelper GetDelayLoadHelperForDynamicHelper(CORCOMPILE_FIXUP_BLOB_KIND kind)
{
    switch (kind)
    {
    case ENCODE_NEW_HELPER:
    case ENCODE_NEW_ARRAY_HELPER:
    case ENCODE_STATIC_BASE_NONGC_HELPER:
    case ENCODE_STATIC_BASE_GC_HELPER:
    case ENCODE_THREAD_STATIC_BASE_NONGC_HELPER:
    case ENCODE_THREAD_STATIC_BASE_GC_HELPER:
    case ENCODE_CCTOR_TRIGGER:
    case ENCODE_FIELD_ADDRESS:
    case ENCODE_DICTIONARY_LOOKUP_THISOBJ:
    case ENCODE_DICTIONARY_LOOKUP_TYPE:
    case ENCODE_DICTIONARY_LOOKUP_METHOD:
        return READYTORUN_HELPER_DelayLoad_Helper;

    case ENCODE_CHKCAST_HELPER:
    case ENCODE_ISINSTANCEOF_HELPER:
        return READYTORUN_HELPER_DelayLoad_Helper_Obj;

    case ENCODE_VIRTUAL_ENTRY:
    // case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
    // case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
    // case ENCODE_VIRTUAL_ENTRY_SLOT:
        return READYTORUN_HELPER_DelayLoad_Helper_Obj;

    case ENCODE_DELEGATE_CTOR:
        return READYTORUN_HELPER_DelayLoad_Helper_ObjObj;

    default:
        UNREACHABLE();
    }
}

void ZapImportSectionSignatures::PlaceDynamicHelperCell(ZapImport * pImport)
{
    ZapDynamicHelperCell * pCell = (ZapDynamicHelperCell *)pImport;

    if (m_pImportSection->GetNodeCount() == 0)
    {
        m_dwIndex = m_pImage->GetImportSectionsTable()->Append(CORCOMPILE_IMPORT_TYPE_UNKNOWN, CORCOMPILE_IMPORT_FLAGS_PCODE,
            TARGET_POINTER_SIZE, m_pImportSection, this, m_pGCRefMapTable);
    }

    // Create the delay load helper
    ReadyToRunHelper helperNum = GetDelayLoadHelperForDynamicHelper(pCell->GetKind());
    ZapNode * pDelayLoadHelper = m_pImage->GetImportTable()->GetPlacedIndirectHelperThunk(helperNum, (PVOID)(SIZE_T)m_dwIndex);

    pCell->SetDelayLoadHelper(pDelayLoadHelper);

    // Add entry to both the the cell and data sections
    m_pImportSection->Place(pCell);

    m_pImage->GetImportTable()->PlaceImportBlob(pCell);
}

ZapImport * ZapImportTable::GetDictionaryLookupCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE containingMethod, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_LOOKUP_KIND * pLookup)
{
    _ASSERTE(pLookup->needsRuntimeLookup);

    SigBuilder sigBuilder;

    sigBuilder.AppendData(kind);

    if (kind == ENCODE_DICTIONARY_LOOKUP_THISOBJ)
    {
        CORINFO_CLASS_HANDLE hClassContext = GetJitInfo()->getMethodClass(containingMethod);
        GetCompileInfo()->EncodeClass(m_pImage->GetModuleHandle(), hClassContext, &sigBuilder, this, EncodeModuleHelper);
    }

    switch (pLookup->runtimeLookupFlags)
    {
    case READYTORUN_FIXUP_TypeHandle:
    case READYTORUN_FIXUP_DeclaringTypeHandle:
        {
            if (pResolvedToken->pTypeSpec == NULL)
            {
                _ASSERTE(!"Invalid IL that directly references __Canon");
                ThrowHR(E_NOTIMPL);
            }

            if (pLookup->runtimeLookupFlags == READYTORUN_FIXUP_DeclaringTypeHandle)
            {
                _ASSERTE(pLookup->runtimeLookupArgs != NULL);
                sigBuilder.AppendData(ENCODE_DECLARINGTYPE_HANDLE);
                GetCompileInfo()->EncodeClass(m_pImage->GetModuleHandle(), (CORINFO_CLASS_HANDLE)pLookup->runtimeLookupArgs, &sigBuilder, this, EncodeModuleHelper);
            }
            else
            {
                sigBuilder.AppendData(ENCODE_TYPE_HANDLE);
            }

            if (pResolvedToken->tokenType == CORINFO_TOKENKIND_Newarr)
                sigBuilder.AppendElementType(ELEMENT_TYPE_SZARRAY);
            sigBuilder.AppendBlob((PVOID)pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
        }
        break;

    case READYTORUN_FIXUP_MethodHandle:
        EncodeMethod(ENCODE_METHOD_HANDLE, pResolvedToken->hMethod, &sigBuilder, pResolvedToken, NULL, TRUE);
        break;

    case READYTORUN_FIXUP_MethodEntry:
        EncodeMethod(ENCODE_METHOD_ENTRY, pResolvedToken->hMethod, &sigBuilder, pResolvedToken, (CORINFO_RESOLVED_TOKEN*)pLookup->runtimeLookupArgs, TRUE);
        break;

    case READYTORUN_FIXUP_VirtualEntry:
        EncodeMethod(ENCODE_VIRTUAL_ENTRY, pResolvedToken->hMethod, &sigBuilder, pResolvedToken, NULL, TRUE);
        break;

    case READYTORUN_FIXUP_FieldHandle:
        EncodeField(ENCODE_FIELD_HANDLE, pResolvedToken->hField, &sigBuilder, pResolvedToken, TRUE);
        break;

    default:
        _ASSERTE(!"Invalid R2R fixup kind!");
        ThrowHR(E_NOTIMPL);
    }

    return GetImportForSignature<ZapDynamicHelperCell, ZapNodeType_DynamicHelperCell>((void*)containingMethod, &sigBuilder);
}

ZapImport * ZapImportTable::GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_CLASS_HANDLE handle)
{
    ZapImport * pImport = GetImport<ZapDynamicHelperCell, ZapNodeType_DynamicHelperCell>((void *)(uintptr_t)((kind << 1) | 1), handle);

    if (!pImport->HasBlob())
    {
        SigBuilder sigBuilder;

        sigBuilder.AppendData(kind);

        GetCompileInfo()->EncodeClass(m_pImage->GetModuleHandle(), handle, &sigBuilder, this, EncodeModuleHelper);
        pImport->SetBlob(GetBlob(&sigBuilder));
    }

    return pImport;
}

ZapImport * ZapImportTable::GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken,
    CORINFO_CLASS_HANDLE delegateType /*=NULL*/)
{
    SigBuilder sigBuilder;

    EncodeMethod(kind, handle, &sigBuilder, pResolvedToken);

    if (delegateType != NULL)
    {
        _ASSERTE(kind == ENCODE_DELEGATE_CTOR);
        GetCompileInfo()->EncodeClass(m_pImage->GetModuleHandle(), delegateType, &sigBuilder, this, EncodeModuleHelper);
    }

    return GetImportForSignature<ZapDynamicHelperCell, ZapNodeType_DynamicHelperCell>((void *)(uintptr_t)((kind << 1) | 1), &sigBuilder);
}

ZapImport * ZapImportTable::GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    SigBuilder sigBuilder;

    EncodeField(kind, handle, &sigBuilder, pResolvedToken);

    return GetImportForSignature<ZapDynamicHelperCell, ZapNodeType_DynamicHelperCell>((void *)(uintptr_t)((kind << 1) | 1), &sigBuilder);
}

class ZapIndirectHelperThunk : public ZapImport
{
    DWORD SaveWorker(ZapWriter * pZapWriter);

public:
    ReadyToRunHelper GetReadyToRunHelper()
    {
        return (ReadyToRunHelper)((DWORD)(SIZE_T)GetHandle() & ~READYTORUN_HELPER_FLAG_VSD);
    }

    DWORD GetSectionIndex()
    {
        return (DWORD)(SIZE_T)GetHandle2();
    }

    BOOL IsDelayLoadHelper()
    {
        ReadyToRunHelper helper = GetReadyToRunHelper();
        return (helper == READYTORUN_HELPER_DelayLoad_MethodCall) ||
               (helper == READYTORUN_HELPER_DelayLoad_Helper) ||
               (helper == READYTORUN_HELPER_DelayLoad_Helper_Obj) ||
               (helper == READYTORUN_HELPER_DelayLoad_Helper_ObjObj);
    }

    BOOL IsDelayLoadMethodCallHelper()
    {
        ReadyToRunHelper helper = GetReadyToRunHelper();
        return (helper == READYTORUN_HELPER_DelayLoad_MethodCall);
    }

    BOOL IsVSD()
    {
        return ((DWORD)(SIZE_T)GetHandle() & READYTORUN_HELPER_FLAG_VSD) != 0;
    }

    BOOL IsLazyHelper()
    {
        ReadyToRunHelper helper = GetReadyToRunHelper();
        return (helper == READYTORUN_HELPER_GetString);
    }

    DWORD GetSize()
    {
        return SaveWorker(NULL);
    }

    void Save(ZapWriter * pZapWriter)
    {
        SaveWorker(pZapWriter);
    }

    virtual UINT GetAlignment()
    {
        return MINIMUM_CODE_ALIGN;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_IndirectHelperThunk;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        // Encode should be unreachable for ready-to-run cells
        _ASSERTE(false);
    }
};

#ifdef TARGET_ARM
static void MovRegImm(BYTE* p, int reg)
{
    *(WORD *)(p + 0) = 0xF240;
    *(WORD *)(p + 2) = (UINT16)(reg << 8);
    *(WORD *)(p + 4) = 0xF2C0;
    *(WORD *)(p + 6) = (UINT16)(reg << 8);
}
#endif // TARGET_ARM

DWORD ZapIndirectHelperThunk::SaveWorker(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    BYTE buffer[44];
    BYTE * p = buffer;

#if defined(TARGET_X86)
    if (IsDelayLoadHelper())
    {
        // xor eax, eax
        *p++ = 0x33;
        *p++ = 0xC0;

        // push index
        *p++ = 0x6A;
        _ASSERTE(GetSectionIndex() <= 0x7F);
        *p++ = (BYTE)GetSectionIndex();

        // push [module]
        *p++ = 0xFF;
        *p++ = 0x35;
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_PTR);
        p += 4;
    }
    else
    if (IsLazyHelper())
    {
        // mov edx, [module]
        *p++ = 0x8B;
        *p++ = 0x15;
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_PTR);
        p += 4;
    }

    // jmp [helper]
    *p++ = 0xFF;
    *p++ = 0x25;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(GetReadyToRunHelper()), 0, IMAGE_REL_BASED_PTR);
    p += 4;
#elif defined(TARGET_AMD64)
    if (IsDelayLoadHelper())
    {
        if (IsVSD())
        {
            // mov rax, r11
            *p++ = 0x49;
            *p++ = 0x8b;
            *p++ = 0xc3;
        }
        else
        {
            // xor eax, eax
            *p++ = 0x33;
            *p++ = 0xC0;
        }

        // push index
        *p++ = 0x6A;
        _ASSERTE(GetSectionIndex() <= 0x7F);
        *p++ = (BYTE)GetSectionIndex();

        // push [module]
        *p++ = 0xFF;
        *p++ = 0x35;
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_REL32);
        p += 4;
    }
    else
    if (IsLazyHelper())
    {
        *p++ = 0x48;
        *p++ = 0x8B;
#ifdef UNIX_AMD64_ABI
        // mov rsi, [module]
        *p++ = 0x35;
#else
        // mov rdx, [module]
        *p++ = 0x15;
#endif
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_REL32);
        p += 4;
    }

    // jmp [helper]
    *p++ = 0xFF;
    *p++ = 0x25;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(GetReadyToRunHelper()), 0, IMAGE_REL_BASED_REL32);
    p += 4;
#elif defined(TARGET_ARM)
    if (IsDelayLoadHelper())
    {
        // r4 contains indirection cell
        // push r4
        *(WORD *)(p + 0) = 0xB410;
        p += 2;

        // mov r4, index
        _ASSERTE(GetSectionIndex() <= 0x7F);
        *(WORD *)(p + 0) = 0x2400 | (BYTE)GetSectionIndex();
        p += 2;

        // push r4
        *(WORD *)(p + 0) = 0xB410;
        p += 2;

        // mov r4, [module]
        MovRegImm(p, 4);
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_THUMB_MOV32);
        p += 8;

        // ldr r4, [r4]
        *(WORD *)p = 0x6824;
        p += 2;

        // push r4
        *(WORD *)(p + 0) = 0xB410;
        p += 2;

        // mov r4, [helper]
        MovRegImm(p, 4);
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int)(p - buffer), pImage->GetImportTable()->GetHelperImport(GetReadyToRunHelper()), 0, IMAGE_REL_BASED_THUMB_MOV32);
        p += 8;

        // ldr r4, [r4]
        *(WORD *)p = 0x6824;
        p += 2;

        // bx r4
        *(WORD *)p = 0x4720;
        p += 2;
    }
    else
    {
        if (IsLazyHelper())
        {
            // mov r1, [helper]
            MovRegImm(p, 1);
            if (pImage != NULL)
                pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_THUMB_MOV32);
            p += 8;

            // ldr r1, [r1]
            *(WORD *)p = 0x6809;
            p += 2;
        }

        // mov r12, [helper]
        MovRegImm(p, 12);
        if (pImage != NULL)
            pImage->WriteReloc(buffer, (int) (p - buffer), pImage->GetImportTable()->GetHelperImport(GetReadyToRunHelper()), 0, IMAGE_REL_BASED_THUMB_MOV32);
        p += 8;

        // ldr r12, [r12]
        *(DWORD *)p = 0xC000F8DC;
        p += 4;

        // bx r12
        *(WORD *)p = 0x4760;
        p += 2;
    }
#elif defined(TARGET_ARM64)
    if (IsDelayLoadHelper())
    {
        // x11 contains indirection cell
        // Do nothing x11 contains our first param

        //  movz x9, #index
        DWORD index = GetSectionIndex();
        _ASSERTE(index <= 0x7F);
        *(DWORD*)p = 0xd2800009 | (index << 5);
        p += 4;

        // move Module* -> x10
        // ldr x10, [PC+0x14]
        *(DWORD*)p = 0x580000AA;
        p += 4;

        //ldr x10, [x10]
        *(DWORD*)p = 0xf940014A;
        p += 4;
    }
    else
    if (IsLazyHelper())
    {
        // Move Module* -> x1
        // ldr x1, [PC+0x14]
        *(DWORD*)p = 0x580000A1;
        p += 4;

        // ldr x1, [x1]
        *(DWORD*)p = 0xf9400021;
        p += 4;
    }

    // branch to helper
    // ldr x12, [PC+0x14]
    *(DWORD*)p = 0x580000AC;
    p += 4;

    // ldr x12, [x12]
    *(DWORD *)p = 0xf940018c;
    p += 4;

    // br x12
    *(DWORD *)p = 0xd61f0180;
    p += 4;

    // [Module*]
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_Module), 0, IMAGE_REL_BASED_PTR);
    p += 8;

    // [helper]
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), pImage->GetImportTable()->GetHelperImport(GetReadyToRunHelper()), 0, IMAGE_REL_BASED_PTR);
    p += 8;
#else
    PORTABILITY_ASSERT("ZapIndirectHelperThunk::SaveWorker");
#endif

    _ASSERTE((DWORD)(p - buffer) <= sizeof(buffer));

    if (pZapWriter != NULL)
        pZapWriter->Write(&buffer, (int)(p - buffer));

    return (DWORD)(p - buffer);
}

void ZapImportTable::PlaceIndirectHelperThunk(ZapNode * pImport)
{
    ZapIndirectHelperThunk * pThunk = (ZapIndirectHelperThunk *)pImport;

    if (pThunk->IsDelayLoadMethodCallHelper())
        m_pImage->m_pLazyMethodCallHelperSection->Place(pThunk);
    else
        m_pImage->m_pLazyHelperSection->Place(pThunk);

    if (pThunk->IsDelayLoadHelper() || pThunk->IsLazyHelper())
       GetPlacedHelperImport(READYTORUN_HELPER_Module);

    GetPlacedHelperImport(pThunk->GetReadyToRunHelper());
}

ZapNode * ZapImportTable::GetIndirectHelperThunk(ReadyToRunHelper helperNum, PVOID pArg)
{
    ZapNode * pImport = GetImport<ZapIndirectHelperThunk, ZapNodeType_IndirectHelperThunk>((void *)helperNum, pArg);
#if defined(TARGET_ARM)
    pImport = m_pImage->GetInnerPtr(pImport, THUMB_CODE);
#endif
    return pImport;
}

ZapNode * ZapImportTable::GetPlacedIndirectHelperThunk(ReadyToRunHelper helperNum, PVOID pArg)
{
    ZapNode * pImport = GetImport<ZapIndirectHelperThunk, ZapNodeType_IndirectHelperThunk>((void *)helperNum, pArg);
    if (!pImport->IsPlaced())
        PlaceIndirectHelperThunk(pImport);
#if defined(TARGET_ARM)
    pImport = m_pImage->GetInnerPtr(pImport, THUMB_CODE);
#endif
    return pImport;
}

class ZapHelperImport : public ZapImport
{
public:
    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        *pfIsEager = TRUE;
        *pfNeedsSignature = TRUE;
        return ZapImportSectionType_Handle;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Import_Helper;
    }

    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder)
    {
        // Encode should be unreachable for ready-to-run cells
        _ASSERTE(false);
    }
};

ZapImport * ZapImportTable::GetHelperImport(ReadyToRunHelper helperNum)
{
    ZapImport * pImport = GetImport<ZapHelperImport, ZapNodeType_Import_Helper>((void *)helperNum);

    if (!pImport->HasBlob())
    {
        SigBuilder sigBuilder;

        sigBuilder.AppendByte(ENCODE_READYTORUN_HELPER);
        sigBuilder.AppendData(helperNum);

        pImport->SetBlob(GetBlob(&sigBuilder));
    }

    return pImport;
}

ZapImport * ZapImportTable::GetPlacedHelperImport(ReadyToRunHelper helperNum)
{
    ZapImport * pImport = GetHelperImport(helperNum);
    if (!pImport->IsPlaced())
        PlaceImport(pImport);
    return pImport;
}

#endif // FEATURE_READYTORUN_COMPILER
