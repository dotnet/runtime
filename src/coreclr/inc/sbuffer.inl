// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef _SBUFFER_INL_
#define _SBUFFER_INL_

#include "sbuffer.h"

#if defined(_MSC_VER)
#pragma inline_depth (20)
#endif

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4702) // Disable bogus unreachable code warning
#endif // _MSC_VER

inline SBuffer::SBuffer(PreallocFlag flag, void *buffer, COUNT_T size)
  : m_size(0),
    m_allocation(NULL),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    m_buffer = UseBuffer((BYTE *) buffer, &size);
    m_allocation = size;

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline SBuffer::SBuffer()
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline SBuffer::SBuffer(COUNT_T size)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACT_VOID
    {;
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Resize(size);

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline SBuffer::SBuffer(const SBuffer &buffer)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(buffer.Check());
        POSTCONDITION(Equals(buffer));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Set(buffer);

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline SBuffer::SBuffer(const BYTE *buffer, COUNT_T size)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(Equals(buffer, size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Set(buffer, size);

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}


inline SBuffer::SBuffer(ImmutableFlag immutable, const BYTE *buffer, COUNT_T size)
  : m_size(size),
    m_allocation(size),
    m_flags(IMMUTABLE),
    m_buffer(const_cast<BYTE*>(buffer))
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(Equals(buffer, size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline SBuffer::~SBuffer()
{
    CONTRACT_VOID
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (IsAllocated())
    {
        DeleteBuffer(m_buffer, m_allocation);
    }

#ifdef _DEBUG
    m_revision = 0;
#endif

    RETURN;
}

inline void SBuffer::Set(const SBuffer &buffer)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(buffer.Check());
        POSTCONDITION(Equals(buffer));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (buffer.IsImmutable()
        && (IsImmutable() || m_allocation < buffer.GetSize()))
    {
        // Share immutable block rather than reallocate and copy
        // (Note that we prefer to copy to our buffer if we
        // don't have to reallocate it.)

        if (IsAllocated())
            DeleteBuffer(m_buffer, m_allocation);

        m_size = buffer.m_size;
        m_allocation = buffer.m_allocation;
        m_buffer = buffer.m_buffer;
        m_flags = buffer.m_flags;

#if _DEBUG
        // Increment our revision to invalidate iterators
        m_revision++;
#endif

    }
    else
    {
        Resize(buffer.m_size, DONT_PRESERVE);
        EnsureMutable();

        // PreFix seems to think it can choose m_allocation==0 and buffer.m_size > 0 here.
        // From the code for Resize and EnsureMutable, this is clearly impossible.
        PREFIX_ASSUME( (this->m_buffer != NULL) || (buffer.m_size == 0) );

        MoveMemory(m_buffer, buffer.m_buffer, buffer.m_size);
    }

    RETURN;
}

inline void SBuffer::Set(const BYTE *buffer, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(Equals(buffer, size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Resize(size);
    EnsureMutable();

    // PreFix seems to think it can choose m_allocation==0 and size > 0 here.
    // From the code for Resize, this is clearly impossible.
    PREFIX_ASSUME( (this->m_buffer != NULL) || (size == 0) );

    MoveMemory(m_buffer, buffer, size);

    RETURN;
}

inline void SBuffer::SetImmutable(const BYTE *buffer, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(Equals(buffer, size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;

    }
    CONTRACT_END;

    SBuffer temp(Immutable, buffer, size);

    {
        // This can't really throw
        CONTRACT_VIOLATION(ThrowsViolation);
        Set(temp);
    }

    RETURN;
}

inline COUNT_T SBuffer::GetSize() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_size;
}

inline void SBuffer::SetSize(COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Resize(size);

    RETURN;
}

inline void SBuffer::MaximizeSize()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (!IsImmutable())
        Resize(m_allocation);

    RETURN;
}

inline COUNT_T SBuffer::GetAllocation() const
{
    CONTRACT(COUNT_T)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN m_allocation;
}

inline void SBuffer::Preallocate(COUNT_T allocation) const
{
    CONTRACT_VOID
    {
        if (allocation) THROWS; else NOTHROW;
        INSTANCE_CHECK;
        PRECONDITION(CheckAllocation(allocation));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (allocation > m_allocation)
        const_cast<SBuffer *>(this)->ReallocateBuffer(allocation, PRESERVE);

    RETURN;
}

inline void SBuffer::Trim() const
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (!IsImmutable())
        const_cast<SBuffer *>(this)->ReallocateBuffer(m_size, PRESERVE);

    RETURN;
}

inline void SBuffer::Zero()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    ZeroMemory(m_buffer, m_size);

    RETURN;
}

inline void SBuffer::Fill(BYTE value)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    memset(m_buffer, value, m_size);

    RETURN;
}

inline void SBuffer::Fill(const Iterator &i, BYTE value, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    memset(i.m_ptr, value, size);

    RETURN;
}

inline void SBuffer::Copy(const Iterator &to, const CIterator &from, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(to, size));
        PRECONDITION(CheckIteratorRange(from, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    DebugDestructBuffer(to.m_ptr, size);

    DebugCopyConstructBuffer(to.m_ptr, from.m_ptr, size);

    RETURN;
}

inline void SBuffer::Move(const Iterator &to, const CIterator &from, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(to, size));
        PRECONDITION(CheckIteratorRange(from, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    DebugDestructBuffer(to.m_ptr, size);

    DebugMoveBuffer(to.m_ptr, from.m_ptr, size);

    DebugConstructBuffer(from.m_ptr, size);

    RETURN;
}

inline void SBuffer::Copy(const Iterator &i, const SBuffer &source)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, source.GetSize()));
        PRECONDITION(source.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    DebugDestructBuffer(i.m_ptr, source.m_size);

    DebugCopyConstructBuffer(i.m_ptr, source.m_buffer, source.m_size);

    RETURN;
}

inline void SBuffer::Copy(const Iterator &i, const void *source, COUNT_T size)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(CheckIteratorRange(i, size));
        PRECONDITION(CheckPointer(source, size == 0 ? NULL_OK : NULL_NOT_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    DebugDestructBuffer(i.m_ptr, size);

    DebugCopyConstructBuffer(i.m_ptr, (const BYTE *) source, size);

    RETURN;
}

inline void SBuffer::Copy(void *dest, const CIterator &i, COUNT_T size)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(CheckIteratorRange(i, size));
        PRECONDITION(CheckPointer(dest, size == 0 ? NULL_OK : NULL_NOT_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    memcpy(dest, i.m_ptr, size);

    RETURN;
}

inline void SBuffer::Insert(const Iterator &i, const SBuffer &source)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        PRECONDITION(CheckIteratorRange(i,0));
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Replace(i, 0, source.GetSize());
    Copy(i, source, source.GetSize());

    RETURN;
}

inline void SBuffer::Insert(const Iterator &i, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        PRECONDITION(CheckIteratorRange(i,0));
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Replace(i, 0, size);

    RETURN;
}

inline void SBuffer::Clear()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Delete(Begin(), GetSize());

    RETURN;
}

inline void SBuffer::Delete(const Iterator &i, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Replace(i, size, 0);

    RETURN;
}

inline void SBuffer::Replace(const Iterator &i, COUNT_T deleteSize, const SBuffer &insert)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, deleteSize));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Replace(i, deleteSize, insert.GetSize());
    Copy(i, insert, insert.GetSize());

    RETURN;
}

inline int SBuffer::Compare(const SBuffer &compare) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(compare.Check());
        POSTCONDITION(RETVAL == -1 || RETVAL == 0 || RETVAL == 1);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN Compare(compare.m_buffer, compare.m_size);
}

inline int SBuffer::Compare(const BYTE *compare, COUNT_T size) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(compare));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(RETVAL == -1 || RETVAL == 0 || RETVAL == 1);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T smaller;
    int equals;
    int result;

    if (m_size < size)
    {
        smaller = m_size;
        equals = -1;
    }
    else if (m_size > size)
    {
        smaller = size;
        equals = 1;
    }
    else
    {
        smaller = size;
        equals = 0;
    }

    result = memcmp(m_buffer, compare, size);

    if (result == 0)
        RETURN equals;
    else
        RETURN result;
}

inline BOOL SBuffer::Equals(const SBuffer &compare) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(compare.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN Equals(compare.m_buffer, compare.m_size);
}

inline BOOL SBuffer::Equals(const BYTE *compare, COUNT_T size) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(compare));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (m_size != size)
        RETURN FALSE;
    else
        RETURN (memcmp(m_buffer, compare, size) == 0);
}

inline BOOL SBuffer::Match(const CIterator &i, const SBuffer &match) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(match.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN Match(i, match.m_buffer, match.m_size);
}

inline BOOL SBuffer::Match(const CIterator &i, const BYTE *match, COUNT_T size) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(match));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T remaining = (COUNT_T) (m_buffer + m_size - i.m_ptr);

    if (remaining < size)
        RETURN FALSE;

    RETURN (memcmp(i.m_ptr, match, size) == 0);
}

//----------------------------------------------------------------------------
// EnsureMutable
// Ensures that the buffer is mutable
//----------------------------------------------------------------------------
inline void SBuffer::EnsureMutable() const
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckBufferClosed());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (IsImmutable())
        const_cast<SBuffer *>(this)->ReallocateBuffer(m_allocation, PRESERVE);

    RETURN;
}

//----------------------------------------------------------------------------
// Resize
// Change the visible size of the buffer; realloc if necessary
//----------------------------------------------------------------------------
FORCEINLINE void SBuffer::Resize(COUNT_T size, Preserve preserve)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(GetSize() == size);
        POSTCONDITION(m_allocation >= GetSize());
        POSTCONDITION(CheckInvariant(*this));
        if (size > 0) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#ifdef _DEBUG
    // Change our revision
    m_revision++;
#endif

    SCOUNT_T delta = size - m_size;

    if (delta < 0)
        DebugDestructBuffer(m_buffer + size, -delta);

    // Only actually allocate if we are growing
    if (size > m_allocation)
        ReallocateBuffer(size, preserve);

    if (delta > 0)
        DebugConstructBuffer(m_buffer + m_size, delta);

    m_size = size;

    RETURN;
}

//----------------------------------------------------------------------------
// ResizePadded
// Change the visible size of the buffer; realloc if necessary
// add extra space to minimize further growth
//----------------------------------------------------------------------------
inline void SBuffer::ResizePadded(COUNT_T size, Preserve preserve)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        POSTCONDITION(GetSize() == size);
        POSTCONDITION(m_allocation >= GetSize());
        POSTCONDITION(CheckInvariant(*this));
        if (size > 0) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#ifdef _DEBUG
    // Change our revision
    m_revision++;
#endif

    SCOUNT_T delta = size - m_size;

    if (delta < 0)
        DebugDestructBuffer(m_buffer + size, -delta);

    // Only actually allocate if we are growing
    if (size > m_allocation)
    {
        COUNT_T padded = (size*3)/2;

        ReallocateBuffer(padded, preserve);
    }

    if (delta > 0)
        DebugConstructBuffer(m_buffer + m_size, delta);

    m_size = size;

    RETURN;
}

//----------------------------------------------------------------------------
// TweakSize
// An optimized form of Resize, which can only adjust the size within the
// currently allocated range, and never reallocates
//----------------------------------------------------------------------------
inline void SBuffer::TweakSize(COUNT_T size)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(size <= GetAllocation());
        POSTCONDITION(GetSize() == size);
        POSTCONDITION(CheckInvariant(*this));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#ifdef _DEBUG
    // Change our revision
    m_revision++;
#endif

    SCOUNT_T delta = size - m_size;

    if (delta < 0)
        DebugDestructBuffer(m_buffer + size, -delta);
    else
        DebugConstructBuffer(m_buffer + m_size, delta);

    m_size = size;

    RETURN;
}

//-----------------------------------------------------------------------------
// SBuffer allocates all memory via NewBuffer & DeleteBuffer members.
// If SBUFFER_CANARY_CHECKS is defined, NewBuffer will place Canaries at the start
// and end of the buffer to detect overflows.
//-----------------------------------------------------------------------------

#ifdef _DEBUG
#define SBUFFER_CANARY_CHECKS 1
#endif

#ifdef SBUFFER_CANARY_CHECKS

// The value we place at the start/end of the buffer,
static const UINT64 SBUFFER_CANARY_VALUE = UI64(0xD00BED00BED00BAA);

// Expose the quantity of padding needed when providing a prealloced
// buffer. This is an unrolled version of the actualAllocation calculated
// below for use as a constant value for InlineSString<X> to use. It is
// padded with one additional sizeof(SBUFFER_CANARY_VALUE) to account for
// possible alignment problems issues (pre- and post-padding).
#define SBUFFER_PADDED_SIZE(desiredUsefulSize) \
    ((((SIZE_T)(desiredUsefulSize) + sizeof(SBUFFER_CANARY_VALUE) - 1) & \
    ~(sizeof(SBUFFER_CANARY_VALUE)-1)) + 3 * sizeof(SBUFFER_CANARY_VALUE))

#else // SBUFFER_CANARY_CHECKS

#define SBUFFER_PADDED_SIZE(desiredUsefulSize) (desiredUsefulSize)

#endif // SBUFFER_CANARY_CHECKS else

// Must match expected guaranteed alignment of new []
#ifdef ALIGN_ACCESS
static const int SBUFFER_ALIGNMENT = ALIGN_ACCESS;
#else
static const int SBUFFER_ALIGNMENT = 4;
#endif

//----------------------------------------------------------------------------
// Allocate memory, use canaries.
//----------------------------------------------------------------------------
inline BYTE *SBuffer::NewBuffer(COUNT_T allocation)
{
    CONTRACT(BYTE*)
    {
        PRECONDITION(CheckSize(allocation));
        PRECONDITION(allocation > 0);
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#ifdef SBUFFER_CANARY_CHECKS

    COUNT_T alignPadding = AlignmentPad(allocation, sizeof(SBUFFER_CANARY_VALUE));
    COUNT_T actualAllocation= sizeof(SBUFFER_CANARY_VALUE) + allocation + alignPadding + sizeof(SBUFFER_CANARY_VALUE);
    BYTE *raw = new BYTE [actualAllocation];

    *(UINT64*) raw = SBUFFER_CANARY_VALUE;
    *(UINT64*) (raw + sizeof(SBUFFER_CANARY_VALUE) + allocation + alignPadding) = SBUFFER_CANARY_VALUE;

    BYTE *buffer = raw + sizeof(SBUFFER_CANARY_VALUE);

#else

    BYTE *buffer = new BYTE [allocation];

#endif

    DebugStompUnusedBuffer(buffer, allocation);

    CONSISTENCY_CHECK(CheckBuffer(buffer, allocation));

    RETURN buffer;
}

//----------------------------------------------------------------------------
// Use existing memory, use canaries.
//----------------------------------------------------------------------------
inline BYTE *SBuffer::UseBuffer(BYTE *buffer, COUNT_T *allocation)
{
    CONTRACT(BYTE*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC_HOST_ONLY;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(*allocation));
//        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(CheckSize(*allocation));
    }
    CONTRACT_END;

#ifdef SBUFFER_CANARY_CHECKS

    COUNT_T prepad = AlignmentPad((SIZE_T) buffer, sizeof(SBUFFER_CANARY_VALUE));
    COUNT_T postpad = AlignmentTrim((SIZE_T) buffer+*allocation, sizeof(SBUFFER_CANARY_VALUE));

    SCOUNT_T usableAllocation = *allocation - prepad - sizeof(SBUFFER_CANARY_VALUE) - sizeof(SBUFFER_CANARY_VALUE) - postpad;
    if (usableAllocation <= 0)
    {
        buffer = NULL;
        *allocation = 0;
    }
    else
    {
        BYTE *result = buffer + prepad + sizeof(SBUFFER_CANARY_VALUE);

        *(UINT64*) (buffer + prepad) = SBUFFER_CANARY_VALUE;
        *(UINT64*) (buffer + prepad + sizeof(SBUFFER_CANARY_VALUE) + usableAllocation) = SBUFFER_CANARY_VALUE;

        buffer = result;
        *allocation = usableAllocation;
    }

#endif

    DebugStompUnusedBuffer(buffer, *allocation);

    CONSISTENCY_CHECK(CheckBuffer(buffer, *allocation));

    RETURN buffer;
}

//----------------------------------------------------------------------------
// Free memory allocated by NewHelper
//----------------------------------------------------------------------------
inline void SBuffer::DeleteBuffer(BYTE *buffer, COUNT_T allocation)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckSize(allocation));
        POSTCONDITION(CheckPointer(buffer));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    CONSISTENCY_CHECK(CheckBuffer(buffer, allocation));

#ifdef SBUFFER_CANARY_CHECKS

    delete [] (buffer - sizeof(SBUFFER_CANARY_VALUE));

#else

    delete [] buffer;

#endif

    RETURN;
}

//----------------------------------------------------------------------------
// Check the buffer at the given address. The memory must have been a pointer
// returned by NewHelper.
//----------------------------------------------------------------------------
inline CHECK SBuffer::CheckBuffer(const BYTE *buffer, COUNT_T allocation) const
{
    CONTRACT_CHECK
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        PRECONDITION(CheckPointer(buffer));
    }
    CONTRACT_CHECK_END;

    if (allocation > 0)
    {
#ifdef SBUFFER_CANARY_CHECKS
        const BYTE *raw = buffer - sizeof(SBUFFER_CANARY_VALUE);

        COUNT_T alignPadding    = ((allocation + (sizeof(SBUFFER_CANARY_VALUE) - 1)) & ~((sizeof(SBUFFER_CANARY_VALUE) - 1))) - allocation;

        CHECK_MSG(*(UINT64*) raw == SBUFFER_CANARY_VALUE, "SBuffer underflow");
        CHECK_MSG(*(UINT64*) (raw + sizeof(SBUFFER_CANARY_VALUE) + allocation + alignPadding) == SBUFFER_CANARY_VALUE, "SBuffer overflow");

#endif

        CHECK_MSG((((SIZE_T)buffer) & (SBUFFER_ALIGNMENT-1)) == 0, "SBuffer not properly aligned");
    }

    CHECK_OK;
}


inline BYTE *SBuffer::OpenRawBuffer(COUNT_T size)
{
    CONTRACT(BYTE*)
    {
#if _DEBUG
        PRECONDITION_MSG(!IsOpened(), "Can't nest calls to OpenBuffer()");
#endif
        PRECONDITION(CheckSize(size));
        POSTCONDITION(GetSize() == size);
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    Resize(size);
    EnsureMutable();

#if _DEBUG
    SetOpened();
#endif

    RETURN m_buffer;
}

//----------------------------------------------------------------------------
// Close an open buffer. Assumes that we wrote exactly number of characters
// we requested in OpenBuffer.
//----------------------------------------------------------------------------
inline void SBuffer::CloseRawBuffer()
{
    CONTRACT_VOID
    {
#if _DEBUG
        PRECONDITION(IsOpened());
#endif
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    CloseRawBuffer(m_size);

    RETURN;
}

//----------------------------------------------------------------------------
// CloseBuffer() tells the SBuffer that we're done using the unsafe buffer.
// finalSize is the count of bytes actually used (so we can set m_count).
// This is important if we request a buffer larger than what we actually
// used.
//----------------------------------------------------------------------------
inline void SBuffer::CloseRawBuffer(COUNT_T finalSize)
{
    CONTRACT_VOID
    {
#if _DEBUG
        PRECONDITION_MSG(IsOpened(),  "Can only CloseRawBuffer() after a call to OpenRawBuffer()");
#endif
        PRECONDITION(CheckSize(finalSize));
        PRECONDITION_MSG(finalSize <= GetSize(), "Can't use more characters than requested via OpenRawBuffer()");
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

#if _DEBUG
    ClearOpened();
#endif

    TweakSize(finalSize);

    CONSISTENCY_CHECK(CheckBuffer(m_buffer, m_allocation));

    RETURN;
}

inline SBuffer::operator const void *() const
{
    LIMITED_METHOD_CONTRACT;

    return (void *) m_buffer;
}

inline SBuffer::operator const BYTE *() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_buffer;
}

inline BYTE &SBuffer::operator[](int index)
{
    LIMITED_METHOD_CONTRACT;

    return m_buffer[index];
}

inline const BYTE &SBuffer::operator[](int index) const
{
    LIMITED_METHOD_CONTRACT;

    return m_buffer[index];
}

inline SBuffer::Iterator SBuffer::Begin()
{
    CONTRACT(SBuffer::Iterator)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    // This is a bit unfortunate to have to do here, but it's our
    // last opportunity before possibly doing a *i= with the iterator
    EnsureMutable();

    RETURN Iterator(this, 0);
}

inline SBuffer::Iterator SBuffer::End()
{
    CONTRACT(SBuffer::Iterator)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // This is a bit unfortunate to have to do here, but it's our
    // last opportunity before possibly doing a *i= with the iterator
    EnsureMutable();

    RETURN Iterator(this, m_size);
}

inline SBuffer::CIterator SBuffer::Begin() const
{
    CONTRACT(SBuffer::CIterator)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN SBuffer::CIterator(this, 0);
}

inline SBuffer::CIterator SBuffer::End() const
{
    CONTRACT(SBuffer::CIterator)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN CIterator(const_cast<SBuffer*>(this), m_size);
}

inline BOOL SBuffer::IsAllocated() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_flags & ALLOCATED) != 0;
}

inline void SBuffer::SetAllocated()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_flags |= ALLOCATED;
}

inline void SBuffer::ClearAllocated()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_flags &= ~ALLOCATED;
}

inline BOOL SBuffer::IsImmutable() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_flags & IMMUTABLE) != 0;
}

inline void SBuffer::SetImmutable()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_flags |= IMMUTABLE;
}

inline void SBuffer::ClearImmutable()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_flags &= ~IMMUTABLE;
}

inline BOOL SBuffer::IsFlag1() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_flags & FLAG1) != 0;
}

inline void SBuffer::SetFlag1()
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_flags |= FLAG1;
}

inline void SBuffer::ClearFlag1()
{
    LIMITED_METHOD_CONTRACT;

    m_flags &= ~FLAG1;
}

inline BOOL SBuffer::IsFlag2() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_flags & FLAG2) != 0;
}

inline void SBuffer::SetFlag2()
{
    LIMITED_METHOD_CONTRACT;

    m_flags |= FLAG2;
}

inline void SBuffer::ClearFlag2()
{
    LIMITED_METHOD_CONTRACT;

    m_flags &= ~FLAG2;
}

inline BOOL SBuffer::IsFlag3() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_flags & FLAG3) != 0;
}

inline void SBuffer::SetFlag3()
{
    LIMITED_METHOD_CONTRACT;

    m_flags |= FLAG3;
}

inline void SBuffer::ClearFlag3()
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_flags &= ~FLAG3;
}

inline int SBuffer::GetRepresentationField() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_flags & REPRESENTATION_MASK);
}

inline void SBuffer::SetRepresentationField(int value)
{
    CONTRACT_VOID
    {
        PRECONDITION((value & ~REPRESENTATION_MASK) == 0);
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    m_flags &= ~REPRESENTATION_MASK;
    m_flags |= value;

    RETURN;
}

#if _DEBUG
inline BOOL SBuffer::IsOpened() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_flags & OPENED) != 0;
}

inline void SBuffer::SetOpened()
{
    LIMITED_METHOD_CONTRACT;

    m_flags |= OPENED;
}

inline void SBuffer::ClearOpened()
{
    LIMITED_METHOD_CONTRACT;

    m_flags &= ~OPENED;
}
#endif

inline void SBuffer::DebugMoveBuffer(_Out_writes_bytes_(size) BYTE *to, BYTE *from, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(to, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckPointer(from, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (size == 0) // special case
      RETURN;

    // Handle overlapping ranges
    if (to > from && to < from + size)
        CONSISTENCY_CHECK(CheckUnusedBuffer(from + size, (COUNT_T) (to - from)));
    else if (to < from && to + size > from)
        CONSISTENCY_CHECK(CheckUnusedBuffer(to, (COUNT_T) (from - to)));
    else
        CONSISTENCY_CHECK(CheckUnusedBuffer(to, size));

    memmove(to, from, size);

    // Handle overlapping ranges
    if (to > from && to < from + size)
        DebugStompUnusedBuffer(from, (COUNT_T) (to - from));
    else if (to < from && to + size > from)
        DebugStompUnusedBuffer(to + size, (COUNT_T) (from - to));
    else
        DebugStompUnusedBuffer(from, size);

    RETURN;
}

inline void SBuffer::DebugCopyConstructBuffer(_Out_writes_bytes_(size) BYTE *to, const BYTE *from, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(to, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckPointer(from, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (size != 0) {
        CONSISTENCY_CHECK(CheckUnusedBuffer(to, size));
        memmove(to, from, size);
    }

    RETURN;
}

inline void SBuffer::DebugConstructBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        DEBUG_ONLY;
    }
    CONTRACT_END;

    if (size != 0) {
      CONSISTENCY_CHECK(CheckUnusedBuffer(buffer, size));
    }

    RETURN;
}

inline void SBuffer::DebugDestructBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (size != 0)
    {
        DebugStompUnusedBuffer(buffer, size);
    }

    RETURN;
}

static const BYTE GARBAGE_FILL_CHARACTER = '$';

extern const DWORD g_garbageFillBuffer[];

inline void SBuffer::DebugStompUnusedBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        DEBUG_ONLY;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

#if _DEBUG
    if (!IsImmutable()
        || buffer < m_buffer || buffer > m_buffer + m_allocation) // Allocating a new buffer
    {
        // Whack the memory
        if (size > GARBAGE_FILL_BUFFER_SIZE) size = GARBAGE_FILL_BUFFER_SIZE;
        memset(buffer, GARBAGE_FILL_CHARACTER, size);
    }
#endif

    RETURN;
}

#if _DEBUG
inline BOOL SBuffer::EnsureGarbageCharOnly(const BYTE *buffer, COUNT_T size)
{
    LIMITED_METHOD_CONTRACT;
    BOOL bRet = TRUE;
    if (size > GARBAGE_FILL_BUFFER_SIZE)
    {
        size = GARBAGE_FILL_BUFFER_SIZE;
    }
    if (bRet && size > 0)
    {
        bRet &= (memcmp(buffer, g_garbageFillBuffer, size) == 0);
    }
    return bRet;
}
#endif

inline CHECK SBuffer::CheckUnusedBuffer(const BYTE *buffer, COUNT_T size) const
{
    WRAPPER_NO_CONTRACT;
    // This check is too expensive.
#if 0 // _DEBUG
    if (!IsImmutable()
        || buffer < m_buffer || buffer > m_buffer + m_allocation) // Allocating a new buffer
    {
        if (!SBuffer::EnsureGarbageCharOnly(buffer, size))
        {
            CHECK_FAIL("Overwrite of unused buffer region found");
        }
    }
#endif
    CHECK_OK;
}

inline CHECK SBuffer::Check() const
{
    WRAPPER_NO_CONTRACT;
    CHECK(CheckBufferClosed());
    CHECK_OK;
}

inline CHECK SBuffer::Invariant() const
{
    LIMITED_METHOD_CONTRACT;

    CHECK_OK;
}

inline CHECK SBuffer::InternalInvariant() const
{
    WRAPPER_NO_CONTRACT;
    CHECK(m_size <= m_allocation);

    CHECK(CheckUnusedBuffer(m_buffer + m_size, m_allocation - m_size));

    if (IsAllocated())
        CHECK(CheckBuffer(m_buffer, m_allocation));

    CHECK_OK;
}

inline CHECK SBuffer::CheckBufferClosed() const
{
    WRAPPER_NO_CONTRACT;
#if _DEBUG
    CHECK_MSG(!IsOpened(), "Cannot use buffer API while raw open is in progress");
#endif
    CHECK_OK;
}

inline CHECK SBuffer::CheckSize(COUNT_T size)
{
    LIMITED_METHOD_CONTRACT;
    // !todo: add any range checking here
    CHECK_OK;
}

inline CHECK SBuffer::CheckAllocation(COUNT_T size)
{
    LIMITED_METHOD_CONTRACT;

    // !todo: add any range checking here
    CHECK_OK;
}

inline CHECK SBuffer::CheckIteratorRange(const CIterator &i) const
{
    WRAPPER_NO_CONTRACT;
    CHECK(i.Check());
    CHECK(i.CheckContainer(this));
    CHECK(i >= Begin());
    CHECK(i < End());
    CHECK_OK;
}

inline CHECK SBuffer::CheckIteratorRange(const CIterator &i, COUNT_T size) const
{
    WRAPPER_NO_CONTRACT;
    CHECK(i.Check());
    CHECK(i.CheckContainer(this));
    CHECK(i >= Begin());
    CHECK(i + size <= End());
    CHECK_OK;
}

inline SBuffer::Index::Index()
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_ptr = NULL;
}

inline SBuffer::Index::Index(SBuffer *container, SCOUNT_T index)
  : CheckedIteratorBase<SBuffer>(container)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_ptr = container->m_buffer + index;
}

inline BYTE &SBuffer::Index::GetAt(SCOUNT_T delta) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_ptr[delta];
}

inline void SBuffer::Index::Skip(SCOUNT_T delta)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    m_ptr += delta;
}

inline SCOUNT_T SBuffer::Index::Subtract(const Index &i) const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    return (SCOUNT_T) (m_ptr - i.m_ptr);
}

inline CHECK SBuffer::Index::DoCheck(SCOUNT_T delta) const
{
    WRAPPER_NO_CONTRACT;
#if _DEBUG
    CHECK(m_ptr + delta >= GetContainerDebug()->m_buffer);
    CHECK(m_ptr + delta < GetContainerDebug()->m_buffer + GetContainerDebug()->m_size);
#endif
    CHECK_OK;
}

inline void SBuffer::Index::Resync(const SBuffer *buffer, BYTE *value) const
{
    CONTRACT_VOID
    {
        // INSTANCE_CHECK -  Iterator is out of sync with its object now by definition
        POSTCONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(buffer));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    const_cast<Index*>(this)->CheckedIteratorBase<SBuffer>::Resync(const_cast<SBuffer*>(buffer));
    const_cast<Index*>(this)->m_ptr = value;

    RETURN;
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

#endif  // _SBUFFER_INL_
