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

//----------------------------------------------------------------------------
// Default constructor. Sets the string to the empty string.
//----------------------------------------------------------------------------
inline SString::SString()
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
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

inline SString::SString(void *buffer, COUNT_T size)
  : SBuffer(Prealloc, buffer, size)
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckSize(size));
        SS_POSTCONDITION(IsEmpty());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    if (size < sizeof(WCHAR))
    {
        // Ignore the useless buffer
        SetImmutable(s_EmptyBuffer, sizeof(s_EmptyBuffer));
    }
    else
    {
        SBuffer::TweakSize(sizeof(WCHAR));
        GetRawUnicode()[0] = 0;
    }

    SS_RETURN;
}

inline SString::SString(const SString &s)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s.Check());
        SS_POSTCONDITION(Equals(s));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s);

    SS_RETURN;
}

inline SString::SString(const SString &s1, const SString &s2)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s1, s2);

    SS_RETURN;
}

inline SString::SString(const SString &s1, const SString &s2, const SString &s3)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        PRECONDITION(s3.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s1, s2, s3);

    SS_RETURN;
}

inline SString::SString(const SString &s1, const SString &s2, const SString &s3, const SString &s4)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        PRECONDITION(s3.Check());
        PRECONDITION(s4.Check());
        THROWS;
    }
    SS_CONTRACT_END;

    Set(s1, s2, s3, s4);

    SS_RETURN;
}

inline SString::SString(const SString &s, const CIterator &i, COUNT_T count)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(i.Check());
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(s.Match(i, *this));
        SS_POSTCONDITION(GetRawCount() == count);
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s, i, count);

    SS_RETURN;
}

inline SString::SString(const SString &s, const CIterator &start, const CIterator &end)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(start.Check());
        PRECONDITION(s.CheckIteratorRange(start));
        PRECONDITION(end.Check());
        PRECONDITION(s.CheckIteratorRange(end));
        PRECONDITION(start <= end);
        SS_POSTCONDITION(s.Match(start, *this));
        SS_POSTCONDITION(GetRawCount() == (COUNT_T) (end - start));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s, start, end);

    SS_RETURN;
}

inline SString::SString(const WCHAR *string)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(string);

    SS_RETURN;
}

inline SString::SString(const WCHAR *string, COUNT_T count)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(string, count);

    SS_RETURN;
}

inline SString::SString(enum tagASCII, const ASCII *string)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckASCIIString(string));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetASCII(string);

    SS_RETURN;
}

inline SString::SString(enum tagASCII, const ASCII *string, COUNT_T count)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckASCIIString(string, count));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetASCII(string, count);

    SS_RETURN;
}

inline SString::SString(tagUTF8 dummytag, const UTF8 *string)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        // !!! Check for illegal UTF8 encoding?
        PRECONDITION(CheckPointer(string, NULL_OK));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SetUTF8(string);

    SS_RETURN;
}

inline SString::SString(tagUTF8 dummytag, const UTF8 *string, COUNT_T count)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        // !!! Check for illegal UTF8 encoding?
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetUTF8(string, count);

    SS_RETURN;
}

inline SString::SString(WCHAR character)
  : SBuffer(Immutable, s_EmptyBuffer, sizeof(s_EmptyBuffer))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(character);

    SS_RETURN;
}

inline SString::SString(tagLiteral dummytag, const ASCII *literal)
  : SBuffer(Immutable, (const BYTE *) literal, (COUNT_T) (strlen(literal)+1)*sizeof(CHAR))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(literal));
        PRECONDITION(CheckASCIIString(literal));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    SetRepresentation(REPRESENTATION_ASCII);

    SS_RETURN;
}

inline SString::SString(tagUTF8Literal dummytag, const UTF8 *literal)
  : SBuffer(Immutable, (const BYTE *) literal, (COUNT_T) (strlen(literal)+1)*sizeof(CHAR))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(literal));
        NOTHROW;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetRepresentation(REPRESENTATION_UTF8);

    SS_RETURN;
}

inline SString::SString(tagLiteral dummytag, const WCHAR *literal)
  : SBuffer(Immutable, (const BYTE *) literal, (COUNT_T) (wcslen(literal)+1)*sizeof(WCHAR))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(literal));
        NOTHROW;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetRepresentation(REPRESENTATION_UNICODE);
    SetNormalized();

    SS_RETURN;
}

inline SString::SString(tagLiteral dummytag, const WCHAR *literal, COUNT_T count)
  : SBuffer(Immutable, (const BYTE *) literal, (count + 1) * sizeof(WCHAR))
{
    SS_CONTRACT_VOID
    {
        SS_CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(literal));
        NOTHROW;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SetRepresentation(REPRESENTATION_UNICODE);
    SetNormalized();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to s
// s - source string
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s)
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
    SetRepresentation(s.GetRepresentation());
    ClearNormalized();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to concatenation of s1 and s2
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s1, const SString &s2)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Preallocate(s1.GetCount() + s2.GetCount());

    Set(s1);
    Append(s2);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to concatenation of s1, s2, and s3
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s1, const SString &s2, const SString &s3)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        PRECONDITION(s3.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Preallocate(s1.GetCount() + s2.GetCount() + s3.GetCount());

    Set(s1);
    Append(s2);
    Append(s3);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to concatenation of s1, s2, s3, and s4
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s1, const SString &s2, const SString &s3, const SString &s4)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s1.Check());
        PRECONDITION(s2.Check());
        PRECONDITION(s3.Check());
        PRECONDITION(s4.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Preallocate(s1.GetCount() + s2.GetCount() + s3.GetCount() + s4.GetCount());

    Set(s1);
    Append(s2);
    Append(s3);
    Append(s4);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the substring from s.
// s - the source string
// start - the character to start at
// length - number of characters to copy from s.
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s, const CIterator &i, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        PRECONDITION(i.Check());
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(s.Match(i, *this));
        SS_POSTCONDITION(GetRawCount() == count);
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    // @todo: detect case where we can reuse literal?
    Resize(count, s.GetRepresentation());
    SBuffer::Copy(SBuffer::Begin(), i.m_ptr, count<<i.m_characterSizeShift);
    NullTerminate();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the substring from s.
// s - the source string
// start - the position to start
// end - the position to end (exclusive)
//-----------------------------------------------------------------------------
inline void SString::Set(const SString &s, const CIterator &start, const CIterator &end)
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
        SS_POSTCONDITION(GetRawCount() == (COUNT_T) (end - start));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    Set(s, start, end - start);

    SS_RETURN;
}

// Return a global empty string
inline const SString &SString::Empty()
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACTL
    {
        // POSTCONDITION(RETVAL.IsEmpty());
        PRECONDITION(CheckStartup());
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
#else
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SUPPORTS_DAC;
#endif

    _ASSERTE(s_Empty != NULL);  // Did you call SString::Startup()?
    return *s_Empty;
}

// Get a const pointer to the internal buffer as a unicode string.
inline const WCHAR *SString::GetUnicode() const
{
    SS_CONTRACT(const WCHAR *)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        if (IsRepresentation(REPRESENTATION_UNICODE)) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    ConvertToUnicode();

    SS_RETURN GetRawUnicode();
}

// Get a const pointer to the internal buffer as a UTF8 string.
inline const UTF8 *SString::GetUTF8() const
{
    SS_CONTRACT(const UTF8 *)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        if (IsRepresentation(REPRESENTATION_UTF8)) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    ConvertToUTF8();

    SS_RETURN GetRawUTF8();
}

// Normalize the string to unicode.  This will make many operations nonfailing.
inline void SString::Normalize() const
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        SS_POSTCONDITION(IsNormalized());
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    ConvertToUnicode();
    SetNormalized();

    SS_RETURN;
}

// Get a const pointer to the internal buffer as a unicode string.
inline const WCHAR *SString::GetUnicode(const CIterator &i) const
{
    SS_CONTRACT(const WCHAR *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    PRECONDITION(CheckPointer(this));

    ConvertToUnicode(i);

    SS_RETURN i.GetUnicode();
}

// Append s to the end of this string.
inline void SString::Append(const SString &s)
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

inline void SString::Append(const WCHAR *string)
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

    // Wrap the string in temporary SString without copying it
    SString s(SString::Literal, string);
    s.ClearImmutable();
    Append(s);

    SS_RETURN;
}

inline void SString::AppendASCII(const CHAR *string)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(string));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Ascii, string);
    Append(s);

    SS_RETURN;
}

inline void SString::AppendUTF8(const CHAR *string)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(string));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Utf8, string);
    Append(s);

    SS_RETURN;
}

inline void SString::Append(const WCHAR c)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        THROWS;
    }
    SS_CONTRACT_END;

    InlineSString<2 * sizeof(c)> s(c);
    Append(s);

    SS_RETURN;
}

inline void SString::AppendUTF8(const CHAR c)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        THROWS;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    InlineSString<2 * sizeof(c)> s(SString::Utf8, c);
    Append(s);

    SS_RETURN;
}

// Turn this on to test that these if you are testing common scenarios dealing with
// ASCII strings that do not touch the cases where this family of function differs
// in behavior for expected reasons.
//#define VERIFY_CRT_EQUIVALNCE 1

// Helpers for CRT function equivalance.
/* static */
inline int __cdecl SString::_stricmp(const CHAR *buffer1, const CHAR *buffer2) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelperA(buffer1, buffer2, 0, TRUE, FALSE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_stricmp(buffer1, buffer2) == 0));
#endif
    return returnValue;

}

/* static */
inline int __cdecl SString::_strnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelperA(buffer1, buffer2, count, TRUE, TRUE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_strnicmp(buffer1, buffer2, count) == 0));
#endif
    return returnValue;
}

/* static */
inline int __cdecl SString::_wcsicmp(const WCHAR *buffer1, const WCHAR *buffer2) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelper(buffer1, buffer2, 0, TRUE, FALSE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_wcsicmp(buffer1, buffer2) == 0));
#endif
    return returnValue;

}

/* static */
inline int __cdecl SString::_wcsnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count) {
    WRAPPER_NO_CONTRACT;
    int returnValue = CaseCompareHelper(buffer1, buffer2, count, TRUE, TRUE);
#ifdef VERIFY_CRT_EQUIVALNCE
    _ASSERTE((returnValue == 0) == (::_wcsnicmp(buffer1, buffer2, count) == 0));
#endif
    return returnValue;
}

inline int SString::_tstricmp(const CHAR *buffer1, const CHAR *buffer2)
{
    return _stricmp(buffer1, buffer2);
}

inline int SString::_tstricmp(const WCHAR *buffer1, const WCHAR *buffer2)
{
    return _wcsicmp(buffer1, buffer2);
}

inline int SString::_tstrnicmp(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count)
{
    return _strnicmp(buffer1, buffer2, count);
}

inline int SString::_tstrnicmp(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count)
{
    return _wcsnicmp(buffer1, buffer2, count);
}

inline BOOL SString::Match(const CIterator &i, WCHAR c) const
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

inline BOOL SString::Skip(CIterator &i, const SString &s) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
    }
    SS_CONTRACT_END;

    if (Match(i, s))
    {
        i += s.GetRawCount();
        SS_RETURN TRUE;
    }
    else
        SS_RETURN FALSE;
}

inline BOOL SString::Skip(CIterator &i, WCHAR c) const
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

// Find string within this string. Return TRUE and update iterator if found
inline BOOL SString::Find(CIterator &i, const WCHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(string);
    SS_RETURN Find(i, s);
}

inline BOOL SString::FindASCII(CIterator &i, const CHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(SString::Ascii, string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Ascii, string);
    SS_RETURN Find(i, s);
}

inline BOOL SString::FindUTF8(CIterator &i, const CHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(SString::Ascii, string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Utf8, string);
    SS_RETURN Find(i, s);
}

inline BOOL SString::FindBack(CIterator &i, const WCHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(string);
    SS_RETURN FindBack(i, s);
}

inline BOOL SString::FindBackASCII(CIterator &i, const CHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(SString::Ascii, string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Ascii, string);
    SS_RETURN FindBack(i, s);
}

inline BOOL SString::FindBackUTF8(CIterator &i, const CHAR *string) const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(CheckPointer(string));
        SS_POSTCONDITION(RETVAL == Match(i, SString(SString::Ascii, string)));
        THROWS;
    }
    SS_CONTRACT_END;

    StackSString s(SString::Utf8, string);
    SS_RETURN FindBack(i, s);
}

// Insert string at iterator position
inline void SString::Insert(const Iterator &i, const SString &s)
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

inline void SString::Insert(const Iterator &i, const WCHAR *string)
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

    StackSString s(string);
    Replace(i, 0, s);

    SS_RETURN;
}

inline void SString::InsertASCII(const Iterator &i, const CHAR *string)
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

    StackSString s(SString::Ascii, string);
    Replace(i, 0, s);

    SS_RETURN;
}

inline void SString::InsertUTF8(const Iterator &i, const CHAR *string)
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

    StackSString s(SString::Utf8, string);
    Replace(i, 0, s);

    SS_RETURN;
}

// Delete string at iterator position
inline void SString::Delete(const Iterator &i, COUNT_T length)
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

// Preallocate some space for the string buffer
inline void SString::Preallocate(COUNT_T characters) const
{
    WRAPPER_NO_CONTRACT;

    // Assume unicode since we may get converted
    SBuffer::Preallocate(characters * sizeof(WCHAR));
}

// Trim unused space from the buffer
inline void SString::Trim() const
{
    WRAPPER_NO_CONTRACT;

    if (GetRawCount() == 0)
    {
        // Share the global empty string buffer.
        const_cast<SString *>(this)->SBuffer::SetImmutable(s_EmptyBuffer, sizeof(s_EmptyBuffer));
    }
    else
    {
        SBuffer::Trim();
    }
}

// RETURN true if the string is empty.
inline BOOL SString::IsEmpty() const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        NOTHROW;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SS_RETURN (GetRawCount() == 0);
}

// RETURN true if the string rep is ASCII.
inline BOOL SString::IsASCII() const
{
    SS_CONTRACT(BOOL)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        NOTHROW;
    }
    SS_CONTRACT_END;

    SS_RETURN IsRepresentation(REPRESENTATION_ASCII);
}

// Get the number of characters in the string (excluding the terminating NULL)
inline COUNT_T SString::GetCount() const
{
    SS_CONTRACT(COUNT_T)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckCount(RETVAL));
        THROWS_UNLESS_NORMALIZED;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    ConvertToFixed();

    SS_RETURN SizeToCount(GetSize());
}

// Private helpers:
// Return the current size of the string (even if it is multibyte)
inline COUNT_T SString::GetRawCount() const
{
    WRAPPER_NO_CONTRACT;

    return SizeToCount(GetSize());
}

// Private helpers:
// get string contents as a particular character set:

inline ASCII *SString::GetRawASCII() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (ASCII *) m_buffer;
}

inline UTF8 *SString::GetRawUTF8() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (UTF8 *) m_buffer;
}

inline ANSI *SString::GetRawANSI() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (ANSI *) m_buffer;
}

inline WCHAR *SString::GetRawUnicode() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    return (WCHAR *)m_buffer;
}

// Private helper:
// get the representation (ansi, unicode, utf8)
inline SString::Representation SString::GetRepresentation() const
{
    WRAPPER_NO_CONTRACT;

    return (Representation) SBuffer::GetRepresentationField();
}

// Private helper.
// Set the representation.
inline void SString::SetRepresentation(SString::Representation representation)
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACT_VOID
    {
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckRepresentation(representation));
        POSTCONDITION(GetRepresentation() == representation);
    }
    CONTRACT_END;
#else //SSTRING_EXTRA_CHECKS
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;
#endif //SSTRING_EXTRA_CHECKS

    SBuffer::SetRepresentationField((int) representation);

    SS_RETURN;
}

// Private helper:
// Get the amount to shift the byte size to get a character count
inline int SString::GetCharacterSizeShift() const
{
    WRAPPER_NO_CONTRACT;

    // Note that the flag is backwards; we want the default
    // value to match the default representation (empty)
    return (GetRepresentation()&REPRESENTATION_SINGLE_MASK) == 0;
}

//----------------------------------------------------------------------------
// Private helper.
// We know the buffer should be m_count characters. Place a null terminator
// in the buffer to make our internal string null-terminated at that length.
//----------------------------------------------------------------------------
FORCEINLINE void SString::NullTerminate()
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

    if (GetRepresentation()&REPRESENTATION_SINGLE_MASK)
    {
        ((CHAR *)end)[-1] = 0;
    }
    else
    {
        ((WCHAR *)end)[-1] = 0;
    }

    SS_RETURN;
}

//----------------------------------------------------------------------------
// private helper
// Return true if the string is a literal.
// A literal string has immutable memory.
//----------------------------------------------------------------------------
inline BOOL SString::IsLiteral() const
{
    WRAPPER_NO_CONTRACT;

    return SBuffer::IsImmutable() && (m_buffer != s_EmptyBuffer);
}

//----------------------------------------------------------------------------
// private helper:
// RETURN true if the string allocated (and should delete) its buffer.
// IsAllocated() will RETURN false for Literal strings and
// stack-based strings (the buffer is on the stack)
//----------------------------------------------------------------------------
inline BOOL SString::IsAllocated() const
{
    WRAPPER_NO_CONTRACT;

    return SBuffer::IsAllocated();
}

//----------------------------------------------------------------------------
// Return true after we call OpenBuffer(), but before we close it.
// All SString operations are illegal while the buffer is open.
//----------------------------------------------------------------------------
#if _DEBUG
inline BOOL SString::IsBufferOpen() const
{
    WRAPPER_NO_CONTRACT;

    return SBuffer::IsOpened();
}
#endif

//----------------------------------------------------------------------------
// Return true if we've scanned the string to see if it is in the ASCII subset.
//----------------------------------------------------------------------------
inline BOOL SString::IsASCIIScanned() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return SBuffer::IsFlag1();
}

//----------------------------------------------------------------------------
// Set that we've scanned the string to see if it is in the ASCII subset.
//----------------------------------------------------------------------------
inline void SString::SetASCIIScanned() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    const_cast<SString *>(this)->SBuffer::SetFlag1();
}

//----------------------------------------------------------------------------
// Return true if we've normalized the string to unicode
//----------------------------------------------------------------------------
inline BOOL SString::IsNormalized() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return SBuffer::IsFlag3();
}

//----------------------------------------------------------------------------
// Set that we've normalized the string to unicode
//----------------------------------------------------------------------------
inline void SString::SetNormalized() const
{
    WRAPPER_NO_CONTRACT;

    const_cast<SString *>(this)->SBuffer::SetFlag3();
}

//----------------------------------------------------------------------------
// Clear normalization
//----------------------------------------------------------------------------
inline void SString::ClearNormalized() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    const_cast<SString *>(this)->SBuffer::ClearFlag3();
}

//----------------------------------------------------------------------------
// Private helper.
// Check to see if the string representation has single byte size
//----------------------------------------------------------------------------
inline BOOL SString::IsSingleByte() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    return ((GetRepresentation()&REPRESENTATION_SINGLE_MASK) != 0);
}

//----------------------------------------------------------------------------
// Private helper.
// Check to see if the string representation has fixed size characters
//----------------------------------------------------------------------------
inline BOOL SString::IsFixedSize() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC;

    if (GetRepresentation()&REPRESENTATION_VARIABLE_MASK)
        return FALSE;
    else
        return TRUE;
}

//----------------------------------------------------------------------------
// Private helper.
// Check to see if the string representation is appropriate for iteration
//----------------------------------------------------------------------------
inline BOOL SString::IsIteratable() const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC;

    // Note that in many cases ANSI may be fixed width.  However we
    // currently still do not allow iterating on them, because we would have to
    // do character-by-character conversion on a character dereference (which must
    // go to unicode) .  We may want to adjust this going forward to
    // depending on perf in the non-ASCII but fixed width ANSI case.

    return ((GetRepresentation()&REPRESENTATION_VARIABLE_MASK) == 0);
}

//----------------------------------------------------------------------------
// Private helper
// Return the size of the given string in bytes
// in the given representation.
// count does not include the null-terminator, but the RETURN value does.
//----------------------------------------------------------------------------
inline COUNT_T SString::CountToSize(COUNT_T count) const
{
    SS_CONTRACT(COUNT_T)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(SizeToCount(RETVAL) == count);
        NOTHROW;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SS_RETURN (count+1) << GetCharacterSizeShift();
}

//----------------------------------------------------------------------------
// Private helper.
// Return the maxmimum count of characters that could fit in a buffer of
// 'size' bytes in the given representation.
// 'size' includes the null terminator, but the RETURN value does not.
//----------------------------------------------------------------------------
inline COUNT_T SString::SizeToCount(COUNT_T size) const
{
    SS_CONTRACT(COUNT_T)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckSize(size));
        SS_POSTCONDITION(CountToSize(RETVAL) == size);
        NOTHROW;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    SS_RETURN (size >> GetCharacterSizeShift()) - 1;
}

//----------------------------------------------------------------------------
// Private helper.
// Return the maxmimum count of characters that could fit in the current
// buffer including NULL terminator.
//----------------------------------------------------------------------------
inline COUNT_T SString::GetBufferSizeInCharIncludeNullChar() const
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return (GetSize() >> GetCharacterSizeShift());
}



//----------------------------------------------------------------------------
// Assert helper
// Assert that the iterator is within the given string.
//----------------------------------------------------------------------------
inline CHECK SString::CheckIteratorRange(const CIterator &i) const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(i >= Begin());
    CHECK(i <= End()); // Note that it's OK to look at the terminating null
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Assert helper
// Assert that the iterator is within the given string.
//----------------------------------------------------------------------------
inline CHECK SString::CheckIteratorRange(const CIterator &i, COUNT_T length) const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(i >= Begin());
    CHECK(i + length <= End());  // Note that it's OK to look at the terminating null
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Assert that the string is empty
//----------------------------------------------------------------------------
inline CHECK SString::CheckEmpty() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(IsEmpty());
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Check the range of a count
//----------------------------------------------------------------------------
inline CHECK SString::CheckCount(COUNT_T count)
{
    CANNOT_HAVE_CONTRACT;
    CHECK(CheckSize(count*sizeof(WCHAR)));
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Check the representation field
//----------------------------------------------------------------------------
inline CHECK SString::CheckRepresentation(int representation)
{
    CANNOT_HAVE_CONTRACT;
    CHECK(representation == REPRESENTATION_EMPTY
          || representation == REPRESENTATION_UNICODE
          || representation == REPRESENTATION_ASCII
          || representation == REPRESENTATION_UTF8);
    CHECK((representation & REPRESENTATION_MASK) == representation);

    CHECK_OK;
}

#if CHECK_INVARIANTS
//----------------------------------------------------------------------------
// Assert helper. Check that the string only uses the ASCII subset of
// codes.
//----------------------------------------------------------------------------
inline CHECK SString::CheckASCIIString(const CHAR *string)
{
    CANNOT_HAVE_CONTRACT;
    if (string != NULL)
        CHECK(CheckASCIIString(string, (int) strlen(string)));
    CHECK_OK;
}

inline CHECK SString::CheckASCIIString(const CHAR *string, COUNT_T count)
{
    CANNOT_HAVE_CONTRACT;
#if _DEBUG
    const CHAR *sEnd = string + count;
    while (string < sEnd)
    {
        CHECK_MSG((*string & 0x80) == 0x00, "Found non-ASCII character in string.");
        string++;
    }
#endif
    CHECK_OK;
}

//----------------------------------------------------------------------------
// Check routine and invariants.
//----------------------------------------------------------------------------

inline CHECK SString::Check() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::Check());
    CHECK_OK;
}

inline CHECK SString::Invariant() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::Invariant());
    CHECK_OK;
}

inline CHECK SString::InternalInvariant() const
{
    CANNOT_HAVE_CONTRACT;
    CHECK(SBuffer::InternalInvariant());
    CHECK(SBuffer::GetSize() >= 2);
    if (IsNormalized())
        CHECK(IsRepresentation(REPRESENTATION_UNICODE));
    CHECK_OK;
}
#endif  // CHECK_INVARIANTS

//----------------------------------------------------------------------------
// Return a writeable buffer that can store 'countChars'+1 unicode characters.
// Call CloseBuffer when done.
//----------------------------------------------------------------------------
inline WCHAR *SString::OpenUnicodeBuffer(COUNT_T countChars)
{
    SS_CONTRACT(WCHAR*)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckCount(countChars));
#if _DEBUG
        SS_POSTCONDITION(IsBufferOpen());
#endif
        SS_POSTCONDITION(GetRawCount() == countChars);
        SS_POSTCONDITION(GetRepresentation() == REPRESENTATION_UNICODE || countChars == 0);
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
    }
    SS_CONTRACT_END;

    OpenBuffer(REPRESENTATION_UNICODE, countChars);
    SS_RETURN GetRawUnicode();
}

//----------------------------------------------------------------------------
// Return a copy of the underlying  buffer, the caller is responsible for managing
// the returned memory
//----------------------------------------------------------------------------
inline WCHAR *SString::GetCopyOfUnicodeString()
{
    SS_CONTRACT(WCHAR*)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(buffer));
        THROWS;
    }
    SS_CONTRACT_END;
    NewArrayHolder<WCHAR> buffer = NULL;

    buffer = new WCHAR[GetCount() +1];
    wcscpy_s(buffer, GetCount() + 1, GetUnicode());

    SS_RETURN buffer.Extract();
}

//----------------------------------------------------------------------------
// Return a writeable buffer that can store 'countChars'+1 ansi characters.
// Call CloseBuffer when done.
//----------------------------------------------------------------------------
inline UTF8 *SString::OpenUTF8Buffer(COUNT_T countBytes)
{
    SS_CONTRACT(UTF8*)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckCount(countBytes));
#if _DEBUG
        SS_POSTCONDITION(IsBufferOpen());
#endif
        SS_POSTCONDITION(GetRawCount() == countBytes);
        SS_POSTCONDITION(GetRepresentation() == REPRESENTATION_UTF8 || countBytes == 0);
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
    }
    SS_CONTRACT_END;

    OpenBuffer(REPRESENTATION_UTF8, countBytes);
    SS_RETURN GetRawUTF8();
}

//----------------------------------------------------------------------------
// Private helper to open a raw buffer.
// Called by public functions to open the buffer in the specific
// representation.
// While the buffer is opened, all other operations are illegal. Call
// CloseBuffer() when done.
//----------------------------------------------------------------------------
inline void SString::OpenBuffer(SString::Representation representation, COUNT_T countChars)
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION_MSG(!IsBufferOpen(), "Can't nest calls to OpenBuffer()");
        PRECONDITION(CheckRepresentation(representation));
        PRECONDITION(CheckSize(countChars));
#if _DEBUG
        POSTCONDITION(IsBufferOpen());
#endif
        POSTCONDITION(GetRawCount() == countChars);
        POSTCONDITION(GetRepresentation() == representation || countChars == 0);
        THROWS;
    }
    CONTRACT_END;
#else
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
#endif

    Resize(countChars, representation);

    SBuffer::OpenRawBuffer(CountToSize(countChars));

    SS_RETURN;
}

//----------------------------------------------------------------------------
// Get the max size that can be passed to OpenUnicodeBuffer without causing
// allocations.
//----------------------------------------------------------------------------
inline COUNT_T SString::GetUnicodeAllocation()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    COUNT_T allocation = GetAllocation();
    return ( (allocation > sizeof(WCHAR))
        ? (allocation - sizeof(WCHAR)) / sizeof(WCHAR) : 0 );
}

//----------------------------------------------------------------------------
// Close an open buffer. Assumes that we wrote exactly number of characters
// we requested in OpenBuffer.
//----------------------------------------------------------------------------
inline void SString::CloseBuffer()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
#if _DEBUG
        PRECONDITION_MSG(IsBufferOpen(), "Can only CloseBuffer() after a call to OpenBuffer()");
#endif
        SS_POSTCONDITION(CheckPointer(this));
        THROWS;
    }
    SS_CONTRACT_END;

    SBuffer::CloseRawBuffer();
    NullTerminate();

    SS_RETURN;
}

//----------------------------------------------------------------------------
// CloseBuffer() tells the SString that we're done using the unsafe buffer.
// countChars is the count of characters actually used (so we can set m_count).
// This is important if we request a buffer larger than what we actually
// used.
//----------------------------------------------------------------------------
inline void SString::CloseBuffer(COUNT_T finalCount)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
#if _DEBUG
        PRECONDITION_MSG(IsBufferOpen(), "Can only CloseBuffer() after a call to OpenBuffer()");
#endif
        PRECONDITION(CheckSize(finalCount));
        SS_POSTCONDITION(CheckPointer(this));
        SS_POSTCONDITION(GetRawCount() == finalCount);
        THROWS;
    }
    SS_CONTRACT_END;

    SBuffer::CloseRawBuffer(CountToSize(finalCount));
    NullTerminate();

    SS_RETURN;
}

//----------------------------------------------------------------------------
// EnsureWritable
// Ensures that the buffer is writable
//----------------------------------------------------------------------------
inline void SString::EnsureWritable() const
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
        const_cast<SString *>(this)->Resize(GetRawCount(), GetRepresentation(), PRESERVE);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Convert the internal representation to be a fixed size
//-----------------------------------------------------------------------------
inline void SString::ConvertToFixed() const
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        SS_PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(IsFixedSize());
        THROWS_UNLESS_NORMALIZED;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    // If we're already fixed size, great.
    if (IsFixedSize())
        SS_RETURN;

    // See if we can coerce it to ASCII.
    if (ScanASCII())
        SS_RETURN;

    // Convert to unicode then.
    ConvertToUnicode();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Convert the internal representation to be an iteratable one (current
// requirements here are that it be trivially convertible to unicode chars.)
//-----------------------------------------------------------------------------
inline void SString::ConvertToIteratable() const
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        SS_PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(IsIteratable());
        THROWS_UNLESS_NORMALIZED;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    // If we're already iteratable, great.
    if (IsIteratable())
        SS_RETURN;

    // See if we can coerce it to ASCII.
    if (ScanASCII())
        SS_RETURN;

    // Convert to unicode then.
    ConvertToUnicode();

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Create CIterators on the string.
//-----------------------------------------------------------------------------

FORCEINLINE SString::CIterator SString::Begin() const
{
    SS_CONTRACT(SString::CIterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS_UNLESS_NORMALIZED;
    }
    SS_CONTRACT_END;

    ConvertToIteratable();

    SS_RETURN CIterator(this, 0);
}

FORCEINLINE SString::CIterator SString::End() const
{
    SS_CONTRACT(SString::CIterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS_UNLESS_NORMALIZED;
    }
    SS_CONTRACT_END;

    ConvertToIteratable();

    SS_RETURN CIterator(this, GetCount());
}

//-----------------------------------------------------------------------------
// Create Iterators on the string.
//-----------------------------------------------------------------------------

FORCEINLINE SString::Iterator SString::Begin()
{
    SS_CONTRACT(SString::Iterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS; // EnsureMutable always throws
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    ConvertToIteratable();
    EnsureMutable();

    SS_RETURN Iterator(this, 0);
}

FORCEINLINE SString::Iterator SString::End()
{
    SS_CONTRACT(SString::Iterator)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckValue(RETVAL));
        THROWS; // EnsureMutable always Throws
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    ConvertToIteratable();
    EnsureMutable();

    SS_RETURN Iterator(this, GetCount());
}

//-----------------------------------------------------------------------------
// CIterator support routines
//-----------------------------------------------------------------------------

inline SString::Index::Index()
{
    LIMITED_METHOD_CONTRACT;
}

inline SString::Index::Index(SString *string, SCOUNT_T index)
  : SBuffer::Index(string, index<<string->GetCharacterSizeShift())
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(string));
        PRECONDITION(string->IsIteratable());
        PRECONDITION(DoCheck(0));
        SS_POSTCONDITION(CheckPointer(this));
        // POSTCONDITION(Subtract(string->Begin()) == index); contract violation - fix later
        NOTHROW;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    m_characterSizeShift = string->GetCharacterSizeShift();

    SS_RETURN;
}

inline BYTE &SString::Index::GetAt(SCOUNT_T delta) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_ptr[delta<<m_characterSizeShift];
}

inline void SString::Index::Skip(SCOUNT_T delta)
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_ptr += (delta<<m_characterSizeShift);
}

inline SCOUNT_T SString::Index::Subtract(const Index &i) const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (SCOUNT_T) ((m_ptr - i.m_ptr)>>m_characterSizeShift);
}

inline CHECK SString::Index::DoCheck(SCOUNT_T delta) const
{
    CANNOT_HAVE_CONTRACT;
#if _DEBUG
    const SString *string = (const SString *) GetContainerDebug();

    CHECK(m_ptr + (delta<<m_characterSizeShift) >= string->m_buffer);
    CHECK(m_ptr + (delta<<m_characterSizeShift) < string->m_buffer + string->GetSize());
#endif
    CHECK_OK;
}

inline void SString::Index::Resync(const SString *string, BYTE *ptr) const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    SBuffer::Index::Resync(string, ptr);

    const_cast<SString::Index*>(this)->m_characterSizeShift = string->GetCharacterSizeShift();
}


inline const WCHAR *SString::Index::GetUnicode() const
{
    LIMITED_METHOD_CONTRACT;

    return (const WCHAR *) m_ptr;
}

inline const CHAR *SString::Index::GetASCII() const
{
    LIMITED_METHOD_CONTRACT;

    return (const CHAR *) m_ptr;
}

inline WCHAR SString::Index::operator*() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (m_characterSizeShift == 0)
        return *(CHAR*)&GetAt(0);
    else
        return *(WCHAR*)&GetAt(0);
}

inline void SString::Index::operator->() const
{
    LIMITED_METHOD_CONTRACT;
}

inline WCHAR SString::Index::operator[](int index) const
{
    WRAPPER_NO_CONTRACT;

    if (m_characterSizeShift == 0)
        return *(CHAR*)&GetAt(index);
    else
        return *(WCHAR*)&GetAt(index);
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

#endif  // _SSTRING_INL_
