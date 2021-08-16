// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __objectlist_h__
#define __objectlist_h__


#include "arraylist.h"
#include "holder.h"

#define INVALID_COMPRESSEDSTACK_INDEX ((DWORD)-1)
#ifdef _DEBUG
#define FREE_LIST_SIZE 128
#else
#define FREE_LIST_SIZE 1024
#endif



class ObjectList
{
public:
	class Iterator
    	{
		friend class ObjectList;

		  protected:
		   ArrayList::Iterator _iter;

		  public:

		PTR_VOID GetElement()
		{
			LIMITED_METHOD_CONTRACT;
			PTR_VOID ptr = _iter.GetElement();
			if (((DWORD)(size_t)(dac_cast<TADDR>(ptr)) & 0x1) == 0)
			{
				return ptr;
			}
			else
			{
				return NULL;
			}
		}

		DWORD GetIndex()
		{
			LIMITED_METHOD_CONTRACT;
			return _iter.GetIndex();
		}

		BOOL Next()
		{
			LIMITED_METHOD_CONTRACT;
			return _iter.Next();
		}
   	};

	ObjectList() DAC_EMPTY();

	DWORD AddToList( PVOID ptr );
	void RemoveFromList( PVOID ptr );
	void RemoveFromList( DWORD index, PVOID ptr );
	PVOID Get( DWORD index );

	ObjectList::Iterator Iterate()
	{
		LIMITED_METHOD_CONTRACT;
		ObjectList::Iterator i;
		i._iter = this->allEntries_.Iterate();
		return i;
	}

private:
    ArrayList allEntries_;
    DWORD freeIndexHead_;
    Crst listLock_;
};

class UnsynchronizedBlockAllocator
{
public:
    UnsynchronizedBlockAllocator( size_t blockSize );
    ~UnsynchronizedBlockAllocator( void );

    PVOID Allocate( size_t size );

private:
    ArrayList blockList_;

    size_t blockSize_;
    size_t offset_;
    DWORD index_;

};

#endif // __objectlist_h__
