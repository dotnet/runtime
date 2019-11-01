// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// FString.h  (Fast String)
// 

// ---------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------
// FString is fast string handling namespace


// 1) Simple
// 2) No C++ exception
// 3) Optimized for speed


#ifndef _FSTRING_H_
#define _FSTRING_H_

namespace FString
{
    // Note: All "length" parameters do not count the space for the null terminator.
    // Caller of Unicode_Utf8 and Utf8_Unicode must pass in a buffer of size at least length + 1.

    // Scan for ASCII only string, calculate result UTF8 string length
    HRESULT Unicode_Utf8_Length(__in_z LPCWSTR pString, __out bool * pAllAscii, __out DWORD * pLength);

    // Convert UNICODE string to UTF8 string. Direct/fast conversion if ASCII
    HRESULT Unicode_Utf8(__in_z LPCWSTR pString, bool allAscii, __out_z LPSTR pBuffer, DWORD length);

    // Scan for ASCII string, calculate result UNICODE string length
    HRESULT Utf8_Unicode_Length(__in_z LPCSTR pString, __out bool * pAllAscii, __out DWORD * pLength);

    // Convert UTF8 string to UNICODE. Direct/fast conversion if ASCII
    HRESULT Utf8_Unicode(__in_z LPCSTR pString, bool allAscii, __out_z LPWSTR pBuffer, DWORD length);

    HRESULT ConvertUnicode_Utf8(__in_z LPCWSTR pString, __out_z LPSTR * pBuffer);

    HRESULT ConvertUtf8_Unicode(__in_z LPCSTR pString, __out_z LPWSTR * pBuffer);

}  // namespace FString

#endif  // _FSTRING_H_
