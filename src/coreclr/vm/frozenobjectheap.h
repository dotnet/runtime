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
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize,
        void(*initFunc)(Object*,void*) = nullptr, void* pParam = nullptr);

private:
    Crst m_Crst;
    Crst m_SegmentRegistrationCrst;
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
    void RegisterOrUpdate(uint8_t* current, size_t sizeCommited);

private:
    Object* GetFirstObject() const;
    Object* GetNextObject(Object* obj) const;

    // Start of the reserved memory, the first object starts at "m_pStart + sizeof(ObjHeader)" (its pMT)
    uint8_t* m_pStart;

    // NOTE: To handle potential race conditions, only m_[x]Registered fields should be accessed
    // externally as they guarantee that GC is aware of the current state of the segment.

    // Pointer to the end of the current segment, ready to be used as a pMT for a new object
    // meaning that "m_pCurrent - sizeof(ObjHeader)" is the actual start of the new object (header).
    //
    // m_pCurrent <= m_SizeCommitted
    uint8_t* m_pCurrent;

    // Last known value of m_pCurrent that GC is aware of.
    //
    // m_pCurrentRegistered <= m_pCurrent
    uint8_t* m_pCurrentRegistered;

    // Memory committed in the current segment
    //
    // m_SizeCommitted <= m_pStart + FOH_SIZE_RESERVED
    size_t m_SizeCommitted;

    // Total memory reserved for the current segment
    size_t m_Size;

    segment_handle m_SegmentHandle;

    friend class ProfilerObjectEnum;
    friend class ProfToEEInterfaceImpl;
    friend class FrozenObjectHeapManager;
};

#endif // _FROZENOBJECTHEAP_H

