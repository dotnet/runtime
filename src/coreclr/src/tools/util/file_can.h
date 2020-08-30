// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FILE_CAN_H__
#define __FILE_CAN_H__

class CFileChecksum;

enum FileType
{
    ftUnknown = 0,
    ftUnicode,
    ftSwappedUnicode,
    ftUTF8,
    ftASCII,
    ftBinary
};

HANDLE OpenFileEx( LPCWSTR filename, DWORD *fileLen, LPCWSTR relPath = NULL, bool bWrite = false);
HRESULT ReadTextFile (PCWSTR pszFileName, UINT uiCodePage, WCAllocBuffer & textBuffer, FileType *fileType);
#if !defined(TARGET_UNIX) && !defined(CSEE)
// If you call ReadTextFile a lot you should create one HCRYPTPROV and pass it in to every call, otherwise
// ReadTextFile indirectly creates and destroys a new HCRYPTPROV for every call, which is slow and unnecessary.
// You can use CryptProvider to manage an HCRYPTPROV for you.
HRESULT ReadTextFile (PCWSTR pszFileName, UINT uiCodePage, WCAllocBuffer & textBuffer, FileType *fileType, CFileChecksum *pChecksum, HCRYPTPROV hCryptProv = NULL);
#endif

// Src and Dest may be the same buffer
// Returns 0 for error (check via GetLastError()) or count of characters
// (not including NULL) copied to Dest.
// if fPreserveSrcCasing is set, ignores on-disk casing of filename (but still gets on-disk casing of directories)
// if fPreserveSrcCasing is set and and existing file matches with different short/longness it will fail
// and set the error code to ERROR_FILE_EXISTS
DWORD GetCanonFilePath(LPCWSTR wszSrcFileName, WCBuffer outBuffer, bool fPreserveSrcCasing);

// GetCanonFilePath uses a cache to eliminate redundant calls to FindFirstFile. This cache
// is global and is thus long lived. The IDE would like to minimize memory impact, so
// ClearGetCanonFilePathCache is provided here for them to clear the cache when appropriate.
void ClearGetCanonFilePathCache();

// Remove quote marks from a string.
// Translation is done in-place
LPWSTR RemoveQuotes(WCBuffer textBuffer);

// Remove quote marks from a string.
// Replace various characters with other illegal characters if unquoted.
// Translation is done in-place.
LPWSTR RemoveQuotesAndReplaceComma(WCBuffer textBuffer);        // ","  -> "|"
LPWSTR RemoveQuotesAndReplacePathDelim(WCBuffer textBuffer);    // ",;" -> "|"
LPWSTR RemoveQuotesAndReplaceAlias(WCBuffer textBuffer);        // ",;" -> "|" and "=" -> "\x1"

// Safe version of ToLowerCase
// Gaurantees null termination even if buffer size is too small
inline PWSTR WINAPI SafeToLowerCase (PCWSTR pSrc, WCBuffer textBuffer)
{
    PWSTR returnValue = ToLowerCase(pSrc, textBuffer.GetData(), textBuffer.Count());
    if (textBuffer.Count() > 0)
    {
        textBuffer.SetAt(textBuffer.Count() - 1, 0);
    }
    return returnValue;
}

// Joins a relative or absolute filename to the given path and stores the new
// filename in lpBuffer
bool MakePath( /*[in]*/LPCWSTR lpPath,  /*[in]*/LPCWSTR lpFileName, WCBuffer pathBuffer);

#endif // __FILE_CAN_H__
