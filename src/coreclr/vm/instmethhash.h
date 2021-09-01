// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: instmethhash.h
//


//

//
// ============================================================================

#ifndef _INSTMETHHASH_H
#define _INSTMETHHASH_H

#include "ngenhash.h"

class AllocMemTracker;

//========================================================================================
// The hash table types defined in this header file are used by the loader to
// look up instantiation-specific methods:
// - per-instantiation static method stubs e.g ArrayList<string>.HelperMeth
// - instantiated methods e.g. Array.Sort<string>
//
// Each persisted Module has an InstMethodHashTable used for such methods that
// were ngen'ed into that module. See ceeload.hpp for more information about ngen modules.
//
// Methods created at runtime are placed in an InstMethHashTable in BaseDomain.
//
// Keys are always derivable from the data stored in the table (MethodDesc)
//
// Keys are always derivable from the data stored in the table (MethodDesc),
// with the exception of some flag values that cannot be computed for unrestore MDs
// (we need to be able to look up entries without restoring other entries along
// the way!)
//
// The table is safe for multiple readers and a single writer i.e. only one thread
// can be in InsertMethodDesc but multiple threads can be in FindMethodDesc.
//========================================================================================

class InstMethodHashTable;

// One of these is present for each element in the table
// It simply chains together (hash,data) pairs
typedef DPTR(struct InstMethodHashEntry) PTR_InstMethodHashEntry;
typedef struct InstMethodHashEntry
{
    PTR_MethodDesc GetMethod();
    DWORD GetFlags();
#ifndef DACCESS_COMPILE
    void SetMethodAndFlags(MethodDesc *pMethod, DWORD dwFlags);
#endif // !DACCESS_COMPILE

    enum
    {
        UnboxingStub    = 0x01,
        RequiresInstArg = 0x02
    };

private:
    friend class InstMethodHashTable;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    PTR_MethodDesc      data;
} InstMethodHashEntry_t;


// The method-desc hash table itself
typedef DPTR(class InstMethodHashTable) PTR_InstMethodHashTable;
class InstMethodHashTable : public NgenHashTable<InstMethodHashTable, InstMethodHashEntry, 4>
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

public:
    // This is the allocator
    PTR_LoaderAllocator  m_pLoaderAllocator;

#ifdef _DEBUG
private:
    Volatile<LONG> m_dwSealCount; // Can more types be added to the table?

public:
    void            InitUnseal() { LIMITED_METHOD_CONTRACT; m_dwSealCount = 0; }
    bool            IsUnsealed() { LIMITED_METHOD_CONTRACT; return (m_dwSealCount == 0); }
    void            Seal()   { LIMITED_METHOD_CONTRACT; FastInterlockIncrement(&m_dwSealCount); }
    void            Unseal() { LIMITED_METHOD_CONTRACT; FastInterlockDecrement(&m_dwSealCount); }
#endif  // _DEBUG

private:
    InstMethodHashTable();
    ~InstMethodHashTable();

public:
    static InstMethodHashTable* Create(LoaderAllocator *pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker);

private:
    friend class NgenHashTable<InstMethodHashTable, InstMethodHashEntry, 4>;

#ifndef DACCESS_COMPILE
    InstMethodHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets) :
        NgenHashTable<InstMethodHashTable, InstMethodHashEntry, 4>(pModule, pHeap, cInitialBuckets) {}
#endif
    void               operator delete(void *p);

public:
    // Add a method desc to the hash table
    void InsertMethodDesc(MethodDesc *pMD);

    // Look up a method in the hash table
    MethodDesc *FindMethodDesc(TypeHandle declaringType,
                               mdMethodDef token,
                               BOOL unboxingStub,
                               Instantiation inst,
                               BOOL getSharedNotStub);

    BOOL ContainsMethodDesc(MethodDesc* pMD);

    // An iterator for the table, currently used only by Module::Save
    struct Iterator
    {
    public:
        // This iterator can be reused for walking different tables
        void Reset();
        Iterator();

        Iterator(InstMethodHashTable * pTable);
        ~Iterator();

    private:
        friend class InstMethodHashTable;

        void Init();

        InstMethodHashTable*m_pTable;
        BaseIterator        m_sIterator;
        bool                m_fIterating;
    };

    BOOL FindNext(Iterator *it, InstMethodHashEntry **ppEntry);

    DWORD GetCount();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    void EnumMemoryRegionsForEntry(InstMethodHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags);
#endif

private:
    LoaderAllocator* GetLoaderAllocator();
};

#endif /* _INSTMETHHASH_H */
