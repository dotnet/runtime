// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef _SSTRING_INL_
#define _SSTRING_INL_

#include "sstring.h"

#if defined(_MSC_VER)
#pragma inline_depth (20)
#endif

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4702) // Disable bogus unreachable code warning
#endif // _MSC_VER

//#define SSTRING_EXTRA_CHECKS
#ifdef SSTRING_EXTRA_CHECKS
#define SS_CONTRACT CONTRACT
#define SS_CONTRACT_VOID CONTRACT_VOID
#define SS_CONTRACT_END CONTRACT_END
#define SS_RETURN RETURN
#define SS_CONSTRUCTOR_CHECK CONSTRUCTOR_CHECK
#define SS_PRECONDITION PRECONDITION
#define SS_POSTCONDITION POSTCONDITION

#else //SSTRING_EXTRA_CHECKS

#define SS_CONTRACT(x) CONTRACTL
#define SS_CONTRACT_VOID CONTRACTL
#define SS_CONTRACT_END CONTRACTL_END
#define SS_RETURN return
#define SS_CONSTRUCTOR_CHECK
#define SS_PRECONDITION(x)
#define SS_POSTCONDITION(x)
//Do I need this instance check at all?

#endif


// ---------------------------------------------------------------------------
// Inline implementations. Pay no attention to that man behind the curtain.
// ---------------------------------------------------------------------------

// Turn this on to test that these if you are testing common scenarios dealing with
// ASCII strings that do not touch the cases where this family of function differs
// in behavior for expected reasons.
//#define VERIFY_CRT_EQUIVALNCE 1

// Helpers for CRT function equivalance.
/* static */
inline int __cdecl StaticStringHelpers::_stricmp(const CHAR *buffer1, const CHAR *buffer2) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelperA(buffer1, buffer2, 0, TRUE, FALSE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_stricmp(buffer1, buffer2) == 0));
#endif
    return returnValue;

}

/* static */
inline int __cdecl StaticStringHelpers::_strnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelperA(buffer1, buffer2, count, TRUE, TRUE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_strnicmp(buffer1, buffer2, count) == 0));
#endif
    return returnValue;
}

/* static */
inline int __cdecl StaticStringHelpers::_wcsicmp(const WCHAR *buffer1, const WCHAR *buffer2) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelper(buffer1, buffer2, 0, TRUE, FALSE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_wcsicmp(buffer1, buffer2) == 0));
#endif
    return returnValue;

}

/* static */
inline int __cdecl StaticStringHelpers::_wcsnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelper(buffer1, buffer2, count, TRUE, TRUE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_wcsnicmp(buffer1, buffer2, count) == 0));
#endif
    return returnValue;
}

inline int StaticStringHelpers::_tstricmp(const CHAR *buffer1, const CHAR *buffer2)
{
    return _stricmp(buffer1, buffer2);
}

inline int StaticStringHelpers::_tstricmp(const WCHAR *buffer1, const WCHAR *buffer2)
{
    return _wcsicmp(buffer1, buffer2);
}

inline int StaticStringHelpers::_tstrnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count)
{
    return _strnicmp(buffer1, buffer2, count);
}

inline int StaticStringHelpers::_tstrnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count)
{
    return _wcsnicmp(buffer1, buffer2, count);
}
//----------------------------------------------------------------------------
// Default constructor. Sets the string to the empty string.
//----------------------------------------------------------------------------
template<typename TEncoding>
inline EString<TEncoding>::EString()
  : SBuffer(Immutable, StaticStringHelpers::s_EmptyBuffer, sizeof(char_t))
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        POSTCONDITION(IsEmpty());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN;
#else
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;
#endif
}

template<typename TEncoding>
inline EString<TEncoding>::EString(void* buffer, COUNT_T size, bool isAllocated)
    : SBuffer(Prealloc, buffer, size)
{
    // If this buffer was allocated on the heap, that ownership is passed to this instance.
    if (isAllocated)
    {
        if (GetRawBuffer() == nullptr)
        {
            // If we were unable to use the provided buffer, we need to release it as we now own the memory.
            DeleteBuffer((BYTE*)buffer, size);
        }
        else
        {
            // Make sure to set the Allocated flag so we release it when resizing.
            SetAllocated();
        }
    }
}

template<typename TEncoding>
inline EString<TEncoding>::EString(const EString &s, const CIterator &i, COUNT_T count)
  : EString()
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(i.Check());
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(s.Match(i, *this));
        SS_POSTCONDITION(GetCount() == count);
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s, i, count);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the substring from s.
// s - the source string
// start - the character to start at
// length - number of characters to copy from s.
//-----------------------------------------------------------------------------
template<typename TEncoding>
inline void EString<TEncoding>::Set(const EString &s, const CIterator &i, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(i.Check());
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(s.Match(i, *this));
        SS_POSTCONDITION(GetCount() == count);
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    // @todo: detect case where we can reuse literal?
    Resize(count);
    SBuffer::Copy(SBuffer::Begin(), i.m_ptr, count * sizeof(char_t));
    NullTerminate();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the substring from s.
// s - the source string
// start - the position to start
// end - the position to end (exclusive)
//-----------------------------------------------------------------------------
template<typename TEncoding>
inline void EString<TEncoding>::Set(const EString &s, const CIterator &start, const CIterator &end)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(start.Check());
        PRECONDITION(s.CheckIteratorRange(start));
        PRECONDITION(end.Check());
        PRECONDITION(s.CheckIteratorRange(end));
        PRECONDITION(end >= start);
        SS_POSTCONDITION(s.Match(start, *this));
        SS_POSTCONDITION(GetCount() == (COUNT_T) (end - start));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s, start, end - start);

    SS_RETURN;
}


//-----------------------------------------------------------------------------
// Hash the string contents
//-----------------------------------------------------------------------------
template<typename TEncoding>
ULONG EString<TEncoding>::Hash() const
{
    SS_CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    StackEString<EncodingUnicode> buffer;

    ConvertToUnicode(buffer);

    SS_RETURN HashString(buffer);
}

//-----------------------------------------------------------------------------
// Hash the string contents
//-----------------------------------------------------------------------------
template<typename TEncoding>
ULONG EString<TEncoding>::HashCaseInsensitive() const
{
    SS_CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    ULONG result = TEncoding::CaseHashHelper(GetRawBuffer(), GetCount());

    SS_RETURN result;
}

// Append s to the end of this string.
template<typename TEncoding>
inline void EString<TEncoding>::Append(const EString &s)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(s.Check());
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    Insert(End(), s);

    SS_RETURN;
}

template<typename TEncoding>
inline void EString<TEncoding>::Append(const char_t *string)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(string));
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    // Wrap the string in temporary EString without copying it
    EString s(EString::Literal, string);
    s.ClearImmutable();
    Append(s);

    SS_RETURN;
}

template<typename TEncoding>
inline void EString<TEncoding>::Append(const char_t c)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        THROWS;
    }
    SS_CONTRACT_END;

    InlineEString<2 * sizeof(c), TEncoding> s(c);
    Append(s);

    SS_RETURN;
}

template<typename TEncoding>
BOOL EString<TEncoding>::Equals(const EString &s) const
{
    LIMITED_METHOD_CONTRACT;
    return Compare(s) == 0;
}

template<typename TEncoding>
BOOL EString<TEncoding>::EqualsCaseInsensitive(const EString &s) const
{
    LIMITED_METHOD_CONTRACT;
    return CompareCaseInsensitive(s) == 0;
}

//-----------------------------------------------------------------------------
// Compare this string's contents to s's contents.
// The comparison does not take into account localization issues like case folding.
// Return 0 if equal, <0 if this < s, >0 is this > s. (same as strcmp).
//-----------------------------------------------------------------------------
template<typename TEncoding>
int EString<TEncoding>::Compare(const EString &source) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(source.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T smaller;
    int equals = 0;
    int result = 0;

    if (GetCount() < source.GetCount())
    {
        smaller = GetCount();
        equals = -1;
    }
    else if (GetCount() > source.GetCount())
    {
        smaller = source.GetCount();
        equals = 1;
    }
    else
    {
        smaller = GetCount();
        equals = 0;
    }

    result = TEncoding::strncmp(GetRawBuffer(), source.GetRawBuffer(), smaller);

    if (result == 0)
        RETURN equals;
    else
        RETURN result;
}
//-----------------------------------------------------------------------------
// Compare this string's contents to s's contents.
// Return 0 if equal, <0 if this < s, >0 is this > s. (same as strcmp).
//-----------------------------------------------------------------------------
template<typename TEncoding>
int EString<TEncoding>::CompareCaseInsensitive(const EString &source) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(source.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T smaller;
    int equals = 0;
    int result = 0;

    if (GetCount() < source.GetCount())
    {
        smaller = GetCount();
        equals = -1;
    }
    else if (GetCount() > source.GetCount())
    {
        smaller = source.GetCount();
        equals = 1;
    }
    else
    {
        smaller = GetCount();
        equals = 0;
    }

    result = TEncoding::CaseCompareHelper(GetRawBuffer(), source.GetRawBuffer(), smaller, FALSE, TRUE);

    if (result == 0)
        RETURN equals;
    else
        RETURN result;
}

template<typename TEncoding>
void EString<TEncoding>::Set(const EString& s)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        SS_POSTCONDITION(Equals(s));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SBuffer::Set(s);

    SS_RETURN;
}

template<typename TEncoding>
void EString<TEncoding>::Set(const char_t* string)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (string == NULL || *string == 0)
        Clear();
    else
    {
        COUNT_T count = static_cast<COUNT_T>(TEncoding::strlen(string));
        Resize(count);
        TEncoding::strncpy_s(GetRawBuffer(), GetSize() / sizeof(char_t), string, count + 1);
    }

    RETURN;
}

template<typename TEncoding>
void EString<TEncoding>::Set(const char_t* string, COUNT_T count)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (string == NULL || *string == 0)
        Clear();
    else
    {
        Resize(count);
        TEncoding::strncpy_s(GetRawBuffer(), GetSize() / sizeof(char_t), string, count);
        GetRawBuffer()[count] = static_cast<char_t>(0);
    }

    RETURN;
}

template<typename TEncoding>
void EString<TEncoding>::Set(const char_t c)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (c == 0)
        Clear();
    else
    {
        Resize(1);
        GetRawBuffer()[0] = c;
        GetRawBuffer()[1] = static_cast<char_t>(0);
    }

    RETURN;
}

template<typename TEncoding>
void EString<TEncoding>::SetPreallocated(const char_t* string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
       INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    SetImmutable();
    SetImmutable((BYTE*) string, static_cast<COUNT_T>(count * sizeof(char_t)));
    ClearAllocated();

    SS_RETURN;
}

template<typename TEncoding>
void EString<TEncoding>::SetLiteral(const char_t* string)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        GC_NOTRIGGER;
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    Set(EString(EString::Literal, string));

    SS_RETURN;
}

// Preallocate some space for the string buffer
template<typename TEncoding>
inline void EString<TEncoding>::Preallocate(COUNT_T characters) const
{
    WRAPPER_NO_CONTRACT;

    SBuffer::Preallocate(characters * sizeof(char_t));
}

// Trim unused space from the buffer
template<typename TEncoding>
inline void EString<TEncoding>::Trim() const
{
    WRAPPER_NO_CONTRACT;

    if (IsEmpty())
    {
        // Share the global empty string buffer.
        const_cast<EString *>(this)->SBuffer::SetImmutable(StaticStringHelpers::s_EmptyBuffer, sizeof(StaticStringHelpers::s_EmptyBuffer));
    }
    else
    {
        SBuffer::Trim();
    }
}

// RETURN true if the string is empty.
template<typename TEncoding>
inline BOOL EString<TEncoding>::IsEmpty() const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        NOTHROW;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SS_RETURN (SizeToCount(GetSize()) == 0);
}

//----------------------------------------------------------------------------
// Private helper.
// We know the buffer should be m_count characters. Place a null terminator
// in the buffer to make our internal string null-terminated at that length.
//----------------------------------------------------------------------------
template<typename TEncoding>
FORCEINLINE void EString<TEncoding>::NullTerminate()
{
    SUPPORTS_DAC_HOST_ONLY;
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACT_VOID
    {
        POSTCONDITION(CheckPointer(this));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
#else //SSTRING_EXTRA_CHECKS
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
#endif //SSTRING_EXTRA_CHECKS

    BYTE *end = m_buffer + GetSize();

    reinterpret_cast<char_t*>(end)[-1] = 0;

    SS_RETURN;
}

//----------------------------------------------------------------------------
// private helper
// Return true if the string is a literal.
// A literal string has immutable memory.
//----------------------------------------------------------------------------
template<typename TEncoding>
inline BOOL EString<TEncoding>::IsLiteral() const
{
    WRAPPER_NO_CONTRACT;

    return SBuffer::IsImmutable() && (m_buffer != StaticStringHelpers::s_EmptyBuffer);
}

//----------------------------------------------------------------------------
// private helper:
// RETURN true if the string allocated (and should delete) its buffer.
// IsAllocated() will RETURN false for Literal strings and
// stack-based strings (the buffer is on the stack)
//----------------------------------------------------------------------------
template<typename TEncoding>
inline BOOL EString<TEncoding>::IsAllocated() const
{
    WRAPPER_NO_CONTRACT;

    return SBuffer::IsAllocated();
}

//----------------------------------------------------------------------------
// Assert helper
// Asser that the iterator is within the given string.
//----------------------------------------------------------------------------
template<typename TEncoding>
inline CHECK EString<TEncoding>::CheckIteratorRange(const CIterator &i) const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(i >= Begin());
    CHECK(i <= End()); // Note that it's OK to look at the terminating null
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Assert helper
// Asser that the iterator is within the given string.
//----------------------------------------------------------------------------
template<typename TEncoding>
inline CHECK EString<TEncoding>::CheckIteratorRange(const CIterator &i, COUNT_T length) const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(i >= Begin());
    CHECK(i + length <= End());  // Note that it's OK to look at the terminating null
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Assert that the string is empty
//----------------------------------------------------------------------------
template<typename TEncoding>
inline CHECK EString<TEncoding>::CheckEmpty() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(IsEmpty());
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Check the range of a count
//----------------------------------------------------------------------------
template<typename TEncoding>
inline CHECK EString<TEncoding>::CheckCount(COUNT_T count)
{
    CANNOT_HAVE_CONTRACT;
    CHECK(CheckSize(count*sizeof(WCHAR)));
    CHECK_OK;
}

#if CHECK_INVARIANTS
//----------------------------------------------------------------------------
// Check routine and invariants.
//----------------------------------------------------------------------------

template<typename TEncoding>
inline CHECK EString<TEncoding>::Check() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::Check());
    CHECK_OK;
}

template<typename TEncoding>
inline CHECK EString<TEncoding>::Invariant() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::Invariant());
    CHECK_OK;
}

template<typename TEncoding>
inline CHECK EString<TEncoding>::InternalInvariant() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::InternalInvariant());
    CHECK(SBuffer::GetSize() >= 2);
    CHECK_OK;
}
#endif  // CHECK_INVARIANTS

//----------------------------------------------------------------------------
// EnsureWritable
// Ensures that the buffer is writable
//----------------------------------------------------------------------------
template<typename TEncoding>
inline void EString<TEncoding>::EnsureWritable() const
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        POSTCONDITION(!IsLiteral());
        THROWS;
    }
    CONTRACT_END;
#else //SSTRING_EXTRA_CHECKS
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
#endif //SSTRING_EXTRA_CHECKS

    if (IsLiteral())
        const_cast<EString *>(this)->Resize(GetSize(), PRESERVE);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Create CIterators on the string.
//-----------------------------------------------------------------------------
template<typename TEncoding>
FORCEINLINE typename EString<TEncoding>::CIterator EString<TEncoding>::Begin() const
{
    SS_CONTRACT(EString::CIterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        NOTHROW;
    }
    SS_CONTRACT_END;

    SS_RETURN CIterator(this, 0);
}

template<typename TEncoding>
FORCEINLINE typename EString<TEncoding>::CIterator EString<TEncoding>::End() const
{
    SS_CONTRACT(EString<TEncoding>::CIterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        NOTHROW;
    }
    SS_CONTRACT_END;

    SS_RETURN CIterator(this, SizeToCount(GetSize()));
}

//-----------------------------------------------------------------------------
// Create Iterators on the string.
//-----------------------------------------------------------------------------

template<typename TEncoding>
FORCEINLINE typename EString<TEncoding>::Iterator EString<TEncoding>::Begin()
{
    SS_CONTRACT(EString<TEncoding>::Iterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS; // EnsureMutable always throws
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    EnsureMutable();

    SS_RETURN Iterator(this, 0);
}

template<typename TEncoding>
FORCEINLINE typename EString<TEncoding>::Iterator EString<TEncoding>::End()
{
    SS_CONTRACT(EString<TEncoding>::Iterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS; // EnsureMutable always Throws
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    EnsureMutable();

    SS_RETURN Iterator(this, SizeToCount(GetSize()));
}

//-----------------------------------------------------------------------------
// CIterator support routines
//-----------------------------------------------------------------------------

template<typename TEncoding>
inline EString<TEncoding>::Index::Index()
{
    LIMITED_METHOD_CONTRACT;
}

template<typename TEncoding>
inline EString<TEncoding>::Index::Index(EString *string, SCOUNT_T index)
  : SBuffer::Index(string, index * sizeof(char_t))
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(string));
        PRECONDITION(DoCheck(0));
        SS_POSTCONDITION(CheckPointer(this));
        // POSTCONDITION(Subtract(string->Begin()) == index); contract violation - fix later
        NOTHROW;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SS_RETURN;
}

template<typename TEncoding>
inline BYTE &EString<TEncoding>::Index::GetAt(SCOUNT_T delta) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_ptr[delta * sizeof(char_t)];
}

template<typename TEncoding>
inline void EString<TEncoding>::Index::Skip(SCOUNT_T delta)
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_ptr += delta * sizeof(char_t);
}

template<typename TEncoding>
inline SCOUNT_T EString<TEncoding>::Index::Subtract(const Index &i) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (SCOUNT_T) ((m_ptr - i.m_ptr) / (SCOUNT_T)sizeof(char_t));
}

template<typename TEncoding>
inline CHECK EString<TEncoding>::Index::DoCheck(SCOUNT_T delta) const
{
    CANNOT_HAVE_CONTRACT;
#if _DEBUG
    const EString *string = (const EString *) GetContainerDebug();

    CHECK(m_ptr + (delta * sizeof(char_t)) >= string->m_buffer);
    CHECK(m_ptr + (delta * sizeof(char_t)) < string->m_buffer + string->GetSize());
#endif
    CHECK_OK;
}

template<typename TEncoding>
inline void EString<TEncoding>::Index::Resync(const EString *string, BYTE *ptr) const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    SBuffer::Index::Resync(string, ptr);
}

template<typename TEncoding>
inline auto EString<TEncoding>::Index::operator*() const -> const char_t&
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return *reinterpret_cast<const char_t*>(&GetAt(0));
}

template<typename TEncoding>
inline auto EString<TEncoding>::Index::operator*() -> char_t&
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return *reinterpret_cast<char_t*>(&GetAt(0));
}

template<typename TEncoding>
inline void EString<TEncoding>::Index::operator->() const
{
    LIMITED_METHOD_CONTRACT;
}

template<typename TEncoding>
inline auto EString<TEncoding>::Index::operator[](int index) const -> char_t
{
    WRAPPER_NO_CONTRACT;

    return *reinterpret_cast<char_t*>(&GetAt(index));
}

template<typename TEncoding>
inline auto EString<TEncoding>::Index::GetBuffer() const -> const char_t*
{
    WRAPPER_NO_CONTRACT;
    return reinterpret_cast<const char_t*>(m_ptr);
}

#ifdef _DEBUG
//
// Check the Printf use for potential globalization bugs. %S formatting
// specifier does Unicode->Ansi or Ansi->Unicode conversion using current
// C-locale. This almost always means globalization bug in the CLR codebase.
//
// Ideally, we would elimitate %S from all format strings. Unfortunately,
// %S is too widespread in non-shipping code that such cleanup is not feasible.
//
template<typename TEncoding>
void CheckForFormatStringGlobalizationIssues(const EString<TEncoding> &format, const EString<TEncoding> &result)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    BOOL fDangerousFormat = FALSE;

    // Check whether the format string contains the %S formatting specifier
    typename EString<TEncoding>::CIterator itrFormat = format.Begin();
    while (*itrFormat)
    {
        if (*itrFormat++ == '%')
        {
            // <TODO>Handle the complex format strings like %blahS</TODO>
            if (*itrFormat++ == 'S')
            {
                fDangerousFormat = TRUE;
                break;
            }
        }
    }

    if (fDangerousFormat)
    {
        BOOL fNonAsciiUsed = FALSE;

        // Now check whether there are any non-ASCII characters in the output.

        // Check whether the result contains non-Ascii characters
        typename EString<TEncoding>::CIterator itrResult = format.Begin();
        while (*itrResult)
        {
            if (*itrResult++ > 127)
            {
                fNonAsciiUsed = TRUE;
                break;
            }
        }

        CONSISTENCY_CHECK_MSGF(!fNonAsciiUsed,
            ("Non-ASCII string was produced by %%S format specifier. This is likely globalization bug."
            "To fix this, change the format string to %%s and do the correct encoding at the Printf callsite"));
    }
}
#endif

//-----------------------------------------------------------------------------
// Truncate this string to count characters.
//-----------------------------------------------------------------------------
template<typename TEncoding>
void EString<TEncoding>::Truncate(const Iterator &i)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        SS_POSTCONDITION(GetCount() == i - Begin());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    COUNT_T size = i - Begin();

    Resize(size, PRESERVE);

    i.Resync(this, (BYTE *) (GetRawBuffer() + size));

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// This is essentially a specialized version of the above for size 0
//-----------------------------------------------------------------------------
template<typename TEncoding>
void EString<TEncoding>::Clear()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        POSTCONDITION(IsEmpty());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (IsImmutable())
    {
        // Use shared empty string rather than allocating a new buffer
        static_assert(sizeof(char_t) <= sizeof(StaticStringHelpers::s_EmptyBuffer), "Empty buffer size must be as large as or larger than the size of a character.");
        SBuffer::SetImmutable(StaticStringHelpers::s_EmptyBuffer, sizeof(char_t));
    }
    else
    {
        // Leave allocated buffer for future growth
        SBuffer::TweakSize(sizeof(char_t));
        m_buffer[0] = 0;
    }

    RETURN;
}


#ifndef EBADF
#define EBADF 9
#endif

#ifndef ENOMEM
#define ENOMEM 12
#endif

#ifndef ERANGE
#define ERANGE 34
#endif

template<typename TEncoding>
void CheckForFormatStringGlobalizationIssues(const EString<TEncoding> &format, const EString<TEncoding> &result);

template<typename TEncoding>
void EString<TEncoding>::VPrintf(const char_t* format, va_list args)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(format));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    va_list ap;
    // sprintf gives us no means to know how many characters are written
    // other than guessing and trying

    if (GetCount() > 0)
    {
        // First, try to use the existing buffer
        va_copy(ap, args);
        int result = TEncoding::vsnprintf_s((char_t*)m_buffer, GetCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >= 0)
        {
            // succeeded
            Resize(result, PRESERVE);
            EString<TEncoding> sss(format);
            INDEBUG(CheckForFormatStringGlobalizationIssues(sss, *this));
            RETURN;
        }
    }

    // Make a guess how long the result will be (note this will be doubled)

    COUNT_T guess = (COUNT_T) TEncoding::strlen(format)+1;
    if (guess < GetCount())
        guess = GetCount();
    if (guess < MINIMUM_GUESS)
        guess = MINIMUM_GUESS;

    while (TRUE)
    {
        // Double the previous guess - eventually we will get enough space
        guess *= 2;
        Resize(guess);

        // Clear errno to avoid false alarms
        errno = 0;

        va_copy(ap, args);
        int result = TEncoding::vsnprintf_s((char_t*)m_buffer, GetCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >= 0)
        {
            Resize(result, PRESERVE);
            EString<TEncoding> sss(format);
            INDEBUG(CheckForFormatStringGlobalizationIssues(sss, *this));
            RETURN;
        }

        if (errno==ENOMEM)
        {
            ThrowOutOfMemory();
        }
        else
        if (errno!=0 && errno!=EBADF && errno!=ERANGE)
        {
            CONSISTENCY_CHECK_MSG(FALSE, "vsnprintf_s failed. Potential globalization bug.");
            ThrowHR(HRESULT_FROM_WIN32(ERROR_NO_UNICODE_TRANSLATION));
        }
    }
    RETURN;
}


template<>
inline BOOL EString<EncodingUnicode>::FormatMessage(DWORD dwFlags, LPCVOID lpSource, DWORD dwMessageId, DWORD dwLanguageId,
                            const EString<EncodingUnicode> &arg1, const EString<EncodingUnicode> &arg2,
                            const EString<EncodingUnicode> &arg3, const EString<EncodingUnicode> &arg4,
                            const EString<EncodingUnicode> &arg5, const EString<EncodingUnicode> &arg6,
                            const EString<EncodingUnicode> &arg7, const EString<EncodingUnicode> &arg8,
                            const EString<EncodingUnicode> &arg9, const EString<EncodingUnicode> &arg10)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    const WCHAR *args[] = {arg1, arg2, arg3, arg4,
                           arg5, arg6, arg7, arg8,
                           arg9, arg10};

    if (GetCount() > 0)
    {
        // First, try to use our existing buffer to hold the result.
        Resize(GetCount());

        DWORD result = ::WszFormatMessage(dwFlags | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                          lpSource, dwMessageId, dwLanguageId,
                                          GetRawBuffer(), GetCount()+1, (va_list*)args);

        // Although we cannot directly detect truncation, we can tell if we
        // used up all the space (in which case we will assume truncation.)

        if (result != 0 && result < GetCount())
        {
            if (GetRawBuffer()[result-1] == W(' '))
            {
                GetRawBuffer()[result-1] = W('\0');
                result -= 1;
            }
            Resize(result, PRESERVE);
            RETURN TRUE;
        }
    }

    // We don't have enough space in our buffer, do dynamic allocation.
    LocalAllocHolder<WCHAR> string;

    DWORD result = ::WszFormatMessage(dwFlags | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                      lpSource, dwMessageId, dwLanguageId,
                                      (LPWSTR)(LPWSTR*)&string, 0, (va_list*)args);

    if (result == 0)
        RETURN FALSE;
    else
    {
        if (string[result-1] == W(' '))
            string[result-1] = W('\0');

        Set(string);
        RETURN TRUE;
    }
}

#ifdef DACCESS_COMPILE
template<>
inline const WCHAR * EString<EncodingUnicode>::DacGetRawUnicode() const
{
    if (IsEmpty())
    {
        return W("");
    }

    return static_cast<WCHAR*>(SBuffer::DacTryGetRawContent());
}
#endif

template<typename TEncoding>
void EString<TEncoding>::AppendVPrintf(const char_t* format, va_list args)
{
    WRAPPER_NO_CONTRACT;

    StackEString<TEncoding> s;
    s.VPrintf(format, args);
    Append(s);
}

template<>
inline COUNT_T EString<EncodingASCII>::ConvertToUnicode(EString<EncodingUnicode> &s) const
{
    CONTRACT(COUNT_T)
    {
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    // Handle the empty case.
    if (IsEmpty())
    {
        s.Clear();
        RETURN GetCount() + 1;
    }

    CONSISTENCY_CHECK(CheckPointer(GetRawBuffer()));
    CONSISTENCY_CHECK(GetCount() > 0);

    // If dest is the same as this, then we need to preserve on resize.
    WCHAR* buf = s.OpenBuffer(GetCount());

    // Make sure the buffer is big enough.
    CONSISTENCY_CHECK(s.GetAllocation() >= GetCount());

    // This is a poor man's widen. Since we know that the representation is ASCII,
    // we can just pad the string with a bunch of zero-value bytes. Of course,
    // we move from the end of the string to the start so that we can convert in
    // place (in the case that dest.GetRawBuffer() == this.GetRawBuffer()).
    WCHAR *outBuf = buf + s.GetCount();
    ASCII *inBuf = GetRawBuffer() + GetCount();

    while (GetRawBuffer() <= inBuf)
    {
        CONSISTENCY_CHECK(buf <= outBuf);
        // The casting zero-extends the value, thus giving us the zero-valued byte.
        *outBuf = (WCHAR) *inBuf;
        outBuf--;
        inBuf--;
    }
    
    s.CloseBuffer();

    RETURN GetCount() + 1;
}

template<typename TEncoding>
EString<EncodingUTF8> EString<TEncoding>::MoveToUTF8()
{
    StackEString<EncodingUTF8> buff;
    ConvertToUTF8(buff);
    if (IsAllocated())
    {
        ClearAllocated();
        EString<EncodingUTF8> result(GetRawBufferForMove(), GetAllocationForMove(), /* isAllocated */ true);
        SetImmutable(); // Set the existing value as immutable to get SBuffer to reuse the immutable buffer we're setting
        SetImmutable(StaticStringHelpers::s_EmptyBuffer, sizeof(StaticStringHelpers::s_EmptyBuffer));
        result.Set(buff);
        return result;
    }
    return std::move(buff);
}

template<typename TEncoding>
EString<EncodingUnicode> EString<TEncoding>::MoveToUnicode()
{
    StackEString<EncodingUnicode> buff;
    ConvertToUnicode(buff);
    if (IsAllocated())
    {
        ClearAllocated();
        EString<EncodingUnicode> result(GetRawBufferForMove(), GetAllocationForMove(), /* isAllocated */ true);
        SetImmutable(); // Set the existing value as immutable to get SBuffer to reuse the immutable buffer we're setting
        SetImmutable(StaticStringHelpers::s_EmptyBuffer, sizeof(StaticStringHelpers::s_EmptyBuffer));
        result.Set(buff);
        return result;
    }
    return std::move(buff);
}

template<>
inline EString<EncodingUnicode> EString<EncodingASCII>::MoveToUnicode()
{
    // ASCII -> Unicode conversion supports in-place conversion if the buffer is large enough.
    if (IsAllocated() && SBuffer::GetAllocation() > GetCount() * sizeof(WCHAR))
    {
        ClearAllocated();
        EString<EncodingUnicode> result(GetRawBufferForMove(), GetAllocationForMove(), /* isAllocated */ true);
        ConvertToUnicode(result);
        SetImmutable(); // Set the existing value as immutable to get SBuffer to reuse the immutable buffer we're setting
        SetImmutable(StaticStringHelpers::s_EmptyBuffer, sizeof(StaticStringHelpers::s_EmptyBuffer));
        return result;
    }

    StackEString<EncodingUnicode> buff;
    ConvertToUnicode(buff);
    return std::move(buff);
}


// Insert string at iterator position
template<typename TEncoding>
inline void EString<TEncoding>::Insert(const Iterator &i, const EString &s)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    Replace(i, 0, s);

    SS_RETURN;
}

template<typename TEncoding>
inline void EString<TEncoding>::Insert(const Iterator &i, const char_t *string)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        THROWS;
    }
    SS_CONTRACT_END;

    StackEString<TEncoding> s(string);
    Replace(i, 0, s);

    SS_RETURN;
}
//-----------------------------------------------------------------------------
// Replace the substring specified by position, length with the given string s.
//-----------------------------------------------------------------------------
template<typename TEncoding>
void EString<TEncoding>::Replace(const Iterator &i, COUNT_T length, const EString &s)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, length));
        PRECONDITION(s.Check());
        POSTCONDITION(Match(i, s));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    COUNT_T deleteSize = length * sizeof(char_t);
    COUNT_T insertSize = s.GetCount() * sizeof(char_t);

    SBuffer::Replace(i, deleteSize, insertSize);
    SBuffer::Copy(i, s.m_buffer, insertSize);

    RETURN;
}

template<typename TEncoding>
// Delete string at iterator position
inline void EString<TEncoding>::Delete(const Iterator &i, COUNT_T length)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i, length));
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    Replace(i, length, Empty());

    SS_RETURN;
}


template<typename TEncoding>
BOOL EString<TEncoding>::Find(CIterator& i, const EString& s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        POSTCONDITION(RETVAL == Match(i, s));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    COUNT_T count = s.GetCount();
    const char_t *start = i.GetBuffer();
    const char_t *end = GetRawBuffer() + GetCount() - count;
    while (start <= end)
    {
        if (TEncoding::strncmp(start, s.GetRawBuffer(), count) == 0)
        {
            i.Resync(this, (BYTE*) start);
            RETURN TRUE;
        }
        start++;
    }
    RETURN FALSE;
}

template<typename TEncoding>
BOOL EString<TEncoding>::Find(CIterator& i, char_t s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        POSTCONDITION(RETVAL == Match(i, s));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    const char_t *start = i.GetBuffer();
    const char_t *end = GetRawBuffer() + GetCount() - 1;
    while (start <= end)
    {
        if (*start == s)
        {
            i.Resync(this, (BYTE*) start);
            RETURN TRUE;
        }
        start++;
    }
    RETURN FALSE;
}

template<typename TEncoding>
BOOL EString<TEncoding>::FindBack(CIterator& i, const EString& s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        POSTCONDITION(RETVAL == Match(i, s));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    COUNT_T count = s.GetCount();
    const char_t *start = GetRawBuffer() + GetCount() - count;
    if (start > i.GetBuffer())
    {
        start = i.GetBuffer();
    }
    const char_t *end = GetRawBuffer();
    while (start >= end)
    {
        if (TEncoding::strncmp(start, s.GetRawBuffer(), count) == 0)
        {
            i.Resync(this, (BYTE*) start);
            RETURN TRUE;
        }
        start--;
    }
    RETURN FALSE;
}

template<typename TEncoding>
BOOL EString<TEncoding>::FindBack(CIterator& i, char_t s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        POSTCONDITION(RETVAL == Match(i, s));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    const char_t *start = GetRawBuffer() + GetCount() - 1;
    if (start > i.GetBuffer())
    {
        start = i.GetBuffer();
    }
    const char_t *end = GetRawBuffer();
    while (start >= end)
    {
        if (*start == s)
        {
            i.Resync(this, (BYTE*) start);
            RETURN TRUE;
        }
        start--;
    }

    RETURN FALSE;
}

template<typename TEncoding>
inline BOOL EString<TEncoding>::Skip(CIterator &i, const EString &s) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        NOTHROW;
    }
    SS_CONTRACT_END;

    if (Match(i, s))
    {
        i += s.GetCount();
        SS_RETURN TRUE;
    }
    else
        SS_RETURN FALSE;
}

template<typename TEncoding>
inline BOOL EString<TEncoding>::Skip(CIterator &i, char_t c) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        NOTHROW;
    }
    SS_CONTRACT_END;

    if (Match(i, c))
    {
        i++;
        SS_RETURN TRUE;
    }
    else
        SS_RETURN FALSE;
}

template<typename TEncoding>
inline BOOL EString<TEncoding>::Match(const CIterator& i, const EString& s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T remaining = End() - i;
    COUNT_T count = s.GetCount();

    if (remaining < count)
    {
        RETURN FALSE;
    }

    RETURN (TEncoding::strncmp(i.GetBuffer(), s.GetRawBuffer(), count) == 0);
}

template<typename TEncoding>
inline BOOL EString<TEncoding>::MatchCaseInsensitive(const CIterator& i, const EString& s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    COUNT_T remaining = End() - i;
    COUNT_T count = s.GetCount();

    if (remaining < count)
    {
        RETURN FALSE;
    }

    RETURN (TEncoding::CaseCompareHelper(i.GetBuffer(), s.GetRawBuffer(), count, FALSE, TRUE) == 0);
}

template<typename TEncoding>
inline BOOL EString<TEncoding>::Match(const CIterator &i, char_t c) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        NOTHROW;
    }
    SS_CONTRACT_END;

    // End() will not throw here
    CONTRACT_VIOLATION(ThrowsViolation);
    SS_RETURN (i < End() && i[0] == c);
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

#endif  // _SSTRING_INL_
