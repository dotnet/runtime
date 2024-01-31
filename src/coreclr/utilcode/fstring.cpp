// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// FString.cpp
//

// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "ex.h"
#include "holder.h"

#include "fstring.h"


namespace FString
{

#ifdef _MSC_VER
#pragma optimize("t", on)
#endif // _MSC_VER

#define MAX_LENGTH 0x1fffff00


HRESULT Unicode_Utf8_Length(_In_z_ LPCWSTR pString, _Out_ bool * pAllAscii, _Out_ DWORD * pLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    * pAllAscii = true;

    LPCWSTR p = pString;

    while (true)
    {
        WCHAR ch = * p;

        // Single check for termination and non ASCII
        if (((unsigned) (ch - 1)) >= 0x7F)
        {
            if (ch != 0)
            {
                * pAllAscii = false;
            }

            break;
        }

        p ++;
    }

    if (* pAllAscii)
    {
        if ((p - pString) > MAX_LENGTH)
        {
            return COR_E_OVERFLOW;
        }

        * pLength = (DWORD) (p - pString);
    }
    else // use WideCharToMultiByte to calculate result length
    {
        * pLength = WszWideCharToMultiByte(CP_UTF8, 0, pString, -1, NULL, 0, NULL, NULL);

        if (*pLength == 0)
        {
            return HRESULT_FROM_GetLastError();
        }

        // Remove the count of null terminator, to be consistent with the all-ASCII case.
        --*pLength;

        if (*pLength > MAX_LENGTH)
        {
            return COR_E_OVERFLOW;
        }
    }

    return S_OK;
}


// UNICODE to UTF8
HRESULT Unicode_Utf8(_In_z_ LPCWSTR pString, bool allAscii, _Out_writes_bytes_(length) LPSTR pBuffer, DWORD length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pBuffer[length] = 0;

    if (allAscii)
    {
        LPCWSTR p = pString;

        LPSTR q = pBuffer;

        LPCWSTR endP = p + length - 8;

        // Unfold to optimize for long string: 8 chars per iteration
        while (p < endP)
        {
            q[0] = (char) p[0];
            q[1] = (char) p[1];
            q[2] = (char) p[2];
            q[3] = (char) p[3];

            q[4] = (char) p[4];
            q[5] = (char) p[5];
            q[6] = (char) p[6];
            q[7] = (char) p[7];

            q += 8;
            p += 8;
        }

        endP += 8;

        while (p < endP)
        {
            * q ++ = (char) * p ++;
        }
    }
    else
    {
        length = WszWideCharToMultiByte(CP_UTF8, 0, pString, -1, pBuffer, (int) length + 1, NULL, NULL);

        if (length == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    return S_OK;
}


HRESULT Utf8_Unicode_Length(_In_z_ LPCSTR pString, _Out_ bool * pAllAscii, _Out_ DWORD * pLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    * pAllAscii = true;

    LPCSTR p = pString;

    while (true)
    {
        char ch = * p;

        // Single check for termination and non ASCII
        if (((unsigned) (ch - 1)) >= 0x7F)
        {
            if (ch != 0)
            {
                * pAllAscii = false;
            }

            break;
        }

        p ++;
    }

    if (* pAllAscii)
    {
        if ((p - pString) > MAX_LENGTH)
        {
            return COR_E_OVERFLOW;
        }

        * pLength = (DWORD)(p - pString);
    }
    else
    {
        * pLength = WszMultiByteToWideChar(CP_UTF8, 0, pString, -1, NULL, 0);

        if (* pLength == 0)
        {
            return HRESULT_FROM_GetLastError();
        }

        // Remove the count of null terminator, to be consistent with the all-ASCII case.
        --*pLength;

        if (* pLength > MAX_LENGTH)
        {
            return COR_E_OVERFLOW;
        }
    }

    return S_OK;
}


// UTF8 to Unicode

HRESULT Utf8_Unicode(_In_z_ LPCSTR pString, bool allAscii, _Out_writes_bytes_(length) LPWSTR pBuffer, DWORD length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pBuffer[length] = 0;

    if (allAscii)
    {
        LPCSTR p = pString;

        LPWSTR q = pBuffer;

        LPCSTR endP = p + length - 8;

        // Unfold to optimize for long string: 4 chars per iteration
        while (p < endP)
        {
            q[0] = (WCHAR) p[0];
            q[1] = (WCHAR) p[1];
            q[2] = (WCHAR) p[2];
            q[3] = (WCHAR) p[3];

            q[4] = (WCHAR) p[4];
            q[5] = (WCHAR) p[5];
            q[6] = (WCHAR) p[6];
            q[7] = (WCHAR) p[7];

            q += 8;
            p += 8;
        }

        endP += 8;

        while (p < endP)
        {
            * q ++ = (WCHAR) * p ++;
        }
    }
    else
    {
        length = WszMultiByteToWideChar(CP_UTF8, 0, pString, -1, pBuffer, (int) length + 1);

        if (length == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    return S_OK;
}


HRESULT ConvertUnicode_Utf8(_In_z_ LPCWSTR pString, _Outptr_result_z_ LPSTR * pBuffer)
{
    bool  allAscii;
    DWORD length;

    HRESULT hr = Unicode_Utf8_Length(pString, & allAscii, & length);

    if (SUCCEEDED(hr))
    {
        * pBuffer = new (nothrow) char[length + 1];

        if (* pBuffer == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            hr = Unicode_Utf8(pString, allAscii, * pBuffer, length);
        }
    }

    return hr;
}


HRESULT ConvertUtf8_Unicode(_In_z_ LPCSTR pString, _Outptr_result_z_ LPWSTR * pBuffer)
{
    bool  allAscii;
    DWORD length;

    HRESULT hr = Utf8_Unicode_Length(pString, & allAscii, & length);

    if (SUCCEEDED(hr))
    {
        * pBuffer = new (nothrow) WCHAR[length + 1];

        if (* pBuffer == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            hr = Utf8_Unicode(pString, allAscii, * pBuffer, length);
        }
    }

    return hr;
}


#ifdef _MSC_VER
#pragma optimize("", on)
#endif // _MSC_VER

} // namespace FString
