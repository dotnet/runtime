// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef CHECK_INL_
#define CHECK_INL_

#include "check.h"
#include "clrhost.h"
#include "debugmacros.h"
#include "clrtypes.h"

inline LONG *CHECK::InitTls()
{
#pragma push_macro("HeapAlloc")
#pragma push_macro("GetProcessHeap")
#undef HeapAlloc
#undef GetProcessHeap

    LONG *pCount = (LONG *)::HeapAlloc(GetProcessHeap(), 0, sizeof(LONG));
    if (pCount)
        *pCount = 0;

#pragma pop_macro("HeapAlloc")
#pragma pop_macro("GetProcessHeap")
    ClrFlsSetValue(TlsIdx_Check, pCount);
    ClrFlsAssociateCallback(TlsIdx_Check, ReleaseCheckTls);
    return pCount;
}

inline void CHECK::ReleaseTls(void* pCountTLS)
{
#pragma push_macro("HeapFree")
#pragma push_macro("GetProcessHeap")
#undef HeapFree
#undef GetProcessHeap
        LONG* pCount = (LONG*) pCountTLS;
        if (pCount)
            ::HeapFree(GetProcessHeap(), 0, pCount);

#pragma pop_macro("HeapFree")
#pragma pop_macro("GetProcessHeap")
}

FORCEINLINE BOOL CHECK::EnterAssert()
{
    if (s_neverEnforceAsserts)
        return FALSE;

#ifdef _DEBUG_IMPL
    m_pCount = (LONG *)ClrFlsGetValue(TlsIdx_Check);
    if (!m_pCount)
    {
        m_pCount = InitTls();
        if (!m_pCount)
            return FALSE;
    }

    if (!*m_pCount)
    {
        *m_pCount = 1;
        return TRUE;
    }
    else
        return FALSE;
#else
    // Don't bother doing recursive checks on a free build, since checks should
    // be extremely isolated
    return TRUE;
#endif
}

FORCEINLINE void CHECK::LeaveAssert()
{
#ifdef _DEBUG_IMPL
    *m_pCount = 0;
#endif
}

FORCEINLINE BOOL CHECK::IsInAssert()
{
#ifdef _DEBUG_IMPL
    if (!m_pCount)
        m_pCount = (LONG *)ClrFlsGetValue(TlsIdx_Check);

    if (!m_pCount)
        return FALSE;
    else
        return *m_pCount;
#else
    return FALSE;
#endif
}

FORCEINLINE BOOL CHECK::EnforceAssert()
{
    if (s_neverEnforceAsserts)
        return FALSE;
    else
    {
        CHECK chk;
        return !chk.IsInAssert();
    }
}

FORCEINLINE void CHECK::ResetAssert()
{
    CHECK chk;
    if (chk.IsInAssert())
        chk.LeaveAssert();
}

inline void CHECK::SetAssertEnforcement(BOOL value)
{
    s_neverEnforceAsserts = !value;
}

// Fail records the result of a condition check.  Can take either a
// boolean value or another check result
FORCEINLINE BOOL CHECK::Fail(BOOL condition)
{
#ifdef _DEBUG
    if (!condition)
    {
        m_condition = NULL;
        m_file = NULL;
        m_line = 0;
    }
#endif
    return !condition;
}

FORCEINLINE BOOL CHECK::Fail(const CHECK &check)
{
    m_message = check.m_message;
#ifdef _DEBUG
    if (m_message != NULL)
    {
        m_condition = check.m_condition;
        m_file = check.m_file;
        m_line = check.m_line;
    }
#endif
    return m_message != NULL;
}

#ifndef _DEBUG
FORCEINLINE void CHECK::Setup(LPCSTR message)
{
    m_message = message;
}

FORCEINLINE LPCSTR CHECK::FormatMessage(LPCSTR messageFormat, ...)
{
    return messageFormat;
}
#endif

FORCEINLINE CHECK::operator BOOL ()
{
    return m_message == NULL;
}

FORCEINLINE BOOL CHECK::operator!()
{
    return m_message != NULL;
}

inline CHECK CheckAlignment(UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    CHECK((alignment & (alignment-1)) == 0);
    CHECK_OK;
}

inline CHECK CheckAligned(UINT value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    CHECK(AlignmentTrim(value, alignment) == 0);
    CHECK_OK;
}

#ifndef PLATFORM_UNIX
// For Unix this and the previous function get the same types.
// So, exclude this one.
inline CHECK CheckAligned(ULONG value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    CHECK(AlignmentTrim(value, alignment) == 0);
    CHECK_OK;
}
#endif // PLATFORM_UNIX

inline CHECK CheckAligned(UINT64 value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    CHECK(AlignmentTrim(value, alignment) == 0);
    CHECK_OK;
}

inline CHECK CheckAligned(const void *address, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    CHECK(AlignmentTrim((SIZE_T)address, alignment) == 0);
    CHECK_OK;
}

inline CHECK CheckOverflow(UINT value1, UINT value2)
{
    CHECK(value1 + value2 >= value1);
    CHECK_OK;
}

#if defined(_MSC_VER)
inline CHECK CheckOverflow(ULONG value1, ULONG value2)
{
    CHECK(value1 + value2 >= value1);
    CHECK_OK;
}
#endif

inline CHECK CheckOverflow(UINT64 value1, UINT64 value2)
{
    CHECK(value1 + value2 >= value1);
    CHECK_OK;
}

inline CHECK CheckOverflow(PTR_CVOID address, UINT offset)
{
    TADDR targetAddr = dac_cast<TADDR>(address);
#if POINTER_BITS == 32
    CHECK((UINT) (SIZE_T)(targetAddr) + offset >= (UINT) (SIZE_T) (targetAddr));
#else
    CHECK((UINT64) targetAddr + offset >= (UINT64) targetAddr);
#endif

    CHECK_OK;
}

#if defined(_MSC_VER)
inline CHECK CheckOverflow(const void *address, ULONG offset)
{
#if POINTER_BITS == 32
    CHECK((ULONG) (SIZE_T) address + offset >= (ULONG) (SIZE_T) address);
#else
    CHECK((UINT64) address + offset >= (UINT64) address);
#endif

    CHECK_OK;
}
#endif

inline CHECK CheckOverflow(const void *address, UINT64 offset)
{
#if POINTER_BITS == 32
    CHECK(offset >> 32 == 0);
    CHECK((UINT) (SIZE_T) address + (UINT) offset >= (UINT) (SIZE_T) address);
#else
    CHECK((UINT64) address + offset >= (UINT64) address);
#endif

    CHECK_OK;
}


inline CHECK CheckUnderflow(UINT value1, UINT value2)
{
    CHECK(value1 - value2 <= value1);

    CHECK_OK;
}

#ifndef PLATFORM_UNIX
// For Unix this and the previous function get the same types.
// So, exclude this one.
inline CHECK CheckUnderflow(ULONG value1, ULONG value2)
{
    CHECK(value1 - value2 <= value1);

    CHECK_OK;
}
#endif // PLATFORM_UNIX

inline CHECK CheckUnderflow(UINT64 value1, UINT64 value2)
{
    CHECK(value1 - value2 <= value1);

    CHECK_OK;
}

inline CHECK CheckUnderflow(const void *address, UINT offset)
{
#if POINTER_BITS == 32
    CHECK((UINT) (SIZE_T) address - offset <= (UINT) (SIZE_T) address);
#else
    CHECK((UINT64) address - offset <= (UINT64) address);
#endif

    CHECK_OK;
}

#if defined(_MSC_VER)
inline CHECK CheckUnderflow(const void *address, ULONG offset)
{
#if POINTER_BITS == 32
    CHECK((ULONG) (SIZE_T) address - offset <= (ULONG) (SIZE_T) address);
#else
    CHECK((UINT64) address - offset <= (UINT64) address);
#endif

    CHECK_OK;
}
#endif

inline CHECK CheckUnderflow(const void *address, UINT64 offset)
{
#if POINTER_BITS == 32
    CHECK(offset >> 32 == 0);
    CHECK((UINT) (SIZE_T) address - (UINT) offset <= (UINT) (SIZE_T) address);
#else
    CHECK((UINT64) address - offset <= (UINT64) address);
#endif

    CHECK_OK;
}

inline CHECK CheckUnderflow(const void *address, void *address2)
{
#if POINTER_BITS == 32
    CHECK((UINT) (SIZE_T) address - (UINT) (SIZE_T) address2 <= (UINT) (SIZE_T) address);
#else
    CHECK((UINT64) address - (UINT64) address2 <= (UINT64) address);
#endif

    CHECK_OK;
}

inline CHECK CheckZeroedMemory(const void *memory, SIZE_T size)
{
    CHECK(CheckOverflow(memory, size));

    BYTE *p = (BYTE *) memory;
    BYTE *pEnd = p + size;

    while (p < pEnd)
        CHECK(*p++ == 0);

    CHECK_OK;
}

inline CHECK CheckBounds(const void *rangeBase, UINT32 rangeSize, UINT32 offset)
{
    CHECK(CheckOverflow(dac_cast<PTR_CVOID>(rangeBase), rangeSize));
    CHECK(offset <= rangeSize);
    CHECK_OK;
}

inline CHECK CheckBounds(const void *rangeBase, UINT32 rangeSize, UINT32 offset, UINT32 size)
{
    CHECK(CheckOverflow(dac_cast<PTR_CVOID>(rangeBase), rangeSize));
    CHECK(CheckOverflow(offset, size));
    CHECK(offset + size <= rangeSize);
    CHECK_OK;
}

#endif  // CHECK_INL_

