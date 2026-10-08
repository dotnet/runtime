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
// SatoriObject.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "SatoriObject.h"
#include "SatoriObject.inl"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriPage.h"
#include "SatoriPage.inl"

MethodTable* SatoriObject::s_emptyObjectMt;

void SatoriObject::Initialize()
{
    s_emptyObjectMt = GCToEEInterface::GetFreeObjectMethodTable();
}

void SatoriObject::EscapeCheckOnHandleCreation()
{
    if (IsExternal())
    {
        return;
    }

    SatoriRegion* region = ContainingRegion();
    if (region->IsEscapeTrackedByCurrentThread())
    {
        region->EscapeRecursively(this);

        // make sure the object is published after we are done marking escapes
        VolatileStoreBarrier();
    }
}

SatoriObject* SatoriObject::FormatAsFree(size_t location, size_t size)
{
    _ASSERTE(location == ALIGN_UP(location, Satori::OBJECT_ALIGNMENT));
    _ASSERTE(size == ALIGN_UP(size, Satori::OBJECT_ALIGNMENT));
    _ASSERTE(size >= Satori::MIN_FREE_SIZE);
    _ASSERTE(size < Satori::REGION_SIZE_GRANULARITY);

    SatoriObject* o = (SatoriObject*)location;
    _ASSERTE(o->ContainingRegion()->m_used > (location + 2 * sizeof(size_t)));

#ifdef JUNK_FILL_FREE_SPACE
    size_t dirtySize = o->ContainingRegion()->m_used - location;
    if (dirtySize > size)
    {
        dirtySize = size;
    }
    memset((void*)(location - sizeof(size_t)), 0xAC, dirtySize);
#endif

    o->CleanSyncBlock();
    o->RawSetMethodTable(s_emptyObjectMt);
    _ASSERTE(!o->IsMarked());

    // deduct the size of Array header + syncblock and set the size of the free bytes.
    ((size_t*)o)[1] = size - Satori::MIN_FREE_SIZE;
    return o;
}

void SatoriObject::DirtyCardsForContent()
{
    _ASSERTE(IsMarked());
    MethodTable* mt = RawGetMethodTable();
    if (mt->ContainsGCPointersOrCollectible())
    {
        SatoriPage* page = ContainingRegion()->m_containingPage;
        // if dealing with a collectible type, include MT in the dirty range
        // to be treated as a "reference" to the collectible allocator.
        size_t rangeStart = mt->Collectible() ? Start() : Start() + sizeof(size_t);
        page->DirtyCardsForRange(rangeStart, End());
    }
}

void SatoriObject::Validate()
{
#ifdef _DEBUG
    // min object size is the same as min free size because any hole
    // must be able to fit a free obj for parseability reasons
    _ASSERTE(this->Size() >= Satori::MIN_FREE_SIZE);

    if (ContainingRegion()->IsEscapeTrackedByCurrentThread())
    {
        if (IsEscaped())
        {
            _ASSERTE(!IsFree());

            ForEachObjectRef(
                [&](SatoriObject** ref)
                {
                    _ASSERTE(ContainingRegion()->IsExposed(ref));
                    SatoriObject* child = VolatileLoad(ref);
                    if (child->SameRegion(ContainingRegion()))
                    {
                        _ASSERTE(child->IsEscaped());
                    }
                }
            );
        }
        else
        {
            size_t size = Size();
            for (size_t i = Start(); i < size; i++)
            {
                _ASSERTE(!((SatoriObject*)i)->IsMarked());
            }
        }
    }
#endif
}
