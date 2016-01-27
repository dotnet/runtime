// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "stdafx.h"
#include <stdlib.h>
#include "utilcode.h"
#include "strongname.h"
#include "assemblyfilehash.h"
#include "ex.h"
#include "corperm.h"

#include <wincrypt.h>

HRESULT AssemblyFileHash::ReadData()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
#ifdef MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif          
    }
    CONTRACTL_END;
    NewArrayHolder<BYTE> pBuffer;
    
    DWORD dwFileSize = SafeGetFileSize( m_hFile, NULL );

    if (dwFileSize == 0xffffffff)
        return HRESULT_FROM_GetLastError();

    pBuffer = new (nothrow)BYTE[dwFileSize];
    if(pBuffer==NULL)
        return E_OUTOFMEMORY;
    
    DWORD cbBuffer = dwFileSize;
    DWORD cbRead;

    if (!ReadFile( m_hFile, pBuffer, cbBuffer, &cbRead, NULL ) ||
        cbRead != cbBuffer)
        return HRESULT_FROM_GetLastError();

    pBuffer.SuppressRelease();
    this->m_pbData = pBuffer;
    this->m_cbData = cbBuffer;
    this->m_bDataOwner = TRUE;
    this->m_NeedToReadData=FALSE;

    return S_OK;
    
}

HRESULT AssemblyFileHash::SetFileName(LPCWSTR wszFileName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
#ifdef MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif          
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    HandleHolder hFile;

    hFile = WszCreateFile(wszFileName,
                          GENERIC_READ,
                          FILE_SHARE_READ,
                          NULL,
                          OPEN_EXISTING,
                          0,
                          NULL);

    if (hFile == INVALID_HANDLE_VALUE)
        return HRESULT_FROM_GetLastError();

    IfFailRet(SetFileHandle(hFile));
    hFile.SuppressRelease();
    return S_OK;
}


HRESULT AssemblyFileHash::HashData(HCRYPTHASH hHash)
{
    WRAPPER_NO_CONTRACT;
    if(!CryptHashData(hHash, m_pbData, m_cbData, 0))
        return HRESULT_FROM_GetLastError();
    return S_OK;
}

HRESULT AssemblyFileHash::CalculateHash(DWORD algid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if(m_NeedToReadData)
        IfFailRet(ReadData());

    _ASSERTE(!m_NeedToReadData);
    
    HCRYPTPROV pProvider = NULL;
    HCRYPTHASH hHash = NULL;
    DWORD count;

    if(!WszCryptAcquireContext(&pProvider,
                               NULL,
                               NULL,
                               //PROV_RSA_SIG,
                               PROV_RSA_FULL,
                               CRYPT_VERIFYCONTEXT))
        IfFailGo(HRESULT_FROM_GetLastError());

    
    if(!CryptCreateHash(pProvider,
                        algid,
                        0,
                        0,
                        &hHash))
        IfFailGo(HRESULT_FROM_GetLastError());

    IfFailGo(HashData(hHash));

    count = sizeof(m_cbHash);
    if(!CryptGetHashParam(hHash, 
                          HP_HASHSIZE,
                          (PBYTE) &m_cbHash,
                          &count,
                          0))
        IfFailGo(HRESULT_FROM_GetLastError());
        
    if(m_cbHash > 0) {
        m_pbHash = new (nothrow) BYTE[m_cbHash];
        if (!m_pbHash)
            IfFailGo(E_OUTOFMEMORY);

        if(!CryptGetHashParam(hHash, 
                              HP_HASHVAL,
                              m_pbHash,
                              &m_cbHash,
                              0))
            IfFailGo(HRESULT_FROM_GetLastError());
    }

 ErrExit:

    if(hHash) 
        CryptDestroyHash(hHash);
    if(pProvider)
        CryptReleaseContext(pProvider, 0);
    return hr;
}




