// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// --------------------------------------------------------------------------------
// BitMask.h
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// BitMask is an arbitrarily large sized bitfield which has optimal storage 
// for 32 bits or less.  
// Storage is proportional to the highest index which is set. 
// --------------------------------------------------------------------------------


#include <clrtypes.h>

#ifndef _BITMASK_H_
#define _BITMASK_H_

class BitMask
{
 public:

    BitMask();
    ~BitMask();

    BOOL TestBit(int bit);
    void SetBit(int bit);
    void ClearBit(int bit);

    // returns true if any bit is set
    BOOL TestAnyBit();

    void ClearAllBits();

    // Allocation exposed for ngen save/fixup
    size_t GetAllocatedBlockOffset();
    void *GetAllocatedBlock();
    COUNT_T GetAllocatedBlockSize();

 private:

    static const int BIT_SIZE_SHIFT = 5;
    static const int BIT_SIZE = (1<<BIT_SIZE_SHIFT);
    static const int BIT_SIZE_MASK = BIT_SIZE-1;

    static const COUNT_T MIN_ARRAY_ALLOCATION = 3;

    // The first bit is used to indicate whether we've got a flat mask or
    // an array of mask elements
    BOOL IsArray();

    // Indexing computations
    COUNT_T BitToIndex(int bit);
    COUNT_T BitToShift(int bit);

    // Generic mask array access.  Works for either case (array or non-array).
    COUNT_T *GetMaskArray();
    COUNT_T GetMaskArraySize();

    // Need more bits...
    void GrowArray(COUNT_T newSize);
    
    union
    {
        COUNT_T     m_mask;
        COUNT_T     *m_maskArray; // first array element is size of rest of array
    };
};

// provides a wrapper around the BitMask class providing synchronized reads/writes safe for multithreaded access.
// I've only added the public methods that were required by Module which needs a thread-safe BitMask.  add others as required.
class SynchronizedBitMask
{
 public:
    // Allow Module access so we can use Offsetof on this class's private members during native image creation (determinism)
    friend class Module;
    SynchronizedBitMask();
    ~SynchronizedBitMask() {}

    BOOL TestBit(int bit);
    void SetBit(int bit);
    void ClearBit(int bit);

    BOOL TestAnyBit();

    void ClearAllBits();

 private:

    BitMask m_bitMask;

    // note that this lock (at present) doesn't support promotion from reader->writer so be very careful
    // when taking this lock else you might deadlock your own thread!
    SimpleRWLock m_bitMaskLock;
};

#include <bitmask.inl>

#endif // _BITMASK_H_
