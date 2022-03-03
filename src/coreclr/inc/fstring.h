// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    HRESULT Unicode_Utf8_Length(_In_z_ LPCWSTR pString, _Out_ bool * pAllAscii, _Out_ DWORD * pLength);

    // Convert UNICODE string to UTF8 string. Direct/fast conversion if ASCII
    HRESULT Unicode_Utf8(_In_z_ LPCWSTR pString, bool allAscii, _Out_writes_bytes_(length) LPSTR pBuffer, DWORD length);

    // Scan for ASCII string, calculate result UNICODE string length
    HRESULT Utf8_Unicode_Length(_In_z_ LPCSTR pString, _Out_ bool * pAllAscii, _Out_ DWORD * pLength);

    // Convert UTF8 string to UNICODE. Direct/fast conversion if ASCII
    HRESULT Utf8_Unicode(_In_z_ LPCSTR pString, bool allAscii, _Out_writes_bytes_(length) LPWSTR pBuffer, DWORD length);

    HRESULT ConvertUnicode_Utf8(_In_z_ LPCWSTR pString, _Outptr_result_z_ LPSTR * pBuffer);

    HRESULT ConvertUtf8_Unicode(_In_z_ LPCSTR pString, _Outptr_result_z_ LPWSTR * pBuffer);

}  // namespace FString

#endif  // _FSTRING_H_
