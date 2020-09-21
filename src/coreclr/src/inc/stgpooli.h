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



//
//
// CPackedLen
//
//

//*****************************************************************************
// Helper class to pack and unpack lengths.
//*****************************************************************************
struct CPackedLen
{
    enum {MAX_LEN = 0x1fffffff};
    static int Size(ULONG len)
    {
                LIMITED_METHOD_CONTRACT;
        // Smallest.
        if (len <= 0x7F)
            return 1;
        // Medium.
        if (len <= 0x3FFF)
            return 2;
        // Large (too large?).
        _ASSERTE(len <= MAX_LEN);
        return 4;
    }

    // Get a pointer to the data, and store the length.
    static void const *GetData(void const *pData, ULONG *pLength);

    // Get the length value encoded at *pData.  Update ppData to point past data.
    static ULONG GetLength(void const *pData, void const **ppData=0);

    // Get the length value encoded at *pData, and the size of that encoded value.
    static ULONG GetLength(void const *pData, int *pSizeOfLength);

    // Pack a length at *pData; return a pointer to the next byte.
    static void* PutLength(void *pData, ULONG len);

    // This is used for just getting an encoded length, and verifies that
    // there is no buffer or integer overflow.
    static HRESULT SafeGetLength(       // S_OK, or error
        void const  *pDataSource,       // First byte of length.
        void const  *pDataSourceEnd,    // End of valid source data memory
        ULONG       *pLength,           // Encoded value
        void const **ppDataNext);       // Pointer immediately following encoded length

    static HRESULT SafeGetLength(       // S_OK, or error
        BYTE const  *pDataSource,       // First byte of length.
        BYTE const  *pDataSourceEnd,    // End of valid source data memory
        ULONG       *pLength,           // Encoded value
        BYTE const **ppDataNext)        // Pointer immediately following encoded length
    {
        return SafeGetLength(
            reinterpret_cast<void const *>(pDataSource),
            reinterpret_cast<void const *>(pDataSourceEnd),
            pLength,
            reinterpret_cast<void const **>(ppDataNext));
    }

    // This performs the same tasks as GetLength above in addition to checking
    // that the value in *pcbData does not extend *ppData beyond pDataSourceEnd
    // and does not cause an integer overflow.
    static HRESULT SafeGetData(
        void const  *pDataSource,       // First byte of length.
        void const  *pDataSourceEnd,    // End of valid source data memory
        ULONG       *pcbData,           // Length of data
        void const **ppData);           // Start of data

    static HRESULT SafeGetData(
        BYTE const  *pDataSource,       // First byte of length.
        BYTE const  *pDataSourceEnd,    // End of valid source data memory
        ULONG       *pcbData,           // Length of data
        BYTE const **ppData)            // Start of data
    {
        return SafeGetData(
            reinterpret_cast<void const *>(pDataSource),
            reinterpret_cast<void const *>(pDataSourceEnd),
            pcbData,
            reinterpret_cast<void const **>(ppData));
    }

    // This is the same as GetData above except it takes a byte count instead
    // of pointer to determine the source data length.
    static HRESULT SafeGetData(         // S_OK, or error
        void const  *pDataSource,       // First byte of data
        ULONG        cbDataSource,      // Count of valid bytes in data source
        ULONG       *pcbData,           // Length of data
        void const **ppData);           // Start of data

    static HRESULT SafeGetData(
        BYTE const  *pDataSource,       // First byte of length.
        ULONG        cbDataSource,      // Count of valid bytes in data source
        ULONG       *pcbData,           // Length of data
        BYTE const **ppData)            // Start of data
    {
        return SafeGetData(
            reinterpret_cast<void const *>(pDataSource),
            cbDataSource,
            pcbData,
            reinterpret_cast<void const **>(ppData));
    }
};


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
