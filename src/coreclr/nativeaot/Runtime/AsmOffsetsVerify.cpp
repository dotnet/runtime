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

class AsmOffsets
{
    static_assert(sizeof(Thread::m_rgbAllocContextBuffer) >= sizeof(gc_alloc_context), "Thread::m_rgbAllocContextBuffer is not big enough to hold a gc_alloc_context");

    // Some assembly helpers for arrays and strings are shared and use the fact that arrays and strings have similar layouts)
    static_assert(offsetof(Array, m_Length) == offsetof(String, m_Length), "The length field of String and Array have different offsets");
    static_assert(sizeof(((Array*)0)->m_Length) == sizeof(((String*)0)->m_Length), "The length field of String and Array have different sizes");

#define PLAT_ASM_OFFSET(offset, cls, member) \
    static_assert((offsetof(cls, member) == 0x##offset) || (offsetof(cls, member) > 0x##offset), "Bad asm offset for '" #cls "." #member "', the actual offset is smaller than 0x" #offset "."); \
    static_assert((offsetof(cls, member) == 0x##offset) || (offsetof(cls, member) < 0x##offset), "Bad asm offset for '" #cls "." #member "', the actual offset is larger than 0x" #offset ".");

#define PLAT_ASM_SIZEOF(size,   cls        ) \
    static_assert((sizeof(cls) == 0x##size) || (sizeof(cls) > 0x##size), "Bad asm size for '" #cls "', the actual size is smaller than 0x" #size "."); \
    static_assert((sizeof(cls) == 0x##size) || (sizeof(cls) < 0x##size), "Bad asm size for '" #cls "', the actual size is larger than 0x" #size ".");

#define PLAT_ASM_CONST(constant, expr) \
    static_assert(((expr) == 0x##constant) || ((expr) > 0x##constant), "Bad asm constant for '" #expr "', the actual value is smaller than 0x" #constant "."); \
    static_assert(((expr) == 0x##constant) || ((expr) < 0x##constant), "Bad asm constant for '" #expr "', the actual value is larger than 0x" #constant ".");

#include "AsmOffsets.h"

};

#ifdef _MSC_VER
namespace { char WorkaroundLNK4221Warning; };
#endif
