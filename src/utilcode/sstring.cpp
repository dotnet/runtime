// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// SString.cpp
// 

// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "sstring.h"
#include "ex.h"
#include "holder.h"


#if defined(_MSC_VER)
#pragma inline_depth (25)
#endif

//-----------------------------------------------------------------------------
// Static variables
//-----------------------------------------------------------------------------

// Have one internal, well-known, literal for the empty string.
const BYTE SString::s_EmptyBuffer[2] = { 0 };

// @todo: these need to be initialized by calling GetACP() or GetConsoleOutputCP()
// followed by a GetCPInfo to see if the max character size is 1.

UINT SString::s_ACP = 0;
SVAL_IMPL_INIT(BOOL, SString, s_IsANSIMultibyte, TRUE);

#ifndef DACCESS_COMPILE
static BYTE s_EmptySpace[sizeof(SString)] = { 0 };
#endif // DACCESS_COMPILE

#if FEATURE_USE_LCID
const LocaleID SString::s_defaultLCID = MAKELCID(MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US), SORT_DEFAULT);
#else
const LocaleID SString::s_defaultLCID = NULL;
#endif

SPTR_IMPL(SString,SString,s_Empty);

void SString::Startup()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (s_ACP == 0)
    {
        CPINFO info;
        
        UINT ACP = GetACP();
        if (GetCPInfo(ACP, &info) && info.MaxCharSize == 1)
            s_IsANSIMultibyte = FALSE;

#ifndef DACCESS_COMPILE
        s_Empty = PTR_SString(new (s_EmptySpace) SString());
        s_Empty->SetNormalized();
#endif // DACCESS_COMPILE

        MemoryBarrier();
        s_ACP = ACP;
    }
}

CHECK SString::CheckStartup()
{
    WRAPPER_NO_CONTRACT;

    CHECK(s_Empty != NULL);
    CHECK_OK;
}

//-----------------------------------------------------------------------------
// Case insensitive helpers.
//-----------------------------------------------------------------------------

static WCHAR MapChar(WCHAR wc, DWORD dwFlags, LocaleID lcid)
{
    WRAPPER_NO_CONTRACT;

    WCHAR                     wTmp;

#ifndef FEATURE_PAL
    
#ifdef FEATURE_USE_LCID
    int iRet = WszLCMapString(lcid, dwFlags, &wc, 1, &wTmp, 1);
#else
    // TODO: Uncertain if this is the best behavior.  Caller should specify locale name
    if (lcid == NULL || lcid[0]==W('!')) lcid = W("");
    int iRet = ::LCMapStringEx(lcid, dwFlags, &wc, 1, &wTmp, 1, NULL, NULL, 0);
#endif
    if (!iRet) {
        // This can fail in non-exceptional cases becauseof unknown unicode characters. 
        wTmp = wc;
    }

#else // !FEATURE_PAL
    // For PAL, no locale specific processing is done

    if (dwFlags == LCMAP_UPPERCASE)
    {
        wTmp =
#ifdef SELF_NO_HOST
            toupper(wc);
#else
            PAL_ToUpperInvariant(wc);
#endif
    }
    else
    {
        _ASSERTE(dwFlags == LCMAP_LOWERCASE);
        wTmp =
#ifdef SELF_NO_HOST
            tolower(wc);
#else
            PAL_ToLowerInvariant(wc);
#endif
    }
#endif // !FEATURE_PAL

    return wTmp;
}

#define IS_UPPER_A_TO_Z(x) (((x) >= W('A')) && ((x) <= W('Z')))
#define IS_LOWER_A_TO_Z(x) (((x) >= W('a')) && ((x) <= W('z')))
#define CAN_SIMPLE_UPCASE(x) (((x)&~0x7f) == 0)
#define CAN_SIMPLE_DOWNCASE(x) (((x)&~0x7f) == 0)
#define SIMPLE_UPCASE(x) (IS_LOWER_A_TO_Z(x) ? ((x) - W('a') + W('A')) : (x))
#define SIMPLE_DOWNCASE(x) (IS_UPPER_A_TO_Z(x) ? ((x) - W('A') + W('a')) : (x))

/* static */
int SString::CaseCompareHelper(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count, LocaleID lcid, BOOL stopOnNull, BOOL stopOnCount)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(stopOnNull || stopOnCount);

    const WCHAR *buffer1End = buffer1 + count;
    int diff = 0;

    while (!stopOnCount || (buffer1 < buffer1End))
    {
        WCHAR ch1 = *buffer1++;
        WCHAR ch2 = *buffer2++;
        diff = ch1 - ch2;
        if ((ch1 == 0) || (ch2 == 0)) 
        {
            if  (diff != 0 || stopOnNull) 
            {
                break;
            }
        }
        else 
        {
            if (diff != 0)
            {
                diff = ((CAN_SIMPLE_UPCASE(ch1) ? SIMPLE_UPCASE(ch1) : MapChar(ch1, LCMAP_UPPERCASE, lcid))
                        - (CAN_SIMPLE_UPCASE(ch2) ? SIMPLE_UPCASE(ch2) : MapChar(ch2, LCMAP_UPPERCASE, lcid)));
            }
            if (diff != 0) 
            {
                break;
            }
        }
    }

    return diff;
}

#define IS_LOWER_A_TO_Z_ANSI(x) (((x) >= 'a') && ((x) <= 'z'))
#define CAN_SIMPLE_UPCASE_ANSI(x) (((x) >= 0x20) && ((x) <= 0x7f))
#define SIMPLE_UPCASE_ANSI(x) (IS_LOWER_A_TO_Z(x) ? ((x) - 'a' + 'A') : (x))

// TODO: Need to get rid of LocaleID and use a LPCWSTR locale name instead.
int GetCaseInsensitiveValueA(LocaleID lcid, const CHAR *buffer, int length) {
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(buffer != NULL);
    _ASSERTE(length == 1 || ((length == 2) && IsDBCSLeadByte(*buffer)));    

    WCHAR wideCh;
    int sortValue;
    int conversionReturn = MultiByteToWideChar(CP_ACP, MB_ERR_INVALID_CHARS, buffer, length, &wideCh, 1);            
    if (conversionReturn == 0) 
    {
        // An invalid sequence should only compare equal to itself, so use a negative mapping.
        if (length == 1) 
        {
            sortValue = -((int)((unsigned char)(*buffer)));
        }
        else 
        {
            sortValue = -(((((int)((unsigned char)(*buffer))) << 8) | ((int)((unsigned char)(*(buffer + 1))))));        
        }        
    }
    else 
    {
        _ASSERTE(conversionReturn == 1);
        sortValue = MapChar(wideCh, LCMAP_UPPERCASE, lcid);
    }
    return sortValue;
}

/* static */
int SString::CaseCompareHelperA(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count, LocaleID lcid, BOOL stopOnNull, BOOL stopOnCount)
{
    LIMITED_METHOD_CONTRACT;
    
    _ASSERTE(stopOnNull || stopOnCount);

    const CHAR *buffer1End = buffer1 + count;
    int diff = 0;

    while (!stopOnCount || (buffer1 < buffer1End))
    {
        CHAR ch1 = *buffer1;
        CHAR ch2 = *buffer2;
        if ((ch1 == 0) || (ch2 == 0)) 
        {
            diff = ch1 - ch2;
            if  (diff != 0 || stopOnNull) 
            {
                break;
            }
            buffer1++;
            buffer2++;
        }
        else if (CAN_SIMPLE_UPCASE_ANSI(ch1) && CAN_SIMPLE_UPCASE_ANSI(ch2)) 
        {
            diff = ch1 - ch2;
            if (diff != 0) 
            {
                diff = (SIMPLE_UPCASE_ANSI(ch1) - SIMPLE_UPCASE_ANSI(ch2));
                if (diff != 0)
                {
                    break;
                }
            }
            buffer1++;
            buffer2++;
        }
        else 
        {
            int length = 1;
            if (s_IsANSIMultibyte 
                && IsDBCSLeadByte(ch1) 
                && IsDBCSLeadByte(ch2)
                && (!stopOnCount || ((buffer1 + 1) < buffer1End))) 
            {
                length = 2;
            }
            int sortValue1 = GetCaseInsensitiveValueA(lcid, buffer1, length);
            int sortValue2 = GetCaseInsensitiveValueA(lcid, buffer2, length);
            diff = sortValue1 - sortValue2;
            if (diff != 0) 
            {
                break;
            }
            buffer1 += length;
            buffer2 += length;
        }
    }
    return diff;
}


int CaseHashHelper(const WCHAR *buffer, COUNT_T count, LocaleID lcid)
{
    LIMITED_METHOD_CONTRACT;

    const WCHAR *bufferEnd = buffer + count;
    ULONG hash = 5381;

    while (buffer < bufferEnd)
    {
        WCHAR ch = *buffer++;
        ch = CAN_SIMPLE_UPCASE(ch) ? SIMPLE_UPCASE(ch) : MapChar(ch, LCMAP_UPPERCASE, lcid);

        hash = (((hash << 5) + hash) ^ ch);
    }

    return hash;
}

static int CaseHashHelperA(const CHAR *buffer, COUNT_T count)
{
    LIMITED_METHOD_CONTRACT;

    const CHAR *bufferEnd = buffer + count;
    ULONG hash = 5381;

    while (buffer < bufferEnd)
    {
        CHAR ch = *buffer++;
        ch = SIMPLE_UPCASE_ANSI(ch);

        hash = (((hash << 5) + hash) ^ ch);
    }

    return hash;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the unicode string
//-----------------------------------------------------------------------------
void SString::Set(const WCHAR *string)
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
        Resize((COUNT_T) wcslen(string), REPRESENTATION_UNICODE);
        wcscpy_s(GetRawUnicode(), GetBufferSizeInCharIncludeNullChar(), string);
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the first count characters of the given
// unicode string.
//-----------------------------------------------------------------------------
void SString::Set(const WCHAR *string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (count == 0)
        Clear();
    else
    {
        Resize(count, REPRESENTATION_UNICODE);
        wcsncpy_s(GetRawUnicode(), GetBufferSizeInCharIncludeNullChar(), string, count);
        GetRawUnicode()[count] = 0;
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a point to the first count characters of the given
// preallocated unicode string (shallow copy). 
//-----------------------------------------------------------------------------
void SString::SetPreallocated(const WCHAR *string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        SS_POSTCONDITION(IsEmpty());
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    SetImmutable();
    SetImmutable((BYTE*) string, count*2);
    ClearAllocated();
    SetRepresentation(REPRESENTATION_UNICODE);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the given ansi string
//-----------------------------------------------------------------------------
void SString::SetASCII(const ASCII *string)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckASCIIString(string));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (string == NULL || *string == 0)
        Clear();
    else
    {
        Resize((COUNT_T) strlen(string), REPRESENTATION_ASCII);
        strcpy_s(GetRawUTF8(), GetBufferSizeInCharIncludeNullChar(), string);
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the first count characters of the given
// ascii string
//-----------------------------------------------------------------------------
void SString::SetASCII(const ASCII *string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckASCIIString(string, count));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (count == 0)
        Clear();
    else
    {
        Resize(count, REPRESENTATION_ASCII);
        strncpy_s(GetRawASCII(), GetBufferSizeInCharIncludeNullChar(), string, count);
        GetRawASCII()[count] = 0;
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the given UTF8 string
//-----------------------------------------------------------------------------
void SString::SetUTF8(const UTF8 *string)
{
    SS_CONTRACT_VOID
    {
        // !!! Check for illegal UTF8 encoding?
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    if (string == NULL || *string == 0)
        Clear();
    else
    {
        Resize((COUNT_T) strlen(string), REPRESENTATION_UTF8);
        strcpy_s(GetRawUTF8(), GetBufferSizeInCharIncludeNullChar(), string);
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the first count characters of the given
// UTF8 string.
//-----------------------------------------------------------------------------
void SString::SetUTF8(const UTF8 *string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        // !!! Check for illegal UTF8 encoding?
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (count == 0)
        Clear();
    else
    {
        Resize(count, REPRESENTATION_UTF8);
        strncpy_s(GetRawUTF8(), GetBufferSizeInCharIncludeNullChar(), string, count);
        GetRawUTF8()[count] = 0;
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the given ANSI string
//-----------------------------------------------------------------------------
void SString::SetANSI(const ANSI *string)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (string == NULL || *string == 0)
        Clear();
    else
    {
        Resize((COUNT_T) strlen(string), REPRESENTATION_ANSI);
        strcpy_s(GetRawANSI(), GetBufferSizeInCharIncludeNullChar(), string);
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to a copy of the first count characters of the given
// ANSI string.
//-----------------------------------------------------------------------------
void SString::SetANSI(const ANSI *string, COUNT_T count)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(string, NULL_OK));
        PRECONDITION(CheckCount(count));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (count == 0)
        Clear();
    else
    {
        Resize(count, REPRESENTATION_ANSI);
        strncpy_s(GetRawANSI(), GetBufferSizeInCharIncludeNullChar(), string, count);
        GetRawANSI()[count] = 0;
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the given unicode character
//-----------------------------------------------------------------------------
void SString::Set(WCHAR character)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    if (character == 0)
        Clear();
    else
    {
        Resize(1, REPRESENTATION_UNICODE);
        GetRawUnicode()[0] = character;
        GetRawUnicode()[1] = 0;
    }

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the given UTF8 character
//-----------------------------------------------------------------------------
void SString::SetUTF8(CHAR character)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (character == 0)
        Clear();
    else
    {
        Resize(1, REPRESENTATION_UTF8);
        GetRawUTF8()[0] = character;
        GetRawUTF8()[1] = 0;
    }

    SS_RETURN;
}


//-----------------------------------------------------------------------------
// Set this string to the given ansi literal.
// This will share the memory and not make a copy.
//-----------------------------------------------------------------------------
void SString::SetLiteral(const ASCII *literal)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(literal));
        PRECONDITION(CheckASCIIString(literal));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SString s(Literal, literal);
    Set(s);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Set this string to the given unicode literal.
// This will share the memory and not make a copy.
//-----------------------------------------------------------------------------
void SString::SetLiteral(const WCHAR *literal)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(literal));
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    SString s(Literal, literal);
    Set(s);

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Hash the string contents
//-----------------------------------------------------------------------------
ULONG SString::Hash() const
{
    SS_CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    ConvertToUnicode();

    SS_RETURN HashString(GetRawUnicode());
}

//-----------------------------------------------------------------------------
// Hash the string contents
//-----------------------------------------------------------------------------
ULONG SString::HashCaseInsensitive(LocaleID lcid) const
{
    SS_CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    ConvertToIteratable();

    ULONG result;

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
    case REPRESENTATION_EMPTY:
        result = CaseHashHelper(GetRawUnicode(), GetRawCount(), lcid);
        break;

    case REPRESENTATION_ASCII:
        result = CaseHashHelperA(GetRawASCII(), GetRawCount());
        break;

    default:
        UNREACHABLE();
    }

    SS_RETURN result;
}

//-----------------------------------------------------------------------------
// Truncate this string to count characters.
//-----------------------------------------------------------------------------
void SString::Truncate(const Iterator &i)
{
    SS_CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        SS_POSTCONDITION(GetRawCount() == i - Begin());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    SS_CONTRACT_END;

    CONSISTENCY_CHECK(IsFixedSize());

    COUNT_T size = i - Begin();

    Resize(size, GetRepresentation(), PRESERVE);

    i.Resync(this, (BYTE *) (GetRawUnicode() + size));

    SS_RETURN;
}

//-----------------------------------------------------------------------------
// Convert the ASCII representation for this String to Unicode. We can do this
// quickly and in-place (if this == &dest), which is why it is optimized.
//-----------------------------------------------------------------------------
void SString::ConvertASCIIToUnicode(SString &dest) const
{
    CONTRACT_VOID
    {
        PRECONDITION(IsRepresentation(REPRESENTATION_ASCII));
        POSTCONDITION(dest.IsRepresentation(REPRESENTATION_UNICODE));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    // Handle the empty case.
    if (IsEmpty())
    {
        dest.Clear();
        RETURN;
    }

    CONSISTENCY_CHECK(CheckPointer(GetRawASCII()));
    CONSISTENCY_CHECK(GetRawCount() > 0);

    // If dest is the same as this, then we need to preserve on resize. 
    dest.Resize(GetRawCount(), REPRESENTATION_UNICODE,
                this == &dest ? PRESERVE : DONT_PRESERVE);

    // Make sure the buffer is big enough.
    CONSISTENCY_CHECK(dest.GetAllocation() > (GetRawCount() * sizeof(WCHAR)));

    // This is a poor man's widen. Since we know that the representation is ASCII,
    // we can just pad the string with a bunch of zero-value bytes. Of course,
    // we move from the end of the string to the start so that we can convert in
    // place (in the case that &dest == this).
    WCHAR *outBuf = dest.GetRawUnicode() + dest.GetRawCount();
    ASCII *inBuf = GetRawASCII() + GetRawCount();

    while (GetRawASCII() <= inBuf)
    {
        CONSISTENCY_CHECK(dest.GetRawUnicode() <= outBuf);
        // The casting zero-extends the value, thus giving us the zero-valued byte.
        *outBuf = (WCHAR) *inBuf;
        outBuf--;
        inBuf--;
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Convert the internal representation for this String to Unicode.
//-----------------------------------------------------------------------------
void SString::ConvertToUnicode() const
{
    CONTRACT_VOID
    {
        POSTCONDITION(IsRepresentation(REPRESENTATION_UNICODE));
        if (IsRepresentation(REPRESENTATION_UNICODE)) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (!IsRepresentation(REPRESENTATION_UNICODE))
    {
        if (IsRepresentation(REPRESENTATION_ASCII))
        {
            ConvertASCIIToUnicode(*(const_cast<SString *>(this)));
        }
        else
        {
            StackSString s;
            ConvertToUnicode(s);
            PREFIX_ASSUME(!s.IsImmutable());
            (const_cast<SString*>(this))->Set(s);
        }
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Convert the internal representation for this String to Unicode, while
// preserving the iterator if the conversion is done.
//-----------------------------------------------------------------------------
void SString::ConvertToUnicode(const CIterator &i) const
{
    CONTRACT_VOID
    {
        PRECONDITION(i.Check());
        POSTCONDITION(IsRepresentation(REPRESENTATION_UNICODE));
        if (IsRepresentation(REPRESENTATION_UNICODE)) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    if (!IsRepresentation(REPRESENTATION_UNICODE))
    {
        CONSISTENCY_CHECK(IsFixedSize());

        COUNT_T index = 0;
        // Get the current index of the iterator
        if (i.m_ptr != NULL)
        {
            CONSISTENCY_CHECK(GetCharacterSizeShift() == 0);
            index = (COUNT_T) (i.m_ptr - m_buffer);
        }

        if (IsRepresentation(REPRESENTATION_ASCII))
        {
            ConvertASCIIToUnicode(*(const_cast<SString *>(this)));
        }
        else
        {
            StackSString s;
            ConvertToUnicode(s);
            (const_cast<SString*>(this))->Set(s);
        }

        // Move the iterator to the new location.
        if (i.m_ptr != NULL)
        {
            i.Resync(this, (BYTE *) (GetRawUnicode() + index));
        }
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Set s to be a copy of this string's contents, but in the unicode format.
//-----------------------------------------------------------------------------
void SString::ConvertToUnicode(SString &s) const
{
    CONTRACT_VOID
    {
        PRECONDITION(s.Check());
        POSTCONDITION(s.IsRepresentation(REPRESENTATION_UNICODE));
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    int page = 0;

    switch (GetRepresentation())
    {
    case REPRESENTATION_EMPTY:
        s.Clear();
        RETURN;

    case REPRESENTATION_UNICODE:
        s.Set(*this);
        RETURN;

    case REPRESENTATION_UTF8:
        page = CP_UTF8;
        break;

    case REPRESENTATION_ASCII:
        ConvertASCIIToUnicode(s);
        RETURN;

    case REPRESENTATION_ANSI:
        page = CP_ACP;
        break;

    default:
        UNREACHABLE();
    }

    COUNT_T length = WszMultiByteToWideChar(page, 0, GetRawANSI(), GetRawCount()+1, 0, 0);
    if (length == 0)
        ThrowLastError();

    s.Resize(length-1, REPRESENTATION_UNICODE);

    length = WszMultiByteToWideChar(page, 0, GetRawANSI(), GetRawCount()+1, s.GetRawUnicode(), length);
    if (length == 0)
        ThrowLastError();

    RETURN;
}

//-----------------------------------------------------------------------------
// Set s to be a copy of this string's contents, but in the ANSI format.
//-----------------------------------------------------------------------------
void SString::ConvertToANSI(SString &s) const
{
    CONTRACT_VOID
    {
        PRECONDITION(s.Check());
        POSTCONDITION(s.IsRepresentation(REPRESENTATION_ANSI));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    switch (GetRepresentation())
    {
    case REPRESENTATION_EMPTY:
        s.Clear();
        RETURN;

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        s.Set(*this);
        RETURN;

    case REPRESENTATION_UTF8:
        // No direct conversion to ANSI
        ConvertToUnicode();
        // fall through

    case REPRESENTATION_UNICODE:
        break;

    default:
        UNREACHABLE();
    }

    // @todo: use WC_NO_BEST_FIT_CHARS
    COUNT_T length = WszWideCharToMultiByte(CP_ACP, 0, GetRawUnicode(), GetRawCount()+1,
                                        NULL, 0, NULL, NULL);

    s.Resize(length-1, REPRESENTATION_ANSI);

    // @todo: use WC_NO_BEST_FIT_CHARS
    length = WszWideCharToMultiByte(CP_ACP, 0, GetRawUnicode(), GetRawCount()+1,
                                    s.GetRawANSI(), length, NULL, NULL);
    if (length == 0)
        ThrowLastError();

    RETURN;
}

//-----------------------------------------------------------------------------
// Set s to be a copy of this string's contents, but in the utf8 format.
//-----------------------------------------------------------------------------
COUNT_T SString::ConvertToUTF8(SString &s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        POSTCONDITION(s.IsRepresentation(REPRESENTATION_UTF8));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    switch (GetRepresentation())
    {
    case REPRESENTATION_EMPTY:
        s.Clear();
        RETURN 1;

    case REPRESENTATION_ASCII:
    case REPRESENTATION_UTF8:
        s.Set(*this);
        RETURN s.GetRawCount()+1;

    case REPRESENTATION_ANSI:
        // No direct conversion from ANSI to UTF8
        ConvertToUnicode();
        // fall through

    case REPRESENTATION_UNICODE:
        break;

    default:
        UNREACHABLE();
    }

    // <TODO> @todo: use WC_NO_BEST_FIT_CHARS </TODO>
    bool  allAscii;
    DWORD length;

    HRESULT hr = FString::Unicode_Utf8_Length(GetRawUnicode(), & allAscii, & length);

    if (SUCCEEDED(hr))
    {
        s.Resize(length, REPRESENTATION_UTF8);

	//FString::Unicode_Utf8 expects an array all the time
        //we optimize the empty string by replacing it with null for SString above in Resize
        if (length > 0)
        {
            hr = FString::Unicode_Utf8(GetRawUnicode(), allAscii, (LPSTR) s.GetRawUTF8(), length);
        }
    }

    IfFailThrow(hr);

    RETURN length + 1;
}

//-----------------------------------------------------------------------------
// Replace a single character with another character.
//-----------------------------------------------------------------------------
void SString::Replace(const Iterator &i, WCHAR c)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i, 1));
        POSTCONDITION(Match(i, c));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (IsRepresentation(REPRESENTATION_ASCII) && ((c&~0x7f) == 0))
    {
        *(BYTE*)i.m_ptr = (BYTE) c;
    }
    else
    {
        ConvertToUnicode(i);

        *(USHORT*)i.m_ptr = c;
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Replace the substring specified by position, length with the given string s.
//-----------------------------------------------------------------------------
void SString::Replace(const Iterator &i, COUNT_T length, const SString &s)
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

    Representation representation = GetRepresentation();
    if (representation == REPRESENTATION_EMPTY)
    {
        // This special case contains some optimizations (like literal sharing).
        Set(s);
        ConvertToIteratable();
        i.Resync(this, m_buffer);
    }
    else
    {
        StackSString temp;
        const SString &source = GetCompatibleString(s, temp, i);

        COUNT_T deleteSize = length<<GetCharacterSizeShift();
        COUNT_T insertSize = source.GetRawCount()<<source.GetCharacterSizeShift();

        SBuffer::Replace(i, deleteSize, insertSize);
        SBuffer::Copy(i, source.m_buffer, insertSize);
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// Find s in this string starting at i. Return TRUE & update iterator if found.
//-----------------------------------------------------------------------------
BOOL SString::Find(CIterator &i, const SString &s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        POSTCONDITION(RETVAL == Match(i, s));
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // Get a compatible string from s
    StackSString temp;
    const SString &source = GetCompatibleString(s, temp, i);

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        {
            COUNT_T count = source.GetRawCount();
            const WCHAR *start = i.GetUnicode();
            const WCHAR *end = GetUnicode() + GetRawCount() - count;
            while (start <= end)
            {
                if (wcsncmp(start, source.GetRawUnicode(), count) == 0)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start++;
            }
        }
        break;

    case REPRESENTATION_ANSI:
    case REPRESENTATION_ASCII:
        {
            COUNT_T count = source.GetRawCount();
            const CHAR *start = i.GetASCII();
            const CHAR *end = GetRawASCII() + GetRawCount() - count;
            while (start <= end)
            {
                if (strncmp(start, source.GetRawASCII(), count) == 0)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start++;
            }
        }
        break;

    case REPRESENTATION_EMPTY:
        {
            if (source.GetRawCount() == 0)
                RETURN TRUE;
        }
        break;

    case REPRESENTATION_UTF8:
    default:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Find s in this string starting at i. Return TRUE & update iterator if found.
//-----------------------------------------------------------------------------
BOOL SString::Find(CIterator &i, WCHAR c) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        POSTCONDITION(RETVAL == Match(i, c));
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // Get a compatible string
    if (c & ~0x7f)
        ConvertToUnicode(i);

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        {
            const WCHAR *start = i.GetUnicode();
            const WCHAR *end = GetUnicode() + GetRawCount() - 1;
            while (start <= end)
            {
                if (*start == c)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start++;
            }
        }
        break;

    case REPRESENTATION_ANSI:
    case REPRESENTATION_ASCII:
        {
            const CHAR *start = i.GetASCII();
            const CHAR *end = GetRawASCII() + GetRawCount() - 1;
            while (start <= end)
            {
                if (*start == c)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start++;
            }
        }
        break;

    case REPRESENTATION_EMPTY:
        break;

    case REPRESENTATION_UTF8:
    default:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Find s in this string, working backwards staring at i.
// Return TRUE and update iterator if found.
//-----------------------------------------------------------------------------
BOOL SString::FindBack(CIterator &i, const SString &s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        POSTCONDITION(RETVAL == Match(i, s));
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // Get a compatible string from s
    StackSString temp;
    const SString &source = GetCompatibleString(s, temp, i);

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        {
            COUNT_T count = source.GetRawCount();
            const WCHAR *start = GetRawUnicode() + GetRawCount() - count;
            if (start > i.GetUnicode())
                start = i.GetUnicode();
            const WCHAR *end = GetRawUnicode();

            while (start >= end)
            {
                if (wcsncmp(start, source.GetRawUnicode(), count) == 0)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start--;
            }
        }
        break;

    case REPRESENTATION_ANSI:
    case REPRESENTATION_ASCII:
        {
            COUNT_T count = source.GetRawCount();
            const CHAR *start = GetRawASCII() + GetRawCount() - count;
            if (start > i.GetASCII())
                start = i.GetASCII();
            const CHAR *end = GetRawASCII();

            while (start >= end)
            {
                if (strncmp(start, source.GetRawASCII(), count) == 0)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start--;
            }
        }
        break;

    case REPRESENTATION_EMPTY:
        {
            if (source.GetRawCount() == 0)
                RETURN TRUE;
        }
        break;

    case REPRESENTATION_UTF8:
    default:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Find s in this string, working backwards staring at i.
// Return TRUE and update iterator if found.
//-----------------------------------------------------------------------------
BOOL SString::FindBack(CIterator &i, WCHAR c) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        POSTCONDITION(RETVAL == Match(i, c));
        THROWS_UNLESS_NORMALIZED;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // Get a compatible string from s
    if (c & ~0x7f)
        ConvertToUnicode(i);

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        {
            const WCHAR *start = GetRawUnicode() + GetRawCount() - 1;
            if (start > i.GetUnicode())
                start = i.GetUnicode();
            const WCHAR *end = GetRawUnicode();

            while (start >= end)
            {
                if (*start == c)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start--;
            }
        }
        break;

    case REPRESENTATION_ANSI:
    case REPRESENTATION_ASCII:
        {
            const CHAR *start = GetRawASCII() + GetRawCount() - 1;
            if (start > i.GetASCII())
                start = i.GetASCII();
            const CHAR *end = GetRawASCII();

            while (start >= end)
            {
                if (*start == c)
                {
                    i.Resync(this, (BYTE*) start);
                    RETURN TRUE;
                }
                start--;
            }
        }
        break;

    case REPRESENTATION_EMPTY:
        break;

    case REPRESENTATION_UTF8:
    default:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string begins with the contents of s
//-----------------------------------------------------------------------------
BOOL SString::BeginsWith(const SString &s) const
{
    WRAPPER_NO_CONTRACT;

    return Match(Begin(), s);
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string begins with the contents of s
//-----------------------------------------------------------------------------
BOOL SString::BeginsWithCaseInsensitive(const SString &s, LocaleID locale) const
{
    WRAPPER_NO_CONTRACT;

    return MatchCaseInsensitive(Begin(), s, locale);
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string begins with the contents of s. Invariant locale.
//-----------------------------------------------------------------------------
BOOL SString::BeginsWithCaseInsensitive(const SString &s) const
{
    WRAPPER_NO_CONTRACT;

    return BeginsWithCaseInsensitive(s, s_defaultLCID);
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string ends with the contents of s
//-----------------------------------------------------------------------------
BOOL SString::EndsWith(const SString &s) const
{
    WRAPPER_NO_CONTRACT;

    // Need this check due to iterator arithmetic below.
    if (GetCount() < s.GetCount())
    {
        return FALSE;
    }

    return Match(End() - s.GetCount(), s);
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string ends with the contents of s
//-----------------------------------------------------------------------------
BOOL SString::EndsWithCaseInsensitive(const SString &s, LocaleID locale) const
{
    WRAPPER_NO_CONTRACT;

    // Need this check due to iterator arithmetic below.
    if (GetCount() < s.GetCount())
    {
        return FALSE;
    }

    return MatchCaseInsensitive(End() - s.GetCount(), s, locale);
}

//-----------------------------------------------------------------------------
// Returns TRUE if this string ends with the contents of s. Invariant locale.
//-----------------------------------------------------------------------------
BOOL SString::EndsWithCaseInsensitive(const SString &s) const
{
    WRAPPER_NO_CONTRACT;

    return EndsWithCaseInsensitive(s, s_defaultLCID);
}

//-----------------------------------------------------------------------------
// Compare this string's contents to s's contents.
// The comparison does not take into account localization issues like case folding.
// Return 0 if equal, <0 if this < s, >0 is this > s. (same as strcmp).
//-----------------------------------------------------------------------------
int SString::Compare(const SString &s) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp);

    COUNT_T smaller;
    int equals = 0;
    int result = 0;

    if (GetRawCount() < source.GetRawCount())
    {
        smaller = GetRawCount();
        equals = -1;
    }
    else if (GetRawCount() > source.GetRawCount())
    {
        smaller = source.GetRawCount();
        equals = 1;
    }
    else
    {
        smaller = GetRawCount();
        equals = 0;
    }

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        result = wcsncmp(GetRawUnicode(), source.GetRawUnicode(), smaller);
        break;

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        result = strncmp(GetRawASCII(), source.GetRawASCII(), smaller);
        break;

    case REPRESENTATION_EMPTY:
        result = 0;
        break;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    if (result == 0)
        RETURN equals;
    else
        RETURN result;
}

//-----------------------------------------------------------------------------
// Compare this string's contents to s's contents.
// Return 0 if equal, <0 if this < s, >0 is this > s. (same as strcmp).
//-----------------------------------------------------------------------------

int SString::CompareCaseInsensitive(const SString &s, LocaleID lcid) const
{
    CONTRACT(int)
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp);

    COUNT_T smaller;
    int equals = 0;
    int result = 0;

    if (GetRawCount() < source.GetRawCount())
    {
        smaller = GetRawCount();
        equals = -1;
    }
    else if (GetRawCount() > source.GetRawCount())
    {
        smaller = source.GetRawCount();
        equals = 1;
    }
    else
    {
        smaller = GetRawCount();
        equals = 0;
    }

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        result = CaseCompareHelper(GetRawUnicode(), source.GetRawUnicode(), smaller, lcid, FALSE, TRUE);
        break;

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        result = CaseCompareHelperA(GetRawASCII(), source.GetRawASCII(), smaller, lcid, FALSE, TRUE);
        break;

    case REPRESENTATION_EMPTY:
        result = 0;
        break;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    if (result == 0)
        RETURN equals;
    else
        RETURN result;
}

//-----------------------------------------------------------------------------
// Compare this string's contents to s's contents.
// The comparison does not take into account localization issues like case folding.
// Return 1 if equal, 0 if not.
//-----------------------------------------------------------------------------
BOOL SString::Equals(const SString &s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        FAULTS_UNLESS_BOTH_NORMALIZED(s, ThrowOutOfMemory());
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp);

    COUNT_T count = GetRawCount();

    if (count != source.GetRawCount())
        RETURN FALSE;

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        RETURN (wcsncmp(GetRawUnicode(), source.GetRawUnicode(), count) == 0);

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        RETURN (strncmp(GetRawASCII(), source.GetRawASCII(), count) == 0);

    case REPRESENTATION_EMPTY:
        RETURN TRUE;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Compare this string's contents case insensitively to s's contents.
// Return 1 if equal, 0 if not.
//-----------------------------------------------------------------------------
BOOL SString::EqualsCaseInsensitive(const SString &s, LocaleID lcid) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        FAULTS_UNLESS_BOTH_NORMALIZED(s, ThrowOutOfMemory());
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp);

    COUNT_T count = GetRawCount();

    if (count != source.GetRawCount())
        RETURN FALSE;

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        RETURN (CaseCompareHelper(GetRawUnicode(), source.GetRawUnicode(), count, lcid, FALSE, TRUE) == 0);

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        RETURN (CaseCompareHelperA(GetRawASCII(), source.GetRawASCII(), count, lcid, FALSE, TRUE) == 0);

    case REPRESENTATION_EMPTY:
        RETURN TRUE;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Compare s's contents to the substring starting at position
// The comparison does not take into account localization issues like case folding.
// Return TRUE if equal, FALSE if not
//-----------------------------------------------------------------------------
BOOL SString::Match(const CIterator &i, const SString &s) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp, i);

    COUNT_T remaining = End() - i;
    COUNT_T count = source.GetRawCount();

    if (remaining < count)
        RETURN FALSE;

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
        RETURN (wcsncmp(i.GetUnicode(), source.GetRawUnicode(), count) == 0);

    case REPRESENTATION_ASCII:
    case REPRESENTATION_ANSI:
        RETURN (strncmp(i.GetASCII(), source.GetRawASCII(), count) == 0);

    case REPRESENTATION_EMPTY:
        RETURN TRUE;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Compare s's contents case insensitively to the substring starting at position
// Return TRUE if equal, FALSE if not
//-----------------------------------------------------------------------------
BOOL SString::MatchCaseInsensitive(const CIterator &i, const SString &s, LocaleID lcid) const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckIteratorRange(i));
        PRECONDITION(s.Check());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    StackSString temp;
    const SString &source = GetCompatibleString(s, temp, i);

    COUNT_T remaining = End() - i;
    COUNT_T count = source.GetRawCount();

    if (remaining < count)
        RETURN FALSE;

    switch (GetRepresentation())
    {
    case REPRESENTATION_UNICODE:
    case REPRESENTATION_ANSI:
        RETURN (CaseCompareHelper(i.GetUnicode(), source.GetRawUnicode(), count, lcid, FALSE, TRUE) == 0);

    case REPRESENTATION_ASCII:
        RETURN (CaseCompareHelperA(i.GetASCII(), source.GetRawASCII(), count, lcid, FALSE, TRUE) == 0);

    case REPRESENTATION_EMPTY:
        RETURN TRUE;

    default:
    case REPRESENTATION_UTF8:
        UNREACHABLE();
    }

    RETURN FALSE;
}

//-----------------------------------------------------------------------------
// Compare c case insensitively to the character at position
// Return TRUE if equal, FALSE if not
//-----------------------------------------------------------------------------
BOOL SString::MatchCaseInsensitive(const CIterator &i, WCHAR c, LocaleID lcid) const
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
    if (i >= End())
        SS_RETURN FALSE;

    WCHAR test = i[0];

    SS_RETURN (test == c
               || ((CAN_SIMPLE_UPCASE(test) ? SIMPLE_UPCASE(test) : MapChar(test, LCMAP_UPPERCASE, lcid))
                   == (CAN_SIMPLE_UPCASE(c) ? SIMPLE_UPCASE(c) : MapChar(c, LCMAP_UPPERCASE, lcid))));
}

//-----------------------------------------------------------------------------
// Convert string to unicode lowercase using the invariant culture
// Note: Please don't use it in PATH as multiple character can map to the same 
// lower case symbol
//-----------------------------------------------------------------------------
void SString::LowerCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        if (IsRepresentation(REPRESENTATION_UNICODE)) NOTHROW; else THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;
    
    ConvertToUnicode();

    for (WCHAR *pwch = GetRawUnicode(); pwch < GetRawUnicode() + GetRawCount(); ++pwch)
    {
        *pwch = (CAN_SIMPLE_DOWNCASE(*pwch) ? SIMPLE_DOWNCASE(*pwch) : MapChar(*pwch, LCMAP_LOWERCASE, s_defaultLCID));
    }
}

//-----------------------------------------------------------------------------
// Convert null-terminated string to lowercase using the invariant culture
//-----------------------------------------------------------------------------
//static 
void SString::LowerCase(__inout_z LPWSTR wszString)
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;
    
    if (wszString == NULL)
    {
        return;
    }
    
    for (WCHAR * pwch = wszString; *pwch != '\0'; ++pwch)
    {
        *pwch = (CAN_SIMPLE_DOWNCASE(*pwch) ? SIMPLE_DOWNCASE(*pwch) : MapChar(*pwch, LCMAP_LOWERCASE, s_defaultLCID));
    }
}

//-----------------------------------------------------------------------------
// Convert string to unicode uppercase using the invariant culture
// Note: Please don't use it in PATH as multiple character can map to the same 
// upper case symbol
//-----------------------------------------------------------------------------
void SString::UpperCase()
{
    SS_CONTRACT_VOID
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

    for (WCHAR *pwch = GetRawUnicode(); pwch < GetRawUnicode() + GetRawCount(); ++pwch)
    {
        *pwch = (CAN_SIMPLE_UPCASE(*pwch) ? SIMPLE_UPCASE(*pwch) : MapChar(*pwch, LCMAP_UPPERCASE, s_defaultLCID));
    }
}

//-----------------------------------------------------------------------------
// Get a const pointer to the internal buffer as an ANSI string.
//-----------------------------------------------------------------------------
const CHAR *SString::GetANSI(AbstractScratchBuffer &scratch) const
{
    SS_CONTRACT(const CHAR *)
    {
        INSTANCE_CHECK_NULL;
        THROWS;
        GC_NOTRIGGER;
    }
    SS_CONTRACT_END;

    if (this == NULL)
        SS_RETURN NULL;

    if (IsRepresentation(REPRESENTATION_ANSI))
        SS_RETURN GetRawANSI();

    ConvertToANSI((SString&)scratch);
    SS_RETURN ((SString&)scratch).GetRawANSI();
}

//-----------------------------------------------------------------------------
// Get a const pointer to the internal buffer as a UTF8 string.
//-----------------------------------------------------------------------------
const UTF8 *SString::GetUTF8(AbstractScratchBuffer &scratch) const
{
    CONTRACT(const UTF8 *)
    {
        INSTANCE_CHECK_NULL;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (this == NULL)
        RETURN NULL;

    if (IsRepresentation(REPRESENTATION_UTF8))
        RETURN GetRawUTF8();

    ConvertToUTF8((SString&)scratch);
    RETURN ((SString&)scratch).GetRawUTF8();
}

const UTF8 *SString::GetUTF8(AbstractScratchBuffer &scratch, COUNT_T *pcbUtf8) const
{
    CONTRACT(const UTF8 *)
    {
        INSTANCE_CHECK_NULL;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (this == NULL)
        RETURN NULL;

    if (IsRepresentation(REPRESENTATION_UTF8))
    {
        *pcbUtf8 = GetRawCount() + 1;
        RETURN GetRawUTF8();
    }

    *pcbUtf8 = ConvertToUTF8((SString&)scratch);
    RETURN ((SString&)scratch).GetRawUTF8();
}

//-----------------------------------------------------------------------------
// Get a const pointer to the internal buffer which must already be a UTF8 string.
// This avoids the need to create a scratch buffer we know will never be used.
//-----------------------------------------------------------------------------
const UTF8 *SString::GetUTF8NoConvert() const
{
    CONTRACT(const UTF8 *)
    {
        INSTANCE_CHECK_NULL;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (this == NULL)
        RETURN NULL;

    if (IsRepresentation(REPRESENTATION_UTF8))
        RETURN GetRawUTF8();

    ThrowHR(E_INVALIDARG);
}

//-----------------------------------------------------------------------------
// Safe version of sprintf.
// Prints formatted ansi text w/ var args to this buffer.
//-----------------------------------------------------------------------------
void SString::Printf(const CHAR *format, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, format);
    VPrintf(format, args);
    va_end(args);
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
static void CheckForFormatStringGlobalizationIssues(const SString &format, const SString &result)
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
    SString::CIterator itrFormat = format.Begin();
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
        SString::CIterator itrResult = format.Begin();
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

#ifndef EBADF
#define EBADF 9
#endif

#ifndef ENOMEM
#define ENOMEM 12
#endif

#ifndef ERANGE
#define ERANGE 34
#endif

#if defined(_MSC_VER)
#undef va_copy
#define va_copy(dest,src) (dest = src)
#endif

void SString::VPrintf(const CHAR *format, va_list args)
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

    if (GetRawCount() > 0)
    {
        // First, try to use the existing buffer
        va_copy(ap, args);
        int result = _vsnprintf_s(GetRawANSI(), GetRawCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >=0)
        {
            // Succeeded in writing. Now resize - 
            Resize(result, REPRESENTATION_ANSI, PRESERVE);
            SString sss(Ansi, format);
            INDEBUG(CheckForFormatStringGlobalizationIssues(sss, *this));
            RETURN;
        }
    }

    // Make a guess how long the result will be (note this will be doubled)

    COUNT_T guess = (COUNT_T) strlen(format)+1;
    if (guess < GetRawCount())
        guess = GetRawCount();
    if (guess < MINIMUM_GUESS)
        guess = MINIMUM_GUESS;

    while (TRUE)
    {
        // Double the previous guess - eventually we will get enough space
        guess *= 2;
        Resize(guess, REPRESENTATION_ANSI);

        // Clear errno to avoid false alarms
        errno = 0;

        va_copy(ap, args);
        int result = _vsnprintf_s(GetRawANSI(), GetRawCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >= 0)
        {
            // Succeed in writing. Shrink the buffer to fit exactly.
            Resize(result, REPRESENTATION_ANSI, PRESERVE);
            SString sss(Ansi, format);
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
            CONSISTENCY_CHECK_MSG(FALSE, "_vsnprintf_s failed. Potential globalization bug.");
            ThrowHR(HRESULT_FROM_WIN32(ERROR_NO_UNICODE_TRANSLATION));
        }
    }
    RETURN;
}

void SString::Printf(const WCHAR *format, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, format);
    VPrintf(format, args);
    va_end(args);
}

void SString::PPrintf(const WCHAR *format, ...)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(format));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    va_list argItr;
    va_start(argItr, format);
    PVPrintf(format, argItr);
    va_end(argItr);    

    RETURN;
}

void SString::VPrintf(const WCHAR *format, va_list args)
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

    if (GetRawCount() > 0)
    {
        // First, try to use the existing buffer
        va_copy(ap, args);
        int result = _vsnwprintf_s(GetRawUnicode(), GetRawCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >= 0)
        {
            // succeeded
            Resize(result, REPRESENTATION_UNICODE, PRESERVE);
            SString sss(format);
            INDEBUG(CheckForFormatStringGlobalizationIssues(sss, *this));
            RETURN;
        }
    }

    // Make a guess how long the result will be (note this will be doubled)

    COUNT_T guess = (COUNT_T) wcslen(format)+1;
    if (guess < GetRawCount())
        guess = GetRawCount();
    if (guess < MINIMUM_GUESS)
        guess = MINIMUM_GUESS;

    while (TRUE)
    {
        // Double the previous guess - eventually we will get enough space
        guess *= 2;
        Resize(guess, REPRESENTATION_UNICODE);

        // Clear errno to avoid false alarms
        errno = 0;

        va_copy(ap, args);
        int result = _vsnwprintf_s(GetRawUnicode(), GetRawCount()+1, _TRUNCATE, format, ap);
        va_end(ap);

        if (result >= 0)
        {
            Resize(result, REPRESENTATION_UNICODE, PRESERVE);
            SString sss(format);
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
            CONSISTENCY_CHECK_MSG(FALSE, "_vsnwprintf_s failed. Potential globalization bug.");
            ThrowHR(HRESULT_FROM_WIN32(ERROR_NO_UNICODE_TRANSLATION));
        }
    }
    RETURN;
}

void SString::PVPrintf(const WCHAR *format, va_list args)
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

    if (GetRawCount() > 0)
    {
        // First, try to use the existing buffer
        va_copy(ap, args);
#if defined(FEATURE_CORESYSTEM)
        int result = _vsnwprintf_s(GetRawUnicode(), GetRawCount()+1, _TRUNCATE, format, ap);
#else
        int result = _vswprintf_p(GetRawUnicode(), GetRawCount()+1, format, ap);
#endif
        va_end(ap);
        if (result >= 0)
        {
            // succeeded
            Resize(result, REPRESENTATION_UNICODE, PRESERVE);
            SString sss(format);
            INDEBUG(CheckForFormatStringGlobalizationIssues(sss, *this));
            RETURN;
        }
    }

    // Make a guess how long the result will be (note this will be doubled)

    COUNT_T guess = (COUNT_T) wcslen(format)+1;
    if (guess < GetRawCount())
        guess = GetRawCount();
    if (guess < MINIMUM_GUESS)
        guess = MINIMUM_GUESS;

    while (TRUE)
    {
        // Double the previous guess - eventually we will get enough space
        guess *= 2;
        Resize(guess, REPRESENTATION_UNICODE, DONT_PRESERVE);

        // Clear errno to avoid false alarms
        errno = 0;

        va_copy(ap, args);
#if defined(FEATURE_CORESYSTEM)
        int result = _vsnwprintf_s(GetRawUnicode(), GetRawCount()+1, _TRUNCATE, format, ap);
#else
        int result = _vswprintf_p(GetRawUnicode(), GetRawCount()+1, format, ap);
#endif
        va_end(ap);

        if (result >= 0)
        {
            Resize(result, REPRESENTATION_UNICODE, PRESERVE);
            SString sss(format);
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
            CONSISTENCY_CHECK_MSG(FALSE, "_vsnwprintf_s failed. Potential globalization bug.");
            ThrowHR(HRESULT_FROM_WIN32(ERROR_NO_UNICODE_TRANSLATION));
        }
    }
    RETURN;
}

void SString::AppendPrintf(const CHAR *format, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, format);
    AppendVPrintf(format, args);
    va_end(args);
}

void SString::AppendVPrintf(const CHAR *format, va_list args)
{
    WRAPPER_NO_CONTRACT;

    StackSString s;
    s.VPrintf(format, args);
    Append(s);
}

void SString::AppendPrintf(const WCHAR *format, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, format);
    AppendVPrintf(format, args);
    va_end(args);
}

void SString::AppendVPrintf(const WCHAR *format, va_list args)
{
    WRAPPER_NO_CONTRACT;

    StackSString s;
    s.VPrintf(format, args);
    Append(s);
}

//----------------------------------------------------------------------------
// LoadResource - moved to sstring_com.cpp
//----------------------------------------------------------------------------

//----------------------------------------------------------------------------
// Format the message and put the contents in this string
//----------------------------------------------------------------------------

BOOL SString::FormatMessage(DWORD dwFlags, LPCVOID lpSource, DWORD dwMessageId, DWORD dwLanguageId,
                            const SString &arg1, const SString &arg2,
                            const SString &arg3, const SString &arg4,
                            const SString &arg5, const SString &arg6,
                            const SString &arg7, const SString &arg8,
                            const SString &arg9, const SString &arg10)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    const WCHAR *args[] = {arg1.GetUnicode(), arg2.GetUnicode(), arg3.GetUnicode(), arg4.GetUnicode(),
                           arg5.GetUnicode(), arg6.GetUnicode(), arg7.GetUnicode(), arg8.GetUnicode(),
                           arg9.GetUnicode(), arg10.GetUnicode()};

    if (GetRawCount() > 0)
    {
        // First, try to use our existing buffer to hold the result.
        Resize(GetRawCount(), REPRESENTATION_UNICODE);

        DWORD result = ::WszFormatMessage(dwFlags | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                          lpSource, dwMessageId, dwLanguageId,
                                          GetRawUnicode(), GetRawCount()+1, (va_list*)args);

        // Although we cannot directly detect truncation, we can tell if we
        // used up all the space (in which case we will assume truncation.)

        if (result != 0 && result < GetRawCount())
        {
            if (GetRawUnicode()[result-1] == W(' '))
            {
                GetRawUnicode()[result-1] = W('\0');
                result -= 1;
            }
            Resize(result, REPRESENTATION_UNICODE, PRESERVE);
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

#if 1
//----------------------------------------------------------------------------
// Helper
//----------------------------------------------------------------------------

// @todo -this should be removed and placed outside of SString
void SString::MakeFullNamespacePath(const SString &nameSpace, const SString &name)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (nameSpace.GetRepresentation() == REPRESENTATION_UTF8
        && name.GetRepresentation() == REPRESENTATION_UTF8)
    {
        const UTF8 *ns = nameSpace.GetRawUTF8();
        const UTF8 *n = name.GetRawUTF8();
        COUNT_T count = ns::GetFullLength(ns, n)-1;
        Resize(count, REPRESENTATION_UTF8);
        if (count > 0)
            ns::MakePath(GetRawUTF8(), count+1, ns, n);
    }
    else
    {
        const WCHAR *ns = nameSpace;
        const WCHAR *n = name;
        COUNT_T count = ns::GetFullLength(ns, n)-1;
        Resize(count, REPRESENTATION_UNICODE);
        if (count > 0)
            ns::MakePath(GetRawUnicode(), count+1, ns, n);
    }

    RETURN;
}
#endif



//----------------------------------------------------------------------------
// Private helper.
// Check to see if the string fits the suggested representation
//----------------------------------------------------------------------------
BOOL SString::IsRepresentation(Representation representation) const
{
    CONTRACT(BOOL)
    {
        PRECONDITION(CheckRepresentation(representation));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    Representation currentRepresentation = GetRepresentation();

    // If representations are the same, cool.
    if (currentRepresentation == representation)
        RETURN TRUE;

    // If we have an empty representation, we match everything
    if (currentRepresentation == REPRESENTATION_EMPTY)
        RETURN TRUE;

    // If we're a 1 byte charset, there are some more chances to match
    if (currentRepresentation != REPRESENTATION_UNICODE
        && representation != REPRESENTATION_UNICODE)
    {
        // If we're ASCII, we can be any 1 byte rep
        if (currentRepresentation == REPRESENTATION_ASCII)
            RETURN TRUE;

        // We really want to be ASCII - scan to see if we qualify
        if (ScanASCII())
            RETURN TRUE;
    }

    // Sorry, must convert.
    RETURN FALSE;
}

//----------------------------------------------------------------------------
// Private helper.
// Get the contents of the given string in a form which is compatible with our
// string (and is in a fixed character set.)  Updates the given iterator
// if necessary to keep it in sync.
//----------------------------------------------------------------------------
const SString &SString::GetCompatibleString(const SString &s, SString &scratch, const CIterator &i) const
{
    CONTRACTL
    {
        PRECONDITION(s.Check());
        PRECONDITION(scratch.Check());
        PRECONDITION(scratch.CheckEmpty());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Since we have an iterator, we should be fixed size already
    CONSISTENCY_CHECK(IsFixedSize());

    switch (GetRepresentation())
    {
    case REPRESENTATION_EMPTY:
        return s;

    case REPRESENTATION_ASCII:
        if (s.IsRepresentation(REPRESENTATION_ASCII))
            return s;

        // We can't in general convert to ASCII, so try unicode.
        ConvertToUnicode(i);
        // fall through

    case REPRESENTATION_UNICODE:
        if (s.IsRepresentation(REPRESENTATION_UNICODE))
            return s;

        // @todo: we could convert s to unicode - is that a good policy????
        s.ConvertToUnicode(scratch);
        return scratch;

    case REPRESENTATION_UTF8:
    case REPRESENTATION_ANSI:
        // These should all be impossible since we have an CIterator on us.
    default:
        UNREACHABLE_MSG("Unexpected string representation");
    }

    return s;
}

//----------------------------------------------------------------------------
// Private helper.
// Get the contents of the given string in a form which is compatible with our
// string (and is in a fixed character set.)
// May convert our string to unicode.
//----------------------------------------------------------------------------
const SString &SString::GetCompatibleString(const SString &s, SString &scratch) const
{
    CONTRACTL
    {
        PRECONDITION(s.Check());
        PRECONDITION(scratch.Check());
        PRECONDITION(scratch.CheckEmpty());
        THROWS_UNLESS_BOTH_NORMALIZED(s);
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // First, make sure we have a fixed size.
    ConvertToFixed();

    switch (GetRepresentation())
    {
    case REPRESENTATION_EMPTY:
        return s;

    case REPRESENTATION_ANSI:
        if (s.IsRepresentation(REPRESENTATION_ANSI))
            return s;

        s.ConvertToANSI(scratch);
        return scratch;

    case REPRESENTATION_ASCII:
        if (s.IsRepresentation(REPRESENTATION_ASCII))
            return s;

        // We can't in general convert to ASCII, so try unicode.
        ConvertToUnicode();
        // fall through

    case REPRESENTATION_UNICODE:
        if (s.IsRepresentation(REPRESENTATION_UNICODE))
            return s;

        // @todo: we could convert s to unicode in place - is that a good policy????
        s.ConvertToUnicode(scratch);
        return scratch;

    case REPRESENTATION_UTF8:
    default:
        UNREACHABLE();
    }

    return s;
}

//----------------------------------------------------------------------------
// Private helper.
// If we have a 1 byte representation, scan the buffer to see if we can gain
// some conversion flexibility by labelling it ASCII
//----------------------------------------------------------------------------
BOOL SString::ScanASCII() const
{
    CONTRACT(BOOL)
    {
        POSTCONDITION(IsRepresentation(REPRESENTATION_ASCII) || IsASCIIScanned());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!IsASCIIScanned())
    {
        const CHAR *c = GetRawANSI();
        const CHAR *cEnd = c + GetRawCount();
        while (c < cEnd)
        {
            if (*c & 0x80)
                break;
            c++;
        }
        if (c == cEnd)
        {
            const_cast<SString *>(this)->SetRepresentation(REPRESENTATION_ASCII);
            RETURN TRUE;
        }
        else
            const_cast<SString *>(this)->SetASCIIScanned();
    }
    RETURN FALSE;
}

//----------------------------------------------------------------------------
// Private helper.
// Resize updates the geometry of the string and ensures that
// the space can be written to.
// count - number of characters (not including null) to hold
// preserve - if we realloc, do we copy data from old to new?
//----------------------------------------------------------------------------

void SString::Resize(COUNT_T count, SString::Representation representation, Preserve preserve)
{
    CONTRACT_VOID
    {
        PRECONDITION(CountToSize(count) >= count);
        POSTCONDITION(IsRepresentation(representation));
        POSTCONDITION(GetRawCount() == count);
        if (count == 0) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    // If we are resizing to zero, Clear is more efficient
    if (count == 0)
    {
        Clear();
    }
    else
    {
        SetRepresentation(representation);

        COUNT_T size = CountToSize(count);

        // detect overflow
        if (size < count)
            ThrowOutOfMemory();

        ClearNormalized();

        SBuffer::Resize(size, preserve);

        if (IsImmutable())
            EnsureMutable();

        NullTerminate();
    }

    RETURN;
}

//-----------------------------------------------------------------------------
// This is essentially a specialized version of the above for size 0
//-----------------------------------------------------------------------------
void SString::Clear()
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

    SetRepresentation(REPRESENTATION_EMPTY);

    if (IsImmutable())
    {
        // Use shared empty string rather than allocating a new buffer
        SBuffer::SetImmutable(s_EmptyBuffer, sizeof(s_EmptyBuffer));
    }
    else
    {
        // Leave allocated buffer for future growth
        SBuffer::TweakSize(sizeof(WCHAR));
        GetRawUnicode()[0] = 0;
    }

    RETURN;
}


#ifdef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Return a pointer to the raw buffer
//
// Returns:
//    A pointer to the raw string buffer.
//
void * SString::DacGetRawContent() const
{
    if (IsEmpty())
    {
        return NULL;
    }

    switch (GetRepresentation())
    {
        case REPRESENTATION_EMPTY:
            return NULL;

        case REPRESENTATION_UNICODE:
        case REPRESENTATION_UTF8:
        case REPRESENTATION_ASCII:
        case REPRESENTATION_ANSI:
            // Note: no need to call DacInstantiateString because we know the exact length already.
            return SBuffer::DacGetRawContent();

        default:
            DacNotImpl();
            return NULL;
    }
}

//---------------------------------------------------------------------------------------
//
// Return a pointer to the raw buffer as a pointer to a unicode string.  Does not
// do conversion, and thus requires that the representation already be in unicode.
//
// Returns:
//    A pointer to the raw string buffer as a unicode string.
//
const WCHAR * SString::DacGetRawUnicode() const
{
    if (IsEmpty() || (GetRepresentation() == REPRESENTATION_EMPTY))
    {
        return W("");
    }

    if (GetRepresentation() != REPRESENTATION_UNICODE)
    {
        DacError(E_UNEXPECTED);
    }

    HRESULT status = S_OK;
    WCHAR* wszBuf = NULL;
    EX_TRY
    {
        wszBuf = static_cast<WCHAR*>(SBuffer::DacGetRawContent());
    }
    EX_CATCH_HRESULT(status);

    if (SUCCEEDED(status))
    {
        return wszBuf;
    }
    else
    {
        return NULL;
    }
}

//---------------------------------------------------------------------------------------
//
// Copy the string from the target into the provided buffer, converting to unicode if necessary
//
// Arguments:
//    cBufChars - size of pBuffer in count of unicode characters.
//    pBuffer - a buffer of cBufChars unicode chars.
//    pcNeedChars - space to store the number of unicode chars in the SString.
//
// Returns:
//    true if successful - and buffer is filled with the unicode representation of
//       the string.
//    false if unsuccessful.
//
bool SString::DacGetUnicode(COUNT_T                                   cBufChars,
                            __out_z __inout_ecount(cBufChars) WCHAR * pBuffer,
                            COUNT_T *                                 pcNeedChars) const
{
    SUPPORTS_DAC;

    PVOID pContent = NULL;
    int iPage = CP_ACP;

    if (IsEmpty() || (GetRepresentation() == REPRESENTATION_EMPTY))
    {
        if (pcNeedChars)
        {
            *pcNeedChars = 1;
        }
        if (pBuffer && cBufChars)
        {
            pBuffer[0] = 0;
        }
        return true;
    }

    HRESULT status = S_OK;
    EX_TRY
    {
        pContent = SBuffer::DacGetRawContent();
    }
    EX_CATCH_HRESULT(status);

    if (SUCCEEDED(status) && pContent != NULL)
    {
        switch (GetRepresentation())
        {

        case REPRESENTATION_UNICODE:

            if (pcNeedChars)
            {
                *pcNeedChars = GetCount() + 1;
            }

            if (pBuffer && cBufChars)
            {
                if (cBufChars > GetCount() + 1)
                {
                    cBufChars = GetCount() + 1;
                }
                memcpy(pBuffer, pContent, cBufChars * sizeof(*pBuffer));
                pBuffer[cBufChars - 1] = 0;
            }

            return true;

        case REPRESENTATION_UTF8:
            iPage = CP_UTF8;
        case REPRESENTATION_ASCII:
        case REPRESENTATION_ANSI:
            // iPage defaults to CP_ACP.
            if (pcNeedChars)
            {
                *pcNeedChars = WszMultiByteToWideChar(iPage, 0, reinterpret_cast<PSTR>(pContent), -1, NULL, 0);
            }
            if (pBuffer && cBufChars)
            {
                if (!WszMultiByteToWideChar(iPage, 0, reinterpret_cast<PSTR>(pContent), -1, pBuffer, cBufChars))
                {
                    return false;
                }
            }
            return true;

        default:
            DacNotImpl();
            return false;
        }
    }
    return false;
}

#endif //DACCESS_COMPILE
