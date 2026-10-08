// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriObject.inl
//

#ifndef __SATORI_OBJECT_INL__
#define __SATORI_OBJECT_INL__

#include "common.h"
#include "../gc.h"
#include "../gcdesc.h"

#include "SatoriUtil.h"
#include "SatoriObject.h"
#include "SatoriRegion.h"

FORCEINLINE size_t SatoriObject::Size()
{
    MethodTable* mt = RawGetMethodTable();
    size_t size = mt->GetBaseSize();
    if (mt->HasComponentSize())
    {
        size += (size_t)((ArrayBase*)this)->GetNumComponents() * mt->RawGetComponentSize();
        size = ALIGN_UP(size, Satori::OBJECT_ALIGNMENT);
    }

    return size;
}

FORCEINLINE size_t SatoriObject::FreeObjSize()
{
    _ASSERTE(IsFree());
    size_t size = Satori::MIN_FREE_SIZE;
    size += (size_t)((ArrayBase*)this)->GetNumComponents();
    _ASSERTE(size == ALIGN_UP(size, Satori::OBJECT_ALIGNMENT));
    return size;
}

FORCEINLINE size_t SatoriObject::FreeObjCapacity()
{
    _ASSERTE(IsFree());
    return (size_t)((ArrayBase*)this)->GetNumComponents();
}

inline size_t SatoriObject::Start()
{
    return (size_t)this;
}

FORCEINLINE size_t SatoriObject::End()
{
    return Start() + Size();
}

FORCEINLINE SatoriObject* SatoriObject::Next()
{
    return (SatoriObject*)End();
}

inline bool SatoriObject::IsExternal()
{
#if FEATURE_SATORI_EXTERNAL_OBJECTS
    return !SatoriHeap::IsInHeap(this->Start());
#else
    _ASSERTE(SatoriHeap::IsInHeap(this->Start()));
    return false;
#endif
}

inline SatoriRegion* SatoriObject::ContainingRegion()
{
#if FEATURE_SATORI_EXTERNAL_OBJECTS
    _ASSERTE(!IsExternal() || this == nullptr);
#endif

    return (SatoriRegion*)((size_t)this & ~(Satori::REGION_SIZE_GRANULARITY - 1));
}


inline bool SatoriObject::SameRegion(SatoriRegion* otherRegion)
{
    return ((size_t)this ^ (size_t)otherRegion) < Satori::REGION_SIZE_GRANULARITY;
}

inline bool SatoriObject::IsFree()
{
    return RawGetMethodTable() == s_emptyObjectMt;
}

#ifndef HOST_64BIT

32bit is NYI

#endif

inline bool SatoriObject::IsEscaped()
{
    return ContainingRegion()->IsEscaped(this);
}

inline bool SatoriObject::IsMarked()
{
    return ContainingRegion()->IsMarked(this);
}

FORCEINLINE bool SatoriObject::IsMarkedOrOlderThan(int generation)
{
    _ASSERTE(this->Size() > 0);
    SatoriRegion* r = ContainingRegion();
    return r->Generation() > generation || r->IsMarked(this);
}

inline void SatoriObject::SetMarked()
{
    ContainingRegion()->SetMarked(this);
}

inline void SatoriObject::SetMarkedAtomic()
{
    return ContainingRegion()->SetMarkedAtomic(this);
}

inline bool SatoriObject::IsFinalizationSuppressed()
{
    return GetHeader()->GetBits() & BIT_SBLK_FINALIZER_RUN;
}

inline void SatoriObject::SuppressFinalization()
{
    GetHeader()->SetBit(BIT_SBLK_FINALIZER_RUN);
}

inline void SatoriObject::UnSuppressFinalization()
{
    GetHeader()->ClrBit(BIT_SBLK_FINALIZER_RUN);
}

//
// Implementation note on mark overflow and relocation:
//    we could use temporary maps (we would have to on 32bit),
//    but on 64bit we will use unused bits in the syncblock instead.
//

inline int32_t SatoriObject::GetNextInLocalMarkStack()
{
    return *((int32_t*)this - 2);
}

inline void SatoriObject::SetNextInLocalMarkStack(int32_t next)
{
    _ASSERTE(GetNextInLocalMarkStack() == 0);
    *((int32_t*)this - 2) = next;
}

inline void SatoriObject::ClearNextInLocalMarkStack()
{
    *((int32_t*)this - 2) = 0;
}

inline int32_t SatoriObject::GetLocalReloc()
{
    // since local relocation does not intersect with uses of mark stack
    // we will use the same bits.
    return GetNextInLocalMarkStack();
}

inline void SatoriObject::SetLocalReloc(int32_t next)
{
    // since local relocation does not intersect with uses of mark stack
    // we will use the same bits.
    SetNextInLocalMarkStack(next);
}

inline void SatoriObject::ClearMarkCompactStateForRelocation()
{
    // since local relocation does not intersect with uses of mark stack
    // we will use the same bits.
    ClearNextInLocalMarkStack();

    SatoriRegion* r = ContainingRegion();
    _ASSERTE(!r->IsEscaped(this));
    r->ClearMarked(this);
    r->ClearPinned(this);
}

inline SatoriObject* SatoriObject::RelocatedToUnchecked()
{
    _ASSERTE(this->ContainingRegion()->IsRelocated());
    SatoriObject* newLocation = *((SatoriObject**)this - 1);
    _ASSERTE(this->RawGetMethodTable() == newLocation->RawGetMethodTable());
    return newLocation;
}

template <bool notExternal>
FORCEINLINE bool SatoriObject::IsRelocatedTo(SatoriObject** newLocation)
{
    if (notExternal)
    {
        _ASSERTE(!this->IsExternal());
    }
    else if (this->IsExternal())
    {
        return false;
    }

    if (this->ContainingRegion()->IsRelocated())
    {
        *newLocation = RelocatedToUnchecked();
        return true;
    }

    return false;
}

inline void SatoriObject::CleanSyncBlock()
{
    *((size_t*)this - 1) = 0;
}

inline int SatoriObject::GetMarkBitAndWord(size_t* bitmapIndex)
{
    size_t start = Start();
    *bitmapIndex = (start >> 9) & (SatoriRegion::BITMAP_LENGTH - 1); // % words in the bitmap
    return (start >> 3) & 63;     // % bits in a word
}

// used by pinned allocations
inline void SatoriObject::SetUnmovable()
{
    *((DWORD*)this - 1) |= BIT_SBLK_GC_RESERVE;
}

inline bool SatoriObject::IsUnmovable()
{
    return *((DWORD*)this - 1) & BIT_SBLK_GC_RESERVE;
}

// #define BIT_SBLK_UNUSED                     0x80000000
#define BIT_SBLK_UNFINISHED                     0x80000000

// used by shared allocations
inline void SatoriObject::CleanSyncBlockAndSetUnfinished()
{
    *((size_t*)this - 1) = (size_t)BIT_SBLK_UNFINISHED << 32;
}

inline bool SatoriObject::IsUnfinished()
{
    return *((DWORD*)this - 1) & BIT_SBLK_UNFINISHED;
}

inline void SatoriObject::UnsetUnfinished()
{
    *((DWORD*)this - 1) &= ~BIT_SBLK_UNFINISHED;
}

template <typename F>
inline void SatoriObject::ForEachObjectRef(F lambda, bool includeCollectibleAllocator)
{
    MethodTable* mt = RawGetMethodTable();

    if (includeCollectibleAllocator && mt->Collectible())
    {
        uint8_t* loaderAllocator = GCToEEInterface::GetLoaderAllocatorObjectForGC(this);
        // NB: Allocator ref location is fake. The actual location is a handle).
        //     For that same reason relocation callers should not care about the location.
        lambda((SatoriObject**)&loaderAllocator);
    }

    if (!mt->ContainsGCPointers())
    {
        return;
    }

    CGCDesc* map = CGCDesc::GetCGCDescFromMT(mt);
    CGCDescSeries* cur = map->GetHighestSeries();

    // GetNumSeries is actually signed.
    // Negative value means the pattern repeats componentNum times (struct arrays)
    ptrdiff_t numSeries = (ptrdiff_t)map->GetNumSeries();
    if (numSeries >= 0)
    {
        CGCDescSeries* last = map->GetLowestSeries();

        // series size is offset by the object size, so need to compensate for that.
        size_t size = mt->GetBaseSize();
        if (mt->HasComponentSize())
        {
            size += (size_t)((ArrayBase*)this)->GetNumComponents() * mt->RawGetComponentSize();
        }

        do
        {
            size_t refPtr = (size_t)this + cur->GetSeriesOffset();
            size_t refPtrStop = refPtr + size + cur->GetSeriesSize();

            // top check loop. this could be a zero-element array
            while (refPtr < refPtrStop)
            {
                lambda((SatoriObject**)refPtr);
                refPtr += sizeof(size_t);
            }
            cur--;
        } while (cur >= last);
    }
    else
    {
        // repeating patern - an array
        size_t refPtr = (size_t)this + cur->GetSeriesOffset();
        uint32_t componentNum = ((ArrayBase*)this)->GetNumComponents();
        while (componentNum-- > 0)
        {
            for (ptrdiff_t i = 0; i > numSeries; i--)
            {
                val_serie_item item = *(cur->val_serie + i);
                size_t refPtrStop = refPtr + item.nptrs * sizeof(size_t);
                do
                {
                    lambda((SatoriObject**)refPtr);
                    refPtr += sizeof(size_t);
                } while (refPtr < refPtrStop);

                refPtr += item.skip;
            }
        }
    }
}

template <typename F>
inline void SatoriObject::ForEachObjectRef(F lambda, size_t size, bool includeCollectibleAllocator)
{
    MethodTable* mt = RawGetMethodTable();

    if (includeCollectibleAllocator && mt->Collectible())
    {
        uint8_t* loaderAllocator = GCToEEInterface::GetLoaderAllocatorObjectForGC(this);
        // NB: Allocator ref location is fake. The actual location is a handle).
        //     For that same reason relocation callers should not care about the location.
        lambda((SatoriObject**)&loaderAllocator);
    }

    if (!mt->ContainsGCPointers())
    {
        return;
    }

    CGCDesc* map = CGCDesc::GetCGCDescFromMT(mt);
    CGCDescSeries* cur = map->GetHighestSeries();

    // GetNumSeries is actually signed.
    // Negative value means the pattern repeats componentNum times (struct arrays)
    ptrdiff_t numSeries = (ptrdiff_t)map->GetNumSeries();
    if (numSeries >= 0)
    {
        CGCDescSeries* last = map->GetLowestSeries();

        do
        {
            size_t refPtr = (size_t)this + cur->GetSeriesOffset();
            // series size is offset by the object size, so need to compensate for that.
            size_t refPtrStop = refPtr + size + cur->GetSeriesSize();

            // top check loop. this could be a zero-element array
            while (refPtr < refPtrStop)
            {
                lambda((SatoriObject**)refPtr);
                refPtr += sizeof(size_t);
            }
            cur--;
        } while (cur >= last);
    }
    else
    {
        // repeating patern - an array
        size_t refPtr = (size_t)this + cur->GetSeriesOffset();
        uint32_t componentNum = ((ArrayBase*)this)->GetNumComponents();
        while (componentNum-- > 0)
        {
            for (ptrdiff_t i = 0; i > numSeries; i--)
            {
                val_serie_item item = *(cur->val_serie + i);
                size_t refPtrStop = refPtr + item.nptrs * sizeof(size_t);
                do
                {
                    lambda((SatoriObject**)refPtr);
                    refPtr += sizeof(size_t);
                } while (refPtr < refPtrStop);

                refPtr += item.skip;
            }
        }
    }
}

template <typename F>
inline void SatoriObject::ForEachObjectRef(F lambda, size_t start, size_t end)
{
    MethodTable* mt = RawGetMethodTable();

    if (start <= Start() && mt->Collectible())
    {
        uint8_t* loaderAllocator = GCToEEInterface::GetLoaderAllocatorObjectForGC(this);
        // NB: Allocator ref location is fake. The allocator is accessed via a handle indirection.
        //     It is ok to "update" the ref, but it will have no effect.
        //     The real update is when the handle is updated, which should happen separately.
        lambda((SatoriObject**)&loaderAllocator);
    }

    if (!mt->ContainsGCPointers())
    {
        return;
    }

    CGCDesc* map = CGCDesc::GetCGCDescFromMT(mt);
    CGCDescSeries* cur = map->GetHighestSeries();

    // GetNumSeries is actually signed.
    // Negative value means the pattern repeats componentNum times (struct arrays)
    ptrdiff_t cnt = (ptrdiff_t)map->GetNumSeries();
    if (cnt >= 0)
    {
        CGCDescSeries* last = map->GetLowestSeries();

        // series size is offset by the object size, so need to compensate for that.
        size_t size = mt->GetBaseSize();
        if (mt->HasComponentSize())
        {
            size += (size_t)((ArrayBase*)this)->GetNumComponents() * mt->RawGetComponentSize();
        }

        do
        {
            size_t refPtr = (size_t)this + cur->GetSeriesOffset();
            size_t refPtrStop = refPtr + size + cur->GetSeriesSize();

            refPtr = max(refPtr, start);

            // top check loop. this could be a zero-element array
            while (refPtr < refPtrStop)
            {
                if (refPtr >= end)
                {
                    return;
                }

                lambda((SatoriObject**)refPtr);
                refPtr += sizeof(size_t);
            }

            cur--;
        } while (cur >= last);
    }
    else
    {
        // repeating patern - an array of structs
        ptrdiff_t componentNum = ((ArrayBase*)this)->GetNumComponents();
        size_t elementSize = mt->RawGetComponentSize();
        size_t refPtr = (size_t)this + cur->GetSeriesOffset();

        if (refPtr < start)
        {
            size_t skip = (start - refPtr) / elementSize;
            componentNum -= skip;
            refPtr += skip * elementSize;
        }

        while (componentNum-- > 0)
        {
            for (ptrdiff_t i = 0; i > cnt; i--)
            {
                val_serie_item item = *(cur->val_serie + i);
                size_t refPtrStop = refPtr + item.nptrs * sizeof(size_t);

                if (refPtrStop <= start)
                {
                    // serie does not intersect with the range
                    refPtr = refPtrStop;
                }
                else
                {
                    refPtr = max(refPtr, start);
                    do
                    {
                        if (refPtr >= end)
                        {
                            return;
                        }

                        lambda((SatoriObject**)refPtr);
                        refPtr += sizeof(size_t);
                    } while (refPtr < refPtrStop);

                }

                refPtr += item.skip;
            }
        }
    }
}

#endif
