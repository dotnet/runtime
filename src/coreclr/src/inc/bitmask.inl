// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// --------------------------------------------------------------------------------
// BitMask.inl
// --------------------------------------------------------------------------------

#include <bitmask.h>

#ifndef _BITMASK_INL_
#define _BITMASK_INL_

inline BOOL BitMask::IsArray()
{
    LIMITED_METHOD_CONTRACT;
    return (m_mask&1) == 0;
}

// Indexing computations
inline COUNT_T BitMask::BitToIndex(int bit)
{
    LIMITED_METHOD_CONTRACT;
    // First word has one less bit due to tag
    return (bit+1) >> BIT_SIZE_SHIFT;
}

inline COUNT_T BitMask::BitToShift(int bit)
{
    LIMITED_METHOD_CONTRACT;
    // First word has one less bit due to tag
    return (bit+1) & BIT_SIZE_MASK;
}

// Array access.  Note the first array element is the count of the 
// rest of the elements

inline COUNT_T *BitMask::GetMaskArray()
{
    LIMITED_METHOD_CONTRACT;
    if (IsArray())
    {
        CONSISTENCY_CHECK(CheckPointer(m_maskArray));
        return m_maskArray+1;
    }
    else
        return &m_mask;
}
    
inline COUNT_T BitMask::GetMaskArraySize()
{
    LIMITED_METHOD_CONTRACT;
    if (IsArray())
        return *m_maskArray;
    else
        return 1;
}

inline void BitMask::GrowArray(COUNT_T newSize)
{
    CONTRACTL
    {
          THROWS;
    }
    CONTRACTL_END;

    // Ensure we don't grow too often

    COUNT_T oldSize = GetMaskArraySize();
    if (newSize <= oldSize)
        return;

    if (newSize < oldSize*2)
        newSize = oldSize*2;
    if (newSize < MIN_ARRAY_ALLOCATION)
        newSize = MIN_ARRAY_ALLOCATION;

    // Allocate new array

    COUNT_T *newArray = new COUNT_T [newSize+1];
    *newArray = newSize;
        
    CopyMemory(newArray+1, GetMaskArray(), oldSize * sizeof(COUNT_T));
    ZeroMemory(newArray+oldSize+1, (newSize - oldSize) * sizeof(COUNT_T));

    if (IsArray())
        delete [] m_maskArray;

    m_maskArray = newArray;
}
    
inline BitMask::BitMask()
  : m_mask(1)
{
    LIMITED_METHOD_CONTRACT;
}

inline BitMask::~BitMask()
{
    LIMITED_METHOD_CONTRACT;

    if (IsArray())
        delete [] m_maskArray;
}

inline BOOL BitMask::TestBit(int bit)
{
    LIMITED_METHOD_CONTRACT;

    COUNT_T index = BitToIndex(bit);

    if (index >= GetMaskArraySize())
        return FALSE;

    return ( GetMaskArray()[index] >> BitToShift(bit) ) & 1;
}

inline void BitMask::SetBit(int bit)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    COUNT_T index = BitToIndex(bit);

    if (index >= GetMaskArraySize())
        GrowArray(index+1);
            
    GetMaskArray()[index] |= (1 << BitToShift(bit));
}

inline void BitMask::ClearBit(int bit)
{
    LIMITED_METHOD_CONTRACT;

    COUNT_T index = BitToIndex(bit);

    if (index >= GetMaskArraySize())
        return;
            
    GetMaskArray()[index] &= ~(1 << BitToShift(bit));
}

inline BOOL BitMask::TestAnyBit()
{
    LIMITED_METHOD_CONTRACT;

    if (IsArray())
    {
        COUNT_T *mask = m_maskArray+1;
        COUNT_T *maskEnd = mask + m_maskArray[0];

        while (mask < maskEnd)
        {
            if (*mask != 0)
                return TRUE;
            mask++;
        }

        return FALSE;
    }
    else
        return m_mask != (COUNT_T) 1;
}

inline void BitMask::ClearAllBits()
{
    LIMITED_METHOD_CONTRACT;

    if (IsArray())
        delete [] m_maskArray;

    m_mask = 1;
}

inline size_t BitMask::GetAllocatedBlockOffset()
{
    LIMITED_METHOD_CONTRACT;

    return offsetof(BitMask, m_maskArray);
}

inline void *BitMask::GetAllocatedBlock()
{
    LIMITED_METHOD_CONTRACT;

	if (IsArray())
        return m_maskArray;
    else
        return NULL;
}

inline COUNT_T BitMask::GetAllocatedBlockSize()
{
    LIMITED_METHOD_CONTRACT;

	if (IsArray())
        return (GetMaskArraySize()+1) * sizeof(COUNT_T);
    else
        return 0;
}

/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////

inline SynchronizedBitMask::SynchronizedBitMask()
  : m_bitMaskLock(PREEMPTIVE, LOCK_TYPE_DEFAULT)
{
    LIMITED_METHOD_CONTRACT;
}

inline BOOL SynchronizedBitMask::TestBit(int bit)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleReadLockHolder holder(&m_bitMaskLock);

    return m_bitMask.TestBit(bit);
}

inline void SynchronizedBitMask::SetBit(int bit)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleWriteLockHolder holder(&m_bitMaskLock);

    m_bitMask.SetBit(bit);
}

inline void SynchronizedBitMask::ClearBit(int bit)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleWriteLockHolder holder(&m_bitMaskLock);

    m_bitMask.ClearBit(bit);
}

inline BOOL SynchronizedBitMask::TestAnyBit()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleReadLockHolder holder(&m_bitMaskLock);

    return m_bitMask.TestAnyBit();
}

inline void SynchronizedBitMask::ClearAllBits()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleWriteLockHolder holder(&m_bitMaskLock);

    m_bitMask.ClearAllBits();
}

#endif // _BITMASK_INL_

