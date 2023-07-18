// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __COMReflectionCache_hpp__
#define __COMReflectionCache_hpp__

#include <stddef.h>
#include "class.h"
#include "threads.h"
#include "simplerwlock.hpp"

class ReflectClass;

template <class Element, class CacheType, int CacheSize> class ReflectionCache
: public SimpleRWLock
{
public:
    ReflectionCache()
        : SimpleRWLock(COOPERATIVE, LOCK_REFLECTCACHE)
    {
        WRAPPER_NO_CONTRACT;
        index = 0;
        currentStamp = 0;
    }

    void Init();

    BOOL GetFromCache(Element *pElement, CacheType& rv)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pElement));
        }
        CONTRACTL_END;

        this->EnterRead();

        rv = 0;
        int i = SlotInCache(pElement);
        BOOL fGotIt = (i != CacheSize);
        if (fGotIt)
        {
            rv = m_pResult[i].element.GetValue();
            m_pResult[i].stamp = InterlockedIncrement(&currentStamp);
        }
        this->LeaveRead();

        if (fGotIt)
            AdjustStamp(FALSE);

        return fGotIt;
    }

    void AddToCache(Element *pElement, CacheType obj)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pElement));
        }
        CONTRACTL_END;

        this->EnterWrite();
        currentStamp++; // no need to InterlockIncrement b/c write lock taken
        int i = SlotInCache(pElement);
        if (i == CacheSize)
        {
            int slot = index;
            // Not in cache.
            if (slot == CacheSize)
            {
                // Reuse a slot.
                slot = 0;
                LONG minStamp = m_pResult[0].stamp;
                for (i = 1; i < CacheSize; i ++)
                {
                    if (m_pResult[i].stamp < minStamp)
                    {
                        slot = i;
                        minStamp = m_pResult[i].stamp;
                    }
                }
            }
            else
                m_pResult[slot].element.InitValue();

            m_pResult[slot].element = *pElement;
            m_pResult[slot].element.SetValue(obj);
            m_pResult[slot].stamp = currentStamp;

            UpdateHashTable(pElement->GetHash(), slot);

            if (index < CacheSize)
                index++;
        }
        AdjustStamp(TRUE);
        this->LeaveWrite();
    }

private:
    // Lock must have been taken before calling this.
    int SlotInCache(Element *pElement)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(LockTaken());
            PRECONDITION(CheckPointer(pElement));
        }
        CONTRACTL_END;

        if (index == 0)
            return CacheSize;

        size_t hash = pElement->GetHash();

        int slot = m_pHashTable[hash%CacheSize].slot;
        if (slot == -1)
            return CacheSize;

        if (m_pResult[slot].element == *pElement)
            return slot;

        for (int i = 0; i < index; i ++)
        {
            if (i != slot && m_pHashTable[i].hash == hash)
            {
                if (m_pResult[i].element == *pElement)
                    return i;
            }
        }

        return CacheSize;
    }

    void AdjustStamp(BOOL hasWriterLock)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if ((currentStamp & 0x40000000) == 0)
            return;
        if (!hasWriterLock)
        {
            _ASSERTE (!LockTaken());
            this->EnterWrite();
        }
        else
            _ASSERTE (IsWriterLock());

        if (currentStamp & 0x40000000)
        {
            currentStamp >>= 1;
            for (int i = 0; i < index; i ++)
                m_pResult[i].stamp >>= 1;
        }
        if (!hasWriterLock)
            this->LeaveWrite();
    }

    void UpdateHashTable(SIZE_T hash, int slot)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsWriterLock());
        }
        CONTRACTL_END;

        m_pHashTable[slot].hash = hash;
        m_pHashTable[hash%CacheSize].slot = slot;
    }

    struct CacheTable
    {
        Element element;
        int stamp;
    } *m_pResult;

    struct HashTable
    {
        size_t hash;   // Record hash value for each slot
        int   slot;    // The slot corresponding to the hash.
    } *m_pHashTable;

    int index;
    LONG currentStamp;
};


#ifdef FEATURE_COMINTEROP

#define ReflectionMaxCachedNameLength 23
struct DispIDCacheElement
{
    MethodTable *pMT;
    int strNameLength;
    LCID lcid;
    DISPID DispId;
    WCHAR strName[ReflectionMaxCachedNameLength+1];
    DispIDCacheElement ()
        : pMT (NULL), strNameLength(0), lcid(0), DispId(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    BOOL operator==(const DispIDCacheElement& var) const
    {
        LIMITED_METHOD_CONTRACT;
        return (pMT == var.pMT && strNameLength == var.strNameLength
                && lcid == var.lcid && u16_strcmp (strName, var.strName) == 0);
    }

    DispIDCacheElement& operator= (const DispIDCacheElement& var)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (var.strNameLength <= ReflectionMaxCachedNameLength);
        pMT = var.pMT;
        strNameLength = var.strNameLength;
        lcid = var.lcid;
        wcscpy_s (strName, ARRAY_SIZE(strName), var.strName);
        return *this;
    }

    DISPID GetValue ()
    {
        LIMITED_METHOD_CONTRACT;
        return DispId;
    }

    void InitValue ()
    {
        LIMITED_METHOD_CONTRACT;
    }

    void SetValue (DISPID Id)
    {
        LIMITED_METHOD_CONTRACT;
        DispId = Id;
    }

    size_t GetHash ()
    {
        LIMITED_METHOD_CONTRACT;
        return (size_t)pMT + strNameLength + (lcid << 4);
    }
};

typedef ReflectionCache <DispIDCacheElement, DISPID, 128> DispIDCache;

#endif // FEATURE_COMINTEROP

#endif // __COMReflectionCache_hpp__
