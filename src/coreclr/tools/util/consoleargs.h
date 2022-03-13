// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CONSOLEARGS_H__
#define __CONSOLEARGS_H__

#include "list.h"
#include "tree.h"
#include <strsafe.h>

#include "palclr.h"

typedef tree<LPCWSTR> b_tree;
typedef list<WCHAR*> WStrList;

const LPCWSTR kOutOfMemory = W("Out of memory");

class ConsoleArgs
{
public:
    // Place the fully-qualified filename in the given output buffer
    bool GetFullFileName(LPCWSTR szSource, _Out_writes_(cbFilenameBuffer) LPWSTR filenameBuffer, DWORD cbFilenameBuffer, bool fOutputFilename);

    ConsoleArgs() :
            m_rgArgs(NULL),
            m_listArgs(NULL),
            m_errorOccurred(false),
            m_lastErrorMessage(nullptr)
    {
    };

    ~ConsoleArgs()
    {
        CleanUpArgs();
    };

    // returns false if there are errors
    bool ExpandResponseFiles(_In_ int argc, _In_reads_(argc) const LPCWSTR * argv, int * pargc2, _Outptr_result_buffer_(*pargc2) LPWSTR ** pppargv2);

    // Frees all memory used by the arg list and the argv/argc array
    void CleanUpArgs();

    LPCWSTR ErrorMessage()
    {
        if (m_errorOccurred)
        {
            return m_lastErrorMessage;
        }
        else
        {
            return nullptr;
        }
    }

private:
    void SetErrorMessage(_In_ LPCWSTR pwzMessage);
    b_tree * MakeLeaf( LPCWSTR szText);
    void CleanupTree( b_tree * root);
    HRESULT TreeAdd( b_tree ** root, LPCWSTR szAdd);
    void TextToArgs( LPCWSTR szText, WStrList ** listReplace);
    bool ReadTextFile(LPCWSTR pwzFilename, _Outptr_ LPWSTR *ppwzTextBuffer);
    void ProcessResponseArgs();

    LPWSTR * m_rgArgs;
    WStrList * m_listArgs;

    bool m_errorOccurred;
    LPCWSTR m_lastErrorMessage;
};

#endif // __CONSOLEARGS_H__
