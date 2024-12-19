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

    BOOL IsInRangeWorker_Unlocked(TADDR address)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return RangeList::IsInRangeWorker(address);
    }

    template<class F>
    void ForEachInRangeWorker_Unlocked(TADDR address, F func) const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return RangeList::ForEachInRangeWorker(address, func);
    }

  protected:

    virtual BOOL AddRangeWorker(const BYTE *start, const BYTE *end, void *id)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        return RangeList::AddRangeWorker(start,end,id);
    }

    virtual void RemoveRangesWorker(void *id)
    {
        WRAPPER_NO_CONTRACT;
        SimpleWriteLockHolder lh(&m_RangeListRWLock);
        RangeList::RemoveRangesWorker(id);
    }

    virtual BOOL IsInRangeWorker(TADDR address)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        SimpleReadLockHolder lh(&m_RangeListRWLock);
        return RangeList::IsInRangeWorker(address);
    }

    SimpleRWLock m_RangeListRWLock;
};

#endif // __LockedRangeList_h__
