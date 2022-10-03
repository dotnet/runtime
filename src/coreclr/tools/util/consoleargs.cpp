// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include "consoleargs.h"
#include <strsafe.h>

typedef unsigned char byte;

size_t SafeStrCopy( _In_ LPCWSTR wszSrc, _In_ size_t cchSrc, _Out_ LPWSTR wszDest, _In_ size_t cchDest)
{
    if (cchSrc == (size_t)-1)
        cchSrc = wcslen(wszSrc);

    if (cchSrc >= cchDest) {
        SetLastError(ERROR_FILENAME_EXCED_RANGE);
        return 0;
    }

    if (FAILED(StringCchCopyNW( wszDest, cchDest, wszSrc, cchSrc))) {
        SetLastError(ERROR_FILENAME_EXCED_RANGE);
        return 0;
    }
    return cchSrc;
}

size_t SafeStrLower( _In_ LPCWSTR wszSrc, _In_ size_t cchSrc, _Out_ LPWSTR wszDest, _In_ size_t cchDest)
{
    if (cchSrc == (size_t)-1)
        cchSrc = wcslen(wszSrc);

    if (cchSrc >= cchDest) {
        SetLastError(ERROR_FILENAME_EXCED_RANGE);
        return 0;
    }

    SafeStrCopy(wszSrc, cchSrc, wszDest, cchDest);
    _wcslwr_s((WCHAR*)wszDest, cchDest);
    return wcslen(wszDest);
}

inline int HexValue (WCHAR c)
{
    return (c >= '0' && c <= '9') ? c - '0' : (c & 0xdf) - 'A' + 10;
}

#ifndef TARGET_UNIX
//  Get canonical file path from a user specified path.  wszSrcfileName can include relative paths, etc.
//  Much of this function was taken from csc.exe.
DWORD GetCanonFilePath(_In_z_ LPCWSTR wszSrcFileName, _Out_z_cap_(cchDestFileName) LPWSTR wszDestFileName, _In_ DWORD cchDestFileName, _In_ bool fPreserveSrcCasing)
{
    DWORD full_len;
    WCHAR * full_path = new WCHAR[cchDestFileName]; // an intermediate buffer
    WCHAR * temp_path = new WCHAR[cchDestFileName]; // Used if FindFile fails
    WCHAR * full_cur;
    WCHAR * out_cur;
    WCHAR * out_end;
    bool hasDrive = false;

    memset(full_path, 0, cchDestFileName * sizeof(WCHAR));
    out_cur = wszDestFileName;
    out_end = out_cur + cchDestFileName;
    if (wszSrcFileName != wszDestFileName)
        *out_cur = L'\0';
    full_cur = full_path;

    // Replace '\\' with single backslashes in paths, because W_GetFullPathName fails to do this on win9x.
    size_t i = 0;
    size_t j = 0;
    size_t length = wcslen(wszSrcFileName);
    while (j<length)
    {
        // UNC paths start with '\\' so skip the first character if it is a backslash.
        if (j!= 0 && wszSrcFileName[j] == '\\' && wszSrcFileName[j+1] == '\\')
            j++;
        else
            temp_path[i++] = wszSrcFileName[j++];
        if (i >= cchDestFileName) {
            SetLastError(ERROR_FILENAME_EXCED_RANGE);
            goto FAIL;
        }
    }
    temp_path[i] = L'\0';

    full_len = GetFullPathNameW(temp_path, cchDestFileName, full_path, NULL);
    if (wszSrcFileName == wszDestFileName)
        wszDestFileName[cchDestFileName-1] = L'\0';
    if (full_len == 0) {
        goto FAIL;
    } else if (full_len >= cchDestFileName) {
        SetLastError(ERROR_FILENAME_EXCED_RANGE);
        goto FAIL;
    }

    // Allow only 1 ':' for drives and no long paths with "\\?\"
    if (((full_path[0] >= L'a' && full_path[0] <= L'z') ||
        (full_path[0] >= L'A' && full_path[0] <= L'Z')) &&
        full_path[1] == L':')
        hasDrive = true;

    // We don't allow colons (except after the drive letter)
    // long paths beginning with "\\?\"
    // devices beginning with "\\.\"
    // or wildcards
    // or characters 0-31
    if (wcschr( full_path + (hasDrive ? 2 : 0), W(':')) != NULL ||
        wcsncmp( full_path, W("\\\\?\\"), 4) == 0 ||
        wcsncmp( full_path, W("\\\\.\\"), 4) == 0 ||
        wcspbrk(full_path, W("?*\x1\x2\x3\x4\x5\x6\x7\x8\x9")
            W("\xA\xB\xC\xD\xE\xF\x10\x11\x12\x13\x14\x15")
            W("\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F\0")) != NULL) {
        SetLastError(ERROR_INVALID_NAME);
        goto FAIL;
    }


    if (hasDrive) {
        size_t len = SafeStrLower( full_path, 3, out_cur, out_end - out_cur);
        if (len == 0)
            goto FAIL;

        full_cur += 3;
        out_cur += len;

    } else if (full_path[0] == L'\\' && full_path[1] == L'\\') {
        // Must be a UNC pathname, so lower-case the server and share
        // since there's no known way to get the 'correct casing'
        WCHAR * slash = wcschr(full_path + 2, L'\\');
        // slash should now point to the backslash between the server and share
        if (slash == NULL || slash == full_path + 2) {
            SetLastError(ERROR_INVALID_NAME);
            goto FAIL;
        }

        slash = wcschr(slash + 1, L'\\');
        if (slash == NULL) {
            slash = full_path + wcslen(full_path);
        } else if (slash[-1] == L'\\') {
            // An empty share-name?
            SetLastError(ERROR_INVALID_NAME);
            goto FAIL;
        } else
            slash++;
        // slash should now point to char after the slash after the share name
        // or the end of the sharename if there's no trailing slash

        size_t len = SafeStrLower( full_path, slash - full_path, out_cur, out_end - out_cur);
        if (len == 0)
            goto FAIL;

        full_cur = slash;
        out_cur += len;

    } else {
        // Not a drive-leter path or a UNC path, so assume it's invalid
        SetLastError(ERROR_INVALID_NAME);
        goto FAIL;
    }

    // We either have a lower-cased drive letter or a UNC name
    // with it's trailing slash
    // out_cur points to the trailing NULL
    // full_cur points to the character after the slash

    // Now iterate over each element of the path and attempt to canonicalize it
    // It's possible for this loop to never run
    //  (for strings like "C:\" or "\\unc\share" or "\\unc\share2\")
    while (*full_cur) {
        WIN32_FIND_DATAW find_data;
        bool hasSlash = true;
        WCHAR * slash = wcschr(full_cur, '\\');
        if (slash == NULL) {
            // This means we're on the last element of the path
            // so work with everything left in the string
            hasSlash = false;
            slash = full_cur + wcslen(full_cur);
        }

        // Check to make sure we have enough room for the next part of the path
        if (out_cur + (slash - full_cur) >= out_end) {
            SetLastError(ERROR_FILENAME_EXCED_RANGE);
            goto FAIL;
        }

        // Copy over the next path part into the output buffer
        // so we can run FindFile to get the correct casing/long filename
        memcpy(out_cur, full_cur, (BYTE*)slash - (BYTE*)full_cur);
        out_cur[slash - full_cur] = L'\0';
        HANDLE hFind = FindFirstFileW(wszDestFileName, &find_data);
        if (hFind == INVALID_HANDLE_VALUE) {
            size_t temp_len;

            // We coundn't find the file, the general causes are the file doesn't exist
            // or we don't have access to it.  Either way we still try to get a canonical filename
            // but preserve the passed in casing for the filename

            if (!hasSlash && fPreserveSrcCasing) {
                // This is the last component in the filename, we should preserve the user's input text
                // even if we can't find it
                out_cur += slash - full_cur;
                full_cur = slash;
                break;
            }

            // This will succeed even if we don't have access to the file
            // (And on NT4 if the filename is already in 8.3 form)
            temp_len = GetShortPathNameW(wszDestFileName, temp_path, cchDestFileName);
            if (temp_len == 0) {
                // GetShortPathName failed, we have no other way of figuring out the
                // The filename, so just lowercase it so it hashes in a case-insensitive manner

                if (!hasSlash) {
                    // If it doesn't have a slash, then it must be the last part of the filename,
                    // so don't lowercase it, preserve whatever casing the user gave
                    temp_len = SafeStrCopy( full_cur, slash - full_cur, out_cur, out_end - out_cur);
                } else {
                    temp_len = SafeStrLower( full_cur, slash - full_cur, out_cur, out_end - out_cur);
                }
                if (temp_len == 0)
                    goto FAIL;

                full_cur = slash;
                out_cur += temp_len;

            } else if (temp_len >= cchDestFileName) {
                // The short filename is longer than the whole thing?
                // This shouldn't ever happen, right?
                SetLastError(ERROR_FILENAME_EXCED_RANGE);
                goto FAIL;
            } else {
                // GetShortPathName succeeded with a path that is less than BUFFER_LEN
                // find the last slash and copy it.  (We don't want to copy previous
                // path components that we've already 'resolved')
                // However, GetShortPathName doesn't always correct the casing
                // so as a safe-guard, lower-case it (unless it's the last filename)
                WCHAR * temp_slash = wcsrchr(temp_path, L'\\');

                temp_slash++;
                size_t len = 0;
                if (!hasSlash) {
                    len = SafeStrCopy( temp_slash, -1, out_cur, out_end - out_cur);
                } else {
                    len = SafeStrLower( temp_slash, -1, out_cur, out_end - out_cur);
                }
                if (len == 0)
                    goto FAIL;

                full_cur = slash;
                out_cur += len;

            }
        } else {
            // Copy over the properly cased long filename
            FindClose(hFind);
            size_t name_len = wcslen(find_data.cFileName);
            if (out_cur + name_len + (hasSlash ? 1 : 0) >= out_end) {
                SetLastError(ERROR_FILENAME_EXCED_RANGE);
                goto FAIL;
            }

            // out_cur already has the filename with the input casing, so we can just leave it alone
            // if this is not a directory name and the caller asked to perserve the casing
            if (hasSlash || !fPreserveSrcCasing) {
                memcpy(out_cur, find_data.cFileName, name_len * sizeof(WCHAR));
            }
            else if (name_len != (slash - full_cur) || _wcsnicmp(find_data.cFileName, full_cur, name_len) != 0) {
                // The user asked us to preserve the casing of the filename
                // and the filename is different by more than just casing so report
                // an error indicating we can't create the file
                SetLastError(ERROR_FILE_EXISTS);
                goto FAIL;
            }

            out_cur += name_len;
            full_cur = slash;
        }

        if (hasSlash) {
            if (out_cur + 1 >= out_end) {
                SetLastError(ERROR_FILENAME_EXCED_RANGE);
                goto FAIL;
            }
            full_cur++;
            *out_cur++ = L'\\';
        }
        *out_cur = '\0';
    }

    return (DWORD)(out_cur - wszDestFileName);

FAIL:
    if (full_path)
    {
        delete [] full_path;
    }
    if (temp_path)
    {
        delete [] temp_path;
    }
    return 0;
}
#endif // !TARGET_UNIX

bool FreeString(LPCWSTR szText)
{
    if (szText)
        delete [] (const_cast<LPWSTR>(szText));
    return true;
}

bool IsWhitespace(WCHAR c)
{
    return c == L' ' || c == L'\t' || c == L'\n' || c == L'\r';
}

void ConsoleArgs::CleanUpArgs()
{
    while (m_listArgs)
    {
        WStrList * next = m_listArgs->next;
        if (m_listArgs->arg)
            delete [] m_listArgs->arg;
        delete m_listArgs;
        m_listArgs = next;
    }

    if (m_rgArgs)
        delete[] m_rgArgs;

    m_rgArgs = NULL;

    if(m_lastErrorMessage)
    {
        delete[] m_lastErrorMessage;
    }
}

bool ConsoleArgs::GetFullFileName(LPCWSTR szSource, _Out_writes_(cchFilenameBuffer) LPWSTR filenameBuffer, DWORD cchFilenameBuffer, bool fOutputFilename)
{
#ifdef TARGET_UNIX
    WCHAR tempBuffer[MAX_LONGPATH];
    memset(filenameBuffer, 0, cchFilenameBuffer * sizeof(WCHAR));
    if (!PathCanonicalizeW(tempBuffer, szSource) ||
        StringCchCopyW(filenameBuffer, cchFilenameBuffer, tempBuffer) != S_OK)
#else
    if (0 == GetCanonFilePath( szSource, filenameBuffer, cchFilenameBuffer, fOutputFilename))
#endif
    {
        if (filenameBuffer[0] == L'\0')
        {
            // This could easily fail because of an overflow, but that's OK
            // we only want what will fit in the output buffer so we can print
            // a good error message
            StringCchCopyW(filenameBuffer, cchFilenameBuffer - 4, szSource);
            // Don't cat on the ..., only stick it in the last 4 characters
            // to indicate truncation (if the string is short than this it just won't print)
            StringCchCopyW(filenameBuffer + cchFilenameBuffer - 4, 4, W("..."));
        }
        return false;
    }
    return true;
}

//
// Clear previous error message if any and set the new one by copying into m_lastErrorMessage.
// We are responsible for freeing the memory destruction.
//
void ConsoleArgs::SetErrorMessage(_In_ LPCWSTR pwzMessage)
{
    if (m_lastErrorMessage != nullptr)
    {
        delete[] m_lastErrorMessage;
    }
    m_errorOccurred = true;
    m_lastErrorMessage = new WCHAR[wcslen(pwzMessage) + 1];
    if (m_lastErrorMessage == nullptr)
    {
        //
        // Out of memory allocating error string
        //
        m_lastErrorMessage = kOutOfMemory;
        return;
    }

    wcscpy_s((LPWSTR)m_lastErrorMessage, wcslen(pwzMessage) + 1, pwzMessage);
}

//
// Create a simple leaf tree node with the given text
//
b_tree * ConsoleArgs::MakeLeaf(LPCWSTR text)
{
    b_tree * t = NULL;
    size_t name_len = wcslen(text) + 1;
    LPWSTR szCopy = new WCHAR[name_len];

    if (!szCopy)
    {
        return NULL;
    }

    HRESULT hr;
    hr = StringCchCopyW (szCopy, name_len, text);

    t = new b_tree(szCopy);
    if (!t)
    {
        delete [] szCopy;
        return NULL;
    }
    return t;
}

//
// Free the memory allocated by the tree (recursive)
//
void ConsoleArgs::CleanupTree(b_tree *root)
{
    if (root == NULL)
        return ;
    root->InOrderWalk(FreeString);
    delete root;
}

//
// Search the binary tree and add the given string
// return true if it was added or false if it already
// exists
//
HRESULT ConsoleArgs::TreeAdd(b_tree **root, LPCWSTR add
                                )
{
    // Special case - init the tree if it
    // doesn't already exist
    if (*root == NULL)
    {
        *root = MakeLeaf(add
                        );
        return *root == NULL ? E_OUTOFMEMORY : S_OK;
    }

    size_t name_len = wcslen(add
                            ) + 1;
    LPWSTR szCopy = new WCHAR[name_len];

    if (!szCopy)
    {
        return NULL;
    }

    HRESULT hr = StringCchCopyW (szCopy, name_len, add
                                    );
    // otherwise, just let the template do the work
    hr = (*root)->Add(szCopy, _wcsicmp);

    if (hr != S_OK) // S_FALSE means it already existed
        delete [] szCopy;

    return hr;
}

//
// Parse the text into a list of argument
// return the total count
// and set 'args' to point to the last list element's 'next'
// This function assumes the text is NULL terminated
//
void ConsoleArgs::TextToArgs(LPCWSTR szText, WStrList ** listReplace)
{
    WStrList **argLast;
    const WCHAR *pCur;
    size_t iSlash;
    int iCount;

    argLast = listReplace;
    pCur = szText;
    iCount = 0;

    // Guaranteed that all tokens are no bigger than the entire file.
    LPWSTR szTemp = new WCHAR[wcslen(szText) + 1];
    if (!szTemp)
    {
        return ;
    }
    while (*pCur != '\0')
    {
        WCHAR *pPut, *pFirst, *pLast;
        WCHAR chIllegal;

LEADINGWHITE:
        while (IsWhitespace( *pCur) && *pCur != '\0')
            pCur++;

        if (*pCur == '\0')
            break;
        else if (*pCur == L'#')
        {
            while ( *pCur != '\0' && *pCur != '\n')
                pCur++; // Skip to end of line
            goto LEADINGWHITE;
        }

        // The treatment of quote marks is a bit different than the standard
        // treatment. We only remove quote marks at the very beginning and end of the
        // string. We still consider interior quotemarks for space ignoring purposes.
        // All the below are considered a single argument:
        //   "foo bar"  ->   foo bar
        //   "foo bar";"baz"  ->  "foo bar";"baz"
        //   fo"o ba"r -> fo"o ba"r
        //
        // Additionally, in order to allow multi-line arguments we allow a ^ at the
        // end of a line to specify "invisible space". A character sequence matching
        // "\^(\r\n|\r|\n)[ \t]*" will be completely ignored (whether inside a quoted
        // string or not). The below transformations occur (and represent a single
        // argument):
        //   "foo ^
        //    bar"      -> foo bar
        //   foo;^
        //   bar        -> foo;bar
        // Notes:
        //   1. Comments are not recognized in a multi-line argument
        //   2. A caret escapes only one new-line followed by an arbitrary number of
        //      tabs or blanks.
        // The following will be parsed as the names suggest, into several different
        // arguments:
        //   /option1 ^
        //      val1_1;^
        //      val1_2;^
        //      val1_3;^
        //
        //   /option2
        //   /opt^
        //       ion3     -> /option1 val1_1;val1_2;val1_3; /option2 /option3
        int cQuotes = 0;
        pPut = pFirst = szTemp;
        chIllegal = 0;
        while ((!IsWhitespace( *pCur) || !!(cQuotes & 1)) && *pCur != '\0')
        {
            switch (*pCur)
            {
                    // All this weird slash stuff follows the standard argument processing routines
                case L'\\':
                    iSlash = 0;
                    // Copy and advance while counting slashes
                    while (*pCur == L'\\')
                    {
                        *pPut++ = *pCur++;
                        iSlash++;
                    }

                    // Slashes not followed by a quote character don't matter now
                    if (*pCur != L'\"')
                        break;

                    // If there's an odd count of slashes, it's escaping the quote
                    // Otherwise the quote is a quote
                    if ((iSlash & 1) == 0)
                    {
                        ++cQuotes;
                    }
                    *pPut++ = *pCur++;
                    break;

                case L'\"':
                    ++cQuotes;
                    *pPut++ = *pCur++;
                    break;

                case L'^':
                    // ignore this sequence: \^[\r\n|\r|\n]( \t)*
                    if (pCur[1] == L'\r' || pCur[1] == L'\n')
                    {
                        if (pCur[1] == L'\r' && pCur[2] == L'\n')
                            pCur += 3;
                        else
                            pCur += 2;

                        while (*pCur == L' ' || *pCur == L'\t')
                            ++pCur;
                    }
                    else
                    {
                        *pPut++ = *pCur++;  // Copy the caret and advance
                    }
                    break;

                case L'\x01':
                case L'\x02':
                case L'\x03':
                case L'\x04':
                case L'\x05':
                case L'\x06':
                case L'\x07':
                case L'\x08':
                case L'\x09':
                case L'\x0A':
                case L'\x0B':
                case L'\x0C':
                case L'\x0D':
                case L'\x0E':
                case L'\x0F':
                case L'\x10':
                case L'\x11':
                case L'\x12':
                case L'\x13':
                case L'\x14':
                case L'\x15':
                case L'\x16':
                case L'\x17':
                case L'\x18':
                case L'\x19':
                case L'\x1A':
                case L'\x1B':
                case L'\x1C':
                case L'\x1D':
                case L'\x1E':
                case L'\x1F':
                case L'|':
                    // Save the first legal character and skip over them
                    if (chIllegal == 0)
                        chIllegal = *pCur;
                    pCur++;
                    break;

                default:
                    *pPut++ = *pCur++;  // Copy the char and advance
                    break;
            }
        }

        pLast = pPut;
        *pPut++ = '\0';

        // If the string is surrounded by quotes, with no interior quotes, remove them.
        if (cQuotes == 2 && *pFirst == L'\"' && *(pLast - 1) == L'\"')
        {
            ++pFirst;
            --pLast;
            *pLast = L'\0';
        }

        if (chIllegal != 0)
        {
            SetErrorMessage(W("Illegal option character."));
            break;
        }

        size_t cchLen = pLast - pFirst + 1;
        WCHAR * szArgCopy = new WCHAR[cchLen];
        if (!szArgCopy)
        {
            SetErrorMessage(W("Out of memory."));
            break;
        }
        if (FAILED(StringCchCopyW(szArgCopy, cchLen, pFirst)))
        {
            delete[] szArgCopy;
            SetErrorMessage(W("Out of memory."));
            break;
        }
        WStrList * listArgNew = new WStrList( szArgCopy, (*argLast));
        if (!listArgNew)
        {
            delete[] szArgCopy;
            SetErrorMessage(W("Out of memory."));
            break;
        }

        *argLast = listArgNew;
        argLast = &listArgNew->next;
    }

    delete[] szTemp;

}

//
// Pass in the command line args, argc and argv
//
// We expand any response files that may be contained in the args and return a new
// set of args, pargc2 and pppargv2 that contain the full flat command line.
//
bool ConsoleArgs::ExpandResponseFiles(_In_ int argc, _In_reads_(argc) const LPCWSTR * argv, int * pargc2, _Outptr_result_buffer_(*pargc2) LPWSTR ** pppargv2)
{
    *pargc2 = 0;
    *pppargv2 = NULL;
    WStrList **argLast = &m_listArgs;
    while (argc > 0)
    {
        // Make a copy of the original var args so we can just delete[] everything
        // once parsing is done: original args and new args from response files
        // mixed in amongst the originals.
        LPWSTR copyArg = new WCHAR[wcslen(argv[0]) + 1];
        if (!copyArg)
        {
            SetErrorMessage(W("Out of memory."));
            return false;
        }
        wcscpy_s(copyArg, wcslen(argv[0]) + 1, argv[0]);

        WStrList * listArgNew = new WStrList(copyArg, (*argLast));
        if (!listArgNew)
        {
            SetErrorMessage(W("Out of memory."));
            return false;
        }

        *argLast = listArgNew;
        argLast = &listArgNew->next;

        argc--;
        argv++;
    }

    // Process Response Files
    ProcessResponseArgs();
    if (m_errorOccurred)
        return false;

    // Now convert to an argc/argv form for remaining processing.
    int newArgc = 0;
    for (WStrList * listCurArg = m_listArgs; listCurArg != NULL; listCurArg = listCurArg->next)
    {
        if (listCurArg->arg)
            ++newArgc;
    }

    m_rgArgs = new LPWSTR[newArgc];
    if (!m_rgArgs)
    {
        SetErrorMessage(W("Out of memory."));
        return false;
    }
    int i = 0;
    for (WStrList * listCurArg = m_listArgs; listCurArg != NULL; listCurArg = listCurArg->next)
    {
        if (listCurArg->arg)
        {
            LPWSTR newString = new WCHAR[wcslen(listCurArg->arg) + 1];
            wcscpy_s(newString, wcslen(listCurArg->arg) + 1, listCurArg->arg);
            m_rgArgs[i++] = newString;
        }
    }

    *pargc2 = newArgc;
    *pppargv2 = m_rgArgs;
    return !m_errorOccurred;
}

//
// Read file to end, converting to unicode
// ppwzTextBuffer is allocated.  Caller is responsible for freeing
//
bool ConsoleArgs::ReadTextFile(LPCWSTR pwzFilename, _Outptr_ LPWSTR *ppwzTextBuffer)
{
    bool success = false;
    char *bufA = nullptr;
    WCHAR *bufW = nullptr;

    HANDLE hFile = CreateFile(pwzFilename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        SetErrorMessage(W("Cannot open response file."));
        goto ErrExit;
    }

    {
    DWORD size = GetFileSize(hFile, NULL);
    bufA = new char[size];

    if (!bufA)
    {
        SetErrorMessage(W("Out of memory"));
        goto ErrExit;
    }
    DWORD numRead = 0;
    if (!ReadFile(hFile, bufA, size, &numRead, NULL) || numRead != size)
    {
        SetErrorMessage(W("Failure reading response file."));
        goto ErrExit;
    }

    char *postByteOrderMarks = bufA;

    //
    // If there are Byte Order Marks, skip them make sure they are ones that don't
    // require us to handle the wrong endianness
    //

    byte byte0 = (byte)bufA[0];
    byte byte1 = (byte)bufA[1];
    byte byte2 = (byte)bufA[2];
    byte byte3 = (byte)bufA[3];

    bool alreadyUtf16 = false;

    if (byte0 == 0xEF && byte1 == 0xBB && byte2 == 0xBF)
    {
        postByteOrderMarks += 3;
        size -= 3;
    }
    else if (byte0 == 0xFF && byte1 == 0xFE)
    {
        postByteOrderMarks += 2;
        size -= 2;
        alreadyUtf16 = true;
    }
    else if (byte0 == 0xFE && byte1 == 0xFF)
    {
        SetErrorMessage(W("Invalid response file format.  Use little endian encoding with Unicode"));
        goto ErrExit;
    }
    else if ((byte0 == 0xFF && byte1 == 0xFE && byte2 == 0x00 && byte3 == 0x00) ||
        (byte0 == 0x00 && byte1 == 0x00 && byte2 == 0xFE && byte3 == 0xFF))
    {
        SetErrorMessage(W("Invalid response file format.  Use ANSI, UTF-8, or UTF-16"));
        goto ErrExit;
    }

    if (alreadyUtf16)
    {
        //
        // File is already formatted as UTF-16; just copy the bytes into the output buffer
        //
        int requiredSize = size + 2;  // space for 2 nullptr bytes

        // Sanity check - requiredSize better be an even number since we're dealing with UTF-16
        if (requiredSize % 2 != 0)
        {
            SetErrorMessage(W("Response file corrupt.  Expected UTF-16 encoding but we had an odd number of bytes"));
            goto ErrExit;
        }

        requiredSize /= 2;

        bufW = new WCHAR[requiredSize];
        if (!bufW)
        {
            SetErrorMessage(W("Out of memory"));
            goto ErrExit;
        }

        memcpy(bufW, postByteOrderMarks, size);
        bufW[requiredSize - 1] = L'\0';
    }
    else
    {
        //
        // File is formatted as ANSI or UTF-8 and needs converting to UTF-16
        //
        int requiredSize = MultiByteToWideChar(CP_UTF8, 0, postByteOrderMarks, size, nullptr, 0);
        bufW = new WCHAR[requiredSize + 1];
        if (!bufW)
        {
            SetErrorMessage(W("Out of memory"));
            goto ErrExit;
        }

        if (!MultiByteToWideChar(CP_UTF8, 0, postByteOrderMarks, size, bufW, requiredSize))
        {
            SetErrorMessage(W("Failure reading response file."));
            goto ErrExit;
        }

        bufW[requiredSize] = L'\0';
    }

    *ppwzTextBuffer = bufW;

    success = true;
    }

ErrExit:
    if (bufA)
    {
        delete[] bufA;
    }
    CloseHandle(hFile);
    return success;
}

/*
 * Process Response files on the command line
 */
void ConsoleArgs::ProcessResponseArgs()
{
    HRESULT hr;
    b_tree *response_files = NULL;

    WCHAR szFilename[MAX_LONGPATH];

    for (WStrList * listCurArg = m_listArgs;
         listCurArg != NULL && !m_errorOccurred;
         listCurArg = listCurArg->next)
    {
        WCHAR * szArg = listCurArg->arg;

        // Skip everything except Response files
        if (szArg == NULL || szArg[0] != '@')
            continue;

        if (wcslen(szArg) == 1)
        {
            SetErrorMessage(W("No response file specified"));
            goto CONTINUE;
        }

        // Check for duplicates
        if (!GetFullFileName(&szArg[1], szFilename, MAX_LONGPATH, false))
            continue;


        hr = TreeAdd(&response_files, szFilename);
        if (hr == E_OUTOFMEMORY)
        {
            SetErrorMessage(W("Out of memory."));
            goto CONTINUE;
        }
        else if (hr == S_FALSE)
        {
            SetErrorMessage(W("Duplicate response file."));
            goto CONTINUE;
        }

        {
        LPWSTR pwzFileBuffer;
        pwzFileBuffer = nullptr;
        if (!ReadTextFile(szFilename, &pwzFileBuffer))
        {
            goto CONTINUE;
        }

        LPWSTR szActualText = nullptr;
#ifdef TARGET_UNIX
        szActualText = pwzFileBuffer;
#else
        DWORD dwNumChars = ExpandEnvironmentStrings(pwzFileBuffer, NULL, 0);
        LPWSTR szExpandedBuffer = new WCHAR[dwNumChars];
        if (szExpandedBuffer != nullptr)
        {
            DWORD dwRetVal = ExpandEnvironmentStrings(pwzFileBuffer, szExpandedBuffer, dwNumChars);

            if (dwRetVal != 0)
            {
                szActualText = szExpandedBuffer;
            }
            else
            {
                // Expand failed

            }
        }
#endif

        TextToArgs(szActualText, &listCurArg->next);

        delete[] pwzFileBuffer;
#ifndef TARGET_UNIX
        delete[] szExpandedBuffer;
#endif
        }

CONTINUE:  // remove the response file argument, and continue to the next.
        listCurArg->arg = NULL;
    }

    CleanupTree(response_files);
}

