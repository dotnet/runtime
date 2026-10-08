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
// SatoriAllocationContext.cpp
//

#include "common.h"
#include "gcenv.h"
#include "../env/gcenv.os.h"

#include "SatoriAllocationContext.h"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriObject.h"
#include "SatoriObject.inl"

class SatoriRecycler;

SatoriObject* SatoriAllocationContext::FinishAllocFromShared()
{
    _ASSERTE(alloc_limit >= alloc_ptr);
    size_t unusedStart = (size_t)alloc_ptr;
    size_t unused = (size_t)alloc_limit - unusedStart;
    SatoriObject* freeObj = SatoriObject::FormatAsFree(unusedStart, unused + Satori::MIN_FREE_SIZE);
    SatoriRegion* containingRegion = freeObj->ContainingRegion();
    // this portion is now parsable
    freeObj->ContainingRegion()->DecrementUnfinishedAlloc();
    freeObj->ContainingRegion()->Allocator()->AllocationTickDecrement(unused);
    // unclaim unused.
    alloc_bytes -= unused;
    alloc_ptr = alloc_limit = nullptr;

    return freeObj;
}

void SatoriAllocationContext::Deactivate(SatoriRecycler* recycler, bool detach)
{
    SatoriRegion* region = RegularRegion();
    if (region != nullptr)
    {
        size_t allocPtr = (size_t)this->alloc_ptr;

        size_t unused = this->alloc_limit - this->alloc_ptr;
        this->alloc_bytes -= unused;
        this->alloc_limit = this->alloc_ptr = nullptr;
        region->Allocator()->AllocationTickDecrement(unused);

        if (region->IsAllocating())
        {
            region->StopAllocating(allocPtr);
        }

        if (detach ||
            (region->SweepsSinceLastAllocation() > 2 && region->IsReuseCandidate()))
        {
            region->DetachFromAlocatingOwnerRelease();
        }
        // TUNING: force try sharing heuristic
        else if (region->SweepsSinceLastAllocation() > 3)
        {
            // allocation rate seems low, perhaps should use the shared region.
            region->DetachFromAlocatingOwnerRelease();
        }

        recycler->AddEphemeralRegion(region);
    }
    else
    {
        if (alloc_ptr != 0)
        {
            FinishAllocFromShared();
        }

        _ASSERTE(this->alloc_limit == nullptr);
        _ASSERTE(this->alloc_ptr == nullptr);
    }

    region = LargeRegion();
    if (region != nullptr)
    {
        if (region->IsAllocating())
        {
            region->StopAllocating();
        }

        if (detach ||
            (region->SweepsSinceLastAllocation() > 2 && region->IsReuseCandidate()))
        {
            region->DetachFromAlocatingOwnerRelease();
        }
        // TUNING: force try sharing heuristic
        else if (region->SweepsSinceLastAllocation() > 3)
        {
            // allocation rate seems low, perhaps should use the shared region.
            region->DetachFromAlocatingOwnerRelease();
        }

        recycler->AddEphemeralRegion(region);
    }
 }
