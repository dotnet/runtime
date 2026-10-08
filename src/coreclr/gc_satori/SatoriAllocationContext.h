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
// SatoriAllocationContext.h
//

#ifndef __SATORI_ALLOCATION_CONTEXT_H__
#define __SATORI_ALLOCATION_CONTEXT_H__

#include "common.h"
#include "../gc.h"

class SatoriRegion;
class SatoriRecycler;
class SatoriObject;

class SatoriAllocationContext : public gc_alloc_context
{
public:
    SatoriRegion*& RegularRegion()
    {
        return (SatoriRegion*&)this->gc_reserved_1;
    }

    SatoriRegion*& LargeRegion()
    {
        return (SatoriRegion*&)this->gc_reserved_2;
    }

    // stop allocating on all associated regions and optionally detach from the context.
    void Deactivate(SatoriRecycler* recycler, bool detach);
    SatoriObject* FinishAllocFromShared();

private:

};

#endif
