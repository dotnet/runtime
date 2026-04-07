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
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    m_buffer = UseBuffer((BYTE *) buffer, &size);
    m_allocation = size;

#ifdef _DEBUG
    m_revision = 0;
#endif

    CONSISTENCY_CHECK(Check());
}

inline SBuffer::SBuffer()
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_revision = 0;
#endif

    CONSISTENCY_CHECK(Check());
}

inline SBuffer::SBuffer(COUNT_T size)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACTL
    {;
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Resize(size);

#ifdef _DEBUG
    m_revision = 0;
#endif

    CONSISTENCY_CHECK(Check());
}

inline SBuffer::SBuffer(const SBuffer &buffer)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACTL
    {
        PRECONDITION(buffer.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Set(buffer);

#ifdef _DEBUG
    m_revision = 0;
#endif

    _ASSERTE(Equals(buffer));
    CONSISTENCY_CHECK(Check());
}

inline SBuffer::SBuffer(SBuffer &&buffer)
{
    CONTRACTL
    {
        PRECONDITION(buffer.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_size = buffer.m_size;
    m_allocation = buffer.m_allocation;
    m_flags = buffer.m_flags;
    m_buffer = buffer.m_buffer;

#ifdef _DEBUG
    m_revision = buffer.m_revision;
#endif

    buffer.InitializeInstance();

    _ASSERTE(Check());
    CONSISTENCY_CHECK(Check());
}

inline SBuffer::SBuffer(const BYTE *buffer, COUNT_T size)
  : m_size(0),
    m_allocation(0),
    m_flags(0),
    m_buffer(NULL)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Set(buffer, size);

#ifdef _DEBUG
    m_revision = 0;
#endif

    _ASSERTE(Equals(buffer, size));
    CONSISTENCY_CHECK(Check());
}


inline SBuffer::SBuffer(ImmutableFlag immutable, const BYTE *buffer, COUNT_T size)
  : m_size(size),
    m_allocation(size),
    m_flags(IMMUTABLE),
    m_buffer(const_cast<BYTE*>(buffer))
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_revision = 0;
#endif

    _ASSERTE(Equals(buffer, size));
    CONSISTENCY_CHECK(Check());
}

inline SBuffer::~SBuffer()
{
    CONTRACTL
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (IsAllocated())
    {
        DeleteBuffer(m_buffer, m_allocation);
    }

#ifdef _DEBUG
    m_revision = 0;
#endif

    return;
}

inline void SBuffer::InitializeInstance()
{
    m_size = 0;
    m_allocation = 0;
    m_flags = 0;
    m_buffer = NULL;

#ifdef _DEBUG
    m_revision = 0;
#endif
}

inline void SBuffer::Set(const SBuffer &buffer)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(buffer.Check());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

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
        _ASSERTE( (this->m_buffer != NULL) || (buffer.m_size == 0) );

        MoveMemory(m_buffer, buffer.m_buffer, buffer.m_size);
    }

    _ASSERTE(Equals(buffer));
    return;
}

inline void SBuffer::Set(const BYTE *buffer, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Resize(size);
    EnsureMutable();

    // PreFix seems to think it can choose m_allocation==0 and size > 0 here.
    // From the code for Resize, this is clearly impossible.
    _ASSERTE( (this->m_buffer != NULL) || (size == 0) );

    if (size != 0)
        MoveMemory(m_buffer, buffer, size);

    _ASSERTE(Equals(buffer, size));
    return;
}

inline void SBuffer::SetImmutable(const BYTE *buffer, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;

    }
    CONTRACTL_END;

    SBuffer temp(Immutable, buffer, size);

    {
        // This can't really throw
        CONTRACT_VIOLATION(ThrowsViolation);
        Set(temp);
    }

    _ASSERTE(Equals(buffer, size));
    return;
}

inline COUNT_T SBuffer::GetSize() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_size;
}

inline void SBuffer::SetSize(COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Resize(size);

    return;
}

inline void SBuffer::MaximizeSize()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!IsImmutable())
        Resize(m_allocation);

    return;
}

inline COUNT_T SBuffer::GetAllocation() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return m_allocation;
}

inline void SBuffer::Preallocate(COUNT_T allocation)
{
    CONTRACTL
    {
        if (allocation) THROWS; else NOTHROW;
        INSTANCE_CHECK;
        PRECONDITION(CheckAllocation(allocation));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (allocation > m_allocation)
        ReallocateBuffer(allocation, PRESERVE);

    return;
}

inline void SBuffer::Trim()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!IsImmutable())
        ReallocateBuffer(m_size, PRESERVE);

    return;
}

inline void SBuffer::Zero()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ZeroMemory(m_buffer, m_size);

    return;
}

inline void SBuffer::Fill(BYTE value)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    memset(m_buffer, value, m_size);

    return;
}

inline void SBuffer::Fill(const Iterator &i, BYTE value, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    memset(i.m_ptr, value, size);

    return;
}

inline void SBuffer::Copy(const Iterator &to, const CIterator &from, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(to, size));
        PRECONDITION(CheckIteratorRange(from, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebugDestructBuffer(to.m_ptr, size);

    DebugCopyConstructBuffer(to.m_ptr, from.m_ptr, size);

    return;
}

inline void SBuffer::Move(const Iterator &to, const CIterator &from, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(to, size));
        PRECONDITION(CheckIteratorRange(from, size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebugDestructBuffer(to.m_ptr, size);

    DebugMoveBuffer(to.m_ptr, from.m_ptr, size);

    DebugConstructBuffer(from.m_ptr, size);

    return;
}

inline void SBuffer::Copy(const Iterator &i, const SBuffer &source)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, source.GetSize()));
        PRECONDITION(source.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebugDestructBuffer(i.m_ptr, source.m_size);

    DebugCopyConstructBuffer(i.m_ptr, source.m_buffer, source.m_size);

    return;
}

inline void SBuffer::Copy(const Iterator &i, const void *source, COUNT_T size)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(CheckIteratorRange(i, size));
        PRECONDITION(CheckPointer(source, size == 0 ? NULL_OK : NULL_NOT_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DebugDestructBuffer(i.m_ptr, size);

    DebugCopyConstructBuffer(i.m_ptr, (const BYTE *) source, size);

    return;
}

inline void SBuffer::Copy(void *dest, const CIterator &i, COUNT_T size)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(CheckIteratorRange(i, size));
        PRECONDITION(CheckPointer(dest, size == 0 ? NULL_OK : NULL_NOT_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    memcpy(dest, i.m_ptr, size);

    return;
}

inline void SBuffer::Insert(const Iterator &i, const SBuffer &source)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        PRECONDITION(CheckIteratorRange(i,0));
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Replace(i, 0, source.GetSize());
    Copy(i, source, source.GetSize());

    return;
}

inline void SBuffer::Insert(const Iterator &i, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        PRECONDITION(CheckIteratorRange(i,0));
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Replace(i, 0, size);

    return;
}

inline void SBuffer::Clear()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Delete(Begin(), GetSize());

    return;
}

inline void SBuffer::Delete(const Iterator &i, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Replace(i, size, 0);

    return;
}

inline void SBuffer::Replace(const Iterator &i, COUNT_T deleteSize, const SBuffer &insert)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, deleteSize));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Replace(i, deleteSize, insert.GetSize());
    Copy(i, insert, insert.GetSize());

    return;
}

inline int SBuffer::Compare(const SBuffer &compare) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(compare.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return Compare(compare.m_buffer, compare.m_size);
}

inline int SBuffer::Compare(const BYTE *compare, COUNT_T size) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(compare));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

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
        {
        _ASSERTE(equals == -1 || equals == 0 || equals == 1);
            return equals;
        }
    else
        {
        _ASSERTE(result == -1 || result == 0 || result == 1);
            return result;
        }
}

inline BOOL SBuffer::Equals(const SBuffer &compare) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(compare.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return Equals(compare.m_buffer, compare.m_size);
}

inline BOOL SBuffer::Equals(const BYTE *compare, COUNT_T size) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(compare));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_size != size)
        return FALSE;
    else
        return (memcmp(m_buffer, compare, size) == 0);
}

inline BOOL SBuffer::Match(const CIterator &i, const SBuffer &match) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(match.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return Match(i, match.m_buffer, match.m_size);
}

inline BOOL SBuffer::Match(const CIterator &i, const BYTE *match, COUNT_T size) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(match));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    COUNT_T remaining = (COUNT_T) (m_buffer + m_size - i.m_ptr);

    if (remaining < size)
        return FALSE;

    return (memcmp(i.m_ptr, match, size) == 0);
}

//----------------------------------------------------------------------------
// EnsureMutable
// Ensures that the buffer is mutable
//----------------------------------------------------------------------------
inline void SBuffer::EnsureMutable() const
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckBufferClosed());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (IsImmutable())
        const_cast<SBuffer *>(this)->ReallocateBuffer(m_allocation, PRESERVE);

    return;
}

//----------------------------------------------------------------------------
// Resize
// Change the visible size of the buffer; realloc if necessary
//----------------------------------------------------------------------------
FORCEINLINE void SBuffer::Resize(COUNT_T size, Preserve preserve)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        if (size > 0) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

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

    _ASSERTE(GetSize() == size);
    _ASSERTE(m_allocation >= GetSize());
    _ASSERTE(CheckInvariant(*this));
    return;
}

//----------------------------------------------------------------------------
// ResizePadded
// Change the visible size of the buffer; realloc if necessary
// add extra space to minimize further growth
//----------------------------------------------------------------------------
inline void SBuffer::ResizePadded(COUNT_T size, Preserve preserve)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        if (size > 0) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

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

    _ASSERTE(GetSize() == size);
    _ASSERTE(m_allocation >= GetSize());
    _ASSERTE(CheckInvariant(*this));
    return;
}

//----------------------------------------------------------------------------
// TweakSize
// An optimized form of Resize, which can only adjust the size within the
// currently allocated range, and never reallocates
//----------------------------------------------------------------------------
inline void SBuffer::TweakSize(COUNT_T size)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckSize(size));
        PRECONDITION(size <= GetAllocation());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

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

    _ASSERTE(GetSize() == size);
    _ASSERTE(CheckInvariant(*this));
    return;
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
static const UINT64 SBUFFER_CANARY_VALUE = 0xD00BED00BED00BAAULL;

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
    CONTRACTL
    {
        PRECONDITION(CheckSize(allocation));
        PRECONDITION(allocation > 0);
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

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

    return buffer;
}

//----------------------------------------------------------------------------
// Use existing memory, use canaries.
//----------------------------------------------------------------------------
inline BYTE *SBuffer::UseBuffer(BYTE *buffer, COUNT_T *allocation)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC_HOST_ONLY;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(*allocation));
//        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACTL_END;

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

    _ASSERTE(CheckSize(*allocation));
    return buffer;
}

//----------------------------------------------------------------------------
// Free memory allocated by NewHelper
//----------------------------------------------------------------------------
inline void SBuffer::DeleteBuffer(BYTE *buffer, COUNT_T allocation)
{
    CONTRACTL
    {
        PRECONDITION(CheckSize(allocation));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    CONSISTENCY_CHECK(CheckBuffer(buffer, allocation));

#ifdef SBUFFER_CANARY_CHECKS

    delete [] (buffer - sizeof(SBUFFER_CANARY_VALUE));

#else

    delete [] buffer;

#endif

    return;
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
    CONTRACTL
    {
#if _DEBUG
        PRECONDITION_MSG(!IsOpened(), "Can't nest calls to OpenBuffer()");
#endif
        PRECONDITION(CheckSize(size));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Resize(size);
    EnsureMutable();

#if _DEBUG
    SetOpened();
#endif

    _ASSERTE(GetSize() == size);
    return m_buffer;
}

//----------------------------------------------------------------------------
// Close an open buffer. Assumes that we wrote exactly number of characters
// we requested in OpenBuffer.
//----------------------------------------------------------------------------
inline void SBuffer::CloseRawBuffer()
{
    CONTRACTL
    {
#if _DEBUG
        PRECONDITION(IsOpened());
#endif
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CloseRawBuffer(m_size);

    return;
}

//----------------------------------------------------------------------------
// CloseBuffer() tells the SBuffer that we're done using the unsafe buffer.
// finalSize is the count of bytes actually used (so we can set m_count).
// This is important if we request a buffer larger than what we actually
// used.
//----------------------------------------------------------------------------
inline void SBuffer::CloseRawBuffer(COUNT_T finalSize)
{
    CONTRACTL
    {
#if _DEBUG
        PRECONDITION_MSG(IsOpened(),  "Can only CloseRawBuffer() after a call to OpenRawBuffer()");
#endif
        PRECONDITION(CheckSize(finalSize));
        PRECONDITION_MSG(finalSize <= GetSize(), "Can't use more characters than requested via OpenRawBuffer()");
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if _DEBUG
    ClearOpened();
#endif

    TweakSize(finalSize);

    CONSISTENCY_CHECK(CheckBuffer(m_buffer, m_allocation));

    return;
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
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    // This is a bit unfortunate to have to do here, but it's our
    // last opportunity before possibly doing a *i= with the iterator
    EnsureMutable();

    return Iterator(this, 0);
}

inline SBuffer::Iterator SBuffer::End()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // This is a bit unfortunate to have to do here, but it's our
    // last opportunity before possibly doing a *i= with the iterator
    EnsureMutable();

    return Iterator(this, m_size);
}

inline SBuffer::CIterator SBuffer::Begin() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return SBuffer::CIterator(this, 0);
}

inline SBuffer::CIterator SBuffer::End() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return CIterator(const_cast<SBuffer*>(this), m_size);
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
    CONTRACTL
    {
        PRECONDITION((value & ~REPRESENTATION_MASK) == 0);
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    m_flags &= ~REPRESENTATION_MASK;
    m_flags |= value;

    return;
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
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(to, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckPointer(from, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (size == 0) // special case
      return;

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

    return;
}

inline void SBuffer::DebugCopyConstructBuffer(_Out_writes_bytes_(size) BYTE *to, const BYTE *from, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(to, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckPointer(from, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (size != 0) {
        CONSISTENCY_CHECK(CheckUnusedBuffer(to, size));
        memmove(to, from, size);
    }

    return;
}

inline void SBuffer::DebugConstructBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (size != 0) {
      CONSISTENCY_CHECK(CheckUnusedBuffer(buffer, size));
    }

    return;
}

inline void SBuffer::DebugDestructBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(buffer, size == 0 ? NULL_OK : NULL_NOT_OK));
        PRECONDITION(CheckSize(size));
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    if (size != 0)
    {
        DebugStompUnusedBuffer(buffer, size);
    }

    return;
}

static const BYTE GARBAGE_FILL_CHARACTER = '$';

extern const DWORD g_garbageFillBuffer[];

inline void SBuffer::DebugStompUnusedBuffer(BYTE *buffer, COUNT_T size)
{
    CONTRACTL
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
    CONTRACTL_END;

#if _DEBUG
    if (!IsImmutable()
        || buffer < m_buffer || buffer > m_buffer + m_allocation) // Allocating a new buffer
    {
        // Whack the memory
        if (size > GARBAGE_FILL_BUFFER_SIZE) size = GARBAGE_FILL_BUFFER_SIZE;
        memset(buffer, GARBAGE_FILL_CHARACTER, size);
    }
#endif

    return;
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
    CONTRACTL
    {
        // INSTANCE_CHECK -  Iterator is out of sync with its object now by definition
        PRECONDITION(CheckPointer(buffer));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END;

    const_cast<Index*>(this)->CheckedIteratorBase<SBuffer>::Resync(const_cast<SBuffer*>(buffer));
    const_cast<Index*>(this)->m_ptr = value;

    return;
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

#endif  // _SBUFFER_INL_
