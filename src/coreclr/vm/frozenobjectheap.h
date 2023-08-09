// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#include "gcinterface.h"
#include <sarray.h>

// FrozenObjectHeapManager provides a simple API to allocate objects on GC's Frozen Segments, it can be used as 
// an optimization to put certain types of objects there and rely on them to be effectively pinned so, for instance, 
// jit can bake direct addresses of them in codegen and avoid extra indirect loads.
//
// Example: a string literal allocated on a normal heap looks like this in JIT for x64:
//
//  mov      rax, 0xD1FFAB1E ; pinned handle
//  mov      rax, gword ptr [rax] ; actual string object
//
// and here is the same literal but allocated on a frozen segment:
//
//  mov      rax, 0xD1FFAB1E ; actual string object
//

class FrozenObjectSegment;

class FrozenObjectHeapManager
{
public:
    FrozenObjectHeapManager();
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize, bool publish = true);

private:
    Crst m_Crst;
    SArray<FrozenObjectSegment*> m_FrozenSegments;
    FrozenObjectSegment* m_CurrentSegment;

    friend class ProfilerObjectEnum;
    friend class ProfToEEInterfaceImpl;
};

class FrozenObjectSegment
{
public:
    FrozenObjectSegment(size_t sizeHint);
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize);
    size_t GetSize() const
    {
        return m_Size;
    }

private:
    Object* GetFirstObject() const;
    Object* GetNextObject(Object* obj) const;

    // Start of the reserved memory, the first object starts at "m_pStart + sizeof(ObjHeader)" (its pMT)
    uint8_t* m_pStart;

    // Pointer to the end of the current segment, ready to be used as a pMT for a new object
    // meaning that "m_pCurrent - sizeof(ObjHeader)" is the actual start of the new object (header).
    //
    // m_pCurrent <= m_SizeCommitted
    uint8_t* m_pCurrent;

    // Memory committed in the current segment
    //
    // m_SizeCommitted <= m_pStart + FOH_SIZE_RESERVED
    size_t m_SizeCommitted;

    // Total memory reserved for the current segment
    size_t m_Size;

    segment_handle m_SegmentHandle;
    INDEBUG(size_t m_ObjectsCount);

    friend class ProfilerObjectEnum;
    friend class ProfToEEInterfaceImpl;
};

#endif // _FROZENOBJECTHEAP_H

