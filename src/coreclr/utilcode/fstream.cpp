// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "stdafx.h"                     // Precompiled header key.
#include "fstream.h"
#include <dn-stdio.h>

CFileStream::CFileStream()
: _cRef(1)
, _fp(NULL)
{
}

CFileStream::~CFileStream()
{
    Close();
}

HRESULT CFileStream::OpenForRead(LPCWSTR wzFilePath)
{
    HRESULT         hr = S_OK;

    int err = fopen_u16(&_fp, wzFilePath, W("rb"));
    if (err != 0)
    {
        hr = HRESULT_FROM_LAST_STDIO();
    }

    return hr;
}

HRESULT CFileStream::OpenForWrite(LPCWSTR wzFilePath)
{
    HRESULT         hr = S_OK;
    
    int err = fopen_u16(&_fp, wzFilePath, W("wb"));
    if (err != 0)
    {
        hr = HRESULT_FROM_LAST_STDIO();
    }

    return hr;
}

HRESULT CFileStream::QueryInterface(REFIID riid, void **ppv)
{
    HRESULT                                    hr = S_OK;

    if (!ppv)
        return E_POINTER;

    *ppv = NULL;

    if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IStream)) {
        *ppv = static_cast<IStream *>(this);
    }
    else {
        hr = E_NOINTERFACE;
    }

    if (*ppv) {
        AddRef();
    }

    return hr;
}

STDMETHODIMP_(ULONG) CFileStream::AddRef()
{
    return InterlockedIncrement(&_cRef);
}

STDMETHODIMP_(ULONG) CFileStream::Release()
{
    ULONG                    ulRef = InterlockedDecrement(&_cRef);

    if (!ulRef) {
        delete this;
    }

    return ulRef;
}

HRESULT CFileStream::Read(void *pv, ULONG cb, ULONG *pcbRead)
{
    HRESULT                                   hr = S_OK;
    ULONG                                     cbRead = 0;

    if (pcbRead != NULL) {
        *pcbRead = 0;
    }

    _ASSERTE(_fp != NULL);
    if (_fp == NULL) {
        hr = E_UNEXPECTED;
        goto Exit;
    }

    cbRead = (ULONG)fread(pv, 1, cb, _fp);

    if (cbRead <= 0) {
        hr = HRESULT_FROM_LAST_STDIO();
        goto Exit;
    }

    if (cbRead == 0) {
        hr = S_FALSE;
    }
    else {
        hr = NOERROR;
    }

    if (pcbRead != NULL) {
        *pcbRead = cbRead;
    }

Exit:
    return hr;
}

HRESULT CFileStream::Write(void const *pv, ULONG cb, ULONG *pcbWritten)
{
    HRESULT                              hr = S_OK;
    ULONG                                cbWritten = 0;

    if (pcbWritten != NULL) {
        *pcbWritten = 0;
    }

    _ASSERTE(_fp != NULL);
    if (_fp == NULL) {
        hr = E_UNEXPECTED;
        goto Exit;
    }

    cbWritten = (ULONG)fwrite(pv, 1, cb, _fp);

    if (cbWritten <= 0) {
        hr = HRESULT_FROM_LAST_STDIO();
        goto Exit;
    }

    if (cbWritten == 0) {
        hr = S_FALSE;
    }
    else {
        hr = S_OK;
    }

    if (pcbWritten != NULL) {
        *pcbWritten = cbWritten;
    }

Exit:
    return hr;
}

HRESULT CFileStream::Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::SetSize(ULARGE_INTEGER libNewSize)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::CopyTo(IStream *pstm, ULARGE_INTEGER cb, ULARGE_INTEGER *pcbRead, ULARGE_INTEGER *pcbWritten)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::Commit(DWORD grfCommitFlags)
{
    HRESULT                                 hr = S_OK;

    if (grfCommitFlags != 0)  {
        hr = E_INVALIDARG;
        goto Exit;
    }

    if (!Close()) {
        hr = HRESULT_FROM_WIN32(GetLastError());
    }

Exit:
    return hr;
}

HRESULT CFileStream::Revert()
{
    return E_NOTIMPL;
}

HRESULT CFileStream::LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::Stat(STATSTG *pstatstg, DWORD grfStatFlag)
{
    return E_NOTIMPL;
}

HRESULT CFileStream::Clone(IStream **ppIStream)
{
    return E_NOTIMPL;
}


BOOL CFileStream::Close()
{
    BOOL                            fSuccess = FALSE;

    if (_fp != NULL) {
        if (fclose(_fp) == 0) {
            _fp = NULL;
            goto Exit;
        }

        _fp = NULL;
    }

    fSuccess = TRUE;

Exit:
    return fSuccess;
}

