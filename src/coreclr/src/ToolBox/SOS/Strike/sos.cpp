// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "strike.h"
#include "util.h"

#include "sos.h"


#ifdef _ASSERTE
#undef _ASSERTE
#endif

#define _ASSERTE(a) {;}

#include "gcdesc.h"


#undef _ASSERTE

namespace sos
{
    template <class T>
    static bool MemOverlap(T beg1, T end1, // first range
                           T beg2, T end2) // second range
    {
        if (beg2 >= beg1 && beg2 <= end1)       // second range starts within first range
            return true;
        else if (end2 >= beg1 && end2 <= end1)  // second range ends within first range
            return true;
        else if (beg1 >= beg2 && beg1 <= end2)  // first range starts within second range 
            return true;
        else if (end1 >= beg2 && end1 <= end2)  // first range ends within second range
            return true;
        else
            return false;
    }


    Object::Object(TADDR addr)
        : mAddress(addr), mMT(0), mSize(~0), mPointers(false), mMTData(0), mTypeName(0)
    {
        if ((mAddress & ~ALIGNCONST) != mAddress)
            sos::Throw<Exception>("Object %p is misaligned.", mAddress);
    }

    Object::Object(TADDR addr, TADDR mt)
        : mAddress(addr), mMT(mt & ~3), mSize(~0), mPointers(false), mMTData(0), mTypeName(0)
    {
        if ((mAddress & ~ALIGNCONST) != mAddress)
            sos::Throw<Exception>("Object %p is misaligned.", mAddress);
    }
    
    
    Object::Object(const Object &rhs)
        : mAddress(rhs.mAddress), mMT(rhs.mMT), mSize(rhs.mSize), mPointers(rhs.mPointers), mMTData(rhs.mMTData), mTypeName(rhs.mTypeName)
    {
        rhs.mMTData = 0;
        rhs.mTypeName = 0;
    }

    const Object &Object::operator=(TADDR addr)
    {
        if (mMTData)
            delete mMTData;
        
        if (mTypeName)
            delete mTypeName;

        mAddress = addr;
        mMT = 0;
        mSize = ~0;
        mMTData = 0;
        mTypeName = 0;

        return *this;
    }

    bool Object::TryGetHeader(ULONG &outHeader) const
    {
        struct ObjectHeader
        {
    #ifdef _WIN64
            ULONG _alignpad;
    #endif
            ULONG SyncBlockValue;      // the Index and the Bits
        };

        ObjectHeader header;

        if (SUCCEEDED(rvCache->Read(TO_TADDR(GetAddress() - sizeof(ObjectHeader)), &header, sizeof(ObjectHeader), NULL)))
        {
            outHeader = header.SyncBlockValue;
            return true;
        }

        return false;
    }


    ULONG Object::GetHeader() const
    {
        ULONG toReturn = 0;
        if (!TryGetHeader(toReturn))
            sos::Throw<DataRead>("Failed to get header for object %p.", GetAddress());

        return toReturn;
    }

    TADDR Object::GetMT() const
    {
        if (mMT == NULL)
        {
            TADDR temp;
            if (FAILED(MOVE(temp, mAddress)))
                sos::Throw<DataRead>("Object %s has an invalid method table.", DMLListNearObj(mAddress));
            
            if (temp == NULL)
                sos::Throw<HeapCorruption>("Object %s has an invalid method table.", DMLListNearObj(mAddress));

            mMT = temp & ~3;
        }

        return mMT;
    }
    
    TADDR Object::GetComponentMT() const
    {
        if (mMT != NULL && mMT != sos::MethodTable::GetArrayMT())
            return NULL;
        
        DacpObjectData objData;
        if (FAILED(objData.Request(g_sos, TO_CDADDR(mAddress))))
            sos::Throw<DataRead>("Failed to request object data for %s.", DMLListNearObj(mAddress));
        
        if (mMT == NULL)
            mMT = TO_TADDR(objData.MethodTable) & ~3;
        
        return TO_TADDR(objData.ElementTypeHandle);
    }

    const WCHAR *Object::GetTypeName() const
    {
        if (mTypeName == NULL)
            mTypeName = CreateMethodTableName(GetMT(), GetComponentMT());
            
        
        if (mTypeName == NULL)
            return W("<error>");

        return mTypeName;
    }

    void Object::FillMTData() const
    {
        if (mMTData == NULL)
        {
            mMTData = new DacpMethodTableData;
            if (FAILED(mMTData->Request(g_sos, GetMT())))
            {
                delete mMTData;
                mMTData = NULL;
                sos::Throw<DataRead>("Could not request method table data for object %p (MethodTable: %p).", mAddress, mMT);
            }
        }
    }


    void Object::CalculateSizeAndPointers() const
    {
        TADDR mt = GetMT();
        MethodTableInfo* info = g_special_mtCache.Lookup((DWORD_PTR)mt);
        if (!info->IsInitialized())	
        {
            // this is the first time we see this method table, so we need to get the information
            // from the target
            FillMTData();

            info->BaseSize = mMTData->BaseSize;
            info->ComponentSize = mMTData->ComponentSize;
            info->bContainsPointers = mMTData->bContainsPointers;

            // The following request doesn't work on older runtimes. For those, the
            // objects would just look like non-collectible, which is acceptable.
            DacpMethodTableCollectibleData mtcd;
            if (SUCCEEDED(mtcd.Request(g_sos, GetMT())))
            {
                info->bCollectible = mtcd.bCollectible;
                info->LoaderAllocatorObjectHandle = TO_TADDR(mtcd.LoaderAllocatorObjectHandle);
            }
        }
        
        if (mSize == (size_t)~0)
        {
            mSize = info->BaseSize;
            if (info->ComponentSize)
            {
                // this is an array, so the size has to include the size of the components. We read the number
                // of components from the target and multiply by the component size to get the size.
                mSize += info->ComponentSize * GetNumComponents(GetAddress());
            }

            // On x64 we do an optimization to save 4 bytes in almost every string we create.
        #ifdef _WIN64
            // Pad to min object size if necessary
            if (mSize < min_obj_size)
                mSize = min_obj_size;
        #endif // _WIN64
        }

        mPointers = info->bContainsPointers != FALSE;
    }

    size_t Object::GetSize() const
    {
        if (mSize == (size_t)~0) // poison value
        {
            CalculateSizeAndPointers();
        }

        SOS_Assert(mSize != (size_t)~0);
        return mSize;
    }


    bool Object::HasPointers() const
    {
        if (mSize == (size_t)~0)
            CalculateSizeAndPointers();

        SOS_Assert(mSize != (size_t)~0);
        return mPointers;
    }


    bool Object::VerifyMemberFields(TADDR pMT, TADDR obj)
    {
        WORD numInstanceFields = 0;
        return VerifyMemberFields(pMT, obj, numInstanceFields);
    }


    bool Object::VerifyMemberFields(TADDR pMT, TADDR obj, WORD &numInstanceFields)
    {
        DacpMethodTableData vMethTable;
        if (FAILED(vMethTable.Request(g_sos, pMT)))
            return false;

        // Recursively verify the parent (this updates numInstanceFields)
        if (vMethTable.ParentMethodTable)
        {
            if (!VerifyMemberFields(TO_TADDR(vMethTable.ParentMethodTable), obj, numInstanceFields))
                return false;
        }

        DacpMethodTableFieldData vMethodTableFields;

        // Verify all fields on the object.
        CLRDATA_ADDRESS dwAddr = vMethodTableFields.FirstField;
        DacpFieldDescData vFieldDesc;
        
        while (numInstanceFields < vMethodTableFields.wNumInstanceFields)
        {
            CheckInterrupt();
            
            if (FAILED(vFieldDesc.Request(g_sos, dwAddr)))
                return false;

            if (vFieldDesc.Type >= ELEMENT_TYPE_MAX)
                return false;

            dwAddr = vFieldDesc.NextField;
                
            if (!vFieldDesc.bIsStatic)
            {
                numInstanceFields++;            
                TADDR dwTmp = TO_TADDR(obj + vFieldDesc.dwOffset + sizeof(BaseObject));
                if (vFieldDesc.Type == ELEMENT_TYPE_CLASS)
                {
                    // Is it a valid object?  
                    if (FAILED(MOVE(dwTmp, dwTmp)))
                        return false;

                    if (dwTmp != NULL)
                    {
                        DacpObjectData objData;
                        if (FAILED(objData.Request(g_sos, TO_CDADDR(dwTmp))))
                            return false;
                    }
                }
            }        
        }
        
        return true;
    }

    bool MethodTable::IsZombie(TADDR addr)
    {
        // Zombie objects are objects that reside in an unloaded AppDomain.
        MethodTable mt = addr;
        return _wcscmp(mt.GetName(), W("<Unloaded Type>")) == 0;
    }
    
    void MethodTable::Clear()
    {
        if (mName)
        {
            delete [] mName;
            mName = NULL;
        }
    }
    
    const WCHAR *MethodTable::GetName() const
    {
        if (mName == NULL)
            mName = CreateMethodTableName(mMT);
        
        if (mName == NULL)
            return W("<error>");
            
        return mName;
    }

    bool Object::IsValid(TADDR address, bool verifyFields)
    {
        DacpObjectData objectData;
        if (FAILED(objectData.Request(g_sos, TO_CDADDR(address))))
            return false;

        if (verifyFields &&
            objectData.MethodTable != g_special_usefulGlobals.FreeMethodTable &&
            !MethodTable::IsZombie(TO_TADDR(objectData.MethodTable)))
        {
            return VerifyMemberFields(TO_TADDR(objectData.MethodTable), address);
        }
        
        return true;
    }

    bool Object::GetThinLock(ThinLockInfo &out) const
    {
        ULONG header = GetHeader();
        if (header & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK))
        {
            return false;
        }

        out.ThreadId = header & SBLK_MASK_LOCK_THREADID;
        out.Recursion = (header & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT;

        CLRDATA_ADDRESS threadPtr = NULL;
        if (g_sos->GetThreadFromThinlockID(out.ThreadId, &threadPtr) != S_OK)
        {
            out.ThreadPtr = NULL;
        }
        else
        {
            out.ThreadPtr = TO_TADDR(threadPtr);
        }
        
        return out.ThreadId != 0 && out.ThreadPtr != NULL;
    }
    
    bool Object::GetStringData(__out_ecount(size) WCHAR *buffer, size_t size) const
    {
        SOS_Assert(IsString());
        SOS_Assert(buffer);
        SOS_Assert(size > 0);

        return SUCCEEDED(g_sos->GetObjectStringData(mAddress, (ULONG32)size, buffer, NULL));
    }
    
    size_t Object::GetStringLength() const
    {
        SOS_Assert(IsString());

        strobjInfo stInfo;
        if (FAILED(MOVE(stInfo, mAddress)))
            sos::Throw<DataRead>("Failed to read object data at %p.", mAddress);

        // We get the method table for free here, if we don't have it already.
        SOS_Assert((mMT == NULL) || (mMT == TO_TADDR(stInfo.methodTable)));
        if (mMT == NULL)
            mMT = TO_TADDR(stInfo.methodTable);

        return (size_t)stInfo.m_StringLength;
    }


    RefIterator::RefIterator(TADDR obj, LinearReadCache *cache)
        : mCache(cache), mGCDesc(0), mArrayOfVC(false), mDone(false), mBuffer(0), mCurrSeries(0), mLoaderAllocatorObjectHandle(0),
          i(0), mCount(0), mCurr(0), mStop(0), mObject(obj), mObjSize(0)
    {
        Init();
    }

    RefIterator::RefIterator(TADDR obj, CGCDesc *desc, bool arrayOfVC, LinearReadCache *cache)
        : mCache(cache), mGCDesc(desc), mArrayOfVC(arrayOfVC), mDone(false), mBuffer(0), mCurrSeries(0), mLoaderAllocatorObjectHandle(0),
          i(0), mCount(0), mCurr(0), mStop(0), mObject(obj), mObjSize(0)
    {
        Init();
    }
    
    RefIterator::~RefIterator()
    {
        if (mBuffer)
            delete [] mBuffer;
    }
    
    const RefIterator &RefIterator::operator++()
    {
        if (mDone)
            Throw<Exception>("Attempt to move past the end of the iterator.");

        if (mCurr == mLoaderAllocatorObjectHandle)
        {
            // The mLoaderAllocatorObjectHandle is always the last reference returned
            mDone = true;
            return *this;
        }
        
        if (!mArrayOfVC)
        {
            mCurr += sizeof(TADDR);
            if (mCurr >= mStop)
            {
                mCurrSeries--;
                if (mCurrSeries < mGCDesc->GetLowestSeries())
                {
                    mDone = true;
                }
                else
                {
                    mCurr = mObject + mCurrSeries->GetSeriesOffset();
                    mStop = mCurr + mCurrSeries->GetSeriesSize() + mObjSize;
                }
            }
        }
        else
        {
            mCurr += sizeof(TADDR);
            if (mCurr >= mStop)
            {
                int i_last = i;
                i--;
                
                if (i == mCount)
                    i = 0;
                
                mCurr += mCurrSeries->val_serie[i_last].skip;
                mStop = mCurr + mCurrSeries->val_serie[i].nptrs * sizeof(TADDR);
            }
            
            if (mCurr >= mObject + mObjSize - plug_skew)
                mDone = true;
        }
        
        if (mDone && mLoaderAllocatorObjectHandle != NULL)
        {
            // The iteration over all regular object references is done, but there is one more
            // reference for collectible types - the LoaderAllocator for GC
            mCurr = mLoaderAllocatorObjectHandle;
            mDone = false;
        }

        return *this;
    }
    
    TADDR RefIterator::operator*() const
    {
        return ReadPointer(mCurr);
    }
    
    TADDR RefIterator::GetOffset() const
    {
        return mCurr - mObject;
    }
    
    void RefIterator::Init()
    {
        TADDR mt = ReadPointer(mObject);
        BOOL bContainsPointers = FALSE;
        BOOL bCollectible = FALSE;
        TADDR loaderAllocatorObjectHandle;

        if (!GetSizeEfficient(mObject, mt, FALSE, mObjSize, bContainsPointers))
            Throw<DataRead>("Failed to get size of object.");

        if (!GetCollectibleDataEfficient(mt, bCollectible, loaderAllocatorObjectHandle))
            Throw<DataRead>("Failed to get collectible info of object.");

        if (!bContainsPointers && !bCollectible)
        {
            mDone = true;
            return;
        }

        if (bContainsPointers)
        {
            if (!mGCDesc)
            {
                int entries = 0;

                if (FAILED(MOVE(entries, mt-sizeof(TADDR))))
                    Throw<DataRead>("Failed to request number of entries.");

                // array of vc?
                if (entries < 0)
                {
                    entries = -entries;
                    mArrayOfVC = true;
                }
                else
                {
                    mArrayOfVC = false;
                }

                size_t slots = 1 + entries * sizeof(CGCDescSeries)/sizeof(TADDR);

                ArrayHolder<TADDR> buffer = new TADDR[slots];

                ULONG fetched = 0;
                CLRDATA_ADDRESS address = TO_CDADDR(mt - slots*sizeof(TADDR));
                if (FAILED(g_ExtData->ReadVirtual(address, buffer, (ULONG)(slots*sizeof(TADDR)), &fetched)))
                    Throw<DataRead>("Failed to request GCDesc.");

                mBuffer = buffer.Detach();
                mGCDesc = (CGCDesc*)(mBuffer + slots);
            }

            mCurrSeries = mGCDesc->GetHighestSeries();

            if (!mArrayOfVC)
            {
                mCurr = mObject + mCurrSeries->GetSeriesOffset();
                mStop = mCurr + mCurrSeries->GetSeriesSize() + mObjSize;
            }
            else
            {
                i = 0;
                mCurr = mObject + mCurrSeries->startoffset;
                mStop = mCurr + mCurrSeries->val_serie[i].nptrs * sizeof(TADDR);
                mCount = (int)mGCDesc->GetNumSeries();
            }

            if (mCurr == mStop)
                operator++();
            else if (mCurr >= mObject + mObjSize - plug_skew)
                mDone = true;
        }
        else
        {
            mDone = true;
        }

        if (bCollectible)
        {
            mLoaderAllocatorObjectHandle = loaderAllocatorObjectHandle;
            if (mDone)
            {
                // There are no object references, but there is still a reference for 
                // collectible types - the LoaderAllocator for GC
                mCurr = mLoaderAllocatorObjectHandle;
                mDone = false;
            }
        }
    }


    const TADDR GCHeap::HeapStart = 0;
    const TADDR GCHeap::HeapEnd = ~0;

    ObjectIterator::ObjectIterator(const DacpGcHeapDetails *heap, int numHeaps, TADDR start, TADDR stop)
    : bLarge(false), mCurrObj(0), mLastObj(0), mStart(start), mEnd(stop), mSegmentEnd(0), mHeaps(heap),
      mNumHeaps(numHeaps), mCurrHeap(0)
    {
        mAllocInfo.Init();
        SOS_Assert(numHeaps > 0);

        TADDR segStart = TO_TADDR(mHeaps[0].generation_table[GetMaxGeneration()].start_segment);
        if (FAILED(mSegment.Request(g_sos, segStart, mHeaps[0])))
            sos::Throw<DataRead>("Could not request segment data at %p.", segStart);

        mCurrObj = mStart < TO_TADDR(mSegment.mem) ? TO_TADDR(mSegment.mem) : mStart;
        mSegmentEnd = (segStart == TO_TADDR(mHeaps[0].ephemeral_heap_segment)) ? 
                            TO_TADDR(mHeaps[0].alloc_allocated) :
                            TO_TADDR(mSegment.allocated);

        CheckSegmentRange();
    }

    bool ObjectIterator::NextSegment()
    {
        if (mCurrHeap >= mNumHeaps)
            return false;

        TADDR next = TO_TADDR(mSegment.next);
        if (next == NULL)
        {
            if (bLarge)
            {
                mCurrHeap++;
                if (mCurrHeap == mNumHeaps)
                    return false;

                bLarge = false;
                next = TO_TADDR(mHeaps[mCurrHeap].generation_table[GetMaxGeneration()].start_segment);
            }
            else
            {
                bLarge = true;
                next = TO_TADDR(mHeaps[mCurrHeap].generation_table[GetMaxGeneration()+1].start_segment);
            }
        }

        SOS_Assert(next != NULL);
        if (FAILED(mSegment.Request(g_sos, next, mHeaps[mCurrHeap])))
            sos::Throw<DataRead>("Failed to request segment data at %p.", next);

        mLastObj = 0;
        mCurrObj = mStart < TO_TADDR(mSegment.mem) ? TO_TADDR(mSegment.mem) : mStart;
        mSegmentEnd = (next == TO_TADDR(mHeaps[mCurrHeap].ephemeral_heap_segment)) ? 
                            TO_TADDR(mHeaps[mCurrHeap].alloc_allocated) : 
                            TO_TADDR(mSegment.allocated);
        return CheckSegmentRange();
    }

    bool ObjectIterator::CheckSegmentRange()
    {
        CheckInterrupt();

        while (!MemOverlap(mStart, mEnd, TO_TADDR(mSegment.mem), mSegmentEnd))
            if (!NextSegment())
                return false;

        // At this point we know that the current segment contains objects in
        // the correct range.  However, there's no telling if the user gave us
        // a starting address that corresponds to an object.  If mStart is a
        // valid object, then we'll just start there.  If it's not we'll need
        // to walk the segment from the beginning to find the first aligned
        // object on or after mStart.
        if (mCurrObj == mStart && !Object::IsValid(mStart))
        {
            // It's possible mCurrObj will equal mStart after this.  That's fine.
            // It means that the starting object is corrupt (and we'll figure
            // that when the user calls GetNext), or IsValid was wrong.
            mLastObj = 0;
            mCurrObj = TO_TADDR(mSegment.mem);
            while (mCurrObj < mStart)
                MoveToNextObject();
        }

        return true;
    }


    
    const Object &ObjectIterator::operator*() const
    {
        AssertSanity();
        return mCurrObj;
    }


    const Object *ObjectIterator::operator->() const
    {
        AssertSanity();
        return &mCurrObj;
    }

    //Object ObjectIterator::GetNext()
    const ObjectIterator &ObjectIterator::operator++()
    {
        CheckInterrupt();

        // Assert we aren't done walking the heap.
        SOS_Assert(*this);
        AssertSanity();

        MoveToNextObject();
        return *this;
    }

    void ObjectIterator::MoveToNextObjectCarefully()
    {
        CheckInterrupt();

        SOS_Assert(*this);
        AssertSanity();

        // Move to NextObject won't generally throw unless it fails to request the
        // MethodTable of the object.  At which point we won't know how large the
        // current object is, nor how to move past it.  In this case we'll simply
        // move to the next segment if possible to continue iterating from there.
        try
        {
            MoveToNextObject();
        }
        catch(const sos::Exception &)
        {
            NextSegment();
        }
    }

    void ObjectIterator::AssertSanity() const
    {
        // Assert that we are in a sane state. Function which call this assume two things:
        //   1. That the current object is within the segment bounds.
        //   2. That the current object is within the requested memory range.
        SOS_Assert(mCurrObj >= TO_TADDR(mSegment.mem));
        SOS_Assert(mCurrObj <= TO_TADDR(mSegmentEnd - Align(min_obj_size)));

        SOS_Assert(mCurrObj >= mStart);
        SOS_Assert(mCurrObj <= mEnd);
    }

    void ObjectIterator::MoveToNextObject()
    {
        // Object::GetSize can be unaligned, so we must align it ourselves.
        size_t size = (bLarge ? AlignLarge(mCurrObj.GetSize()) : Align(mCurrObj.GetSize()));

        mLastObj = mCurrObj;
        mCurrObj = mCurrObj.GetAddress() + size;

        if (!bLarge)
        {       
            // Is this the end of an allocation context? We need to know this because there can be
            // allocated memory at the end of an allocation context that doesn't yet contain any objects.
            // This happens because we actually allocate a minimum amount of memory (the allocation quantum)
            // whenever we need to get more memory. Typically, a single allocation request won't fill this
            // block, so we'll fulfill subsequent requests out of the remainder of the block until it's
            // depleted. 
            int i;
            for (i = 0; i < mAllocInfo.num; i ++)
            {
                if (mCurrObj == TO_TADDR(mAllocInfo.array[i].alloc_ptr)) // end of objects in this context
                {
                    // Set mCurrObj to point after the context (alloc_limit is the end of the allocation context).
                    mCurrObj = TO_TADDR(mAllocInfo.array[i].alloc_limit) + Align(min_obj_size);
                    break;
                }
            }

            // We also need to look at the gen0 alloc context.
            if (mCurrObj == TO_TADDR(mHeaps[mCurrHeap].generation_table[0].allocContextPtr))
                mCurrObj = TO_TADDR(mHeaps[mCurrHeap].generation_table[0].allocContextLimit) + Align(min_obj_size);
        }

        if (mCurrObj > mEnd || mCurrObj >= mSegmentEnd)
            NextSegment();
    }

    SyncBlkIterator::SyncBlkIterator()
    : mCurr(1), mTotal(0)
    {
        // If DacpSyncBlockData::Request fails with the call "1", then it means
        // there are no SyncBlocks in the process.
        DacpSyncBlockData syncBlockData;
        if (SUCCEEDED(syncBlockData.Request(g_sos, 1)))
            mTotal = syncBlockData.SyncBlockCount;

        mSyncBlk = mCurr;
    }

    GCHeap::GCHeap()
    {
        if (FAILED(mHeapData.Request(g_sos)))
            sos::Throw<DataRead>("Failed to request GC heap data.");

        if (mHeapData.bServerMode)
        {
            mNumHeaps = mHeapData.HeapCount;
            DWORD dwAllocSize = 0;
            if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), mNumHeaps, dwAllocSize))
                sos::Throw<Exception>("Failed to get GCHeaps: Integer overflow.");

            CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
            if (FAILED(g_sos->GetGCHeapList(mNumHeaps, heapAddrs, NULL)))
                sos::Throw<DataRead>("Failed to get GCHeaps.");

            mHeaps = new DacpGcHeapDetails[mNumHeaps];

            for (int i = 0; i < mNumHeaps; i++)
                if (FAILED(mHeaps[i].Request(g_sos, heapAddrs[i])))
                    sos::Throw<DataRead>("Failed to get GC heap details at %p.", heapAddrs[i]);
        }
        else
        {
            mHeaps = new DacpGcHeapDetails[1];
            mNumHeaps = 1;
            
            if (FAILED(mHeaps[0].Request(g_sos)))
                sos::Throw<DataRead>("Failed to request GC details data.");
        }
    }

    ObjectIterator GCHeap::WalkHeap(TADDR start, TADDR stop) const
    {
        return ObjectIterator(mHeaps, mNumHeaps, start, stop);
    }

    bool GCHeap::AreGCStructuresValid() const
    {
        return mHeapData.bGcStructuresValid != FALSE;
    }

    // SyncBlk class
    SyncBlk::SyncBlk()
        : mIndex(0)
    {
    }

    SyncBlk::SyncBlk(int index)
    : mIndex(index)
    {
        Init();
    }
    
    const SyncBlk &SyncBlk::operator=(int index)
    {
        mIndex = index;
        Init();

        return *this;
    }

    void SyncBlk::Init()
    {
        if (FAILED(mData.Request(g_sos, mIndex)))
            sos::Throw<DataRead>("Failed to request SyncBlk at index %d.", mIndex);
    }

    TADDR SyncBlk::GetAddress() const
    {
        SOS_Assert(mIndex);
        return TO_TADDR(mData.SyncBlockPointer);
    }

    TADDR SyncBlk::GetObject() const
    {
        SOS_Assert(mIndex);
        return TO_TADDR(mData.Object);
    }

    int SyncBlk::GetIndex() const
    {
        return mIndex;
    }

    bool SyncBlk::IsFree() const
    {
        SOS_Assert(mIndex);
        return mData.bFree != FALSE;
    }

    unsigned int SyncBlk::GetMonitorHeldCount() const
    {
        SOS_Assert(mIndex);
        return mData.MonitorHeld;
    }

    unsigned int SyncBlk::GetRecursion() const
    {
        SOS_Assert(mIndex);
        return mData.Recursion;
    }

    DWORD SyncBlk::GetCOMFlags() const
    {
        SOS_Assert(mIndex);
    #ifdef FEATURE_COMINTEROP
        return mData.COMFlags;
    #else
        return 0;
    #endif
    }

    unsigned int SyncBlk::GetAdditionalThreadCount() const
    {
        SOS_Assert(mIndex);
        return mData.AdditionalThreadCount;
    }

    TADDR SyncBlk::GetHoldingThread() const
    {
        SOS_Assert(mIndex);
        return TO_TADDR(mData.HoldingThread);
    }

    TADDR SyncBlk::GetAppDomain() const
    {
        SOS_Assert(mIndex);
        return TO_TADDR(mData.appDomainPtr);
    }
    
    void BuildTypeWithExtraInfo(TADDR addr, unsigned int size, __inout_ecount(size) WCHAR *buffer)
    {
        try
        {
            sos::Object obj(addr);
            TADDR mtAddr = obj.GetMT();
            bool isArray = sos::MethodTable::IsArrayMT(mtAddr);
            bool isString = obj.IsString();
            
            sos::MethodTable mt(isArray ? obj.GetComponentMT() : mtAddr);
            
            if (isArray)
            {
                swprintf_s(buffer, size, W("%s[]"), mt.GetName());
            }
            else if (isString)
            {
                WCHAR str[32];
                obj.GetStringData(str, _countof(str));
                
                _snwprintf_s(buffer, size, _TRUNCATE, W("%s: \"%s\""), mt.GetName(), str);
            }
            else
            {
                _snwprintf_s(buffer, size, _TRUNCATE, W("%s"), mt.GetName());
            }
        }
        catch (const sos::Exception &e)
        {
            int len = MultiByteToWideChar(CP_ACP, 0, e.what(), -1, NULL, 0);
            
            ArrayHolder<WCHAR> tmp = new WCHAR[len];
            MultiByteToWideChar(CP_ACP, 0, e.what(), -1, (WCHAR*)tmp, len);
            
            swprintf_s(buffer, size, W("<invalid object: '%s'>"), (WCHAR*)tmp);
        }
    }
}
