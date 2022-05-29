// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "objectlist.h"

#ifndef DACCESS_COMPILE

ObjectList::ObjectList( void )
: freeIndexHead_( INVALID_COMPRESSEDSTACK_INDEX ),
  listLock_( CrstObjectList, CrstFlags(CRST_UNSAFE_SAMELEVEL | CRST_UNSAFE_ANYMODE) )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;
}

#endif

#define MAX_LOOP 2

DWORD
ObjectList::AddToList( PVOID ptr )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(ptr));
    } CONTRACTL_END;


	//  sanity check that the pointer low bit is not set
	 _ASSERTE(  (((DWORD)(size_t)ptr & 0x1) == 0) && "Invalid pointer" );

    DWORD retval = INVALID_COMPRESSEDSTACK_INDEX;

    CrstHolder ch( &listLock_ );

    // If there is an entry in the free list, simply use it.

    if (this->freeIndexHead_ != INVALID_COMPRESSEDSTACK_INDEX)
    {
        _ASSERTE( this->listLock_.OwnedByCurrentThread() );

	// grab the head of the list
	retval = (this->freeIndexHead_ >> 1);

	DWORD nextFreeIndex = (DWORD)(size_t)this->allEntries_.Get( retval );

	// index in use,  pointer values have low bit as 0
	_ASSERTE(  ((nextFreeIndex & 0x01) == 1) && "The free list points to an index that is in use" );
	 // update the head of the list with the next free index stored in the array list
	this->freeIndexHead_ = nextFreeIndex;

	// store the pointer
	this->allEntries_.Set( retval, ptr);
    }
    // Otherwise we place this new entry at that end of the list.
    else
    {
        _ASSERTE( this->listLock_.OwnedByCurrentThread() );
        retval = this->allEntries_.GetCount();
        IfFailThrow(this->allEntries_.Append(ptr));
    }

    _ASSERTE( retval != INVALID_COMPRESSEDSTACK_INDEX );

    return retval;
}

void
ObjectList::RemoveFromList( PVOID ptr )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ptr));
    } CONTRACTL_END;

	//  sanity check that the pointer low bit is not set
	 _ASSERTE(  (((DWORD)(size_t)ptr & 0x1) == 0) && "Invalid pointer" );

    DWORD index = INVALID_COMPRESSEDSTACK_INDEX;

    CrstHolder ch( &listLock_ );

    ObjectList::Iterator iter = Iterate();

    while (iter.Next())
    {
        if (iter.GetElement() == ptr)
        {
            index = iter.GetIndex();
            break;
        }
    }

    if (index == INVALID_COMPRESSEDSTACK_INDEX)
    {
        _ASSERTE( FALSE && "Unable to find object" );
    }
    else
    {
	// add the index to the free list ( shift the freeIndex left and set the low bit)
	this->allEntries_.Set( index, (PVOID)(size_t)(this->freeIndexHead_));
	this-> freeIndexHead_ = ((index<<1) | 0x1);
    }
}



void
ObjectList::RemoveFromList( DWORD index, PVOID ptr )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(ptr));
    } CONTRACTL_END;

    CrstHolder ch( &listLock_ );

	//  sanity check that the pointer low bit is not set
	 _ASSERTE(  (((DWORD)(size_t)ptr & 0x1) == 0) && "Invalid pointer" );

    _ASSERTE( index < this->allEntries_.GetCount() );
    _ASSERTE( this->allEntries_.Get( index ) == ptr && "Index tracking failed for this object" );

     // add the index to the free list ( shift the freeIndex left and set the low bit)
	this->allEntries_.Set( index, (PVOID)(size_t)(this->freeIndexHead_));
	this-> freeIndexHead_ = ((index<<1) | 0x1);

}

PVOID
ObjectList::Get( DWORD index )
{
    LIMITED_METHOD_CONTRACT;
    return this->allEntries_.Get( index );
}


UnsynchronizedBlockAllocator::UnsynchronizedBlockAllocator( size_t blockSize )
: blockSize_( blockSize ),
  offset_( blockSize ),
  index_( INVALID_COMPRESSEDSTACK_INDEX )
{
    LIMITED_METHOD_CONTRACT;
    // We start off the offset at the block size to force the first
    // allocation to create a new (first) block
}

UnsynchronizedBlockAllocator::~UnsynchronizedBlockAllocator( void )
{
    LIMITED_METHOD_CONTRACT;
    ArrayList::Iterator iter = this->blockList_.Iterate();

    while (iter.Next())
    {
        delete [] (BYTE *) iter.GetElement();
    }
}


PVOID
UnsynchronizedBlockAllocator::Allocate( size_t size )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE( size <= this->blockSize_ );

    S_SIZE_T sizecheck = S_SIZE_T(this->offset_) + S_SIZE_T(size) ;
    if( sizecheck.IsOverflow() )
    {
        ThrowOutOfMemory();
    }

    BYTE* bufferPtr;
    if (sizecheck.Value() > this->blockSize_)
    {
        NewArrayHolder<BYTE> buffer = new BYTE[this->blockSize_];
        bufferPtr = (BYTE*)buffer.GetValue();
        IfFailThrow(this->blockList_.Append( bufferPtr ));
        buffer.SuppressRelease();
        ++this->index_;
        this->offset_ = 0;
    }
    else
    {
        bufferPtr = (BYTE*)this->blockList_.Get( index_ );
    }

    void* retval = bufferPtr + this->offset_;
    this->offset_ += size;

    return retval;
}
