// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPooli.h
//

//
// This is helper code for the string and blob pools.  It is here because it is
// secondary to the pooling interface and reduces clutter in the main file.
//
//*****************************************************************************

#ifndef __StgPooli_h__
#define __StgPooli_h__

#include "utilcode.h"                   // Base hashing code.
#include "mdfileformat.h"               // For CPackedLen


class StgPoolReadOnly;

//*****************************************************************************
// This hash class will handle strings inside of a chunk of the pool.
//*****************************************************************************
struct STRINGHASH : HASHLINK
{
    ULONG       iOffset;                // Offset of this item.
};

class CStringPoolHash : public CChainedHash<STRINGHASH>
{
    friend class VerifyLayoutsMD;
public:
    CStringPoolHash(StgPoolReadOnly *pool) : m_Pool(pool)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool InUse(STRINGHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        return (pItem->iOffset != 0xffffffff);
    }

    virtual void SetFree(STRINGHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        pItem->iOffset = 0xffffffff;
    }

    virtual ULONG Hash(const void *pData)
    {
        WRAPPER_NO_CONTRACT;
        return (HashStringA(reinterpret_cast<LPCSTR>(pData)));
    }

    virtual int Cmp(const void *pData, void *pItem);

private:
    StgPoolReadOnly *m_Pool;                // String pool which this hashes.
};


//*****************************************************************************
// This version is for byte streams with a 2 byte WORD giving the length of
// the data.
//*****************************************************************************
typedef STRINGHASH BLOBHASH;

class CBlobPoolHash : public CChainedHash<STRINGHASH>
{
    friend class VerifyLayoutsMD;
public:
    CBlobPoolHash(StgPoolReadOnly *pool) : m_Pool(pool)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool InUse(BLOBHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        return (pItem->iOffset != 0xffffffff);
    }

    virtual void SetFree(BLOBHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        pItem->iOffset = 0xffffffff;
    }

    virtual ULONG Hash(const void *pData)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_FORBID_FAULT;

        ULONG       ulSize;
        ulSize = CPackedLen::GetLength(pData);
        ulSize += CPackedLen::Size(ulSize);
        return (HashBytes(reinterpret_cast<BYTE const *>(pData), ulSize));
    }

    virtual int Cmp(const void *pData, void *pItem);

private:
    StgPoolReadOnly *m_Pool;                // Blob pool which this hashes.
};

//*****************************************************************************
// This hash class will handle guids inside of a chunk of the pool.
//*****************************************************************************
struct GUIDHASH : HASHLINK
{
    ULONG       iIndex;                 // Index of this item.
};

class CGuidPoolHash : public CChainedHash<GUIDHASH>
{
    friend class VerifyLayoutsMD;
public:
    CGuidPoolHash(StgPoolReadOnly *pool) : m_Pool(pool)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool InUse(GUIDHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        return (pItem->iIndex != 0xffffffff);
    }

    virtual void SetFree(GUIDHASH *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        pItem->iIndex = 0xffffffff;
    }

    virtual ULONG Hash(const void *pData)
    {
        WRAPPER_NO_CONTRACT;
        return (HashBytes(reinterpret_cast<BYTE const *>(pData), sizeof(GUID)));
    }

    virtual int Cmp(const void *pData, void *pItem);

private:
    StgPoolReadOnly *m_Pool;                // The GUID pool which this hashes.
};


#endif // __StgPooli_h__
