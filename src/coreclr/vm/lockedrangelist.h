// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __LockedRangeList_h__
#define __LockedRangeList_h__

// -------------------------------------------------------
// This just wraps the RangeList methods in a read or
// write lock depending on the operation.
// -------------------------------------------------------

class LockedRangeList : public RangeList
{
  public:
    VPTR_VTABLE_CLASS(LockedRangeList, RangeList)

    LockedRangeList() : RangeList(), m_RangeListRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~LockedRangeList()
    {
        LIMITED_METHOD_CONTRACT;
    }

    BOOL IsInRangeWorker_Unlocked(TADDR address, TADDR *pID = NULL)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return RangeList::IsInRangeWorker(address, pID);
    }

  protected:

    virtual BOOL AddRangeWorker(const BYTE *start, const BYTE *end, void *id)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        return RangeList::AddRangeWorker(start,end,id);
    }

    virtual void RemoveRangesWorker(void *id, const BYTE *start = NULL, const BYTE *end = NULL)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        RangeList::RemoveRangesWorker(id,start,end);
    }

    virtual BOOL IsInRangeWorker(TADDR address, TADDR *pID = NULL)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        SimpleReadLockHolder lh(&m_RangeListRWLock);
        return RangeList::IsInRangeWorker(address, pID);
    }

    SimpleRWLock m_RangeListRWLock;
};

#endif // __LockedRangeList_h__
