// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MemoryRange.h
//
// defines the code:MemoryRange class.
//*****************************************************************************

#ifndef _memory_range_h
#define _memory_range_h

#include "daccess.h"

// MemoryRange is a descriptor of a memory range. This groups (pointer + size).
// 
// Some key qualities:
// - simple!
// - Not mutable
// - blitabble descriptor which can be useful for out-of-process tools like the debugger.
// - no ownership semantics. 
// - no manipulation, growing semantics. 
// - no memory marshalling, allocation, copying. etc.
// - can be efficiently passed / copied / returned by value 
// 
// This class has general value as an abstraction to group pointer and size together. It also has significant
// value to the debugger. An expected design pattern is that other mutable complex data structures (eg,
// code:SBuffer, code:CGrowableStream) will provide an accessor to expose their underlying storage as a
// MemoryRange to debugger. This mirrors the Debugger's code:TargetBuffer data structure, but as a 
// general-purpose VM utility versus a debugger right-side data structure.  

// 
class MemoryRange
{
public:
    // Constructor to create a memory range around a (start address, size) pair.
    MemoryRange() :
        m_pStartAddress(NULL),
        m_cbBytes(0)
    { 
        SUPPORTS_DAC;
    }

    MemoryRange(PTR_VOID pStartAddress, SIZE_T cbBytes) : 
        m_pStartAddress(pStartAddress),
        m_cbBytes(cbBytes)
    { 
        SUPPORTS_DAC;
    }

    // Note: use compiler-default copy ctor and assignment operator 



    // Check whether a pointer is in the memory range represented by this instance.
    BOOL IsInRange(PTR_VOID pAddress) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (dac_cast<TADDR>(pAddress) - dac_cast<TADDR>(m_pStartAddress)) < m_cbBytes;
    }

    // Check whether a pointer is in the memory range represented by this instance.
    BOOL IsInRange(TADDR pAddress) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (pAddress - dac_cast<TADDR>(m_pStartAddress)) < m_cbBytes;
    }

    // Get the starting address.
    PTR_VOID StartAddress() const
    {
        SUPPORTS_DAC;
        return m_pStartAddress;
    }

    // Get the size of the range in bytes
    SIZE_T Size() const
    {
        SUPPORTS_DAC;
        return m_cbBytes;
    }

private:    
    // The start of the memory range.
    PTR_VOID const m_pStartAddress;

    // The size of the memory range in bytes.
    // This is s SIZE_T so that it can describe any memory range in the process (for example, larger than 4gb on 64-bit machines)
    const SIZE_T        m_cbBytes;

};

typedef ArrayDPTR(MemoryRange) ARRAY_PTR_MemoryRange;

#endif // _memory_range_h

