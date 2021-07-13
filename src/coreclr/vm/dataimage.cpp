// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#ifdef FEATURE_PREJIT

#include "dataimage.h"
#include "compile.h"

#include "field.h"

//
// Include Zapper infrastructure here
//
// dataimage.cpp is the only place where Zapper infrasture should be used directly in the VM.
// The rest of the VM should never use Zapper infrastructure directly for good layering.
// The long term goal is to move all NGen specific parts like Save and Fixup methods out of the VM,
// and remove the dataimage.cpp completely.
//
#include "zapper.h"
#include "../zap/zapwriter.h"
#include "../zap/zapimage.h"
#include "../zap/zapimport.h"
#include "inlinetracking.h"

#define NodeTypeForItemKind(kind) ((ZapNodeType)(ZapNodeType_StoredStructure + (kind)))

class ZapStoredStructure : public ZapNode
{
    DWORD  m_dwSize;
    BYTE    m_kind;
    BYTE    m_align;

public:
    ZapStoredStructure(DWORD dwSize, BYTE kind, BYTE align)
        : m_dwSize(dwSize), m_kind(kind), m_align(align)
    {
    }

    void * GetData()
    {
        return this + 1;
    }

    DataImage::ItemKind GetKind()
    {
        return (DataImage::ItemKind)m_kind;
    }

    virtual DWORD GetSize()
    {
        return m_dwSize;
    }

    virtual UINT GetAlignment()
    {
        return m_align;
    }

    virtual ZapNodeType GetType()
    {
        return NodeTypeForItemKind(m_kind);
    }

    virtual void Save(ZapWriter * pZapWriter);
};

inline ZapStoredStructure * AsStoredStructure(ZapNode * pNode)
{
    // Verify that it is one of the StoredStructure subtypes
    _ASSERTE(pNode->GetType() >= ZapNodeType_StoredStructure);
    return (ZapStoredStructure *)pNode;
}

struct InternedStructureKey
{
    InternedStructureKey(const void * data, DWORD dwSize, DataImage::ItemKind kind)
        : m_data(data), m_dwSize(dwSize), m_kind(kind)
    {
    }

    const void *m_data;
    DWORD       m_dwSize;
    DataImage::ItemKind    m_kind;
};

class InternedStructureTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapStoredStructure *> >
{
public:
    typedef InternedStructureKey key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return InternedStructureKey(e->GetData(), e->GetSize(), e->GetKind());
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return (k1.m_dwSize == k2.m_dwSize) &&
               (k1.m_kind == k2.m_kind) &&
               memcmp(k1.m_data, k2.m_data, k1.m_dwSize) == 0;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)k.m_dwSize ^ (count_t)k.m_kind ^ HashBytes((BYTE *)k.m_data, k.m_dwSize);
    }

    static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
};

DataImage::DataImage(Module *module, CEEPreloader *preloader)
    : m_module(module),
      m_preloader(preloader),
      m_iCurrentFixup(0),       // Dev11 bug 181494 instrumentation
      m_pInternedStructures(NULL),
      m_pCurrentAssociatedMethodTable(NULL)
{
    m_pZapImage = m_preloader->GetDataStore()->GetZapImage();
    m_pZapImage->m_pDataImage = this;

    m_pInternedStructures = new InternedStructureHashTable();
    m_inlineTrackingMap = new InlineTrackingMap();
}

DataImage::~DataImage()
{
    delete m_pInternedStructures;
    delete m_inlineTrackingMap;
}

void DataImage::PreSave()
{
#ifndef ZAP_HASHTABLE_TUNING
    Preallocate();
#endif
}

void DataImage::PostSave()
{
#ifdef ZAP_HASHTABLE_TUNING
    // If ZAP_HASHTABLE_TUNING is defined, preallocate is overloaded to print the tunning constants
    Preallocate();
#endif
}

DWORD DataImage::GetMethodProfilingFlags(MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    // We are not differentiating unboxing stubs vs. normal method descs in IBC data yet
    if (pMD->IsUnboxingStub())
        pMD = pMD->GetWrappedMethodDesc();

    const MethodProfilingData * pData = m_methodProfilingData.LookupPtr(pMD);
    return (pData != NULL) ? pData->flags : 0;
}

void DataImage::SetMethodProfilingFlags(MethodDesc * pMD, DWORD flags)
{
    STANDARD_VM_CONTRACT;

    const MethodProfilingData * pData = m_methodProfilingData.LookupPtr(pMD);
    if (pData != NULL)
    {
        const_cast<MethodProfilingData *>(pData)->flags |= flags;
        return;
    }

    MethodProfilingData data;
    data.pMD = pMD;
    data.flags = flags;
    m_methodProfilingData.Add(data);
}

void DataImage::Preallocate()
{
    STANDARD_VM_CONTRACT;

    // TODO: Move to ZapImage

    PEDecoder pe((void *)m_module->GetFile()->GetManagedFileContents());

    COUNT_T cbILImage = pe.GetSize();

    // Curb the estimate to handle corner cases gracefuly
    cbILImage = min(cbILImage, 50000000);

    PREALLOCATE_HASHTABLE(DataImage::m_structures, 0.019, cbILImage);
    PREALLOCATE_ARRAY(DataImage::m_structuresInOrder, 0.0088, cbILImage);
    PREALLOCATE_ARRAY(DataImage::m_Fixups, 0.046, cbILImage);
    PREALLOCATE_HASHTABLE(DataImage::m_surrogates, 0.0025, cbILImage);
    PREALLOCATE_HASHTABLE((*DataImage::m_pInternedStructures), 0.0007, cbILImage);
}

ZapHeap * DataImage::GetHeap()
{
    LIMITED_METHOD_CONTRACT;
    return m_pZapImage->GetHeap();
}

void DataImage::AddStructureInOrder(ZapNode *pNode, BOOL fMaintainSaveOrder /*=FALSE*/)
{
    WRAPPER_NO_CONTRACT;

    SavedNodeEntry entry;
    entry.pNode = pNode;
    entry.dwAssociatedOrder = 0;

    if (fMaintainSaveOrder)
    {
        entry.dwAssociatedOrder = MAINTAIN_SAVE_ORDER;
    }
    else if (m_pCurrentAssociatedMethodTable)
    {
        TypeHandle th = TypeHandle(m_pCurrentAssociatedMethodTable);
        entry.dwAssociatedOrder = m_pZapImage->LookupClassLayoutOrder(CORINFO_CLASS_HANDLE(th.AsPtr()));
    }

    m_structuresInOrder.Append(entry);
}

ZapStoredStructure * DataImage::StoreStructureHelper(const void *data, SIZE_T size,
                       DataImage::ItemKind kind,
                       int align,
                       BOOL fMaintainSaveOrder)
{
    STANDARD_VM_CONTRACT;

    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapStoredStructure)) + S_SIZE_T(size);
    if(cbAllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    void * pMemory = new (GetHeap()) BYTE[cbAllocSize.Value()];

    // PE files cannot be larger than 4 GB
    if (DWORD(size) != size)
        ThrowHR(E_UNEXPECTED);

    ZapStoredStructure * pStructure = new (pMemory) ZapStoredStructure((DWORD)size, static_cast<BYTE>(kind), static_cast<BYTE>(align));

    if (data != NULL)
    {
        CopyMemory(pStructure->GetData(), data, size);
        BindPointer(data, pStructure, 0);
    }

    m_pLastLookup = NULL;

    AddStructureInOrder(pStructure, fMaintainSaveOrder);

    return pStructure;
}

// Bind pointer to the relative offset in ZapNode
void DataImage::BindPointer(const void *p, ZapNode * pNode, SSIZE_T offset)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_structures.LookupPtr(p) == NULL);

    StructureEntry e;
    e.ptr = p;
    e.pNode = pNode;
    e.offset = offset;
    m_structures.Add(e);

    m_pLastLookup = NULL;
}

void DataImage::CopyData(ZapStoredStructure * pNode, const void * p, ULONG size)
{
    memcpy(pNode->GetData(), p, size);
}

void DataImage::CopyDataToOffset(ZapStoredStructure * pNode, ULONG offset, const void * p, ULONG size)
{
    SIZE_T target = (SIZE_T) (pNode->GetData());
    target += offset;

    memcpy((void *) target, p, size);
}

void DataImage::PlaceStructureForAddress(const void * data, CorCompileSection section)
{
    STANDARD_VM_CONTRACT;

    if (data == NULL)
        return;

    const StructureEntry * pEntry = m_structures.LookupPtr(data);
    if (pEntry == NULL)
        return;

    ZapNode * pNode = pEntry->pNode;
    if (!pNode->IsPlaced())
    {
        ZapVirtualSection * pSection = m_pZapImage->GetSection(section);
        pSection->Place(pNode);
    }
}

void DataImage::PlaceInternedStructureForAddress(const void * data, CorCompileSection sectionIfReused, CorCompileSection sectionIfSingleton)
{
    STANDARD_VM_CONTRACT;

    if (data == NULL)
        return;

    const StructureEntry * pEntry = m_structures.LookupPtr(data);
    if (pEntry == NULL)
        return;

    ZapNode * pNode = pEntry->pNode;
    if (!pNode->IsPlaced())
    {
        CorCompileSection section = m_reusedStructures.Contains(pNode) ? sectionIfReused : sectionIfSingleton;
        ZapVirtualSection * pSection = m_pZapImage->GetSection(section);
        pSection->Place(pNode);
    }
}

void DataImage::FixupPointerField(PVOID p, SSIZE_T offset)
{
    STANDARD_VM_CONTRACT;

    PVOID pTarget = *(PVOID UNALIGNED *)((BYTE *)p + offset);

    if (pTarget == NULL)
    {
        ZeroPointerField(p, offset);
        return;
    }

    FixupField(p, offset, pTarget);
}

void DataImage::FixupRelativePointerField(PVOID p, SSIZE_T offset)
{
    STANDARD_VM_CONTRACT;

    PVOID pTarget = RelativePointer<PTR_VOID>::GetValueMaybeNullAtPtr((TADDR)p + offset);

    if (pTarget == NULL)
    {
        ZeroPointerField(p, offset);
        return;
    }

    FixupField(p, offset, pTarget, 0, IMAGE_REL_BASED_RELPTR);
}

static void EncodeTargetOffset(PVOID pLocation, SSIZE_T targetOffset, ZapRelocationType type)
{
    // Store the targetOffset into the location of the reloc temporarily
    switch (type)
    {
    case IMAGE_REL_BASED_PTR:
    case IMAGE_REL_BASED_RELPTR:
        *(UNALIGNED TADDR *)pLocation = (TADDR)targetOffset;
        break;

    case IMAGE_REL_BASED_ABSOLUTE:
        *(UNALIGNED DWORD *)pLocation = (DWORD)targetOffset;
        break;

    case IMAGE_REL_BASED_ABSOLUTE_TAGGED:
        _ASSERTE(targetOffset == 0);
        *(UNALIGNED TADDR *)pLocation = 0;
        break;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    case IMAGE_REL_BASED_REL32:
        *(UNALIGNED INT32 *)pLocation = (INT32)targetOffset;
        break;
#endif // TARGET_X86 || TARGET_AMD64

    default:
        _ASSERTE(0);
    }
}

static SSIZE_T DecodeTargetOffset(PVOID pLocation, ZapRelocationType type)
{
    // Store the targetOffset into the location of the reloc temporarily
    switch (type)
    {
    case IMAGE_REL_BASED_PTR:
    case IMAGE_REL_BASED_RELPTR:
        return (SSIZE_T)*(UNALIGNED TADDR *)pLocation;

    case IMAGE_REL_BASED_ABSOLUTE:
        return *(UNALIGNED DWORD *)pLocation;

    case IMAGE_REL_BASED_ABSOLUTE_TAGGED:
        _ASSERTE(*(UNALIGNED TADDR *)pLocation == 0);
        return 0;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    case IMAGE_REL_BASED_REL32:
        return *(UNALIGNED INT32 *)pLocation;
#endif // TARGET_X86 || TARGET_AMD64

    default:
        _ASSERTE(0);
        return 0;
    }
}

void DataImage::FixupField(PVOID p, SSIZE_T offset, PVOID pTarget, SSIZE_T targetOffset, ZapRelocationType type)
{
    STANDARD_VM_CONTRACT;

    m_iCurrentFixup++;      // Dev11 bug 181494 instrumentation

    const StructureEntry * pEntry = m_pLastLookup;
    if (pEntry == NULL || pEntry->ptr != p)
    {
        pEntry = m_structures.LookupPtr(p);
        _ASSERTE(pEntry != NULL &&
            "StoreStructure or BindPointer have to be called on all save data.");
        m_pLastLookup = pEntry;
    }
    offset += pEntry->offset;
    _ASSERTE(0 <= offset && (DWORD)offset < pEntry->pNode->GetSize());

    const StructureEntry * pTargetEntry = m_pLastLookup;
    if (pTargetEntry == NULL || pTargetEntry->ptr != pTarget)
    {
        pTargetEntry = m_structures.LookupPtr(pTarget);

        _ASSERTE(pTargetEntry != NULL &&
            "The target of the fixup is not saved into the image");
    }
    targetOffset += pTargetEntry->offset;
    _ASSERTE(0 <= targetOffset && (DWORD)targetOffset <= pTargetEntry->pNode->GetSize());

    FixupEntry entry;
    entry.m_type = type;
    entry.m_offset = (DWORD)offset;
    entry.m_pLocation = AsStoredStructure(pEntry->pNode);
    entry.m_pTargetNode = pTargetEntry->pNode;
    AppendFixup(entry);

    EncodeTargetOffset((BYTE *)AsStoredStructure(pEntry->pNode)->GetData() + offset, targetOffset, type);
}

void DataImage::FixupFieldToNode(PVOID p, SSIZE_T offset, ZapNode * pTarget, SSIZE_T targetOffset, ZapRelocationType type)
{
    STANDARD_VM_CONTRACT;

    m_iCurrentFixup++;      // Dev11 bug 181494 instrumentation

    const StructureEntry * pEntry = m_pLastLookup;
    if (pEntry == NULL || pEntry->ptr != p)
    {
        pEntry = m_structures.LookupPtr(p);
        _ASSERTE(pEntry != NULL &&
            "StoreStructure or BindPointer have to be called on all save data.");
        m_pLastLookup = pEntry;
    }
    offset += pEntry->offset;
    _ASSERTE(0 <= offset && (DWORD)offset < pEntry->pNode->GetSize());

    _ASSERTE(pTarget != NULL);

    FixupEntry entry;
    entry.m_type = type;
    entry.m_offset = (DWORD)offset;
    entry.m_pLocation = AsStoredStructure(pEntry->pNode);
    entry.m_pTargetNode = pTarget;
    AppendFixup(entry);

    EncodeTargetOffset((BYTE *)AsStoredStructure(pEntry->pNode)->GetData() + offset, targetOffset, type);
}

DWORD DataImage::GetRVA(const void *data)
{
    STANDARD_VM_CONTRACT;

    const StructureEntry * pEntry = m_structures.LookupPtr(data);
    _ASSERTE(pEntry != NULL);

    return pEntry->pNode->GetRVA() + (DWORD)pEntry->offset;
}

void DataImage::ZeroField(PVOID p, SSIZE_T offset, SIZE_T size)
{
    STANDARD_VM_CONTRACT;

    ZeroMemory(GetImagePointer(p, offset), size);
}

void * DataImage::GetImagePointer(ZapStoredStructure * pNode)
{
    return pNode->GetData();
}

void * DataImage::GetImagePointer(PVOID p, SSIZE_T offset)
{
    STANDARD_VM_CONTRACT;

    const StructureEntry * pEntry = m_pLastLookup;
    if (pEntry == NULL || pEntry->ptr != p)
    {
        pEntry = m_structures.LookupPtr(p);
        _ASSERTE(pEntry != NULL &&
            "StoreStructure or BindPointer have to be called on all save data.");
        m_pLastLookup = pEntry;
    }
    offset += pEntry->offset;
    _ASSERTE(0 <= offset && (DWORD)offset < pEntry->pNode->GetSize());

    return (BYTE *)AsStoredStructure(pEntry->pNode)->GetData() + offset;
}

ZapNode * DataImage::GetNodeForStructure(PVOID p, SSIZE_T * pOffset)
{
    const StructureEntry * pEntry = m_pLastLookup;
    if (pEntry == NULL || pEntry->ptr != p)
    {
        pEntry = m_structures.LookupPtr(p);
        _ASSERTE(pEntry != NULL &&
            "StoreStructure or BindPointer have to be called on all save data.");
    }
    *pOffset = pEntry->offset;
    return pEntry->pNode;
}

ZapStoredStructure * DataImage::StoreInternedStructure(const void *data, ULONG size,
                       DataImage::ItemKind kind,
                       int align)
{
    STANDARD_VM_CONTRACT;

    ZapStoredStructure * pStructure = m_pInternedStructures->Lookup(InternedStructureKey(data, size, kind));

    if (pStructure != NULL)
    {
        // Just add a new mapping for to the interned structure
        BindPointer(data, pStructure, 0);

        // Track that this structure has been successfully reused by interning
        NoteReusedStructure(data);
    }
    else
    {
        // We have not seen this structure yet. Create a new one.
        pStructure = StoreStructure(data, size, kind);
        m_pInternedStructures->Add(pStructure);
    }

    return pStructure;
}

void DataImage::NoteReusedStructure(const void *data)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(IsStored(data));

    const StructureEntry * pEntry = m_structures.LookupPtr(data);

    if (!m_reusedStructures.Contains(pEntry->pNode))
    {
        m_reusedStructures.Add(pEntry->pNode);
    }
}

// Save the info of an RVA into m_rvaInfoVector.
void DataImage::StoreRvaInfo(FieldDesc * pFD,
                             DWORD      rva,
                             UINT       size,
                             UINT       align)
{
    RvaInfoStructure rvaInfo;

    _ASSERTE(m_module == pFD->GetModule());
    _ASSERTE(m_module == pFD->GetLoaderModule());

    rvaInfo.pFD = pFD;
    rvaInfo.rva = rva;
    rvaInfo.size = size;
    rvaInfo.align = align;

    m_rvaInfoVector.Append(rvaInfo);
}

// qsort compare function.
// Primary key: rva (ascending order). Secondary key: size (descending order).
int __cdecl DataImage::rvaInfoVectorEntryCmp(const void* a_, const void* b_)
{
    LIMITED_METHOD_CONTRACT;
    DataImage::RvaInfoStructure *a = (DataImage::RvaInfoStructure *)a_;
    DataImage::RvaInfoStructure *b = (DataImage::RvaInfoStructure *)b_;
    int rvaComparisonResult = (int)(a->rva - b->rva);
    if (rvaComparisonResult!=0)
        return rvaComparisonResult;        // Ascending order on rva
    return (int)(b->size - a->size); // Descending order on size
}

// Sort the list of RVA statics in an ascending order wrt the RVA and save them.
// For RVA structures with the same RVA, we will only store the one with the largest size.
void DataImage::SaveRvaStructure()
{
    if (m_rvaInfoVector.IsEmpty())
        return;  // No RVA static to save

    // Use qsort to sort the m_rvaInfoVector
    qsort (&m_rvaInfoVector[0],               // start of array
           m_rvaInfoVector.GetCount(),        // array size in elements
           sizeof(RvaInfoStructure),        // element size in bytes
           rvaInfoVectorEntryCmp);          // comparere function

    RvaInfoStructure * previousRvaInfo = NULL;

    for (COUNT_T i=0; i<m_rvaInfoVector.GetCount(); i++) {

        RvaInfoStructure * rvaInfo = &(m_rvaInfoVector[i]);

        // Verify that rvaInfo->rva are actually monotonically increasing and
        // rvaInfo->size are monotonically decreasing if rva are the same.
        _ASSERTE(previousRvaInfo==NULL ||
                 previousRvaInfo->rva < rvaInfo->rva ||
                 ((previousRvaInfo->rva == rvaInfo->rva) && (previousRvaInfo->size >= rvaInfo->size))
                );

        if (previousRvaInfo==NULL || previousRvaInfo->rva != rvaInfo->rva) {
            void * pRVAData = rvaInfo->pFD->GetStaticAddressHandle(NULL);

            // Note that we force the structures to be laid out in the order we save them
            StoreStructureInOrder(pRVAData, rvaInfo->size,
                           DataImage::ITEM_RVA_STATICS,
                           rvaInfo->align);
        }

        previousRvaInfo = rvaInfo;
    }
}

void DataImage::RegisterSurrogate(PVOID ptr, PVOID surrogate)
{
    STANDARD_VM_CONTRACT;

    m_surrogates.Add(ptr, surrogate);
}

PVOID DataImage::LookupSurrogate(PVOID ptr)
{
    STANDARD_VM_CONTRACT;

    const KeyValuePair<PVOID, PVOID> * pEntry = m_surrogates.LookupPtr(ptr);
    if (pEntry == NULL)
        return NULL;
    return pEntry->Value();
}

// Please read comments in corcompile.h for ZapVirtualSectionType before
// putting data items into sections.
FORCEINLINE static CorCompileSection GetSectionForNodeType(ZapNodeType type)
{
    LIMITED_METHOD_CONTRACT;

    switch ((int)type)
    {
    // SECTION_MODULE
    case NodeTypeForItemKind(DataImage::ITEM_MODULE):
        return CORCOMPILE_SECTION_MODULE;

    // CORCOMPILE_SECTION_WRITE       (Hot Writeable)
    // things only go in here if they are:
    //    (a) explicitly identified by profiling data
    // or (b) if we have no profiling for these items but they are frequently written to
    case NodeTypeForItemKind(DataImage::ITEM_FILEREF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_ASSEMREF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_DYNAMIC_STATICS_INFO_TABLE):
    case NodeTypeForItemKind(DataImage::ITEM_DYNAMIC_STATICS_INFO_ENTRY):
    case NodeTypeForItemKind(DataImage::ITEM_CER_RESTORE_FLAGS):
        return CORCOMPILE_SECTION_WRITE;

    // CORCOMPILE_SECTION_WRITEABLE   (Cold Writeable)
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_TABLE_SPECIAL_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_TABLE_DATA_COLD_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_DICTIONARY_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_FROZEN_OBJECTS): // sometimes the objhdr is modified
        return CORCOMPILE_SECTION_WRITEABLE;

    // SECTION_HOT
    // Other things go in here if
    //   (a) identified as reads by the profiling runs
    //   (b) if we have no profiling for these items but are identified as typically being read
    case NodeTypeForItemKind(DataImage::ITEM_CER_ROOT_TABLE):
    case NodeTypeForItemKind(DataImage::ITEM_RID_MAP_HOT):
    case NodeTypeForItemKind(DataImage::ITEM_BINDER):
    case NodeTypeForItemKind(DataImage::ITEM_MODULE_SECDESC):
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_DESC_HOT):
        return CORCOMPILE_SECTION_HOT;

    case NodeTypeForItemKind(DataImage::ITEM_BINDER_ITEMS):         // these are the guaranteed to be hot items
        return CORCOMPILE_SECTION_READONLY_SHARED_HOT;

    // SECTION_READONLY_HOT
    case NodeTypeForItemKind(DataImage::ITEM_GC_STATIC_HANDLES_HOT): // this is assumed to be hot.  it is not written to.
    case NodeTypeForItemKind(DataImage::ITEM_MODULE_CCTOR_INFO_HOT):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_BUCKETLIST_HOT):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_ENTRIES_RO_HOT):
        return CORCOMPILE_SECTION_READONLY_HOT;

    // SECTION_HOT_WRITEABLE
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_DESC_HOT_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_TABLE_DATA_HOT_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_HOT):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_ENTRIES_HOT):
        return CORCOMPILE_SECTION_HOT_WRITEABLE;

    case NodeTypeForItemKind(DataImage::ITEM_METHOD_PRECODE_HOT_WRITEABLE):
        return CORCOMPILE_SECTION_METHOD_PRECODE_WRITE;

    case NodeTypeForItemKind(DataImage::ITEM_METHOD_PRECODE_HOT):
        return CORCOMPILE_SECTION_METHOD_PRECODE_HOT;

    // SECTION_RVA_STATICS
    case NodeTypeForItemKind(DataImage::ITEM_RVA_STATICS):
        return CORCOMPILE_SECTION_RVA_STATICS_COLD; // This MUST go in this section

    // SECTION_WARM
    case NodeTypeForItemKind(DataImage::ITEM_GUID_INFO):
    case NodeTypeForItemKind(DataImage::ITEM_DICTIONARY_LAYOUT):
    case NodeTypeForItemKind(DataImage::ITEM_EECLASS_WARM):
        return CORCOMPILE_SECTION_WARM;

    // SECTION_READONLY_WARM
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_TABLE):
    case NodeTypeForItemKind(DataImage::ITEM_INTERFACE_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_DISPATCH_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_GENERICS_STATIC_FIELDDESCS):
    case NodeTypeForItemKind(DataImage::ITEM_GC_STATIC_HANDLES_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_MODULE_CCTOR_INFO_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_STORED_METHOD_NAME):
    case NodeTypeForItemKind(DataImage::ITEM_PROPERTY_NAME_SET):
    case NodeTypeForItemKind(DataImage::ITEM_STORED_METHOD_SIG_READONLY_WARM):
        return CORCOMPILE_SECTION_READONLY_WARM;

    case NodeTypeForItemKind(DataImage::ITEM_DICTIONARY):
        return CORCOMPILE_SECTION_READONLY_DICTIONARY;

    case NodeTypeForItemKind(DataImage::ITEM_VTABLE_CHUNK):
        return CORCOMPILE_SECTION_READONLY_VCHUNKS;

    // SECTION_CLASS_COLD
    case NodeTypeForItemKind(DataImage::ITEM_PARAM_TYPEDESC):
    case NodeTypeForItemKind(DataImage::ITEM_ARRAY_TYPEDESC):
    case NodeTypeForItemKind(DataImage::ITEM_EECLASS):
    case NodeTypeForItemKind(DataImage::ITEM_FPTR_TYPEDESC):
#ifdef FEATURE_COMINTEROP
    case NodeTypeForItemKind(DataImage::ITEM_SPARSE_VTABLE_MAP_TABLE):
#endif // FEATURE_COMINTEROP
        return CORCOMPILE_SECTION_CLASS_COLD;

    //SECTION_READONLY_COLD
    case NodeTypeForItemKind(DataImage::ITEM_FIELD_DESC_LIST):
    case NodeTypeForItemKind(DataImage::ITEM_ENUM_VALUES):
    case NodeTypeForItemKind(DataImage::ITEM_ENUM_NAME_POINTERS):
    case NodeTypeForItemKind(DataImage::ITEM_ENUM_NAME):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_BUCKETLIST_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_ENTRIES_RO_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_STORED_METHOD_SIG_READONLY):
#ifdef FEATURE_COMINTEROP
    case NodeTypeForItemKind(DataImage::ITEM_SPARSE_VTABLE_MAP_ENTRIES):
#endif // FEATURE_COMINTEROP
    case NodeTypeForItemKind(DataImage::ITEM_CLASS_VARIANCE_INFO):
        return CORCOMPILE_SECTION_READONLY_COLD;

    // SECTION_CROSS_DOMAIN_INFO
    case NodeTypeForItemKind(DataImage::ITEM_CROSS_DOMAIN_INFO):
    case NodeTypeForItemKind(DataImage::ITEM_VTS_INFO):
        return CORCOMPILE_SECTION_CROSS_DOMAIN_INFO;

    // SECTION_METHOD_DESC_COLD
    case NodeTypeForItemKind(DataImage::ITEM_METHOD_DESC_COLD):
        return CORCOMPILE_SECTION_METHOD_DESC_COLD;

    case NodeTypeForItemKind(DataImage::ITEM_METHOD_DESC_COLD_WRITEABLE):
    case NodeTypeForItemKind(DataImage::ITEM_STORED_METHOD_SIG):
        return CORCOMPILE_SECTION_METHOD_DESC_COLD_WRITEABLE;

    case NodeTypeForItemKind(DataImage::ITEM_METHOD_PRECODE_COLD):
        return CORCOMPILE_SECTION_METHOD_PRECODE_COLD;

    case NodeTypeForItemKind(DataImage::ITEM_METHOD_PRECODE_COLD_WRITEABLE):
        return CORCOMPILE_SECTION_METHOD_PRECODE_COLD_WRITEABLE;

    // SECTION_MODULE_COLD
    case NodeTypeForItemKind(DataImage::ITEM_TYPEDEF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_TYPEREF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_METHODDEF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_FIELDDEF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_MEMBERREF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_GENERICPARAM_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_GENERICTYPEDEF_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_PROPERTYINFO_MAP):
    case NodeTypeForItemKind(DataImage::ITEM_TYVAR_TYPEDESC):
    case NodeTypeForItemKind(DataImage::ITEM_EECLASS_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_CER_METHOD_LIST):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_COLD):
    case NodeTypeForItemKind(DataImage::ITEM_NGEN_HASH_ENTRIES_COLD):
        return CORCOMPILE_SECTION_MODULE_COLD;

    // SECTION_DEBUG_COLD
    case NodeTypeForItemKind(DataImage::ITEM_DEBUG):
    case NodeTypeForItemKind(DataImage::ITEM_INLINING_DATA):
        return CORCOMPILE_SECTION_DEBUG_COLD;

    // SECTION_COMPRESSED_MAPS
    case NodeTypeForItemKind(DataImage::ITEM_COMPRESSED_MAP):
        return CORCOMPILE_SECTION_COMPRESSED_MAPS;

    default:
        _ASSERTE(!"Missing mapping between type and section");
        return CORCOMPILE_SECTION_MODULE_COLD;
    }
}

static int __cdecl LayoutOrderCmp(const void* a_, const void* b_)
{
    DWORD a = ((DataImage::SavedNodeEntry*)a_)->dwAssociatedOrder;
    DWORD b = ((DataImage::SavedNodeEntry*)b_)->dwAssociatedOrder;

    if (a > b)
    {
        return 1;
    }
    else
    {
        return (a < b) ? -1 : 0;
    }
}

void DataImage::PlaceRemainingStructures()
{
    if (m_pZapImage->HasClassLayoutOrder())
    {
        // The structures are currently in save order; since we are going to change
        // that to class layout order, first place any that require us to maintain save order.
        // Note that this is necessary because qsort is not stable.
        for (COUNT_T iStructure = 0; iStructure < m_structuresInOrder.GetCount(); iStructure++)
        {
            if (m_structuresInOrder[iStructure].dwAssociatedOrder == MAINTAIN_SAVE_ORDER)
            {
                ZapNode * pStructure = m_structuresInOrder[iStructure].pNode;
                if (!pStructure->IsPlaced())
                {
                    ZapVirtualSection * pSection = m_pZapImage->GetSection(GetSectionForNodeType(pStructure->GetType()));
                    pSection->Place(pStructure);
                }
            }
        }

        qsort(&m_structuresInOrder[0], m_structuresInOrder.GetCount(), sizeof(SavedNodeEntry), LayoutOrderCmp);
    }

    // Place the unplaced structures, which may have been re-sorted according to class-layout order
    for (COUNT_T iStructure = 0; iStructure < m_structuresInOrder.GetCount(); iStructure++)
    {
        ZapNode * pStructure = m_structuresInOrder[iStructure].pNode;
        if (!pStructure->IsPlaced())
        {
            ZapVirtualSection * pSection = m_pZapImage->GetSection(GetSectionForNodeType(pStructure->GetType()));
            pSection->Place(pStructure);
        }
    }
}

int __cdecl DataImage::fixupEntryCmp(const void* a_, const void* b_)
{
    LIMITED_METHOD_CONTRACT;
    FixupEntry *a = (FixupEntry *)a_;
    FixupEntry *b = (FixupEntry *)b_;
    return (a->m_pLocation->GetRVA() + a->m_offset) - (b->m_pLocation->GetRVA() + b->m_offset);
}

void DataImage::FixupRVAs()
{
    STANDARD_VM_CONTRACT;

    FixupModuleRVAs();
    FixupRvaStructure();


    // Dev11 bug 181494 instrumentation
    if (m_Fixups.GetCount() != m_iCurrentFixup) EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);

    qsort(&m_Fixups[0], m_Fixups.GetCount(), sizeof(FixupEntry), fixupEntryCmp);

    // Sentinel
    FixupEntry entry;

    entry.m_type = 0;
    entry.m_offset = 0;
    entry.m_pLocation = NULL;
    entry.m_pTargetNode = NULL;

    m_Fixups.Append(entry);

    // Dev11 bug 181494 instrumentation
    if (m_Fixups.GetCount() -1 != m_iCurrentFixup) EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);

    m_iCurrentFixup = 0;
}

void DataImage::SetRVAsForFields(IMetaDataEmit * pEmit)
{
    for (COUNT_T i=0; i<m_rvaInfoVector.GetCount(); i++) {

        RvaInfoStructure * rvaInfo = &(m_rvaInfoVector[i]);

        void * pRVAData = rvaInfo->pFD->GetStaticAddressHandle(NULL);

        DWORD dwOffset = GetRVA(pRVAData);

        pEmit->SetRVA(rvaInfo->pFD->GetMemberDef(), dwOffset);
    }
}

void ZapStoredStructure::Save(ZapWriter * pWriter)
{
    DataImage * image = ZapImage::GetImage(pWriter)->m_pDataImage;

    DataImage::FixupEntry * pPrevFixupEntry = NULL;

    for (;;)
    {
        DataImage::FixupEntry * pFixupEntry = &(image->m_Fixups[image->m_iCurrentFixup]);

        if (pFixupEntry->m_pLocation != this)
        {
            _ASSERTE(pFixupEntry->m_pLocation == NULL ||
                GetRVA() + GetSize() <= pFixupEntry->m_pLocation->GetRVA());
            break;
        }

        PVOID pLocation = (BYTE *)GetData() + pFixupEntry->m_offset;

        if (pPrevFixupEntry == NULL || pPrevFixupEntry->m_offset != pFixupEntry->m_offset)
        {
            SSIZE_T targetOffset = DecodeTargetOffset(pLocation, pFixupEntry->m_type);

#ifdef _DEBUG
            // All pointers in EE datastructures should be aligned. This is important to
            // avoid stradling relocations that cause issues with ASLR.
            if (pFixupEntry->m_type == IMAGE_REL_BASED_PTR)
            {
                _ASSERTE(IS_ALIGNED(pWriter->GetCurrentRVA() + pFixupEntry->m_offset, sizeof(TADDR)));
            }
#endif

            ZapImage::GetImage(pWriter)->WriteReloc(
                GetData(),
                pFixupEntry->m_offset,
                pFixupEntry->m_pTargetNode,
                (int)targetOffset,
                pFixupEntry->m_type);
        }
        else
        {
            // It's fine to have duplicate fixup entries, but they must target the same data.
            // If this assert fires, Fixup* was called twice on the same field in an NGen'd
            // structure with different targets, which likely indicates the current structure
            // was illegally interned or shared.
            _ASSERTE(pPrevFixupEntry->m_type == pFixupEntry->m_type);
            _ASSERTE(pPrevFixupEntry->m_pTargetNode== pFixupEntry->m_pTargetNode);
        }

        pPrevFixupEntry = pFixupEntry;
        image->m_iCurrentFixup++;
    }

    pWriter->Write(GetData(), m_dwSize);
}

void DataImage::FixupSectionRange(SIZE_T offset, ZapNode * pNode)
{
    STANDARD_VM_CONTRACT;

    if (pNode->GetSize() != 0)
    {
        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offset, pNode);

        SIZE_T * pSize = (SIZE_T *)((BYTE *)GetImagePointer(m_module->m_pNGenLayoutInfo) + offset + sizeof(TADDR));
        *pSize = pNode->GetSize();
    }
}

void DataImage::FixupSectionPtr(SIZE_T offset, ZapNode * pNode)
{
    if (pNode->GetSize() != 0)
        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offset, pNode);
}

void DataImage::FixupJumpStubPtr(SIZE_T offset, CorInfoHelpFunc ftnNum)
{
    ZapNode * pNode = m_pZapImage->GetHelperThunkIfExists(ftnNum);
    if (pNode != NULL)
        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offset, pNode);
}

void DataImage::FixupModuleRVAs()
{
    STANDARD_VM_CONTRACT;

    FixupSectionRange(offsetof(NGenLayoutInfo, m_CodeSections[0]), m_pZapImage->m_pHotCodeSection);
    FixupSectionRange(offsetof(NGenLayoutInfo, m_CodeSections[1]), m_pZapImage->m_pCodeSection);
    FixupSectionRange(offsetof(NGenLayoutInfo, m_CodeSections[2]), m_pZapImage->m_pColdCodeSection);

    NGenLayoutInfo * pSavedNGenLayoutInfo = (NGenLayoutInfo *)GetImagePointer(m_module->m_pNGenLayoutInfo);

    COUNT_T nHotRuntimeFunctions = m_pZapImage->m_pHotRuntimeFunctionSection->GetNodeCount();
    if (nHotRuntimeFunctions != 0)
    {
        pSavedNGenLayoutInfo->m_nRuntimeFunctions[0] = nHotRuntimeFunctions;

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_UnwindInfoLookupTable[0]), m_pZapImage->m_pHotRuntimeFunctionLookupSection);
        pSavedNGenLayoutInfo->m_UnwindInfoLookupTableEntryCount[0] = m_pZapImage->m_pHotRuntimeFunctionLookupSection->GetSize() / sizeof(DWORD) - 1;

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_MethodDescs[0]), m_pZapImage->m_pHotCodeMethodDescsSection);

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_pRuntimeFunctions[0]), m_pZapImage->m_pHotRuntimeFunctionSection);
    }

    COUNT_T nRuntimeFunctions = m_pZapImage->m_pRuntimeFunctionSection->GetNodeCount();
    if (nRuntimeFunctions != 0)
    {
        pSavedNGenLayoutInfo->m_nRuntimeFunctions[1] = nRuntimeFunctions;

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_UnwindInfoLookupTable[1]), m_pZapImage->m_pRuntimeFunctionLookupSection);
        pSavedNGenLayoutInfo->m_UnwindInfoLookupTableEntryCount[1] = m_pZapImage->m_pRuntimeFunctionLookupSection->GetSize() / sizeof(DWORD) - 1;

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_MethodDescs[1]), m_pZapImage->m_pCodeMethodDescsSection);

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_pRuntimeFunctions[1]), m_pZapImage->m_pRuntimeFunctionSection);
    }

    COUNT_T nColdRuntimeFunctions = m_pZapImage->m_pColdRuntimeFunctionSection->GetNodeCount();
    if (nColdRuntimeFunctions != 0)
    {
        pSavedNGenLayoutInfo->m_nRuntimeFunctions[2] = nColdRuntimeFunctions;

        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_pRuntimeFunctions[2]), m_pZapImage->m_pColdRuntimeFunctionSection);
    }

    if (m_pZapImage->m_pColdCodeMapSection->GetNodeCount() != 0)
    {
        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_ColdCodeMap), m_pZapImage->m_pColdCodeMapSection);
    }

    FixupSectionRange(offsetof(NGenLayoutInfo, m_Precodes[0]), m_pZapImage->GetSection(CORCOMPILE_SECTION_METHOD_PRECODE_HOT));
    FixupSectionRange(offsetof(NGenLayoutInfo, m_Precodes[1]), m_pZapImage->GetSection(CORCOMPILE_SECTION_METHOD_PRECODE_COLD));
    FixupSectionRange(offsetof(NGenLayoutInfo, m_Precodes[2]), m_pZapImage->GetSection(CORCOMPILE_SECTION_METHOD_PRECODE_WRITE));
    FixupSectionRange(offsetof(NGenLayoutInfo, m_Precodes[3]), m_pZapImage->GetSection(CORCOMPILE_SECTION_METHOD_PRECODE_COLD_WRITEABLE));

    FixupSectionRange(offsetof(NGenLayoutInfo, m_JumpStubs), m_pZapImage->m_pHelperTableSection);
    FixupSectionRange(offsetof(NGenLayoutInfo, m_StubLinkStubs), m_pZapImage->m_pStubsSection);
    FixupSectionRange(offsetof(NGenLayoutInfo, m_VirtualMethodThunks), m_pZapImage->m_pVirtualImportThunkSection);
    FixupSectionRange(offsetof(NGenLayoutInfo, m_ExternalMethodThunks), m_pZapImage->m_pExternalMethodThunkSection);

    if (m_pZapImage->m_pExceptionInfoLookupTable->GetSize() != 0)
        FixupSectionRange(offsetof(NGenLayoutInfo, m_ExceptionInfoLookupTable), m_pZapImage->m_pExceptionInfoLookupTable);

    FixupJumpStubPtr(offsetof(NGenLayoutInfo, m_pPrestubJumpStub), CORINFO_HELP_EE_PRESTUB);
#ifdef HAS_FIXUP_PRECODE
    FixupJumpStubPtr(offsetof(NGenLayoutInfo, m_pPrecodeFixupJumpStub), CORINFO_HELP_EE_PRECODE_FIXUP);
#endif
    FixupJumpStubPtr(offsetof(NGenLayoutInfo, m_pVirtualImportFixupJumpStub), CORINFO_HELP_EE_VTABLE_FIXUP);
    FixupJumpStubPtr(offsetof(NGenLayoutInfo, m_pExternalMethodFixupJumpStub), CORINFO_HELP_EE_EXTERNAL_FIXUP);

    ZapNode * pFilterPersonalityRoutine = m_pZapImage->GetHelperThunkIfExists(CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET);
    if (pFilterPersonalityRoutine != NULL)
        FixupFieldToNode(m_module->m_pNGenLayoutInfo, offsetof(NGenLayoutInfo, m_rvaFilterPersonalityRoutine), pFilterPersonalityRoutine, 0, IMAGE_REL_BASED_ABSOLUTE);
}

void DataImage::FixupRvaStructure()
{
    STANDARD_VM_CONTRACT;

    for (COUNT_T i=0; i<m_rvaInfoVector.GetCount(); i++) {

        RvaInfoStructure * rvaInfo = &(m_rvaInfoVector[i]);

        void * pRVAData = rvaInfo->pFD->GetStaticAddressHandle(NULL);

        DWORD dwOffset = GetRVA(pRVAData);

        FieldDesc * pNewFD = (FieldDesc *)GetImagePointer(rvaInfo->pFD);
        pNewFD->SetOffset(dwOffset);
    }
}

ZapNode * DataImage::GetCodeAddress(MethodDesc * method)
{
    ZapMethodHeader * pMethod = m_pZapImage->GetCompiledMethod((CORINFO_METHOD_HANDLE)method);
    return (pMethod != NULL) ? pMethod->GetCode() : NULL;
}

BOOL DataImage::CanDirectCall(MethodDesc * method, CORINFO_ACCESS_FLAGS  accessFlags)
{
    return m_pZapImage->canIntraModuleDirectCall(NULL, (CORINFO_METHOD_HANDLE)method, NULL, accessFlags);
}

ZapNode * DataImage::GetFixupList(MethodDesc * method)
{
    ZapMethodHeader * pMethod = m_pZapImage->GetCompiledMethod((CORINFO_METHOD_HANDLE)method);
    return (pMethod != NULL) ? pMethod->GetFixupList() : NULL;
}

ZapNode * DataImage::GetHelperThunk(CorInfoHelpFunc ftnNum)
{
    return m_pZapImage->GetHelperThunk(ftnNum);
}

ZapNode * DataImage::GetTypeHandleImport(TypeHandle th, PVOID pUniqueId)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetClassHandleImport(CORINFO_CLASS_HANDLE(th.AsPtr()), pUniqueId);
    if (!pImport->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceImport(pImport);
    return pImport;
}

ZapNode * DataImage::GetMethodHandleImport(MethodDesc * pMD)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetMethodHandleImport(CORINFO_METHOD_HANDLE(pMD));
    if (!pImport->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceImport(pImport);
    return pImport;
}

ZapNode * DataImage::GetFieldHandleImport(FieldDesc * pMD)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetFieldHandleImport(CORINFO_FIELD_HANDLE(pMD));
    if (!pImport->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceImport(pImport);
    return pImport;
}

ZapNode * DataImage::GetModuleHandleImport(Module * pModule)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetModuleHandleImport(CORINFO_MODULE_HANDLE(pModule));
    if (!pImport->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceImport(pImport);
    return pImport;
}

DWORD DataImage::GetModuleImportIndex(Module * pModule)
{
    return m_pZapImage->GetImportTable()->GetIndexOfModule((CORINFO_MODULE_HANDLE)pModule);
}

ZapNode * DataImage::GetExistingTypeHandleImport(TypeHandle th)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetExistingClassHandleImport(CORINFO_CLASS_HANDLE(th.AsPtr()));
    return (pImport != NULL && pImport->IsPlaced()) ? pImport : NULL;
}

ZapNode * DataImage::GetExistingMethodHandleImport(MethodDesc * pMD)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetExistingMethodHandleImport(CORINFO_METHOD_HANDLE(pMD));
    return (pImport != NULL && pImport->IsPlaced()) ? pImport : NULL;
}

ZapNode * DataImage::GetExistingFieldHandleImport(FieldDesc * pFD)
{
    ZapImport * pImport = m_pZapImage->GetImportTable()->GetExistingFieldHandleImport(CORINFO_FIELD_HANDLE(pFD));
    return (pImport != NULL && pImport->IsPlaced()) ? pImport : NULL;
}

ZapNode * DataImage::GetVirtualImportThunk(MethodTable * pMT, MethodDesc * pMD, int slotNumber)
{
    _ASSERTE(pMD == pMT->GetMethodDescForSlot(slotNumber));
    _ASSERTE(!pMD->IsGenericMethodDefinition());

    ZapImport * pImport = m_pZapImage->GetImportTable()->GetVirtualImportThunk(CORINFO_METHOD_HANDLE(pMD), slotNumber);
    if (!pImport->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceVirtualImportThunk(pImport);
    return pImport;
}

ZapNode * DataImage::GetGenericSignature(PVOID signature, BOOL fMethod)
{
    ZapGenericSignature * pGenericSignature = m_pZapImage->GetImportTable()->GetGenericSignature(signature, fMethod);
    if (!pGenericSignature->IsPlaced())
        m_pZapImage->GetImportTable()->PlaceBlob(pGenericSignature);
    return pGenericSignature;
}

#if defined(TARGET_X86) || defined(TARGET_AMD64)

class ZapStubPrecode : public ZapNode
{
protected:
    MethodDesc * m_pMD;
    DataImage::ItemKind m_kind;

public:
    ZapStubPrecode(MethodDesc * pMethod, DataImage::ItemKind kind)
        : m_pMD(pMethod), m_kind(kind)
    {
    }

    virtual DWORD GetSize()
    {
        return sizeof(StubPrecode);
    }

    virtual UINT GetAlignment()
    {
        return PRECODE_ALIGNMENT;
    }

    virtual ZapNodeType GetType()
    {
        return NodeTypeForItemKind(m_kind);
    }

    virtual DWORD ComputeRVA(ZapWriter * pZapWriter, DWORD dwPos)
    {
        dwPos = AlignUp(dwPos, GetAlignment());

        // Alignment for straddlers. Need a cast to help gcc choose between AlignmentTrim(UINT,UINT) and (UINT64,UINT).
        if (AlignmentTrim(static_cast<UINT>(dwPos + offsetof(StubPrecode, m_pMethodDesc)), RELOCATION_PAGE_SIZE) > RELOCATION_PAGE_SIZE - sizeof(TADDR))
            dwPos += GetAlignment();

        SetRVA(dwPos);

        dwPos += GetSize();

        return dwPos;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        StubPrecode precode;

        precode.Init(&precode, m_pMD);

        SSIZE_T offset;
        ZapNode * pNode = pImage->m_pDataImage->GetNodeForStructure(m_pMD, &offset);
        pImage->WriteReloc(&precode, offsetof(StubPrecode, m_pMethodDesc),
            pNode, (int)offset, IMAGE_REL_BASED_PTR);

        pImage->WriteReloc(&precode, offsetof(StubPrecode, m_rel32),
            pImage->GetHelperThunk(CORINFO_HELP_EE_PRESTUB), 0, IMAGE_REL_BASED_REL32);

        pZapWriter->Write(&precode, sizeof(precode));
    }
};

#ifdef HAS_NDIRECT_IMPORT_PRECODE
class ZapNDirectImportPrecode : public ZapStubPrecode
{
public:
    ZapNDirectImportPrecode(MethodDesc * pMD, DataImage::ItemKind kind)
        : ZapStubPrecode(pMD, kind)
    {
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage * pImage = ZapImage::GetImage(pZapWriter);

        StubPrecode precode;

        precode.Init(&precode, m_pMD);

        SSIZE_T offset;
        ZapNode * pNode = pImage->m_pDataImage->GetNodeForStructure(m_pMD, &offset);
        pImage->WriteReloc(&precode, offsetof(StubPrecode, m_pMethodDesc),
            pNode, (int)offset, IMAGE_REL_BASED_PTR);

        pImage->WriteReloc(&precode, offsetof(StubPrecode, m_rel32),
            pImage->GetHelperThunk(CORINFO_HELP_EE_PINVOKE_FIXUP), 0, IMAGE_REL_BASED_REL32);

        pZapWriter->Write(&precode, sizeof(precode));
    }
};
#endif // HAS_NDIRECT_IMPORT_PRECODE

void DataImage::SavePrecode(PVOID ptr, MethodDesc * pMD, PrecodeType t, ItemKind kind, BOOL fIsPrebound)
{
    ZapNode * pNode = NULL;

    switch (t) {
    case PRECODE_STUB:
        pNode = new (GetHeap()) ZapStubPrecode(pMD, kind);
        GetHelperThunk(CORINFO_HELP_EE_PRESTUB);
        break;

#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        pNode = new (GetHeap()) ZapNDirectImportPrecode(pMD, kind);
        GetHelperThunk(CORINFO_HELP_EE_PINVOKE_FIXUP);
        break;
#endif // HAS_NDIRECT_IMPORT_PRECODE

    default:
        _ASSERTE(!"Unexpected precode type");
        break;
    }

    BindPointer(ptr, pNode, 0);

    AddStructureInOrder(pNode);
}

#endif // TARGET_X86 || TARGET_AMD64

void DataImage::FixupModulePointer(Module * pModule, PVOID p, SSIZE_T offset, ZapRelocationType type)
{
    STANDARD_VM_CONTRACT;

    if (pModule != NULL)
    {
        if (CanEagerBindToModule(pModule) && CanHardBindToZapModule(pModule))
        {
            FixupField(p, offset, pModule, 0, type);
        }
        else
        {
            ZapNode * pImport = GetModuleHandleImport(pModule);
            FixupFieldToNode(p, offset, pImport, FIXUP_POINTER_INDIRECTION, type);
        }
    }
}

void DataImage::FixupMethodTablePointer(MethodTable * pMT, PVOID p, SSIZE_T offset, ZapRelocationType type)
{
    STANDARD_VM_CONTRACT;

    if (pMT != NULL)
    {
        if (CanEagerBindToMethodTable(pMT) && CanHardBindToZapModule(pMT->GetLoaderModule()))
        {
            FixupField(p, offset, pMT, 0, type);
        }
        else
        {
            ZapNode * pImport = GetTypeHandleImport(pMT);
            FixupFieldToNode(p, offset, pImport, FIXUP_POINTER_INDIRECTION, type);
        }
    }
}

void DataImage::FixupTypeHandlePointer(TypeHandle th, PVOID p, SSIZE_T offset, ZapRelocationType type)
{
    STANDARD_VM_CONTRACT;

    if (!th.IsNull())
    {
        if (th.IsTypeDesc())
        {
            if (CanEagerBindToTypeHandle(th) && CanHardBindToZapModule(th.GetLoaderModule()))
            {
                FixupField(p, offset, th.AsTypeDesc(), 2, type);
            }
            else
            {
                ZapNode * pImport = GetTypeHandleImport(th);
                FixupFieldToNode(p, offset, pImport, FIXUP_POINTER_INDIRECTION, type);
            }
        }
        else
        {
            MethodTable * pMT = th.AsMethodTable();
            FixupMethodTablePointer(pMT, p, offset, type);
        }
    }
}

void DataImage::FixupMethodDescPointer(MethodDesc * pMD, PVOID p, SSIZE_T offset, ZapRelocationType type /*=IMAGE_REL_BASED_PTR*/)
{
    STANDARD_VM_CONTRACT;

    if (pMD != NULL)
    {
        if (CanEagerBindToMethodDesc(pMD) && CanHardBindToZapModule(pMD->GetLoaderModule()))
        {
            FixupField(p, offset, pMD, 0, type);
        }
        else
        {
            ZapNode * pImport = GetMethodHandleImport(pMD);
            FixupFieldToNode(p, offset, pImport, FIXUP_POINTER_INDIRECTION, type);
        }
    }
}

void DataImage::FixupFieldDescPointer(FieldDesc * pFD, PVOID p, SSIZE_T offset, ZapRelocationType type /*=IMAGE_REL_BASED_PTR*/)
{
    STANDARD_VM_CONTRACT;

    if (pFD != NULL)
    {
        if (CanEagerBindToFieldDesc(pFD) && CanHardBindToZapModule(pFD->GetLoaderModule()))
        {
            FixupField(p, offset, pFD, 0, type);
        }
        else
        {
            ZapNode * pImport = GetFieldHandleImport(pFD);
            FixupFieldToNode(p, offset, pImport, FIXUP_POINTER_INDIRECTION, type);
        }
    }
}

void DataImage::FixupMethodTablePointer(PVOID p, FixupPointer<PTR_MethodTable> * ppMT)
{
    FixupMethodTablePointer(ppMT->GetValue(), p, (BYTE *)ppMT - (BYTE *)p, IMAGE_REL_BASED_PTR);
}
void DataImage::FixupTypeHandlePointer(PVOID p, FixupPointer<TypeHandle> * pth)
{
    FixupTypeHandlePointer(pth->GetValue(), p, (BYTE *)pth - (BYTE *)p, IMAGE_REL_BASED_PTR);
}
void DataImage::FixupMethodDescPointer(PVOID p, FixupPointer<PTR_MethodDesc> * ppMD)
{
    FixupMethodDescPointer(ppMD->GetValue(), p, (BYTE *)ppMD - (BYTE *)p, IMAGE_REL_BASED_PTR);
}
void DataImage::FixupFieldDescPointer(PVOID p, FixupPointer<PTR_FieldDesc> * ppFD)
{
    FixupFieldDescPointer(ppFD->GetValue(), p, (BYTE *)ppFD - (BYTE *)p, IMAGE_REL_BASED_PTR);
}

void DataImage::FixupModulePointer(PVOID p, RelativeFixupPointer<PTR_Module> * ppModule)
{
    FixupModulePointer(ppModule->GetValueMaybeNull(), p, (BYTE *)ppModule - (BYTE *)p, IMAGE_REL_BASED_RELPTR);
}
void DataImage::FixupMethodTablePointer(PVOID p, RelativeFixupPointer<PTR_MethodTable> * ppMT)
{
    FixupMethodTablePointer(ppMT->GetValueMaybeNull(), p, (BYTE *)ppMT - (BYTE *)p, IMAGE_REL_BASED_RELPTR);
}
void DataImage::FixupTypeHandlePointer(PVOID p, RelativeFixupPointer<TypeHandle> * pth)
{
    FixupTypeHandlePointer(pth->GetValueMaybeNull(), p, (BYTE *)pth - (BYTE *)p, IMAGE_REL_BASED_RELPTR);
}
void DataImage::FixupMethodDescPointer(PVOID p, RelativeFixupPointer<PTR_MethodDesc> * ppMD)
{
    FixupMethodDescPointer(ppMD->GetValueMaybeNull(), p, (BYTE *)ppMD - (BYTE *)p, IMAGE_REL_BASED_RELPTR);
}
void DataImage::FixupFieldDescPointer(PVOID p, RelativeFixupPointer<PTR_FieldDesc> * ppFD)
{
    FixupFieldDescPointer(ppFD->GetValueMaybeNull(), p, (BYTE *)ppFD - (BYTE *)p, IMAGE_REL_BASED_RELPTR);
}

BOOL DataImage::CanHardBindToZapModule(Module *targetModule)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(targetModule == m_module || targetModule->HasNativeImage());
    return targetModule == m_module;
}

BOOL DataImage::CanEagerBindToTypeHandle(TypeHandle th, BOOL fRequirePrerestore, TypeHandleList *pVisited)
{
    STANDARD_VM_CONTRACT;

    Module * pLoaderModule = th.GetLoaderModule();

    BOOL fCanEagerBind;

    if (th.IsTypeDesc())
    {
        fCanEagerBind = CanEagerBindTo(pLoaderModule, Module::GetPreferredZapModuleForTypeDesc(th.AsTypeDesc()), th.AsTypeDesc());
    }
    else
    {
        fCanEagerBind = CanEagerBindTo(pLoaderModule, Module::GetPreferredZapModuleForMethodTable(th.AsMethodTable()), th.AsMethodTable());
    }

    if (GetModule() != th.GetLoaderModule())
    {
        if (th.IsTypeDesc())
        {
            return FALSE;
        }

        // As a performance optimization, don't eager bind to arrays.  They are currently very expensive to
        // fixup so we want to do it lazily.

        if (th.AsMethodTable()->IsArray())
        {
            return FALSE;
        }

        // For correctness in the face of targeted patching, do not eager bind to any instantiation
        // in the target module that might go away.
        if (!th.IsTypicalTypeDefinition() &&
            !Module::IsAlwaysSavedInPreferredZapModule(th.GetInstantiation(),
                                                       Instantiation()))
        {
            return FALSE;
        }

        // #DoNotEagerBindToTypesThatNeedRestore
        //
        // It is important to avoid eager binding to structures that require restore.  The code here stops
        // this from happening for cross-module fixups.  For intra-module cases, eager fixups are allowed to
        // (and often do) target types that require restore, even though this is generally prone to all of
        // the same problems described below.  Correctness is preserved only because intra-module eager
        // fixups are ignored in Module::RunEagerFixups (so their semantics are very close to normal
        // non-eager fixups).
        //
        // For performance, this is the most costly type of eager fixup (and may require otherwise-unneeded
        // assemblies to be loaded) and has the lowest benefit, since it does not avoid the need for the
        // referencing type to require restore.
        //
        // More importantly, this kind of fixup can compromise correctness by causing type loads to occur
        // during eager fixup resolution.  The system is not designed to cope with this and a variety of
        // subtle failures can occur when it happens.  As an example, consider a scenario involving the
        // following assemblies and types:
        //    o A1: softbinds to A2, contains "class A1!Level2 extends A2!Level1"
        //    o A2: hardbinds to A3, contains "class A2!Level1 extends Object", contains methods that use A3!Level3.
        //    o A3: softbinds to A1, contains "class A3!Level3 extends A1!Level2"
        //
        // If eager fixups are allowed to target types that need restore, then it's possible for A2 to end
        // up with an eager fixup targeting A3!Level3, setting up this sequence:
        //    1 Type load starts for A1!Level2.
        //    2 Loading base class A2!Level1 triggers assembly load for A2.
        //    3 Loading A2 involves synchronously resolving its eager fixups, including the fixup to A3!Level3.
        //    4 A3!Level3 needs restore, so type load starts for A3!Level3.
        //    5 Loading A3!Level3 requires loading base class A1!Level2.
        //    6 A1!Level2 is already being loaded on this thread (in #1 above), so type load fails.
        //    7 Since eager fixup resolution failed, FileLoadException is thrown for A2.
        fRequirePrerestore = TRUE;
    }

    if (fCanEagerBind && fRequirePrerestore)
    {
        fCanEagerBind = !th.ComputeNeedsRestore(this, pVisited);
    }

    return fCanEagerBind;
}

BOOL DataImage::CanEagerBindToMethodTable(MethodTable *pMT, BOOL fRequirePrerestore, TypeHandleList *pVisited)
{
    WRAPPER_NO_CONTRACT;

    TypeHandle th =  TypeHandle(pMT);
    return DataImage::CanEagerBindToTypeHandle(th, fRequirePrerestore, pVisited);
}

BOOL DataImage::CanEagerBindToMethodDesc(MethodDesc *pMD, BOOL fRequirePrerestore, TypeHandleList *pVisited)
{
    STANDARD_VM_CONTRACT;

    BOOL fCanEagerBind = CanEagerBindTo(pMD->GetLoaderModule(), Module::GetPreferredZapModuleForMethodDesc(pMD), pMD);

    // Performance optimization -- see comment in CanEagerBindToTypeHandle
    if (GetModule() != pMD->GetLoaderModule())
    {
        // For correctness in the face of targeted patching, do not eager bind to any instantiation
        // in the target module that might go away.
        if (!pMD->IsTypicalMethodDefinition() &&
            !Module::IsAlwaysSavedInPreferredZapModule(pMD->GetClassInstantiation(),
                                                       pMD->GetMethodInstantiation()))
        {
            return FALSE;
        }

        fRequirePrerestore = TRUE;
    }

    if (fCanEagerBind && fRequirePrerestore)
    {
        fCanEagerBind = !pMD->ComputeNeedsRestore(this, pVisited);
    }

    return fCanEagerBind;
}

BOOL DataImage::CanEagerBindToFieldDesc(FieldDesc *pFD, BOOL fRequirePrerestore, TypeHandleList *pVisited)
{
    STANDARD_VM_CONTRACT;

    if (!CanEagerBindTo(pFD->GetLoaderModule(), Module::GetPreferredZapModuleForFieldDesc(pFD), pFD))
        return FALSE;

    MethodTable * pMT = pFD->GetApproxEnclosingMethodTable();

    return CanEagerBindToMethodTable(pMT, fRequirePrerestore, pVisited);
}

BOOL DataImage::CanEagerBindToModule(Module *pModule)
{
    STANDARD_VM_CONTRACT;

    return GetAppDomain()->ToCompilationDomain()->CanEagerBindToZapFile(pModule);
}

// "address" is a data-structure belonging to pTargetModule.
// This function returns whether the Module currently being ngenned can
// hardbind "address"
/* static */
BOOL DataImage::CanEagerBindTo(Module *pTargetModule, Module *pPreferredZapModule, void *address)
{
    STANDARD_VM_CONTRACT;

    if (pTargetModule != pPreferredZapModule)
        return FALSE;

    if (GetModule() == pTargetModule)
        return TRUE;

    BOOL eagerBindToZap = GetAppDomain()->ToCompilationDomain()->CanEagerBindToZapFile(pTargetModule);
    BOOL isPersisted    = pTargetModule->IsPersistedObject(address);

    return eagerBindToZap && isPersisted;
}

BOOL DataImage::CanPrerestoreEagerBindToTypeHandle(TypeHandle th, TypeHandleList *pVisited)
{
    WRAPPER_NO_CONTRACT;
    return CanEagerBindToTypeHandle(th, TRUE, pVisited);
}

BOOL DataImage::CanPrerestoreEagerBindToMethodTable(MethodTable *pMT, TypeHandleList *pVisited)
{
    WRAPPER_NO_CONTRACT;
    return CanEagerBindToMethodTable(pMT, TRUE, pVisited);
}

BOOL DataImage::CanPrerestoreEagerBindToMethodDesc(MethodDesc *pMD, TypeHandleList *pVisited)
{
    WRAPPER_NO_CONTRACT;
    return CanEagerBindToMethodDesc(pMD, TRUE, pVisited);
}


void DataImage::HardBindTypeHandlePointer(PVOID p, SSIZE_T offset)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CanEagerBindToTypeHandle(*(TypeHandle UNALIGNED*)((BYTE *)p + offset)));
    }
    CONTRACTL_END;

    TypeHandle thCopy = *(TypeHandle UNALIGNED*)((BYTE *)p + offset);

    if (!thCopy.IsNull())
    {
        if (thCopy.IsTypeDesc())
        {
            FixupField(p, offset, thCopy.AsTypeDesc(), 2);
        }
        else
        {
            FixupField(p, offset, thCopy.AsMethodTable());
        }
    }
}


    // This is obsolete in-place fixup that we should get rid of. For now, it is used for:
    // - FnPtrTypeDescs. These should not be stored in NGen images at all.
    // - stubs-as-il signatures. These should use tokens when stored in NGen image.
    //
void DataImage::FixupTypeHandlePointerInPlace(PVOID p, SSIZE_T offset, BOOL fForceFixup /*=FALSE*/)
{
    STANDARD_VM_CONTRACT;

    TypeHandle thCopy = *(TypeHandle UNALIGNED*)((BYTE *)p + offset);

    if (!thCopy.IsNull())
    {
        if (!fForceFixup &&
            CanEagerBindToTypeHandle(thCopy) &&
            CanHardBindToZapModule(thCopy.GetLoaderModule()))
        {
            HardBindTypeHandlePointer(p, offset);
        }
        else
        {
            ZapImport * pImport = m_pZapImage->GetImportTable()->GetClassHandleImport((CORINFO_CLASS_HANDLE)thCopy.AsPtr());

            ZapNode * pBlob = m_pZapImage->GetImportTable()->PlaceImportBlob(pImport);
            FixupFieldToNode(p, offset, pBlob, 0, IMAGE_REL_BASED_ABSOLUTE_TAGGED);
        }
    }
}

void DataImage::BeginRegion(CorInfoRegionKind regionKind)
{
    STANDARD_VM_CONTRACT;

    m_pZapImage->BeginRegion(regionKind);
}

void DataImage::EndRegion(CorInfoRegionKind regionKind)
{
    STANDARD_VM_CONTRACT;

    m_pZapImage->EndRegion(regionKind);
}

void DataImage::ReportInlining(CORINFO_METHOD_HANDLE inliner, CORINFO_METHOD_HANDLE inlinee)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(m_inlineTrackingMap);
    m_inlineTrackingMap->AddInlining(GetMethod(inliner), GetMethod(inlinee));
}

InlineTrackingMap * DataImage::GetInlineTrackingMap()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_inlineTrackingMap;
}

//
// Compressed LookupMap Support
//
// See the large comment near the top of ceeload.h for a much more detailed discussion of this.
//
// Basically we support a specialized node, ZapCompressedLookupMap, which knows how to compress the array of
// intra-module pointers present in certain types of LookupMap.
//

// A simple class to write a sequential sequence of variable sized bit-fields into a pre-allocated buffer. I
// was going to use the version defined by GcInfoEncoder (the reader side in ceeload.cpp uses GcInfoDecoder's
// BitStreamReader) but unfortunately the code is not currently factored to make this easy and the resources
// were not available to perform a non-trivial refactorization of the code. In any event the writer is fairly
// trivial and doesn't represent a huge duplication of effort.
// The class requires that the input buffer is DWORD-aligned and sized (it uses a DWORD cache and always
// writes data to the buffer in DWORD-sized chunks).
class BitStreamWriter
{
public:
    // Initialize a writer and point it at the start of a pre-allocated buffer (large enough to accomodate all
    // future writes). The buffer must be DWORD-aligned (we use this for some performance optimization).
    BitStreamWriter(DWORD *pStart)
    {
        LIMITED_METHOD_CONTRACT;

        // Buffer must be DWORD-aligned.
        _ASSERTE(((TADDR)pStart & 0x3) == 0);

        m_pNext = pStart;   // Point at the start of the buffer
        m_dwCurrent = 0;    // We don't have any cached data waiting to write
        m_cCurrentBits = 0; // Ditto
        m_cBitsWritten = 0; // We haven't written any bits
    }

    // Write the low-order cBits of dwData to the stream.
    void Write(DWORD dwData, DWORD cBits)
    {
        LIMITED_METHOD_CONTRACT;

        // We can only write between 1 and 32 bits of data at a time.
        _ASSERTE(cBits > 0 && cBits <= kBitsPerDWORD);

        // Check that none of the unused high-order bits of dwData have stale data in them (we can use this to
        // optimize paths below). Use two conditions here because << of 32-bits or more (on x86) doesn't
        // do what you might expect (the RHS is modulo 32 so "<< 32" is a no-op rather than zero-ing the
        // result).
        _ASSERTE((cBits == kBitsPerDWORD) || ((dwData & ((1U << cBits) - 1)) == dwData));

        // Record the input bits as written (we can't fail and we have multiple exit paths below so it's
        // convenient to update our counter here).
        m_cBitsWritten += cBits;

        // We cache up to a DWORD of data to be written to the stream and only write back to the buffer when
        // we have a full DWORD. Calculate how many bits of the input we're going to write first (either the
        // rest of the input or the remaining bits of space in the current DWORD cache, whichever is smaller).
        DWORD cInitialBits = min(cBits, kBitsPerDWORD - m_cCurrentBits);
        if (cInitialBits == kBitsPerDWORD)
        {
            // Deal with this special case (we're writing all the input, an entire DWORD all at once) since it
            // ensures that none of the << operations below have to deal with a LHS that == 32 (see the <<
            // comment in one of the asserts above for why this matters).

            // Because of the calculations above we should only come here if our DWORD cache was empty and the
            // caller is trying to write a full DWORD (which simplifies many things).
            _ASSERTE(m_dwCurrent == 0 && m_cCurrentBits == 0 && cBits == kBitsPerDWORD);

            *m_pNext++ = dwData;    // Write a full DWORD directly from the input

            // That's it, there's no more data to write and the only state update to the write was advancing
            // the buffer pointer (cache DWORD is already in the correct state, see asserts above).
            return;
        }

        // Calculate a mask of the low-order bits we're going to extract from the input data.
        DWORD dwInitialMask = (1U << cInitialBits) - 1;

        // OR those bits into the cache (properly shifted to fit above the data already there).
        m_dwCurrent |= (dwData & dwInitialMask) << m_cCurrentBits;

        // Update the cache bit counter for the new data.
        m_cCurrentBits += cInitialBits;
        if (m_cCurrentBits == kBitsPerDWORD)
        {
            // The cache filled up. Write the DWORD to the buffer and reset the cache state to empty.
            *m_pNext++ = m_dwCurrent;
            m_dwCurrent = 0;
            m_cCurrentBits = 0;
        }

        // If the bits we just inserted comprised all the input bits we're done.
        if (cInitialBits == cBits)
            return;

        // There's more data to write. But we can only get here if we just flushed the cache. So there is a
        // whole DWORD free in the cache and we're guaranteed to have less than a DWORD of data left to write.
        // As a result we can simply populate the low-order bits of the cache with our remaining data (simply
        // shift down by the number of bits we've already written) and we're done.
        _ASSERTE(m_dwCurrent == 0 && m_cCurrentBits == 0);
        m_dwCurrent = dwData >>= cInitialBits;
        m_cCurrentBits = cBits - cInitialBits;
    }

    // Because we cache a DWORD of data before writing it it's possible that there are still unwritten bits
    // left in the cache once you've finished writing data. Call this operation after all Writes() are
    // completed to flush any such data to memory. It's not legal to call Write() again after a Flush().
    void Flush()
    {
        LIMITED_METHOD_CONTRACT;

        // Nothing to do if the cache is empty.
        if (m_cCurrentBits == 0)
            return;

        // Write what we have to memory (unused high-order bits will be zero).
        *m_pNext = m_dwCurrent;

        // Catch any attempt to make a further Write() call.
        m_pNext = NULL;
    }

    // Get the count of bits written so far (logically, this number does not take caching into account).
    DWORD GetBitsWritten()
    {
        LIMITED_METHOD_CONTRACT;

        return m_cBitsWritten;
    }

private:
    enum { kBitsPerDWORD = sizeof(DWORD) * 8 };

    DWORD  *m_pNext;        // Pointer to the next DWORD that will be written in the buffer
    DWORD   m_dwCurrent;    // We cache up to a DWORD of data before writing it to the buffer
    DWORD   m_cCurrentBits; // Count of valid (low-order) bits in the buffer above
    DWORD   m_cBitsWritten; // Count of bits given to Write() (ignores caching)
};

// A specialized node used to write the compressed portions of a LookupMap to an ngen image. This is
// (optionally) allocated by a call to DataImage::StoreCompressedLayoutMap from LookupMapBase::Save() and
// handles allocation and initialization of the compressed table and an index used to navigate the table
// efficiently. The allocation of the map itself and any hot item list is still handled externally but this
// node will perform any fixups in the base map required to refer to the new compressed data.
//
// Since the compression algorithm used depends on the precise values of the RVAs referenced by the LookupMap
// the compression doesn't happen until ComputeRVA is called (don't call GetSize() until after ComputeRVA()
// returns). Additionally we must ensure that this node's ComputeRVA() is not called until after that of every
// node on those RVA it depends. Currently this is ensured by placing this node near the end of the .text
// section (after pointers to any read-only data structures referenced by LookupMaps and after the .data
// section containing writeable structures).
class ZapCompressedLookupMap : public ZapNode
{
    DataImage      *m_pImage;                                       // Back pointer to the allocating DataImage
    LookupMapBase  *m_pMap;                                         // Back pointer to the LookupMap we're compressing
    BYTE           *m_pTable;                                       // ComputeRVA allocates a compressed table here
    BYTE           *m_pIndex;                                       // ComputeRVA allocates a table index here
    DWORD           m_cbTable;                                      // Size (in bytes) of the table above (after ComputeRVA)
    DWORD           m_cbIndex;                                      // Size (in bytes) of the index above (after ComputeRVA)
    DWORD           m_cBitsPerIndexEntry;                           // Number of bits in each index entry
    DWORD           m_rgHistogram[kBitsPerRVA];                     // Table of frequencies of different delta lengths
    BYTE            m_rgEncodingLengths[kLookupMapLengthEntries];   // Table of different bit lengths value deltas can take
    BYTE            m_eKind;                                        // Item kind (DataImage::ITEM_COMPRESSED_MAP currently)

public:
    ZapCompressedLookupMap(DataImage *pImage, LookupMapBase *pMap, BYTE eKind)
        : m_pImage(pImage), m_pMap(pMap), m_eKind(eKind)
    {
        LIMITED_METHOD_CONTRACT;
    }

    DataImage::ItemKind GetKind()
    {
        LIMITED_METHOD_CONTRACT;

        return (DataImage::ItemKind)m_eKind;
    }

    virtual DWORD GetSize()
    {
        LIMITED_METHOD_CONTRACT;

        if (!ShouldCompressedMapBeSaved())
            return 0;

        // This isn't legal until ComputeRVA() is called. Check this by seeing if the compressed version of
        // the table is allocated yet.
        _ASSERTE(m_pTable != NULL);
        return m_cbIndex + m_cbTable;
    }

    virtual UINT GetAlignment()
    {
        LIMITED_METHOD_CONTRACT;

        if (!ShouldCompressedMapBeSaved())
            return 1;

        // The table and index have no pointers but do require DWORD alignment.
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        STANDARD_VM_CONTRACT;

        return NodeTypeForItemKind(m_eKind);
    }

    virtual DWORD ComputeRVA(ZapWriter *pZapWriter, DWORD dwPos)
    {
        STANDARD_VM_CONTRACT;

        if (ShouldCompressedMapBeSaved())
        {

            // This is the earliest opportunity at which all data is available in order to compress the table. In
            // particular all values in the table (currently MethodTable* or MethodDesc*) point to structures
            // which have been assigned final RVAs in the image. We can thus compute a compressed table value that
            // relies on the relationship between these RVAs.

            // Phase 1: Look through all the entries in the table. Look at the deltas between RVAs for adjacent
            // items and build a histogram of how many entries require a specific number to encode their delta
            // (using a scheme we we discard non-significant low and high-order zero bits). This call will
            // initialize m_rgHistogram so that entry 0 contains the number of entries that require 1 bit to
            // encode their delta, entry 1 the count of those that require 2 bits etc. up to the last entry (how
            // many entries require the full 32 bits). Note that even on 64-bit platforms we only currently
            // support 32-bit RVAs.
            DWORD cRids = AnalyzeTable();

            // Phase 2: Given the histogram above, calculate the set of delta lengths for the encoding table
            // (m_rgEncodingLengths) that will result in optimal table size. We have a fixed size encoding length
            // so we don't have to embed a large fixed-size length field for every compressed entry but we can
            // still cope with the relatively rare but ever-present worst case entries which require many bits of
            // delta entry.
            OptimizeEncodingLengths();

            // Phase 3: We now have enough data to allocate the final data structures (the compressed table itself
            // and an index that bookmarks every kLookupMapIndexStride'th entry). Both structures must start
            // DWORD-aligned and have a DWORD-aligned size (requirements of BitStreamWriter).

            // PredictCompressedSize() returns its result in bits so we must convert (rounding up) to bytes before
            // DWORD aligning.
            m_cbTable = AlignUp((PredictCompressedSize(m_rgEncodingLengths) + 7) / 8, sizeof(DWORD));

            // Each index entry contains a bit offset into the compressed stream (so we must size for the worst
            // case of an offset at the end of the stream) plus an RVA.
            m_cBitsPerIndexEntry = BitsRequired(m_cbTable * 8) + kBitsPerRVA;
            _ASSERTE(m_cBitsPerIndexEntry > 0);

            // Our first index entry is for entry 0 (rather than entry kLookupMapIndexStride) so we must be
            // sure to round up the number of index entries we need in order to cover the table.
            DWORD cIndexEntries = (cRids + (kLookupMapIndexStride - 1)) / kLookupMapIndexStride;

            // Since we calculate the index size in bits we need to round up to bytes before DWORD aligning.
            m_cbIndex = AlignUp(((m_cBitsPerIndexEntry * cIndexEntries) + 7) / 8, sizeof(DWORD));

            // Allocate both table and index from a single chunk of memory.
            BYTE *pMemory = new BYTE[m_cbIndex + m_cbTable];
            m_pTable = pMemory;
            m_pIndex = pMemory + m_cbTable;

            // Phase 4: We've now calculated all the input data we need and allocated memory for the output so we
            // can go ahead and fill in the compressed table and index.
            InitializeTableAndIndex();

            // Phase 5: Go back up update the saved version of the LookupMap (redirect the table pointer to the
            // compressed table and fill in the other fields which aren't valid until the table is compressed).
            LookupMapBase *pSaveMap = (LookupMapBase*)m_pImage->GetImagePointer(m_pMap);
            pSaveMap->pTable = (TADDR*)m_pTable;
            pSaveMap->pIndex = m_pIndex;
            pSaveMap->cIndexEntryBits = m_cBitsPerIndexEntry;
            pSaveMap->cbTable = m_cbTable;
            pSaveMap->cbIndex = m_cbIndex;
            memcpy(pSaveMap->rgEncodingLengths, m_rgEncodingLengths, sizeof(m_rgEncodingLengths));

            // Schedule fixups for the map pointers to the compressed table and index.
            m_pImage->FixupFieldToNode(m_pMap, offsetof(LookupMapBase, pTable), this, 0);
            m_pImage->FixupFieldToNode(m_pMap, offsetof(LookupMapBase, pIndex), this, m_cbTable);
        }

        // We're done with generating the compressed table. Now we need to do the work ComputeRVA() is meant
        // to do:
        dwPos = AlignUp(dwPos, GetAlignment()); // Satisfy our alignment requirements
        SetRVA(dwPos);                          // Set the RVA of the node (both table and index)
        dwPos += GetSize();                     // Advance the RVA past our node

        return dwPos;
    }

    virtual void Save(ZapWriter *pZapWriter)
    {
        STANDARD_VM_CONTRACT;

        if (!ShouldCompressedMapBeSaved())
            return;

        // Save both the table and index.
        pZapWriter->Write(m_pTable, m_cbTable);
        pZapWriter->Write(m_pIndex, m_cbIndex);
    }

private:

    // It's possible that our node has been created and only later the decision is made to store the full
    // uncompressed table.  In this case, we want to early out of our work and make saving our node a no-op.
    BOOL ShouldCompressedMapBeSaved()
    {
        LIMITED_METHOD_CONTRACT;

        // To identify whether compression is desired, use the flag from LookupMapBase::Save
        return (m_pMap->cIndexEntryBits > 0);
    }

    // Phase 1: Look through all the entries in the table. Look at the deltas between RVAs for adjacent items
    // and build a histogram of how many entries require a specific number to encode their delta (using a
    // scheme we we discard non-significant low and high-order zero bits). This call will initialize
    // m_rgHistogram so that entry 0 contains the number of entries that require 1 bit to encode their delta,
    // entry 1 the count of those that require 2 bits etc. up to the last entry (how many entries require the
    // full 32 bits). Note that even on 64-bit platforms we only currently support 32-bit RVAs.
    DWORD AnalyzeTable()
    {
        STANDARD_VM_CONTRACT;

        LookupMapBase *pMap = m_pMap;
        DWORD dwLastValue = 0;
        DWORD cRids = 0;

        // Initialize the histogram to all zeroes.
        memset(m_rgHistogram, 0, sizeof(m_rgHistogram));

        // Walk each node in the map.
        while (pMap)
        {
            // Walk each entry in this node.
            for (DWORD i = 0; i < pMap->dwCount; i++)
            {
                DWORD dwCurrentValue = ComputeElementRVA(pMap, i);

                // Calculate the delta from the last entry. We split the delta into two-components: a bool
                // indicating whether the RVA was higher or lower and an absolute (non-negative) size. Sort of
                // like a ones-complement signed number.
                bool fIncreasingDelta = dwCurrentValue > dwLastValue;
                DWORD dwDelta = fIncreasingDelta ? (dwCurrentValue - dwLastValue) : (dwLastValue - dwCurrentValue);

                // Determine the minimum number of bits required to represent the delta (by stripping
                // non-significant leading zeros) and update the count in the histogram of the number of
                // deltas that required this many bits. We never encode anything with zero bits (only the
                // value zero would be eligibil and it's not a common value) so the first histogram entry
                // records the number of deltas encodable with one bit and so on.
                m_rgHistogram[BitsRequired(dwDelta) - 1]++;

                dwLastValue = dwCurrentValue;
                cRids++;
            }

            pMap = pMap->pNext;
        }

        return cRids;
    }

    // Phase 2: Given the histogram above, calculate the set of delta lengths for the encoding table
    // (m_rgEncodingLengths) that will result in optimal table size. We have a fixed size encoding length so
    // we don't have to embed a large fixed-size length field for every compressed entry but we can still cope
    // with the relatively rare but ever-present worst case entries which require many bits of delta entry.
    void OptimizeEncodingLengths()
    {
        STANDARD_VM_CONTRACT;

        // Find the longest delta (search from the large end of the histogram down for the first non-zero
        // entry).
        BYTE bMaxBits = 0;
#ifdef _MSC_VER
#pragma warning(suppress:6293) // Prefast doesn't understand the unsigned modulo-8 arithmetic below.
#endif
        for (BYTE i = kBitsPerRVA - 1; i < 0xff; i--)
            if (m_rgHistogram[i] > 0)
            {
                bMaxBits = i + 1;  // +1 because we never encode anything with zero bits.
                break;
            }
        _ASSERTE(bMaxBits >= 1);

        // Now find the smallest delta in a similar fashion.
        BYTE bMinBits = bMaxBits;
        for (BYTE i = 0; i < kBitsPerRVA; i++)
            if (m_rgHistogram[i] > 0)
            {
                bMinBits = i + 1;  // +1 because we never encode anything with zero bits.
                break;
            }
        _ASSERTE(bMinBits <= bMaxBits);

        // The encoding lengths table is a sorted list of bit field lengths we can use to encode any
        // entry-to-entry delta in the compressed table. We go through a table so we can use a small number of
        // bits in the compressed stream (the table index) to express a very flexible range of deltas. The one
        // entry we know in advance is the largest (the last). That's because we know we have to be able to
        // encode the largest delta we found in the table or else we couldn't be functionally correct.
        m_rgEncodingLengths[kLookupMapLengthEntries - 1] = bMaxBits;

        // Now find optimal values for the other entries one by one. It doesn't really matter which order we
        // do them in. For each entry we'll loop through all the possible encoding lengths, dwMinBits <=
        // length < dwMaxBits, setting all the uninitialized entries to the candidate value and calculating
        // the resulting compressed size of the table. We don't enforce that the candidate sizes get smaller
        // for each entry so in that if the best use of an extra table entry is to add a larger length rather
        // than a smaller one then we'll take that. The downside is that we have to sort the table before
        // calculating the table size (the sizing algorithm is only fast for a sorted table). Luckily our
        // table is very small (currently 4 entries) and we don't have to sort one of the entries (the last is
        // always largest) so this isn't such a huge deal.
        for (DWORD i = 0; i < kLookupMapLengthEntries - 1; i++)
        {
            DWORD dwBestSize = 0xffffffff;  // Best overall table size so far
            BYTE bBestLength = bMaxBits; // The candidate value that lead to the above

            // Iterate over all the values that could generate a good result (no point trying values smaller
            // than the smallest delta we have or as large as the maximum table entry we've already fixed).
            for (BYTE j = bMinBits; j < bMaxBits; j++)
            {
                // Build a temporary (unsorted) encoding table.
                BYTE rgTempBuckets[kLookupMapLengthEntries];

                // Entries before the current one are set to the values we've already determined in previous
                // iterations.
                for (DWORD k = 0; k < i; k++)
                    rgTempBuckets[k] = m_rgEncodingLengths[k];

                // The current entry and the remaining uninitialized entries are all set to the current
                // candidate value (this is logically the equivalent of removing the non-current uninitialized
                // entries from the table altogether).
                for (DWORD k = i; k < kLookupMapLengthEntries - 1; k++)
                    rgTempBuckets[k] = j;

                // The last entry is always the maximum bit length.
                rgTempBuckets[kLookupMapLengthEntries - 1] = bMaxBits;

                // Sort the temporary table so that the call to PredictCompressedSize() below behaves
                // correctly (and fast).
                SortLengthBuckets(rgTempBuckets);

                // See what size of table this would generate.
                DWORD dwTestSize = PredictCompressedSize(rgTempBuckets);
                if (dwTestSize < dwBestSize)
                {
                    // The result is better than our current best, remember it.
                    dwBestSize = dwTestSize;
                    bBestLength = j;
                }
            }

            // Set the current entry to the best length we found.
            m_rgEncodingLengths[i] = bBestLength;
        }

        // We've picked optimal values for all entries, but the result is unsorted. Fix that now.
        SortLengthBuckets(m_rgEncodingLengths);
    }

    // Phase 4: We've now calculated all the input data we need and allocated memory for the output so we can
    // go ahead and fill in the compressed table and index.
    void InitializeTableAndIndex()
    {
        STANDARD_VM_CONTRACT;

        // Initialize bit stream writers to the start of the compressed table and index.
        BitStreamWriter sTableStream((DWORD*)m_pTable);
        BitStreamWriter sIndexStream((DWORD*)m_pIndex);

        DWORD dwRid = 0;
        DWORD dwLastValue = 0;
        LookupMapBase *pMap = m_pMap;

        // Walk each node in the map.
        while (pMap)
        {
            // Walk each entry in this node.
            for (DWORD i = 0; i < pMap->dwCount; i++)
            {
                DWORD dwCurrentValue = ComputeElementRVA(pMap, i);

                // Calculate the delta from the last entry. We split the delta into two-components: a bool
                // indicating whether the RVA was higher or lower and an absolute (non-negative) size. Sort of
                // like a ones-complement signed number.
                bool fIncreasingDelta = dwCurrentValue > dwLastValue;
                DWORD dwDelta = fIncreasingDelta ? (dwCurrentValue - dwLastValue) : (dwLastValue - dwCurrentValue);

                // As a trade-off we can't store deltas with their most efficient length (because just
                // encoding the length can dominate the space requirement when we have to cope with worst-case
                // deltas). Instead we encode a relatively short index into the table of encoding lengths we
                // calculated back in phase 2. So some deltas will encode in more bits than necessary but
                // overall we'll win due to lowered prefix bit requirements.
                // Look through all the table entries and choose the first that's large enough to accomodate
                // our delta.
                DWORD dwDeltaBitLength = BitsRequired(dwDelta);
                DWORD j;
                for (j = 0; j < kLookupMapLengthEntries; j++)
                {
                    if (m_rgEncodingLengths[j] >= dwDeltaBitLength)
                    {
                        dwDeltaBitLength = m_rgEncodingLengths[j];
                        break;
                    }
                }
                _ASSERTE(j < kLookupMapLengthEntries);

                // Write the entry into the compressed table.
                sTableStream.Write(j, kLookupMapLengthBits);        // The index for the delta length
                sTableStream.Write(fIncreasingDelta ? 1 : 0, 1);    // The +/- delta indicator
                sTableStream.Write(dwDelta, dwDeltaBitLength);      // The delta itself

                // Is this entry one that requires a corresponding index entry?
                if ((dwRid % kLookupMapIndexStride) == 0)
                {
                    // Write an index entry:
                    //  * The current (map-relative) RVA.
                    //  * The position in the table bit stream of the next entry.
                    sIndexStream.Write(dwCurrentValue, kBitsPerRVA);
                    sIndexStream.Write(sTableStream.GetBitsWritten(), m_cBitsPerIndexEntry - kBitsPerRVA);
                }

                dwRid++;

                dwLastValue = dwCurrentValue;
            }

            pMap = pMap->pNext;
        }

        // Flush any remaining bits in the caches of the table and index stream writers.
        sTableStream.Flush();
        sIndexStream.Flush();

        // Make sure what we wrote fitted in what we allocated.
        _ASSERTE((sTableStream.GetBitsWritten() / 8) <= m_cbTable);
        _ASSERTE((sIndexStream.GetBitsWritten() / 8) <= m_cbIndex);

        // Also check that we didn't have more than 31 bits of excess space allocated either (we should have
        // allocated DWORD aligned lengths).
        _ASSERTE(((m_cbTable * 8) - sTableStream.GetBitsWritten()) < 32);
        _ASSERTE(((m_cbIndex * 8) - sIndexStream.GetBitsWritten()) < 32);
    }

    // Determine the final, map-relative RVA of the element at a specified index
    DWORD ComputeElementRVA(LookupMapBase *pMap, DWORD index)
    {
        STANDARD_VM_CONTRACT;

        // We base our RVAs on the RVA of the map (rather than the module). This is purely because individual
        // maps don't store back pointers to their owning module so it's easier to recover pointer values at
        // runtime using the map address instead.
        DWORD rvaBase = m_pImage->GetRVA(m_pMap);

        // Retrieve the pointer value in the specified entry. This is tricky since the pointer is
        // encoded as a RelativePointer.
        DWORD dwFinalRVA;
        TADDR entry = RelativePointer<TADDR>::GetValueMaybeNullAtPtr((TADDR)&pMap->pTable[index]);
        if (entry == 0)
        {
            // The pointer was null. We encode this as a zero RVA (RVA pointing to the map itself,
            // which should never happen otherwise).
            dwFinalRVA = 0;
        }
        else
        {
            // Non-null pointer, go get the RVA it's been mapped to. Transform this RVA into our
            // special map-relative variant by substracting the map base.

            // Some of the pointer alignment bits may have been used as flags; preserve them.
            DWORD flags = entry & ((1 << kFlagBits) - 1);
            entry -= flags;

            // We only support compressing maps of pointers to saved objects (e.g. no indirected FixupPointers)
            // so there is guaranteed to be a valid RVA at this point.  If this does not hold, GetRVA will assert.
            DWORD rvaEntry = m_pImage->GetRVA((void*)entry);

            dwFinalRVA = rvaEntry - rvaBase + flags;
        }

        return dwFinalRVA;
    }

    // Determine the number of bits required to represent the significant portion of a value (i.e. the value
    // without any leading 0s). Always return 1 as a minimum (we do not encode 0 in 0 bits).
    DWORD BitsRequired(DWORD dwValue)
    {
        LIMITED_METHOD_CONTRACT;

#if (defined(TARGET_X86) || defined(TARGET_AMD64)) && defined(_MSC_VER)

        // This this operation could impact the performance of ngen (we call this a *lot*) we'll try and
        // optimize this where we can. x86 and amd64 actually have instructions to find the least and most
        // significant bits in a DWORD and MSVC exposes this as a builtin.
        DWORD dwHighBit;
        if (_BitScanReverse(&dwHighBit, dwValue))
            return dwHighBit + 1;
        else
            return 1;

#else // (TARGET_X86 || TARGET_AMD64) && _MSC_VER

        // Otherwise we'll calculate this the slow way. Pick off the 32-bit case first due to avoid the
        // usual << problem (x << 32 == x, not 0).
        if (dwValue > 0x7fffffff)
            return 32;

        DWORD cBits = 1;
        while (dwValue > ((1U << cBits) - 1))
            cBits++;

        return cBits;

#endif // (TARGET_X86 || TARGET_AMD64) && _MSC_VER
    }

    // Sort the given input array (of kLookupMapLengthEntries entries, where the last entry is already sorted)
    // from lowest to highest value.
    void SortLengthBuckets(BYTE rgBuckets[])
    {
        LIMITED_METHOD_CONTRACT;

        // This simplistic insertion sort algorithm is probably the fastest for small values of
        // kLookupMapLengthEntries.
        _ASSERTE(kLookupMapLengthEntries < 10);

        // Iterate over every entry apart from the last two, moving the correct sorted value into each in
        // turn. Don't do the last value because it's already sorted and the second last because it'll be
        // sorted by the time we've done all the rest.
        for (DWORD i = 0; i < (kLookupMapLengthEntries - 2); i++)
        {
            BYTE bLowValue = rgBuckets[i];    // The lowest value we've seen so far
            DWORD dwLowIndex = i;               // The index which held that value

            // Look through the unsorted entries for the smallest.
            for (DWORD j = i + 1; j < (kLookupMapLengthEntries - 1); j++)
            {
                if (rgBuckets[j] < bLowValue)
                {
                    // Got a bette candidate for smallest.
                    bLowValue = rgBuckets[j];
                    dwLowIndex = j;
                }
            }

            // If the original value at the current index wasn't the smallest, swap it with the one that was.
            if (dwLowIndex != i)
            {
                rgBuckets[dwLowIndex] = rgBuckets[i];
                rgBuckets[i] = bLowValue;
            }
        }

#ifdef _DEBUG
        // Check the table really is sorted.
        for (DWORD i = 1; i < kLookupMapLengthEntries; i++)
            _ASSERTE(rgBuckets[i] >= rgBuckets[i - 1]);
#endif // _DEBUG
    }

    // Given the histogram of the delta lengths and a prospective table of the subset of those lengths that
    // we'd utilize to encode the table, return the size (in bits) of the compressed table we'd get as a
    // result. The algorithm requires that the encoding length table is sorted (smallest to largest length).
    DWORD PredictCompressedSize(BYTE rgBuckets[])
    {
        LIMITED_METHOD_CONTRACT;

        DWORD cTotalBits = 0;

        // Iterate over each entry in the histogram (first entry is the number of deltas that can be encoded
        // in 1 bit, the second is the number of entries encodable in 2 bits etc.).
        for (DWORD i = 0; i < kBitsPerRVA; i++)
        {
            // Start by assuming that we can encode entries in this bucket with their exact length.
            DWORD cBits = i + 1;

            // Look through the encoding table to find the first (lowest) encoding length that can encode the
            // values for this bucket.
            for (DWORD j = 0; j < kLookupMapLengthEntries; j++)
            {
                if (cBits <= rgBuckets[j])
                {
                    // This is the best encoding we can do. Remember the real cost of all entries in this
                    // histogram bucket.
                    cBits = rgBuckets[j];
                    break;
                }
            }

            // Each entry for this histogram bucket costs a fixed size index into the encoding length table
            // (kLookupMapLengthBits), a single bit of delta sign plus the number of bits of delta magnitude
            // that we calculated above.
            cTotalBits += (kLookupMapLengthBits + 1 + cBits) * m_rgHistogram[i];
        }

        return cTotalBits;
    }
};

// Allocate a special zap node that will compress the cold rid map associated with the given LookupMap.
void DataImage::StoreCompressedLayoutMap(LookupMapBase *pMap, ItemKind kind)
{
    STANDARD_VM_CONTRACT;

    ZapNode *pNode = new (GetHeap()) ZapCompressedLookupMap(this, pMap, static_cast<BYTE>(kind));

    AddStructureInOrder(pNode);
}

#endif // FEATURE_PREJIT
