// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgTiggerStream.h
//

//
// TiggerStream is the companion to the TiggerStorage CoClass.  It handles the
// streams managed inside of the storage and does the direct file i/o.
//
//*****************************************************************************
#include "stdafx.h"
#include "stgtiggerstream.h"
#include "stgtiggerstorage.h"
#include "posterror.h"

//
//
// IStream
//
//


HRESULT STDMETHODCALLTYPE TiggerStream::Read(
    void		*pv,
    ULONG		cb,
    ULONG		*pcbRead)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::Write(
    const void	*pv,
    ULONG		cb,
    ULONG		*pcbWritten)
{
	return (m_pStorage->Write(m_rcStream, pv, cb, pcbWritten));
}


HRESULT STDMETHODCALLTYPE TiggerStream::Seek(
    LARGE_INTEGER dlibMove,
    DWORD		dwOrigin,
    ULARGE_INTEGER *plibNewPosition)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::SetSize(
    ULARGE_INTEGER libNewSize)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::CopyTo(
    IStream		*pstm,
    ULARGE_INTEGER cb,
    ULARGE_INTEGER *pcbRead,
    ULARGE_INTEGER *pcbWritten)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::Commit(
    DWORD		grfCommitFlags)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::Revert()
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::LockRegion(
    ULARGE_INTEGER libOffset,
    ULARGE_INTEGER cb,
    DWORD		dwLockType)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::UnlockRegion(
    ULARGE_INTEGER libOffset,
    ULARGE_INTEGER cb,
    DWORD		dwLockType)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::Stat(
    STATSTG		*pstatstg,
    DWORD		grfStatFlag)
{
	return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStream::Clone(
    IStream		**ppstm)
{
	return (E_NOTIMPL);
}






HRESULT TiggerStream::Init(				// Return code.
	TiggerStorage *pStorage,			// Parent storage.
	LPCSTR		szStream)				// Stream name.
{
	// Save off the parent data source object and stream name.
	m_pStorage = pStorage;
	strncpy_s(m_rcStream, sizeof(m_rcStream), szStream, sizeof(m_rcStream)-1);
    m_rcStream[sizeof(m_rcStream)-1] = '\0'; // force nul termination
	return (S_OK);
}


ULONG TiggerStream::GetStreamSize()
{
    PSTORAGESTREAM pStreamInfo;
    if (FAILED(m_pStorage->FindStream(m_rcStream, &pStreamInfo)))
        return 0;
    return (pStreamInfo->GetSize());
}
