// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We would like to allow "util" collection classes to be usable both
// from the VM and from the JIT.  The latter case presents a
// difficulty, because in the (x86, soon to be cross-platform) JIT
// compiler, we require allocation to be done using a "no-release"
// (aka, arena-style) allocator that is provided as methods of the
// JIT's Compiler type.

// To allow utilcode collection classes to deal with this, they may be
// written to do allocation and freeing via an instance of the
// "IAllocator" class defined in this file.  
// 
#ifndef _IALLOCATOR_DEFINED_
#define _IALLOCATOR_DEFINED_

#include "contract.h"
#include "safemath.h"

class IAllocator
{
  public:
    virtual void* Alloc(size_t sz) = 0;

    // Allocate space for an array of "elems" elements, each of size "elemSize".
    virtual void* ArrayAlloc(size_t elems, size_t elemSize) = 0;

    virtual void  Free(void* p) = 0;
};

// The "DefaultAllocator" class may be used by classes that wish to
// provide the flexibility of using an "IAllocator" may avoid writing
// conditionals at allocation sites about whether a non-default
// "IAllocator" has been provided: if none is, they can simply set the
// allocator to DefaultAllocator::Singleton().

class DefaultAllocator: public IAllocator
{
    static DefaultAllocator s_singleton;

  public:
    void* Alloc(size_t sz)
    {
        return ::operator new(sz);
    }

    void* ArrayAlloc(size_t elemSize, size_t numElems)
    {
        ClrSafeInt<size_t> safeElemSize(elemSize);
        ClrSafeInt<size_t> safeNumElems(numElems);
        ClrSafeInt<size_t> sz = safeElemSize * safeNumElems;
        if (sz.IsOverflow())
        {
            return NULL;
        }
        else
        {
            return ::operator new(sz.Value());
        }
    }

    virtual void Free(void * p)
    {
        ::operator delete(p);
    }

    static DefaultAllocator* Singleton()
    {
        return &s_singleton;
    }
};

// This class wraps an allocator that does not allow zero-length allocations,
// producing one that does (every zero-length allocation produces a pointer to the same 
// statically-allocated memory, and freeing that pointer is a no-op).
class AllowZeroAllocator: public IAllocator
{
    static int s_zeroLenAllocTarg;

    IAllocator* m_alloc;

public:
    AllowZeroAllocator(IAllocator* alloc) : m_alloc(alloc) {}

    void* Alloc(size_t sz)
    {
        if (sz == 0)
        { 
            return (void*)(&s_zeroLenAllocTarg);
        }
        else
        {
            return m_alloc->Alloc(sz);
        }
    }

    void* ArrayAlloc(size_t elemSize, size_t numElems)
    {
        if (elemSize == 0 || numElems == 0)
        { 
            return (void*)(&s_zeroLenAllocTarg);
        }
        else
        {
            return m_alloc->ArrayAlloc(elemSize, numElems);
        }
    }

    virtual void Free(void * p)
    {
        if (p != (void*)(&s_zeroLenAllocTarg))
        {
            m_alloc->Free(p);
        }
    }
};


// ProcessHeapAllocator: This class implements an IAllocator that allocates and frees memory
// from the process heap. This is similar to DefaultAllocator, but it can be used by the JIT
// which doesn't allow the use of operator new/delete.

class ProcessHeapAllocator: public IAllocator
{
    static ProcessHeapAllocator s_singleton;

public:

    virtual void* Alloc(size_t sz)
    {
        return ClrAllocInProcessHeap(0, S_SIZE_T(sz));
    }

    virtual void* ArrayAlloc(size_t elemSize, size_t numElems)
    {
        ClrSafeInt<size_t> safeElemSize(elemSize);
        ClrSafeInt<size_t> safeNumElems(numElems);
        ClrSafeInt<size_t> sz = safeElemSize * safeNumElems;
        if (sz.IsOverflow())
        {
            return nullptr;
        }
        else
        {
            return ClrAllocInProcessHeap(0, S_SIZE_T(sz));
        }
    }

    virtual void Free(void* p)
    {
        if (p != nullptr)
        {
            (void)ClrFreeInProcessHeap(0, p);
        }
    }

    static ProcessHeapAllocator* Singleton()
    {
        return &s_singleton;
    }
};

#endif // _IALLOCATOR_DEFINED_
        

