// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _MEMORYPOOL_
#define _MEMORYPOOL_

#include "daccess.h"
#include "contract.h"

//
// A MemoryPool is an allocator for a fixed size elements.
// Allocating and freeing elements from the pool is very cheap compared
// to a general allocator like new.  However, a MemoryPool is slightly
// more greedy - it preallocates a bunch of elements at a time, and NEVER
// RELEASES MEMORY FROM THE POOL ONCE IT IS ALLOCATED, (unless you call
// FreeAllElements.)
//
// It also has several additional features:
//	* you can free the entire pool of objects cheaply.
//	* you can test an object to see if it's an element of the pool.
//

class MemoryPool
{
  public:

#ifndef DACCESS_COMPILE
	MemoryPool(SIZE_T elementSize, SIZE_T initGrowth = 20, SIZE_T initCount = 0);
#else
        MemoryPool() {}
#endif
	~MemoryPool() DAC_EMPTY();

	BOOL IsElement(void *element);
	BOOL IsAllocatedElement(void *element);
	void *AllocateElement();
	void *AllocateElementNoThrow();
	void FreeElement(void *element);
	void FreeAllElements();
        size_t GetSize();
  private:

	struct Element
	{
		Element *next;
#if _DEBUG
		int		deadBeef;
#endif
	};

	struct Block
	{
		Block	*next;
		Element *elementsEnd;
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4200)
#endif
		Element elements[0];
#ifdef _MSC_VER
#pragma warning(pop)
#endif
	};

	SIZE_T m_elementSize;
	SIZE_T m_growCount;
	Block *m_blocks;
	Element *m_freeList;

	BOOL AddBlock(SIZE_T elementCount);
	void DeadBeef(Element *element);

 public:

	//
	// NOTE: You can currently only iterate the elements
	// if none have been freed.
	//

	class Iterator
    {
	private:
		Block	*m_next;
		BYTE	*m_e, *m_eEnd;
		BYTE	*m_end;
		SIZE_T	m_size;

	public:
		Iterator(MemoryPool *pool);

		BOOL Next();

		void *GetElement() {LIMITED_METHOD_CONTRACT;  return (void *) (m_e-m_size); }
	};

	friend class Iterator;
};

class MemoryPoolElementHolder
{
    protected:
        MemoryPool* m_pool;
        void* m_element;
        BOOL bRelease;
    public:
    void SuppressRelease()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(bRelease);
        bRelease=false;
    }
    void Release()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(bRelease);
        m_pool->FreeElement(m_element);
        bRelease=false;
    }
    MemoryPoolElementHolder(MemoryPool* pool, void* element)
    {
        LIMITED_METHOD_CONTRACT;
        m_pool=pool;
        m_element=element;
        bRelease=true;
    }

    ~MemoryPoolElementHolder()
    {
        LIMITED_METHOD_CONTRACT;
        if (bRelease)
            Release();
    }

    operator void* ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_element;
    }
};

#endif // _MEMORYPOOL_
