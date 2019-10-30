// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _DEFAULTALLOCATOR_H_
#define _DEFAULTALLOCATOR_H_

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

#endif // _DEFAULTALLOCATOR_H_
