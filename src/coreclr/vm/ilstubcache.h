// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

class LoaderAllocator;

//
// This class caches MethodDesc's for dynamically generated IL stubs, it is not
// persisted in NGEN images.
//
class ILStubCache final
{
public:

    //---------------------------------------------------------
    // Constructor
    //---------------------------------------------------------
    ILStubCache(LoaderAllocator* pAllocator = NULL);

    void Init(LoaderAllocator* pAllocator);

    MethodDesc* GetStubMethodDesc(
        MethodDesc *pTargetMD,
        ILStubHashBlob* pHashBlob,
        DWORD dwStubFlags,      // bitmask of NDirectStubFlags
        Module* pSigModule,
        Module* pSigLoaderModule,
        SigTypeContext* pTypeContext,
        PCCOR_SIGNATURE pSig,
        DWORD cbSig,
        AllocMemTracker* pamTracker,
        bool& bILStubCreator,
        MethodDesc* pLastMD);

    void DeleteEntry(ILStubHashBlob* pHashBlob);

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

    MethodDesc* LookupStubMethodDesc(ILStubHashBlob* pHashBlob);

    // Insert a stub MethodDesc into the cache
    // If one is already present at a matching hash blob, return the already present one, otherwise, return pMD
    MethodDesc* InsertStubMethodDesc(MethodDesc* pMD, ILStubHashBlob* pHashBlob);

private: // static
    static MethodDesc* CreateNewMethodDesc(
        LoaderHeap* pCreationHeap,
        MethodTable* pMT,
        DWORD dwStubFlags,      // bitmask of NDirectStubFlags
        Module* pSigModule,
        PCCOR_SIGNATURE pSig,
        DWORD cbSig,
        SigTypeContext *pTypeContext,
        AllocMemTracker* pamTracker);

private: // Inner classes
    struct ILStubCacheEntry
    {
        MethodDesc *m_pMethodDesc;
        ILStubHashBlob *m_pBlob;
    };

    class ILStubCacheTraits : public DefaultSHashTraits<ILStubCacheEntry>
    {
    public:
        using key_t = const ILStubHashBlob *;
        static const key_t GetKey(_In_ const element_t& e) { LIMITED_METHOD_CONTRACT; return e.m_pBlob; }
        static count_t Hash(_In_ key_t key);
        static bool Equals(_In_ key_t lhs, _In_ key_t rhs);
        static bool IsNull(_In_ const element_t& e) { LIMITED_METHOD_CONTRACT; return e.m_pMethodDesc == NULL; }
        static const element_t Null() { LIMITED_METHOD_CONTRACT; return ILStubCacheEntry(); }
        static bool IsDeleted(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.m_pMethodDesc == (MethodDesc *)-1; }
        static const element_t Deleted() { LIMITED_METHOD_CONTRACT; return ILStubCacheEntry{(MethodDesc *)-1, NULL}; }
    };

private:
    Crst            m_crst;
    LoaderAllocator*m_pAllocator;
    MethodTable*    m_pStubMT;
    SHash<ILStubCacheTraits> m_hashMap;
};

#endif //_ILSTUBCACHE_H
