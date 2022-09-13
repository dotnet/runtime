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
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize);

private:
    Crst m_Crst;
    SArray<FrozenObjectSegment*> m_FrozenSegments;
    FrozenObjectSegment* m_CurrentSegment;
    bool m_Enabled;
};

class FrozenObjectSegment
{
public:
    FrozenObjectSegment();
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize);

private:
    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    size_t m_SizeCommitted;
    segment_handle m_SegmentHandle;
    INDEBUG(size_t m_ObjectsCount);
};

#endif // _FROZENOBJECTHEAP_H

