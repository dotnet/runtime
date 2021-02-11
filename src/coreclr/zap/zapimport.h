// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapImport.h
//

//
// Import is soft bound references to elements outside the current module
//
// ======================================================================================

#ifndef __ZAPIMPORT_H__
#define __ZAPIMPORT_H__

class ZapImportTable;
class ZapGCRefMapTable;
class NibbleWriter;

//---------------------------------------------------------------------------------------
//
// ZapImport is the import cell itself
//
// Every import cell is uniquely identified by its ZapNodeType and two handles
// (the second handle is optional and is often NULL)
//
// Actual implementations inherits from this abstract base class.
//
class ZapImport : public ZapNode
{
    COUNT_T m_index;
    DWORD m_offset;

    PVOID m_handle;
    PVOID m_handle2;

    ZapBlob * m_pBlob;

public:
    void SetHandle(PVOID handle)
    {
        _ASSERTE(m_handle == NULL);
        m_handle = handle;
    }

    void SetHandle2(PVOID handle2)
    {
        _ASSERTE(m_handle2 == NULL);
        m_handle2 = handle2;
    }

    PVOID GetHandle()
    {
        return m_handle;
    }

    PVOID GetHandle2()
    {
        return m_handle2;
    }

    void SetBlob(ZapBlob * pBlob)
    {
        _ASSERTE(m_pBlob == NULL);
        m_pBlob = pBlob;
    }

    ZapBlob * GetBlob()
    {
        _ASSERTE(m_pBlob != NULL);
        return m_pBlob;
    }

    BOOL HasBlob()
    {
        return m_pBlob != NULL;
    }

    virtual ZapImportSectionType ComputePlacement(ZapImage * pImage, BOOL * pfIsEager, BOOL * pfNeedsSignature)
    {
        *pfIsEager = FALSE;
        *pfNeedsSignature = TRUE;
        return ZapImportSectionType_Handle;
    }

    // All subtypes have to override
    virtual void EncodeSignature(ZapImportTable * pTable, SigBuilder * pSigBuilder) = 0;

    virtual DWORD GetSize()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual UINT GetAlignment()
    {
        return TARGET_POINTER_SIZE;
    }

    virtual void Save(ZapWriter * pZapWriter);

    //
    // Offset of the fixup cell within its section
    //

    void SetSectionIndexAndOffset(COUNT_T index, DWORD offset)
    {
        m_index = index;
        m_offset = offset;
    }

    DWORD GetSectionIndex()
    {
        return m_index;
    }

    DWORD GetOffset()
    {
        return m_offset;
    }
};

//---------------------------------------------------------------------------------------
//
// ZapGenericSignature is signature of generic dictionary entry.
//
class ZapGenericSignature : public ZapBlob
{
public:
    ZapGenericSignature(SIZE_T cbSize)
        : ZapBlob(cbSize)
    {
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_GenericSignature;
    }
};

//---------------------------------------------------------------------------------------
//
// ZapImportTable is the main class that keeps track of all ZapImports.
//
// There is a single instance of it per image.
//
class ZapImportTable
{
    //
    // Hashtable key of the import
    // The same key is used for both ZapImport and ZapImportBlob
    //
    struct ImportKey
    {
        FORCEINLINE ImportKey(PVOID handle, ZapNodeType type)
            : m_handle(handle), m_handle2(NULL), m_type(type)
        {
        }

        FORCEINLINE ImportKey(PVOID handle, PVOID handle2, ZapNodeType type)
            : m_handle(handle), m_handle2(handle2), m_type(type)
        {
        }

        PVOID m_handle;
        PVOID m_handle2;
        ZapNodeType m_type;
    };

    //
    // Hashtable of ZapImports
    //
    class ImportTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapImport *> >
    {
    public:
        typedef ImportKey key_t;

        static FORCEINLINE key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return ImportKey(e->GetHandle(), e->GetHandle2(), e->GetType());
        }
        static FORCEINLINE BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return (k1.m_handle == k2.m_handle) && (k1.m_handle2 == k2.m_handle2) && (k1.m_type == k2.m_type);
        }
        static FORCEINLINE count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k.m_handle ^ ((count_t)(size_t)k.m_handle2 << 1) ^ k.m_type;
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash< ImportTraits > ImportTable;

    //
    // Hashtable of module indices
    //
    struct ModuleReferenceEntry
    {
        CORINFO_MODULE_HANDLE m_module;
        DWORD m_index;
    };

    class ModuleReferenceTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ModuleReferenceEntry *> >
    {
    public:
        typedef CORINFO_MODULE_HANDLE key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e->m_module;
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

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash< ModuleReferenceTraits > ModuleReferenceTable;

    //
    // Helpers for inserting actual implementations of ZapImports into hashtable
    //
    template < typename impl, ZapNodeType type >
    ZapImport * GetImport(PVOID handle)
    {
        ZapImport * pImport = m_imports.Lookup(ImportKey(handle, type));

        if (pImport != NULL)
        {
            return pImport;
        }

        pImport = new (m_pImage->GetHeap()) impl();
        _ASSERTE(pImport->GetType() == type);
        pImport->SetHandle(handle);
        m_imports.Add(pImport);
        return pImport;
    }

    template < typename impl, ZapNodeType type >
    ZapImport * GetImport(PVOID handle, PVOID handle2)
    {
        ZapImport * pImport = m_imports.Lookup(ImportKey(handle, handle2, type));

        if (pImport != NULL)
        {
            return pImport;
        }

        pImport = new (m_pImage->GetHeap()) impl();
        _ASSERTE(pImport->GetType() == type);
        pImport->SetHandle(handle);
        pImport->SetHandle2(handle2);
        m_imports.Add(pImport);
        return pImport;
    }

    template < typename impl, ZapNodeType type >
    ZapImport * GetImportForSignature(PVOID handle, SigBuilder * pSigBuilder)
    {
        ZapBlob * pBlob = GetBlob(pSigBuilder);

        ZapImport * pImport = m_imports.Lookup(ImportKey(handle, pBlob, type));

        if (pImport != NULL)
        {
            return pImport;
        }

        pImport = new (m_pImage->GetHeap()) impl();
        _ASSERTE(pImport->GetType() == type);
        pImport->SetHandle(handle);
        pImport->SetHandle2(pBlob);
        pImport->SetBlob(pBlob);
        m_imports.Add(pImport);
        return pImport;
    }

    ZapImport * GetExistingImport(ZapNodeType type, PVOID handle)
    {
        return m_imports.Lookup(ImportKey(handle, type));
    }

    ModuleReferenceEntry * GetModuleReference(CORINFO_MODULE_HANDLE handle);

    static DWORD EncodeModuleHelper(LPVOID referencingModule, CORINFO_MODULE_HANDLE referencedModule);

    ImportTable m_imports;          // Interned ZapImport *
    SHash< NoRemoveSHashTraits < ZapBlob::SHashTraits > > m_blobs; // Interned ZapBlos for signatures and fixups

    ModuleReferenceTable m_moduleReferences;

    SHash< NoRemoveSHashTraits < ZapBlob::SHashTraits > > m_genericSignatures;

    DWORD   m_nImportSectionSizes[ZapImportSectionType_Total];
    COUNT_T m_nImportSectionIndices[ZapImportSectionType_Total];

    ZapImage * m_pImage;

public:
    ZapImportTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
         // Everything else is zero initialized by the allocator
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapImportTable::m_imports, 0.0030, cbILImage);
        PREALLOCATE_HASHTABLE(ZapImportTable::m_blobs, 0.0025, cbILImage);

        PREALLOCATE_HASHTABLE_NOT_NEEDED(ZapImportTable::m_moduleReferences, cbILImage);
    }

    //
    // Helpers for encoding import blobs
    //

    void EncodeModule(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_MODULE_HANDLE module, SigBuilder * pSigBuilder);
    void EncodeClass(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_CLASS_HANDLE handle, SigBuilder * pSigBuilder);
    void EncodeClassInContext(CORINFO_MODULE_HANDLE context, CORINFO_CLASS_HANDLE handle, SigBuilder * pSigBuilder);
    void EncodeField(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, SigBuilder * pSigBuilder,
            CORINFO_RESOLVED_TOKEN * pResolvedToken = NULL, BOOL fEncodeUsingResolvedTokenSpecStreams = FALSE);
    void EncodeMethod(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle, SigBuilder * pSigBuilder,
            CORINFO_RESOLVED_TOKEN * pResolvedToken = NULL, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken = NULL,
            BOOL fEncodeUsingResolvedTokenSpecStreams = FALSE);

    // Encode module if the reference is within current version bubble. If not, return a suitable module within current version bubble.
    CORINFO_MODULE_HANDLE TryEncodeModule(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_MODULE_HANDLE module, SigBuilder * pSigBuilder);

    ICorDynamicInfo * GetJitInfo()
    {
        return m_pImage->GetJitInfo();
    }

    ICorCompileInfo * GetCompileInfo()
    {
        return m_pImage->GetCompileInfo();
    }

    ZapImage * GetImage()
    {
        return m_pImage;
    }

    // Returns index of module in the import table for encoding module fixups in EE datastructures.
    DWORD GetIndexOfModule(CORINFO_MODULE_HANDLE handle)
    {
        ZapImportTable::ModuleReferenceEntry * pModuleReference = GetModuleReference(handle);
        _ASSERTE(pModuleReference != NULL);
        return pModuleReference->m_index;
    }

    // Get the import blob for given signature
    ZapBlob * GetBlob(SigBuilder * pSigBuilder, BOOL fEager = FALSE);

    // Place give import blob
    void PlaceBlob(ZapBlob * pBlob, BOOL fEager = FALSE);

    // Encodes the import blob and places it into the image
    ZapBlob * PlaceImportBlob(ZapImport * pImport, BOOL fEager = FALSE);

    // Places import cell into the image.
    // This also encoded and places all the import blobs if they are not placed yet.
    void PlaceImport(ZapImport * pImport);

    // Encodes list of fixups and places it into the image.
    // This also places all the import cells if they are not placed yet.
    ZapFixupInfo * PlaceFixups(ZapImport ** pImports);
    void PlaceFixups(ZapImport ** pImports, NibbleWriter& writer);

    ZapGenericSignature * GetGenericSignature(PVOID signature, BOOL fMethod);

    //
    // The actual implementations of import cells
    //
    ZapImport * GetFunctionEntryImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetModuleHandleImport(CORINFO_MODULE_HANDLE handle);
    ZapImport * GetClassHandleImport(CORINFO_CLASS_HANDLE handle, PVOID pUniqueId = NULL);
    ZapImport * GetMethodHandleImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetFieldHandleImport(CORINFO_FIELD_HANDLE handle);
    ZapImport * GetStringHandleImport(CORINFO_MODULE_HANDLE tokenScope, mdString metaTok);
    ZapImport * GetStaticFieldAddressImport(CORINFO_FIELD_HANDLE handle);
    ZapImport * GetClassDomainIdImport(CORINFO_CLASS_HANDLE handle);
    ZapImport * GetModuleDomainIdImport(CORINFO_MODULE_HANDLE handleToModule, CORINFO_CLASS_HANDLE handleToClass);
    ZapImport * GetSyncLockImport(CORINFO_CLASS_HANDLE handle);
    ZapImport * GetIndirectPInvokeTargetImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetPInvokeTargetImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetProfilingHandleImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetVarArgImport(CORINFO_MODULE_HANDLE handle, mdToken sigOrMemberRefOrDef);
    ZapImport * GetActiveDependencyImport(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo);

    ZapImport * GetExistingClassHandleImport(CORINFO_CLASS_HANDLE handle);
    ZapImport * GetExistingMethodHandleImport(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetExistingFieldHandleImport(CORINFO_FIELD_HANDLE handle);

    ZapImport * GetVirtualImportThunk(CORINFO_METHOD_HANDLE handle, int slot);
    void        PlaceVirtualImportThunk(ZapImport * pImportThunk);

    ZapImport * GetExternalMethodThunk(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetExternalMethodCell(CORINFO_METHOD_HANDLE handle);
    ZapImport * GetStubDispatchCell(CORINFO_CLASS_HANDLE typeHnd, CORINFO_METHOD_HANDLE methHnd);

    //
    // Ready-to-run imports
    //
    ZapImport * GetClassImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_RESOLVED_TOKEN * pResolvedToken);
    ZapImport * GetMethodImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken = NULL);
    ZapImport * GetFieldImport(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken);

    ZapImport * GetCheckTypeLayoutImport(CORINFO_CLASS_HANDLE handle);
    ZapImport * GetCheckFieldOffsetImport(CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, DWORD offset);

    ZapImport * GetStubDispatchCell(CORINFO_RESOLVED_TOKEN * pResolvedToken);
    ZapImport * GetExternalMethodCell(CORINFO_METHOD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken);

    ZapImport * GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_CLASS_HANDLE handle);
    ZapImport * GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_CLASS_HANDLE delegateType = NULL);
    ZapImport * GetDynamicHelperCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_FIELD_HANDLE handle, CORINFO_RESOLVED_TOKEN * pResolvedToken);

    ZapImport * GetDictionaryLookupCell(CORCOMPILE_FIXUP_BLOB_KIND kind, CORINFO_METHOD_HANDLE containingMethod, CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_LOOKUP_KIND * pLookup);

#ifdef FEATURE_READYTORUN_COMPILER
    ZapNode * GetPlacedIndirectHelperThunk(ReadyToRunHelper helperNum, PVOID pArg = NULL);
    ZapNode * GetIndirectHelperThunk(ReadyToRunHelper helperNum, PVOID pArg = NULL);
    void PlaceIndirectHelperThunk(ZapNode * pImport);

    ZapImport * GetPlacedHelperImport(ReadyToRunHelper helperNum);
    ZapImport * GetHelperImport(ReadyToRunHelper helperNum);
#endif
};

//
// CORCOMPILE_CODE_IMPORT_SECTION
//
class ZapImportSectionsTable : public ZapNode
{
    struct ImportSection
    {
        ZapVirtualSection * m_pSection;
        ZapNode * m_pSignatures;
        ZapNode * m_pAuxiliaryData;
        USHORT    m_Flags;
        BYTE      m_Type;
        BYTE      m_EntrySize;
    };

    SArray<ImportSection> m_ImportSectionsTable;

public:
    ZapImportSectionsTable(ZapImage * pImage)
    {
    }

    COUNT_T Append(BYTE Type, USHORT Flags, BYTE EntrySize, ZapVirtualSection * pSection, ZapNode * pSignatures = NULL, ZapNode * pAuxiliaryData = NULL);

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual DWORD GetSize();

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ImportSectionsTable;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

//
// ZapImportSectionSignatures contains an array of signature RVAs for given import section.
//
class ZapImportSectionSignatures : public ZapNode
{
    ZapVirtualSection * m_pImportSection;
    ZapGCRefMapTable * m_pGCRefMapTable;

    DWORD m_dwIndex;

    ZapImage * m_pImage;

public:
    ZapImportSectionSignatures(ZapImage * pImage, ZapVirtualSection * pImportSection, ZapVirtualSection * pGCSection = NULL);
    ~ZapImportSectionSignatures();

    void PlaceExternalMethodThunk(ZapImport * pImport);
    void PlaceExternalMethodCell(ZapImport * pImport);
    void PlaceStubDispatchCell(ZapImport * pImport);
    void PlaceDynamicHelperCell(ZapImport * pImport);

    virtual DWORD GetSize();

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_ImportSectionSignatures;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

#include "gcrefmap.h"

class ZapGCRefMapTable : public ZapNode
{
    ZapImage * m_pImage;
    GCRefMapBuilder m_GCRefMapBuilder;
    COUNT_T m_nCount;

public:
    ZapGCRefMapTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Append(CORINFO_METHOD_HANDLE handle, bool isDispatchCell = false);

    virtual DWORD GetSize();

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_GCRefMapTable;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

#endif // __ZAPIMPORT_H__
