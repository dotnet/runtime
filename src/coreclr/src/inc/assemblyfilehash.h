// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef __ASSEMBLYHASH_H__
#define __ASSEMBLYHASH_H__

#include "wincrypt.h"
#include "ex.h"



class AssemblyFileHash
{
protected:
    BOOL m_bDataOwner;
    PBYTE m_pbData;
    DWORD m_cbData;
    PBYTE m_pbHash;
    DWORD m_cbHash;
    HRESULT HashData(HCRYPTHASH);
    HANDLE m_hFile;
    BOOL    m_NeedToReadData;

    HRESULT ReadData();
public:

    HRESULT SetFileName(LPCWSTR wszFileName);
    HRESULT ReleaseFileHandle()
    {
        WRAPPER_NO_CONTRACT;
        return SetFileHandle(INVALID_HANDLE_VALUE);
    };
    HRESULT SetFileHandle(HANDLE hFile)
    {
        LIMITED_METHOD_CONTRACT;
        m_hFile=hFile;
        return S_OK;
    };

    HRESULT SetData(PBYTE pbData, DWORD cbData) // Owned by owners context. Will not be deleted
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        m_pbData = pbData;
        m_cbData = cbData;
        m_bDataOwner = FALSE;
        m_NeedToReadData=FALSE;
        return S_OK;
    }

    PBYTE GetHash() { LIMITED_METHOD_CONTRACT; _ASSERTE(!m_NeedToReadData);return m_pbHash; }
    DWORD GetHashSize() { LIMITED_METHOD_CONTRACT;_ASSERTE(!m_NeedToReadData); return m_cbHash; }

    HRESULT CalculateHash(DWORD algid);

    AssemblyFileHash()
        : m_pbData( NULL ),
          m_cbData( 0 ),
          m_pbHash( NULL ),
          m_cbHash( 0 ),
          m_hFile(INVALID_HANDLE_VALUE),
          m_NeedToReadData(TRUE)
    {
        CONTRACTL
        {
            CONSTRUCTOR_CHECK;
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;
    }

    ~AssemblyFileHash()
    {
        delete [] m_pbHash;
        if (m_bDataOwner)
            delete [] m_pbData;
        if (m_hFile!=INVALID_HANDLE_VALUE)
            CloseHandle(m_hFile);
        m_hFile=INVALID_HANDLE_VALUE;
    }
};

#endif
