#ifndef _THREADLOCALPOOLALLOCATOR_H_
#define _THREADLOCALPOOLALLOCATOR_H_

// Some mono embed APIs allocate an iterator object to iterate over attributes or fields.
// Usually these are short-lived, and only used from one thread, one at a time. But some
// of these are used frequently enough for these allocations to become a performance bottleneck.
// So we build a custom, per-thread pool allocator, which can keep `size` objects alive for reuse.
// This requires that the object will be freed from the same thread it is used in.
template<class T, int size>
class ThreadLocalPoolAllocator
{
    T* freeList[size];
    int numFree;
public:
    ThreadLocalPoolAllocator()
    {
        numFree = 0;
    }

    T* Alloc()
    {
        if (numFree > 0)
            return freeList[--numFree];
        return new T();
    }

    void Free(T* t)
    {
        if (numFree < size)
            freeList[numFree++] = t;
        else
            delete t;
    }
};

#endif
