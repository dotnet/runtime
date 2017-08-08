// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef _DATAIMAGE_H_
#define _DATAIMAGE_H_

#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)

// All we really need is to pre-declare the PrecodeType enum, but g++ doesn't
// support enum pre-declaration, so we need to include the declaration itself.
/*#include "cgensys.h" // needed to include precode.h*/
#include "precode.h"

typedef BYTE ZapRelocationType; // IMAGE_REL_XXX enum

// IMAGE_REL_BASED_PTR is architecture specific reloc of virtual address
#ifdef _WIN64
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_DIR64
#else
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_HIGHLOW
#endif

// Special NGEN-specific relocation type for relative pointer (used to make NGen relocation section smaller)
#define IMAGE_REL_BASED_RELPTR            0x7D

class CEEPreloader;

class ZapImage;
class TypeHandleList;

class ZapNode;
class ZapStoredStructure;

class ZapHeap;
void *operator new(size_t size, ZapHeap * pZapHeap);
void *operator new[](size_t size, ZapHeap * pZapHeap);

class InternedStructureTraits;
typedef SHash<InternedStructureTraits> InternedStructureHashTable;

struct LookupMapBase;
class InlineTrackingMap;

class DataImage
{
public:
    //
    // As items are recorded for saving we note some information about the item
    // to help guide later heuristics.
    //
    enum ItemKind
    {
        #define DEFINE_ITEM_KIND(id)  id,
        #include "dataimagesection.h"

        ITEM_COUNT,
    };

    Module *m_module;    
    CEEPreloader *m_preloader;
    ZapImage * m_pZapImage;

    struct StructureEntry
    {
        const void *    ptr;
        ZapNode *       pNode;
        SSIZE_T         offset;
    };

    class StructureTraits : public NoRemoveSHashTraits< DefaultSHashTraits<StructureEntry> >
    {
    public:
        typedef const void * key_t;

        static key_t GetKey(element_t e) 
        { 
            LIMITED_METHOD_CONTRACT;
            return e.ptr;
        }
        static BOOL Equals(key_t k1, key_t k2) 
        { 
            LIMITED_METHOD_CONTRACT;
            return (k1 == k2);
        }
        static count_t Hash(key_t k) 
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; StructureEntry e; e.ptr = NULL; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.ptr == NULL; }
    };
    typedef SHash<StructureTraits> StructureHashTable;

    StructureHashTable m_structures;
    const StructureEntry * m_pLastLookup; // Cached result of last lookup

    #define MAINTAIN_SAVE_ORDER (0xFFFFFFFF)

    struct SavedNodeEntry
    {
        ZapNode * pNode;
        DWORD     dwAssociatedOrder;
    };

    // These are added in save order, however after PlaceRemainingStructures they may have been
    // rearranged based on the class layout order stored in the dwAssociatedOrder field.
    SArray<SavedNodeEntry> m_structuresInOrder;

    void AddStructureInOrder(ZapNode *pNode, BOOL fMaintainSaveOrder = FALSE);

    struct FixupEntry
    {
        ZapRelocationType   m_type;
        DWORD               m_offset;
#ifdef _DEBUG
        DWORD               m_ordinal;
#endif // _DEBUG

        ZapStoredStructure * m_pLocation;
        ZapNode *           m_pTargetNode;
    };

    SArray<FixupEntry> m_Fixups;
    COUNT_T m_iCurrentFixup;

    void AppendFixup(FixupEntry entry)
    {
#ifdef _DEBUG
        static DWORD s_ordinal = 1;
        entry.m_ordinal = s_ordinal++;
#endif // _DEBUG
        m_Fixups.Append(entry);
    }

    static int __cdecl fixupEntryCmp(const void* a_, const void* b_);

    void FixupSectionRange(SIZE_T offset, ZapNode * pNode);
    void FixupSectionPtr(SIZE_T offset, ZapNode * pNode);
    void FixupJumpStubPtr(SIZE_T offset, CorInfoHelpFunc ftnNum);

    void FixupModuleRVAs();

    InternedStructureHashTable * m_pInternedStructures;
    SetSHash<ZapNode *> m_reusedStructures;

    struct RvaInfoStructure
    {
        FieldDesc * pFD;
        DWORD      rva;
        UINT       size;
        UINT       align;
    };

    SArray<RvaInfoStructure> m_rvaInfoVector;

    static int __cdecl rvaInfoVectorEntryCmp(const void* a_, const void* b_);

    MapSHash<PVOID,PVOID> m_surrogates;

    // Often set while a class is being saved in order to associate
    // stored structures with the class, and therefore its layout order.
    // Note that it is a best guess and not always set.
    MethodTable * m_pCurrentAssociatedMethodTable;

    struct MethodProfilingData
    {
        MethodDesc      *pMD;
        DWORD           flags;
    };

    class MethodProfilingDataTraits : public NoRemoveSHashTraits< DefaultSHashTraits<MethodProfilingData> >
    {
    public:
        typedef const MethodDesc * key_t;

        static key_t GetKey(element_t e)
        { 
            LIMITED_METHOD_CONTRACT;
            return e.pMD;
        }
        static BOOL Equals(key_t k1, key_t k2) 
        { 
            LIMITED_METHOD_CONTRACT;
            return (k1 == k2);
        }
        static count_t Hash(key_t k) 
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; MethodProfilingData e; e.pMD = NULL; e.flags = 0; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pMD == NULL; }
    };
    typedef SHash<MethodProfilingDataTraits> MethodProfilingDataHashTable;

    MethodProfilingDataHashTable m_methodProfilingData;

    // This is a hashmap from inlinee method to an array of inliner methods
    // So it can answer question: "where did this method get inlined ?"
    InlineTrackingMap *m_inlineTrackingMap;

  public:
    DataImage(Module *module, CEEPreloader *preloader);
    ~DataImage();

    void Preallocate();

    void PreSave();
    void PostSave();

    Module *GetModule() { LIMITED_METHOD_CONTRACT; return m_module; }

    DWORD GetMethodProfilingFlags(MethodDesc * pMD);
    void SetMethodProfilingFlags(MethodDesc * pMD, DWORD flags);

    CEEPreloader *GetPreloader() { LIMITED_METHOD_CONTRACT; return m_preloader; }

    ZapHeap * GetHeap();

    //
    // Data is stored in the image store in three phases. 
    //

    //
    // In the first phase, all objects are assigned locations in the
    // data store.  This is done by calling StoreStructure on all
    // structures which are being stored into the image.
    //
    // This would typically done by methods on the objects themselves,
    // each of which stores itself and any objects it references.  
    // Reference loops must be explicitly tested for using IsStored.
    // (Each structure can be stored only once.)
    //
    // Note that StoreStructure makes no guarantees about layout order.
    // If you want structures of a particular kind to be laid out in
    // the order they are saved, use StoreStructureInOrder.
    //

    inline ZapStoredStructure * StoreStructure(const void *data, SIZE_T size,
                           ItemKind kind,
                           int align = sizeof(TADDR))
    {
        return StoreStructureHelper(data, size, kind, align, FALSE);
    }

    inline ZapStoredStructure * StoreStructureInOrder(const void *data, SIZE_T size,
                           ItemKind kind,
                           int align = sizeof(TADDR))
    {
        return StoreStructureHelper(data, size, kind, align, TRUE);
    }

    ZapStoredStructure * StoreStructureHelper(const void *data, SIZE_T size,
                           ItemKind kind,
                           int align,
                           BOOL fMaintainSaveOrder);

    // Often set while a class is being saved in order to associate
    // stored structures with the class, and therefore its layout order.
    // Note that it is a best guess and not always set.
    inline void BeginAssociatingStoredObjectsWithMethodTable(MethodTable *pMT)
    {
        m_pCurrentAssociatedMethodTable = pMT;
    }

    inline void EndAssociatingStoredObjectsWithMethodTable()
    {
        m_pCurrentAssociatedMethodTable = NULL;
    }

    // Bind pointer to the relative offset in ZapNode
    void BindPointer(const void *p, ZapNode * pNode, SSIZE_T offset);

    void BindPointer(const void *p, ZapStoredStructure * pNode, SSIZE_T offset)
    {
        BindPointer(p, (ZapNode *)pNode, offset);
    }

    void CopyData(ZapStoredStructure * pNode, const void * p, ULONG size);
    void CopyDataToOffset(ZapStoredStructure * pNode, ULONG offset, const void * p, ULONG size);

    //
    // In the second phase, data is arranged in the image by successive calls
    // to PlaceMappedRange.  Items are arranged using pointers to data structures in the
    // original heap, or by giving a StoredStructure along with the original 
    // mapping.
    //

    // Concrete mapped ranges are the ones that actually correspond to allocations
    // of new space within the image.  They should be placed first.  We do not
    // necessarily populate the space in the image (i.e. copy the data to the image) 
    // from the concrete range: for example the space associated with a
    // combo structure gets filled by copying the data from the individual items
    // that make up the parts of the combo structure.
    //
    // These can tolerate placing the same item multiple times
    // PlaceInternedStructureForAddress allows a different section to be used depending on
    // whether an interned structure actually had duplicates in this image.
    //
    void PlaceStructureForAddress(const void * data, CorCompileSection section);
    void PlaceInternedStructureForAddress(const void * data, CorCompileSection sectionIfReused, CorCompileSection sectionIfSingleton);

    void FixupPointerField(PVOID p, SSIZE_T offset);
    void FixupRelativePointerField(PVOID p, SSIZE_T offset);

    template<typename T, typename PT>
    void FixupPlainOrRelativePointerField(const T *base, const RelativePointer<PT> T::* pPointerFieldMember)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupRelativePointerField((PVOID)base, offset);
    }

    template<typename T, typename C, typename PT>
    void FixupPlainOrRelativePointerField(const T *base, const C T::* pFirstPointerFieldMember, const RelativePointer<PT> C::* pSecondPointerFieldMember)
    {
        STANDARD_VM_CONTRACT;
        const RelativePointer<PT> *ptr = &(base->*pFirstPointerFieldMember.*pSecondPointerFieldMember);
        SSIZE_T offset = (SSIZE_T) ptr - (SSIZE_T) base;
        FixupRelativePointerField((PVOID)base, offset);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativePointerField(const T *base, const PlainPointer<PT> T::* pPointerFieldMember)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupPointerField((PVOID)base, offset);
    }

    template<typename T, typename C, typename PT>
    void FixupPlainOrRelativePointerField(const T *base, const C T::* pFirstPointerFieldMember, const PlainPointer<PT> C::* pSecondPointerFieldMember)
    {
        STANDARD_VM_CONTRACT;
        const PlainPointer<PT> *ptr = &(base->*pFirstPointerFieldMember.*pSecondPointerFieldMember);
        SSIZE_T offset = (SSIZE_T) ptr - (SSIZE_T) base;
        FixupPointerField((PVOID)base, offset);
    }

    void FixupField(PVOID p, SSIZE_T offset, PVOID pTarget, SSIZE_T targetOffset = 0, ZapRelocationType type = IMAGE_REL_BASED_PTR);

    template<typename T, typename PT>
    void FixupPlainOrRelativeField(const T *base, const RelativePointer<PT> T::* pPointerFieldMember, PVOID pTarget, SSIZE_T targetOffset = 0)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupField((PVOID)base, offset, pTarget, targetOffset, IMAGE_REL_BASED_RELPTR);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativeField(const T *base, const PlainPointer<PT> T::* pPointerFieldMember, PVOID pTarget, SSIZE_T targetOffset = 0)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupField((PVOID)base, offset, pTarget, targetOffset, IMAGE_REL_BASED_PTR);
    }

    void FixupFieldToNode(PVOID p, SSIZE_T offset, ZapNode * pTarget, SSIZE_T targetOffset = 0, ZapRelocationType type = IMAGE_REL_BASED_PTR);

    void FixupFieldToNode(PVOID p, SSIZE_T offset, ZapStoredStructure * pTarget, SSIZE_T targetOffset = 0, ZapRelocationType type = IMAGE_REL_BASED_PTR)
    {
        return FixupFieldToNode(p, offset, (ZapNode *)pTarget, targetOffset, type);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativeFieldToNode(const T *base, const RelativePointer<PT> T::* pPointerFieldMember, ZapNode * pTarget, SSIZE_T targetOffset = 0)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupFieldToNode((PVOID)base, offset, pTarget, targetOffset, IMAGE_REL_BASED_RELPTR);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativeFieldToNode(const T *base, const RelativePointer<PT> T::* pPointerFieldMember, ZapStoredStructure * pTarget, SSIZE_T targetOffset = 0)
    {
        return FixupPlainOrRelativeFieldToNode(base, pPointerFieldMember, (ZapNode *)pTarget, targetOffset);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativeFieldToNode(const T *base, const PlainPointer<PT> T::* pPointerFieldMember, ZapNode * pTarget, SSIZE_T targetOffset = 0)
    {
        STANDARD_VM_CONTRACT;
        SSIZE_T offset = (SSIZE_T) &(base->*pPointerFieldMember) - (SSIZE_T) base;
        FixupFieldToNode((PVOID)base, offset, pTarget, targetOffset, IMAGE_REL_BASED_PTR);
    }

    template<typename T, typename PT>
    void FixupPlainOrRelativeFieldToNode(const T *base, const PlainPointer<PT> T::* pPointerFieldMember, ZapStoredStructure * pTarget, SSIZE_T targetOffset = 0)
    {
        return FixupPlainOrRelativeFieldToNode(base, pPointerFieldMember, (ZapNode *)pTarget, targetOffset);
    }

    BOOL IsStored(const void *data)
      { WRAPPER_NO_CONTRACT; return m_structures.LookupPtr(data) != NULL; }

    DWORD GetRVA(const void *data);

    void ZeroField(PVOID p, SSIZE_T offset, SIZE_T size);
    void *GetImagePointer(ZapStoredStructure * pNode);
    void *GetImagePointer(PVOID p, SSIZE_T offset = 0);
    ZapNode * GetNodeForStructure(PVOID p, SSIZE_T * pOffset);

    void ZeroPointerField(PVOID p, SSIZE_T offset) 
      { WRAPPER_NO_CONTRACT; ZeroField(p, offset, sizeof(void*)); }


    ZapStoredStructure * StoreInternedStructure(const void *data, ULONG size,
                           ItemKind kind,
                           int align = sizeof(TADDR));

    void NoteReusedStructure(const void *data);

    void StoreRvaInfo(FieldDesc * pFD,
                      DWORD      rva,
                      UINT       size,
                      UINT       align);

    void SaveRvaStructure();
    void FixupRvaStructure();

    // Surrogates are used to reorganize the data before they are saved. RegisterSurrogate and LookupSurrogate 
    // maintains mapping from the original data to the reorganized data.
    void RegisterSurrogate(PVOID ptr, PVOID surrogate);
    PVOID LookupSurrogate(PVOID ptr);

    void PlaceRemainingStructures();

    void FixupRVAs();

    void SetRVAsForFields(IMetaDataEmit * pEmit);

    // Called when data contains a function address.  The data store
    // can return a fixed compiled code address if it is compiling
    // code for the module.
    ZapNode * GetCodeAddress(MethodDesc * method);

    // Returns TRUE if the method can be called directly without going through prestub
    BOOL CanDirectCall(MethodDesc * method, CORINFO_ACCESS_FLAGS  accessFlags = CORINFO_ACCESS_ANY);

    // Returns the method fixup info if it has one, NULL if method has no fixup info
    ZapNode * GetFixupList(MethodDesc * method);

    ZapNode * GetHelperThunk(CorInfoHelpFunc ftnNum);

    // pUniqueId is used to allocate unique cells for cases where we cannot use the shared cell.
    ZapNode * GetTypeHandleImport(TypeHandle th, PVOID pUniqueId = NULL);
    ZapNode * GetMethodHandleImport(MethodDesc * pMD);
    ZapNode * GetFieldHandleImport(FieldDesc * pFD);
    ZapNode * GetModuleHandleImport(Module * pModule);
    DWORD     GetModuleImportIndex(Module * pModule);

    ZapNode * GetExistingTypeHandleImport(TypeHandle th);
    ZapNode * GetExistingMethodHandleImport(MethodDesc * pMD);
    ZapNode * GetExistingFieldHandleImport(FieldDesc * pFD);

    ZapNode * GetVirtualImportThunk(MethodTable * pMT, MethodDesc * pMD, int slotNumber);

    ZapNode * GetGenericSignature(PVOID signature, BOOL fMethod);

    void SavePrecode(PVOID ptr, MethodDesc * pMD, PrecodeType t, ItemKind kind, BOOL fIsPrebound = FALSE);

    void StoreCompressedLayoutMap(LookupMapBase *pMap, ItemKind kind);

    // "Fixup" here means "save the pointer either as a poiter or indirection"
    void FixupModulePointer(Module * pModule, PVOID p, SSIZE_T offset, ZapRelocationType type);
    void FixupMethodTablePointer(MethodTable * pMT, PVOID p, SSIZE_T offset, ZapRelocationType type);
    void FixupTypeHandlePointer(TypeHandle th, PVOID p, SSIZE_T offset, ZapRelocationType type);
    void FixupMethodDescPointer(MethodDesc * pMD, PVOID p, SSIZE_T offset, ZapRelocationType type);
    void FixupFieldDescPointer(FieldDesc * pFD, PVOID p, SSIZE_T offset, ZapRelocationType type);

    void FixupModulePointer(PVOID p, FixupPointer<PTR_Module> * ppModule);
    void FixupMethodTablePointer(PVOID p, FixupPointer<PTR_MethodTable> * ppMT);
    void FixupTypeHandlePointer(PVOID p, FixupPointer<TypeHandle> * pth);
    void FixupMethodDescPointer(PVOID p, FixupPointer<PTR_MethodDesc> * ppMD);
    void FixupFieldDescPointer(PVOID p, FixupPointer<PTR_FieldDesc> * ppFD);

    void FixupModulePointer(PVOID p, RelativeFixupPointer<PTR_Module> * ppModule);
    void FixupMethodTablePointer(PVOID p, RelativeFixupPointer<PTR_MethodTable> * ppMT);
    void FixupTypeHandlePointer(PVOID p, RelativeFixupPointer<TypeHandle> * pth);
    void FixupMethodDescPointer(PVOID p, RelativeFixupPointer<PTR_MethodDesc> * ppMD);
    void FixupFieldDescPointer(PVOID p, RelativeFixupPointer<PTR_FieldDesc> * ppFD);

    // "HardBind" here means "save a reference using a (relocatable) pointer,
    // where the object we're referring to lives either in an external hard-bound DLL
    // or in the image currently being saved"
    //
    BOOL CanHardBindToZapModule(Module *targetModule);

    void ReportInlining(CORINFO_METHOD_HANDLE inliner, CORINFO_METHOD_HANDLE inlinee);
    InlineTrackingMap *GetInlineTrackingMap();

private:
    BOOL CanEagerBindTo(Module *targetModule, Module *pPreferredZapModule, void *address);

public:
    // "EagerBind" here means "save a reference using pointer in the image currently being saved
    // or indirection cell refering to to external DLL
    BOOL CanEagerBindToTypeHandle(TypeHandle th, BOOL fRequirePrerestore = FALSE, TypeHandleList *pVisited = NULL);
    BOOL CanEagerBindToMethodTable(MethodTable *pMT, BOOL fRequirePrerestore = FALSE, TypeHandleList *pVisited = NULL);
    BOOL CanEagerBindToMethodDesc(MethodDesc *pMD, BOOL fRequirePrerestore = FALSE, TypeHandleList *pVisited = NULL);
    BOOL CanEagerBindToFieldDesc(FieldDesc *pFD, BOOL fRequirePrerestore = FALSE, TypeHandleList *pVisited = NULL);
    BOOL CanEagerBindToModule(Module *pModule);

    // These also check that the target object doesn't need a restore action
    // upon reload.
    BOOL CanPrerestoreEagerBindToTypeHandle(TypeHandle th, TypeHandleList *pVisited);
    BOOL CanPrerestoreEagerBindToMethodTable(MethodTable *pMT, TypeHandleList *pVisited);
    BOOL CanPrerestoreEagerBindToMethodDesc(MethodDesc *pMD, TypeHandleList *pVisited);

    void HardBindTypeHandlePointer(PVOID p, SSIZE_T offset);

    // This is obsolete in-place fixup that we should get rid of. For now, it is used for:
    // - FnPtrTypeDescs. These should not be stored in NGen images at all.
    // - stubs-as-il signatures. These should use tokens when stored in NGen image.
    void FixupTypeHandlePointerInPlace(PVOID p, SSIZE_T offset, BOOL fForceFixup = FALSE);

    void BeginRegion(CorInfoRegionKind regionKind);
    void EndRegion(CorInfoRegionKind regionKind);
};

#endif // FEATURE_PREJIT && !DACCESS_COMPILE

#endif // _DATAIMAGE_H_
