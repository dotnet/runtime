//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifndef __CONSOLEARGS_H__
#define __CONSOLEARGS_H__

#include "list.h"
#include "tree.h"
#include <strsafe.h>

typedef tree<LPCWSTR> b_tree;
typedef list<WCHAR*> WStrList;

const LPWSTR kOutOfMemory = L"Out of memory";

class ConsoleArgs
{
public:
    // Place the fully-qualified filename in the given output buffer
    bool GetFullFileName(LPCWSTR szSource, __deref_out_ecount(cbFilenameBuffer) LPWSTR filenameBuffer, DWORD cbFilenameBuffer, bool fOutputFilename);

    ConsoleArgs() :
            m_rgArgs(NULL),
            m_listArgs(NULL),
            m_errorOccured(false),
            m_lastErrorMessage(nullptr)
    {
    };

    ~ConsoleArgs()
    {
        CleanUpArgs();
    };

    // returns false if there are errors
    bool ExpandResponseFiles(__in int argc, __deref_in_ecount(argc) const LPCWSTR * argv, __deref_out int * pargc2, __out LPWSTR ** pppargv2);

    // Frees all memory used by the arg list and the argv/argc array
    void CleanUpArgs();

    LPWSTR ErrorMessage()
    {
        if (m_errorOccured)
        {
            return m_lastErrorMessage;
        }
        else
        {
            return nullptr;
        }
    }

private:
    void SetErrorMessage(__deref_in LPCWSTR pwzMessage);
    b_tree * MakeLeaf( LPCWSTR szText);
    void CleanupTree( b_tree * root);
    HRESULT TreeAdd( b_tree ** root, LPCWSTR szAdd);
    void TextToArgs( LPCWSTR szText, WStrList ** listReplace);
    bool ReadTextFile(LPCWSTR pwzFilename, __deref_out LPWSTR *ppwzTextBuffer);
    void ProcessResponseArgs();

    LPWSTR * m_rgArgs;
    WStrList * m_listArgs;

    bool m_errorOccured;
    LPWSTR m_lastErrorMessage;
};

#endif // __CONSOLEARGS_H__
