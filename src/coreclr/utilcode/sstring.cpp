// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// EString.cpp
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
const BYTE StaticStringHelpers::s_EmptyBuffer[2] = { 0 };

SPTR_IMPL(EString<EncodingUnicode>, StaticStringHelpers, s_EmptyUnicode);
SPTR_IMPL(EString<EncodingUTF8>, StaticStringHelpers, s_EmptyUtf8);
SPTR_IMPL(EString<EncodingASCII>, StaticStringHelpers, s_EmptyAscii);

#ifndef DACCESS_COMPILE
namespace
{
    alignas(SString)
    BYTE emptyUnicodeSpace[(sizeof(SString))] = { 0 };
    alignas(EString<EncodingUTF8>)
    BYTE emptyUtf8Space[(sizeof(EString<EncodingUTF8>))] = { 0 };
    alignas(EString<EncodingASCII>)
    BYTE emptyAsciiSpace[(sizeof(EString<EncodingASCII>))] = { 0 };
}
#endif

void StaticStringHelpers::Startup()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
#ifndef DACCESS_COMPILE
    if (s_EmptyUnicode == NULL)
    {
        s_EmptyUnicode = PTR_SString(new (emptyUnicodeSpace) SString());
        MemoryBarrier();
    }
    if (s_EmptyUtf8 == NULL)
    {
        s_EmptyUtf8 = PTR_EString<EncodingUTF8>(new (emptyUtf8Space) EString<EncodingUTF8>());
        MemoryBarrier();
    }
    if (s_EmptyAscii == NULL)
    {
        s_EmptyAscii = PTR_EString<EncodingASCII>(new (emptyAsciiSpace) EString<EncodingASCII>());
        MemoryBarrier();
    }
#endif // DACCESS_COMPILE
}

CHECK StaticStringHelpers::CheckStartup()
{
    WRAPPER_NO_CONTRACT;
    
    CHECK(s_EmptyUnicode != NULL);
    CHECK(s_EmptyUtf8 != NULL);
    CHECK(s_EmptyAscii != NULL);
    CHECK_OK;
}

//-----------------------------------------------------------------------------
// Case insensitive helpers.
//-----------------------------------------------------------------------------

static WCHAR MapChar(WCHAR wc, DWORD dwFlags)
{
    WRAPPER_NO_CONTRACT;

    WCHAR                     wTmp;

#ifndef TARGET_UNIX

    int iRet = ::LCMapStringEx(LOCALE_NAME_INVARIANT, dwFlags, &wc, 1, &wTmp, 1, NULL, NULL, 0);
    if (!iRet) {
        // This can fail in non-exceptional cases becauseof unknown unicode characters.
        wTmp = wc;
    }

#else // !TARGET_UNIX
    // For PAL, no locale specific processing is done

    if (dwFlags == LCMAP_UPPERCASE)
    {
        wTmp = (WCHAR)
#ifdef SELF_NO_HOST
            toupper(wc);
#else
            PAL_ToUpperInvariant(wc);
#endif
    }
    else
    {
        _ASSERTE(dwFlags == LCMAP_LOWERCASE);
        wTmp = (WCHAR)
#ifdef SELF_NO_HOST
            tolower(wc);
#else
            PAL_ToLowerInvariant(wc);
#endif
    }
#endif // !TARGET_UNIX

    return wTmp;
}

#define IS_UPPER_A_TO_Z(x) (((x) >= W('A')) && ((x) <= W('Z')))
#define IS_LOWER_A_TO_Z(x) (((x) >= W('a')) && ((x) <= W('z')))
#define CAN_SIMPLE_UPCASE(x) (((x)&~0x7f) == 0)
#define CAN_SIMPLE_DOWNCASE(x) (((x)&~0x7f) == 0)
#define SIMPLE_UPCASE(x) (IS_LOWER_A_TO_Z(x) ? ((x) - W('a') + W('A')) : (x))
#define SIMPLE_DOWNCASE(x) (IS_UPPER_A_TO_Z(x) ? ((x) - W('A') + W('a')) : (x))

int CaseCompareHelper(const WCHAR *buffer1, const WCHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount)
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
                diff = ((CAN_SIMPLE_UPCASE(ch1) ? SIMPLE_UPCASE(ch1) : MapChar(ch1, LCMAP_UPPERCASE))
                        - (CAN_SIMPLE_UPCASE(ch2) ? SIMPLE_UPCASE(ch2) : MapChar(ch2, LCMAP_UPPERCASE)));
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

int CaseCompareHelperA(const CHAR *buffer1, const CHAR *buffer2, COUNT_T count, BOOL stopOnNull, BOOL stopOnCount)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(stopOnNull || stopOnCount);

    const CHAR *buffer1End = buffer1 + count;
    int diff = 0;

    while (!stopOnCount || (buffer1 < buffer1End))
    {
        CHAR ch1 = *buffer1;
        CHAR ch2 = *buffer2;
        diff = ch1 - ch2;
        if  (diff != 0 || stopOnNull)
        {
            if (ch1 == 0 || ch2 == 0)
            {
                break;
            }
            diff = (SIMPLE_UPCASE_ANSI(ch1) - SIMPLE_UPCASE_ANSI(ch2));
            if (diff != 0)
            {
                break;
            }
        }
        buffer1++;
        buffer2++;
    }
    return diff;
}


int CaseHashHelper(const WCHAR *buffer, COUNT_T count)
{
    LIMITED_METHOD_CONTRACT;

    const WCHAR *bufferEnd = buffer + count;
    ULONG hash = 5381;

    while (buffer < bufferEnd)
    {
        WCHAR ch = *buffer++;
        ch = CAN_SIMPLE_UPCASE(ch) ? SIMPLE_UPCASE(ch) : MapChar(ch, LCMAP_UPPERCASE);

        hash = (((hash << 5) + hash) ^ ch);
    }

    return hash;
}

int CaseHashHelperA(const CHAR *buffer, COUNT_T count)
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

template<>
COUNT_T EString<EncodingUTF8>::ConvertToUTF8(EString<EncodingUTF8>& s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    s.Set(GetRawBuffer());
    RETURN GetCount() + 1;
}

template<>
COUNT_T EString<EncodingASCII>::ConvertToUTF8(EString<EncodingUTF8> &s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    s.Set(GetRawBuffer());
    RETURN GetCount() + 1;
}

template<>
COUNT_T SString::ConvertToUTF8(EString<EncodingUTF8>& s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;
    // <TODO> @todo: use WC_NO_BEST_FIT_CHARS </TODO>
    bool  allAscii;
    DWORD length;

    HRESULT hr = FString::Unicode_Utf8_Length(GetRawBuffer(), & allAscii, & length);

    if (SUCCEEDED(hr))
    {
        LPUTF8 buffer = s.OpenBuffer(length);

	//FString::Unicode_Utf8 expects an array all the time
        //we optimize the empty string by replacing it with null for EString above in Resize
        if (length > 0)
        {
            hr = FString::Unicode_Utf8(GetRawBuffer(), allAscii, buffer, length);
        }

        s.CloseBuffer();
    }

    IfFailThrow(hr);

    RETURN length + 1;
}

template<>
COUNT_T EString<EncodingUTF8>::ConvertToUnicode(SString& s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;
    COUNT_T length = WszMultiByteToWideChar(CP_UTF8, 0, GetRawBuffer(), GetCount()+1, 0, 0);
    if (length == 0)
        ThrowLastError();

    LPWSTR buffer = s.OpenBuffer(length - 1);

    length = WszMultiByteToWideChar(CP_UTF8, 0, GetRawBuffer(), GetCount()+1, buffer, length);
    s.CloseBuffer();
    if (length == 0)
        ThrowLastError();

    RETURN length + 1;
}

template<>
COUNT_T SString::ConvertToUnicode(SString& s) const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(s.Check());
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    s.Set(GetRawBuffer());
    RETURN GetCount() + 1;
}
//-----------------------------------------------------------------------------
// Convert string to unicode lowercase using the invariant culture
// Note: Please don't use it in PATH as multiple character can map to the same
// lower case symbol
//-----------------------------------------------------------------------------

template<>
void SString::LowerCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;
    for (WCHAR *pwch = GetRawBuffer(); pwch < GetRawBuffer() + GetCount(); ++pwch)
    {
        *pwch = (CAN_SIMPLE_DOWNCASE(*pwch) ? SIMPLE_DOWNCASE(*pwch) : MapChar(*pwch, LCMAP_LOWERCASE));
    }
}
template<>
void EString<EncodingUTF8>::LowerCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    StackSString buffer;
    ConvertToUnicode(buffer);

    buffer.LowerCase();
    buffer.ConvertToUTF8(*this);
}

template<>
void EString<EncodingASCII>::LowerCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    StackSString buffer;
    ConvertToUnicode(buffer);

    for (CHAR *pwch = GetRawBuffer(); pwch < GetRawBuffer() + GetCount(); ++pwch)
    {
        *pwch = (CHAR)tolower(*pwch);
    }
}

//-----------------------------------------------------------------------------
// Convert string to unicode uppercase using the invariant culture
// Note: Please don't use it in PATH as multiple character can map to the same
// upper case symbol
//-----------------------------------------------------------------------------


template<>
void SString::UpperCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;
    for (WCHAR *pwch = GetRawBuffer(); pwch < GetRawBuffer() + GetCount(); ++pwch)
    {
        *pwch = (CAN_SIMPLE_UPCASE(*pwch) ? SIMPLE_UPCASE(*pwch) : MapChar(*pwch, LCMAP_UPPERCASE));
    }
}
template<>
void EString<EncodingUTF8>::UpperCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    StackSString buffer;
    ConvertToUnicode(buffer);

    buffer.UpperCase();
    buffer.ConvertToUTF8(*this);
}

template<>
void EString<EncodingASCII>::UpperCase()
{
    SS_CONTRACT_VOID
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        SS_POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        SUPPORTS_DAC;
    }
    SS_CONTRACT_END;

    StackSString buffer;
    ConvertToUnicode(buffer);

    for (CHAR *pwch = GetRawBuffer(); pwch < GetRawBuffer() + GetCount(); ++pwch)
    {
        *pwch = (CHAR)toupper(*pwch);
    }
}


#ifdef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
// Copy the string from the target into the provided buffer, converting to unicode if necessary
//
// Arguments:
//    cBufChars - size of pBuffer in count of unicode characters.
//    pBuffer - a buffer of cBufChars unicode chars.
//    pcNeedChars - space to store the number of unicode chars in the EString.
//
// Returns:
//    true if successful - and buffer is filled with the unicode representation of
//       the string.
//    false if unsuccessful.
//
template<>
bool SString::DacGetUnicode(COUNT_T                                   cBufChars,
                            _Inout_updates_z_(cBufChars) WCHAR * pBuffer,
                            COUNT_T *                                 pcNeedChars) const
{
    SUPPORTS_DAC;

    PVOID pContent = NULL;
    int iPage = CP_ACP;

    if (IsEmpty())
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
    }
    return false;
}
//---------------------------------------------------------------------------------------
//
// Copy the string from the target into the provided buffer, converting to unicode if necessary
//
// Arguments:
//    cBufChars - size of pBuffer in count of unicode characters.
//    pBuffer - a buffer of cBufChars unicode chars.
//    pcNeedChars - space to store the number of unicode chars in the EString.
//
// Returns:
//    true if successful - and buffer is filled with the unicode representation of
//       the string.
//    false if unsuccessful.
//
template<>
bool EString<EncodingASCII>::DacGetUnicode(COUNT_T                                   cBufChars,
                            _Inout_updates_z_(cBufChars) WCHAR * pBuffer,
                            COUNT_T *                                 pcNeedChars) const
{
    SUPPORTS_DAC;

    PVOID pContent = NULL;

    if (IsEmpty())
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
        if (pcNeedChars)
        {
            *pcNeedChars = WszMultiByteToWideChar(CP_ACP, 0, reinterpret_cast<PSTR>(pContent), -1, NULL, 0);
        }
        if (pBuffer && cBufChars)
        {
            if (!WszMultiByteToWideChar(CP_ACP, 0, reinterpret_cast<PSTR>(pContent), -1, pBuffer, cBufChars))
            {
                return false;
            }
        }
        return true;
    }
    return false;
}
//---------------------------------------------------------------------------------------
//
// Copy the string from the target into the provided buffer, converting to unicode if necessary
//
// Arguments:
//    cBufChars - size of pBuffer in count of unicode characters.
//    pBuffer - a buffer of cBufChars unicode chars.
//    pcNeedChars - space to store the number of unicode chars in the EString.
//
// Returns:
//    true if successful - and buffer is filled with the unicode representation of
//       the string.
//    false if unsuccessful.
//
template<>
bool EString<EncodingUTF8>::DacGetUnicode(COUNT_T                                   cBufChars,
                            _Inout_updates_z_(cBufChars) WCHAR * pBuffer,
                            COUNT_T *                                 pcNeedChars) const
{
    SUPPORTS_DAC;

    PVOID pContent = NULL;

    if (IsEmpty())
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
        if (pcNeedChars)
        {
            *pcNeedChars = WszMultiByteToWideChar(CP_UTF8, 0, reinterpret_cast<PSTR>(pContent), -1, NULL, 0);
        }
        if (pBuffer && cBufChars)
        {
            if (!WszMultiByteToWideChar(CP_UTF8, 0, reinterpret_cast<PSTR>(pContent), -1, pBuffer, cBufChars))
            {
                return false;
            }
        }
        return true;
    }
    return false;
}

#endif //DACCESS_COMPILE

// Return a global empty string
template<>
const SString &SString::Empty()
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACTL
    {
        // POSTCONDITION(RETVAL.IsEmpty());
        PRECONDITION(StaticStringHelpers::CheckStartup());
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

    _ASSERTE(StaticStringHelpers::s_EmptyUnicode != NULL);  // Did you call StaticStringHelpers::Startup()?
    return *StaticStringHelpers::s_EmptyUnicode;
}

// Return a global empty string
template<>
const EString<EncodingUTF8> &EString<EncodingUTF8>::Empty()
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACTL
    {
        // POSTCONDITION(RETVAL.IsEmpty());
        PRECONDITION(StaticStringHelpers::CheckStartup());
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

    _ASSERTE(StaticStringHelpers::s_EmptyUtf8 != NULL);  // Did you call StaticStringHelpers::Startup()?
    return *StaticStringHelpers::s_EmptyUtf8;
}

// Return a global empty string
template<>
const EString<EncodingASCII> &EString<EncodingASCII>::Empty()
{
#ifdef SSTRING_EXTRA_CHECKS
    CONTRACTL
    {
        // POSTCONDITION(RETVAL.IsEmpty());
        PRECONDITION(StaticStringHelpers::CheckStartup());
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

    _ASSERTE(StaticStringHelpers::s_EmptyAscii != NULL);  // Did you call StaticStringHelpers::Startup()?
    return *StaticStringHelpers::s_EmptyAscii;
}
