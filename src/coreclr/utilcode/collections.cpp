// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// Collections.cpp
//
// This contains Collections C++ utility classes.
//*****************************************************************************

#include "stdafx.h"
#include "utilcode.h"
#include "ex.h"

//
//
// CClosedHashBase
//
//

//*****************************************************************************
// Delete the given value.  This will simply mark the entry as deleted (in
// order to keep the collision chain intact).  There is an optimization that
// consecutive deleted entries leading up to a free entry are themselves freed
// to reduce collisions later on.
//*****************************************************************************
void CClosedHashBase::Delete(
    void        *pData)                 // Key value to delete.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    BYTE        *ptr;

    // Find the item to delete.
    if ((ptr = Find(pData)) == 0)
    {
        // You deleted something that wasn't there, why?
        _ASSERTE(0);
        return;
    }

    // For a perfect system, there are no collisions so it is free.
    if (m_bPerfect)
    {
        SetStatus(ptr, FREE);

        // One less non free entry.
        --m_iCount;

        return;
    }

    // Mark this entry deleted.
    SetStatus(ptr, DELETED);

    // If the next item is free, then we can go backwards freeing
    // deleted entries which are no longer part of a chain.  This isn't
    // 100% great, but it will reduce collisions.
    BYTE        *pnext;
    if ((pnext = ptr + m_iEntrySize) > EntryPtr(m_iSize - 1))
        pnext = &m_rgData[0];
    if (Status(pnext) != FREE)
        return;

    // We can now free consecutive entries starting with the one
    // we just deleted, up to the first non-deleted one.
    while (Status(ptr) == DELETED)
    {
        // Free this entry.
        SetStatus(ptr, FREE);

        // One less non free entry.
        --m_iCount;

        // Check the one before it, handle wrap around.
        if ((ptr -= m_iEntrySize) < &m_rgData[0])
            ptr = EntryPtr(m_iSize - 1);
    }
}

//*****************************************************************************
// Lookup a key value and return a pointer to the element if found.
//*****************************************************************************
BYTE *CClosedHashBase::Find(            // The item if found, 0 if not.
    void        *pData)                 // The key to lookup.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    unsigned int iHash;                // Hash value for this data.
    int         iBucket;                // Which bucke to start at.
    int         i;                      // Loop control.

    // Safety check.
    if (!m_rgData || m_iCount == 0)
        return (0);

    // Hash to the bucket.
    iHash = Hash(pData);
    iBucket = iHash % m_iBuckets;

    // For a perfect table, the bucket is the correct one.
    if (m_bPerfect)
    {
        // If the value is there, it is the correct one.
        if (Status(EntryPtr(iBucket)) != FREE)
            return (EntryPtr(iBucket));
        return (0);
    }

    // Walk the bucket list looking for the item.
    for (i=iBucket;  Status(EntryPtr(i)) != FREE;  )
    {
        // Don't look at deleted items.
        if (Status(EntryPtr(i)) == DELETED)
        {
            // Handle wrap around.
            if (++i >= m_iSize)
                i = 0;
            continue;
        }

        // Check this one.
        if (Compare(pData, EntryPtr(i)) == 0)
            return (EntryPtr(i));

        // If we never collided while adding items, then there is
        // no point in scanning any further.
        if (!m_iCollisions)
            return (0);

        // Handle wrap around.
        if (++i >= m_iSize)
            i = 0;
    }
    return (0);
}



//*****************************************************************************
// Look for an item in the table.  If it isn't found, then create a new one and
// return that.
//*****************************************************************************
BYTE *CClosedHashBase::FindOrAdd(       // The item if found, 0 if not.
    void        *pData,                 // The key to lookup.
    bool        &bNew)                  // true if created.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    unsigned int iHash;                // Hash value for this data.
    int         iBucket;                // Which bucke to start at.
    int         i;                      // Loop control.

    // If we haven't allocated any memory, or it is too small, fix it.
    if (!m_rgData || ((m_iCount + 1) > (m_iSize * 3 / 4) && !m_bPerfect))
    {
        if (!ReHash())
            return (0);
    }

    // Assume we find it.
    bNew = false;

    // Hash to the bucket.
    iHash = Hash(pData);
    iBucket = iHash % m_iBuckets;

    // For a perfect table, the bucket is the correct one.
    if (m_bPerfect)
    {
        // If the value is there, it is the correct one.
        if (Status(EntryPtr(iBucket)) != FREE)
            return (EntryPtr(iBucket));
        i = iBucket;
    }
    else
    {
        // Walk the bucket list looking for the item.
        for (i=iBucket;  Status(EntryPtr(i)) != FREE;  )
        {
            // Don't look at deleted items.
            if (Status(EntryPtr(i)) == DELETED)
            {
                // Handle wrap around.
                if (++i >= m_iSize)
                    i = 0;
                continue;
            }

            // Check this one.
            if (Compare(pData, EntryPtr(i)) == 0)
                return (EntryPtr(i));

            // One more to count.
            ++m_iCollisions;

            // Handle wrap around.
            if (++i >= m_iSize)
                i = 0;
        }
    }

    // We've found an open slot, use it.
    _ASSERTE(Status(EntryPtr(i)) == FREE);
    bNew = true;
    ++m_iCount;
    return (EntryPtr(i));
}

//*****************************************************************************
// This helper actually does the add for you.
//*****************************************************************************
BYTE *CClosedHashBase::DoAdd(void *pData, BYTE *rgData, int &iBuckets, int iSize,
            int &iCollisions, int &iCount)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    unsigned int iHash;                // Hash value for this data.
    int         iBucket;                // Which bucke to start at.
    int         i;                      // Loop control.

    // Hash to the bucket.
    iHash = Hash(pData);
    iBucket = iHash % iBuckets;

    // For a perfect table, the bucket is free.
    if (m_bPerfect)
    {
        i = iBucket;
        _ASSERTE(Status(EntryPtr(i, rgData)) == FREE);
    }
    // Need to scan.
    else
    {
        // Walk the bucket list looking for a slot.
        for (i=iBucket;  Status(EntryPtr(i, rgData)) != FREE;  )
        {
            // Handle wrap around.
            if (++i >= iSize)
                i = 0;

            // If we made it this far, we collided.
            ++iCollisions;
        }
    }

    // One more item in list.
    ++iCount;

    // Return the new slot for the caller.
    return (EntryPtr(i, rgData));
}

//*****************************************************************************
// This function is called either to init the table in the first place, or
// to rehash the table if we ran out of room.
//*****************************************************************************
bool CClosedHashBase::ReHash()          // true if successful.
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    // Allocate memory if we don't have any.
    if (!m_rgData)
    {
        if ((m_rgData = new (nothrow) BYTE [m_iSize * m_iEntrySize]) == 0)
            return (false);
        InitFree(&m_rgData[0], m_iSize);
        return (true);
    }

    // We have entries already, allocate a new table.
    BYTE        *rgTemp, *p;
    int         iBuckets = m_iBuckets * 2 - 1;
    int         iSize = iBuckets + 7;
    int         iCollisions = 0;
    int         iCount = 0;

    if ((rgTemp = new (nothrow) BYTE [iSize * m_iEntrySize]) == 0)
        return (false);
    InitFree(&rgTemp[0], iSize);
    m_bPerfect = false;

    // Rehash the data.
    for (int i=0;  i<m_iSize;  i++)
    {
        // Only copy used entries.
        if (Status(EntryPtr(i)) != USED)
            continue;

        // Add this entry to the list again.
        VERIFY((p = DoAdd(GetKey(EntryPtr(i)), rgTemp, iBuckets,
                iSize, iCollisions, iCount)));
        memmove(p, EntryPtr(i), m_iEntrySize);
    }

    // Reset internals.
    delete [] m_rgData;
    m_rgData = rgTemp;
    m_iBuckets = iBuckets;
    m_iSize = iSize;
    m_iCollisions = iCollisions;
    m_iCount = iCount;
    return (true);
}


//
//
// CStructArray
//
//


//*****************************************************************************
// Returns a pointer to the (iIndex)th element of the array, shifts the elements
// in the array if the location is already full. The iIndex cannot exceed the count
// of elements in the array.
//*****************************************************************************
void *CStructArray::InsertThrowing(
    int         iIndex)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(iIndex >= 0);

    // We can not insert an element further than the end of the array.
    if (iIndex > m_iCount)
        return (NULL);

    // The array should grow, if we can't fit one more element into the array.
    Grow(1);

    // The pointer to be returned.
    BYTE *pcList = m_pList + iIndex * m_iElemSize;

    // See if we need to slide anything down.
    if (iIndex < m_iCount)
        memmove(pcList + m_iElemSize, pcList, (m_iCount - iIndex) * m_iElemSize);
    ++m_iCount;
    return(pcList);
}

//*****************************************************************************
// Non-throwing variant
//*****************************************************************************
void *CStructArray::Insert(int iIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void *result = NULL;
    EX_TRY
    {
        result = InsertThrowing(iIndex);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return result;
}


//*****************************************************************************
// Allocate a new element at the end of the dynamic array and return a pointer
// to it.
//*****************************************************************************
void *CStructArray::AppendThrowing()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The array should grow, if we can't fit one more element into the array.
    Grow(1);

    return (m_pList + m_iCount++ * m_iElemSize);
}


//*****************************************************************************
// Non-throwing variant
//*****************************************************************************
void *CStructArray::Append()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void *result = NULL;
    EX_TRY
    {
        result = AppendThrowing();
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return result;
}


//*****************************************************************************
// Allocate enough memory to have at least iCount items.  This is a one shot
// check for a block of items, instead of requiring singleton inserts.  It also
// avoids realloc headaches caused by growth, since you can do the whole block
// in one piece of code.  If the array is already large enough, this is a no-op.
//*****************************************************************************
void CStructArray::AllocateBlockThrowing(int iCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_iSize < m_iCount+iCount)
        Grow(iCount);
    m_iCount += iCount;
}

//*****************************************************************************
// Non-throwing variant
//*****************************************************************************
int CStructArray::AllocateBlock(int iCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    int result = FALSE;
    EX_TRY
    {
        AllocateBlockThrowing(iCount);
        result = TRUE;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return result;
}


//*****************************************************************************
// Deletes the specified element from the array.
//*****************************************************************************
void CStructArray::Delete(
    int         iIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(iIndex >= 0);

    // See if we need to slide anything down.
    if (iIndex < --m_iCount)
    {
        BYTE *pcList = m_pList + iIndex * m_iElemSize;
        memmove(pcList, pcList + m_iElemSize, (m_iCount - iIndex) * m_iElemSize);
    }
}


//*****************************************************************************
// Grow the array if it is not possible to fit iCount number of new elements.
//*****************************************************************************
void CStructArray::Grow(
    int         iCount)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    BYTE        *pTemp;                 // temporary pointer used in realloc.
    int         iGrow;

    if (m_iSize < m_iCount+iCount)
    {
        if (m_pList == NULL)
        {
            iGrow = max(m_iGrowInc, iCount);

            //<TODO>@todo this is a workaround because I don't want to do resize right now.</TODO>
            //check added to make PREFAST happy
            S_SIZE_T newSize = S_SIZE_T(iGrow) * S_SIZE_T(m_iElemSize);
            if(newSize.IsOverflow())
                ThrowOutOfMemory();
            else
            {
                m_pList = new BYTE[newSize.Value()];
                m_iSize = iGrow;
                m_bFree = true;
            }
        }
        else
        {
            // Adjust grow size as a ratio to avoid too many reallocs.
            if (m_iSize / m_iGrowInc >= 3)
            {   // Don't overflow and go negative.
                int newinc = m_iGrowInc * 2;
                if (newinc > m_iGrowInc)
                    m_iGrowInc = newinc;
            }

            iGrow = max(m_iGrowInc, iCount);

            // try to allocate memory for reallocation.
            S_SIZE_T allocSize = (S_SIZE_T(m_iSize) + S_SIZE_T(iGrow)) * S_SIZE_T(m_iElemSize);
            S_SIZE_T copyBytes = S_SIZE_T(m_iSize) * S_SIZE_T(m_iElemSize);
            if(allocSize.IsOverflow() || copyBytes.IsOverflow())
                ThrowOutOfMemory();
            if (m_bFree)
            {   // We already own memory.
                pTemp = new BYTE[allocSize.Value()];
                memcpy (pTemp, m_pList, copyBytes.Value());
                delete [] m_pList;
            }
            else
            {   // We don't own memory; get our own.
                pTemp = new BYTE[allocSize.Value()];
                memcpy(pTemp, m_pList, copyBytes.Value());
                m_bFree = true;
            }
            m_pList = pTemp;
            m_iSize += iGrow;
        }
    }
}


//*****************************************************************************
// Free the memory for this item.
//*****************************************************************************
void CStructArray::Clear() noexcept
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Free the chunk of memory.
    if (m_bFree && m_pList != NULL)
        delete [] m_pList;

    m_pList = NULL;
    m_iSize = 0;
    m_iCount = 0;
}
