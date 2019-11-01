// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: ILStubCache.h
// 

//


#ifdef _MSC_VER
#pragma once
#endif // _MSC_VER
#ifndef _ILSTUBCACHE_H
#define _ILSTUBCACHE_H


#include "vars.hpp"
#include "util.hpp"
#include "crst.h"
#include "ngenhash.h"
#include "stubgen.h"

class ILStubHashBlobBase
{
public:
    size_t m_cbSizeOfBlob;  // this is size of entire object!!
};

class ILStubHashBlob : public ILStubHashBlobBase
{
public:
    BYTE    m_rgbBlobData[1];
};


//
// This class caches MethodDesc's for dynamically generated IL stubs, it is not
// persisted in NGEN images.
//
class ILStubCache : private CClosedHashBase
{
private:
    //---------------------------------------------------------
    // Hash entry for CClosedHashBase.
    //---------------------------------------------------------
    struct ILCHASHENTRY
    {
        // Values:
        //   NULL  = free
        //   -1    = deleted
        //   other = used
        MethodDesc*     m_pMethodDesc;
        ILStubHashBlob* m_pBlob;
    };

public:

    //---------------------------------------------------------
    // Constructor
    //---------------------------------------------------------
    ILStubCache(LoaderHeap* heap = NULL);

    //---------------------------------------------------------
    // Destructor
    //---------------------------------------------------------
    virtual ~ILStubCache();

    void Init(LoaderHeap* pHeap);

    MethodDesc* GetStubMethodDesc(
        MethodDesc *pTargetMD,
        ILStubHashBlob* pParams,
        DWORD dwStubFlags,      // bitmask of NDirectStubFlags
        Module* pSigModule, 
        PCCOR_SIGNATURE pSig, 
        DWORD cbSig,
        AllocMemTracker* pamTracker,
        bool& bILStubCreator,
        MethodDesc* pLastMD);

    void DeleteEntry(void *pParams);

    void AddMethodDescChunkWithLockTaken(MethodDesc *pMD);

    static MethodDesc* CreateAndLinkNewILStubMethodDesc(
        LoaderAllocator* pAllocator,
        MethodTable* pMT,
        DWORD dwStubFlags,      // bitmask of NDirectStubFlags
        Module* pSigModule, 
        PCCOR_SIGNATURE pSig, 
        DWORD cbSig,
        SigTypeContext *pTypeContext,
        ILStubLinker* pStubLinker);

    MethodTable * GetStubMethodTable()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pStubMT;
    }

    MethodTable* GetOrCreateStubMethodTable(Module* pLoaderModule);

private:

    static MethodDesc* CreateNewMethodDesc(
        LoaderHeap* pCreationHeap,
        MethodTable* pMT,
        DWORD dwStubFlags,      // bitmask of NDirectStubFlags
        Module* pSigModule, 
        PCCOR_SIGNATURE pSig, 
        DWORD cbSig,
        SigTypeContext *pTypeContext,
        AllocMemTracker* pamTracker);

    // *** OVERRIDES FOR CClosedHashBase ***/

    //*****************************************************************************
    // Hash is called with a pointer to an element in the table.  You must override
    // this method and provide a hash algorithm for your element type.
    //*****************************************************************************
    virtual unsigned int Hash(             // The key value.
        void const*  pData);                // Raw data to hash.
    
    //*****************************************************************************
    // Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
    // direction of miscompare.  In this system everything is always equal or not.
    //*****************************************************************************
    virtual unsigned int Compare(          // 0, -1, or 1.
        void const*  pData,                 // Raw key data on lookup.
        BYTE*        pElement);             // The element to compare data against.
    
    //*****************************************************************************
    // Return true if the element is free to be used.
    //*****************************************************************************
    virtual ELEMENTSTATUS Status(           // The status of the entry.
        BYTE*        pElement);             // The element to check.

    //*****************************************************************************
    // Sets the status of the given element.
    //*****************************************************************************
    virtual void SetStatus(
        BYTE*         pElement,             // The element to set status for.
        ELEMENTSTATUS eStatus);             // New status.
    
    //*****************************************************************************
    // Returns the internal key value for an element.
    //*****************************************************************************
    virtual void* GetKey(                   // The data to hash on.
        BYTE*        pElement);             // The element to return data ptr for.

private:
    Crst            m_crst;
    LoaderHeap*     m_heap;
    MethodTable*    m_pStubMT;
};


#ifdef FEATURE_PREJIT
//========================================================================================
//
// This hash table is used by interop to lookup NGENed marshaling stubs for methods
// in cases where the MethodDesc cannot point to the stub directly.
//
// Keys are arbitrary MethodDesc's, values are IL stub MethodDescs.
//
//========================================================================================

typedef DPTR(struct StubMethodHashEntry) PTR_StubMethodHashEntry;
typedef struct StubMethodHashEntry
{
    PTR_MethodDesc GetMethod();
    PTR_MethodDesc GetStubMethod();
#ifndef DACCESS_COMPILE
    void SetMethodAndStub(MethodDesc *pMD, MethodDesc *pStubMD);
#endif // !DACCESS_COMPILE

private:
    friend class StubMethodHashTable;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    PTR_MethodDesc      pMD;
    PTR_MethodDesc      pStubMD;

} StubMethodHashEntry_t;


// The hash table itself
typedef DPTR(class StubMethodHashTable) PTR_StubMethodHashTable;
class StubMethodHashTable : public NgenHashTable<StubMethodHashTable, StubMethodHashEntry, 2>
{
#ifndef DACCESS_COMPILE
    StubMethodHashTable();

    StubMethodHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets) :
        NgenHashTable<StubMethodHashTable, StubMethodHashEntry, 2>(pModule, pHeap, cInitialBuckets) {}

    ~StubMethodHashTable();
#endif
public:
    static StubMethodHashTable *Create(LoaderAllocator *pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker);

private:
    void operator delete(void *p);

public:
    // Looks up a stub MethodDesc in the hash table, returns NULL if not found
    MethodDesc *FindMethodDesc(MethodDesc *pMD);

#ifndef DACCESS_COMPILE
    // Inserts a method-stub pair into the hash table
    VOID InsertMethodDesc(MethodDesc *pMD, MethodDesc *pStubMD);

    void Save(DataImage *image, CorProfileData *profileData);
    void Fixup(DataImage *image);

    bool ShouldSave(DataImage *pImage, StubMethodHashEntry_t *pEntry);

    bool IsHotEntry(StubMethodHashEntry_t *pEntry, CorProfileData *pProfileData)
    { LIMITED_METHOD_CONTRACT; return true; }

    bool SaveEntry(DataImage *pImage, CorProfileData *pProfileData, StubMethodHashEntry_t *pOldEntry, StubMethodHashEntry_t *pNewEntry, EntryMappingTable *pMap)
    { LIMITED_METHOD_CONTRACT; return false; }

    void FixupEntry(DataImage *pImage, StubMethodHashEntry_t *pEntry, void *pFixupBase, DWORD cbFixupOffset);
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    void EnumMemoryRegionsForEntry(StubMethodHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags);
#endif
};
#endif // FEATURE_PREJIT

#endif //_ILSTUBCACHE_H
