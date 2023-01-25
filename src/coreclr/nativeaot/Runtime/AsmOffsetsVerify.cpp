// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "rhassert.h"
#include "RedhawkWarnings.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "TargetPtrs.h"
#include "rhbinder.h"
#include "RWLock.h"
#include "RuntimeInstance.h"
#include "CachedInterfaceDispatch.h"
#include "shash.h"
#include "CallDescr.h"

template<size_t A, size_t B>
struct ExpectedAndActualValues
{
    static constexpr bool Equal()
    {
        return A == B;
    }
};

class AsmOffsets
{
    static_assert(sizeof(Thread::m_rgbAllocContextBuffer) >= sizeof(gc_alloc_context), "Thread::m_rgbAllocContextBuffer is not big enough to hold a gc_alloc_context");

    // Some assembly helpers for arrays and strings are shared and use the fact that arrays and strings have similar layouts)
    static_assert(offsetof(Array, m_Length) == offsetof(String, m_Length), "The length field of String and Array have different offsets");
    static_assert(sizeof(((Array*)0)->m_Length) == sizeof(((String*)0)->m_Length), "The length field of String and Array have different sizes");

#define PLAT_ASM_OFFSET(offset, cls, member) \
    static_assert(ExpectedAndActualValues<0x##offset, offsetof(cls, member)>::Equal(), "Bad asm offset for '" #cls "." #member "'.");

#define PLAT_ASM_SIZEOF(size,   cls        ) \
    static_assert(ExpectedAndActualValues<0x##size, sizeof(cls)>::Equal(), "Bad asm size for '" #cls "'.");

#define PLAT_ASM_CONST(constant, expr) \
    static_assert(ExpectedAndActualValues<0x##constant, (expr)>::Equal(),  "Bad asm constant for '" #expr "'.");

#include "AsmOffsets.h"

};

#ifdef _MSC_VER
namespace { char WorkaroundLNK4221Warning; };
#endif
