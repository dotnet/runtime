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
#ifndef __StgTiggerStream_h__
#define __StgTiggerStream_h__



#include "stgtiggerstorage.h"			// Data definitions.

enum
{
	STREAM_DATA_NAME
};


class TiggerStorage;


class TiggerStream :
	public IStream
{
public:
	TiggerStream() :
		m_pStorage(0),
		m_cRef(1)
	{}

    virtual ~TiggerStream() {}

	virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *pp)
	{ return (BadError(E_NOTIMPL)); }
	virtual ULONG STDMETHODCALLTYPE AddRef()
	{ return InterlockedIncrement(&m_cRef); }
	virtual ULONG STDMETHODCALLTYPE Release()
	{
		ULONG	cRef;
		if ((cRef = InterlockedDecrement(&m_cRef)) == 0)
			delete this;
		return (cRef);
	}

// IStream
    virtual HRESULT STDMETHODCALLTYPE Read(
        void		*pv,
        ULONG		cb,
        ULONG		*pcbRead);

    virtual HRESULT STDMETHODCALLTYPE Write(
        const void	*pv,
        ULONG		cb,
        ULONG		*pcbWritten);

    virtual HRESULT STDMETHODCALLTYPE Seek(
        LARGE_INTEGER dlibMove,
        DWORD		dwOrigin,
        ULARGE_INTEGER *plibNewPosition);

    virtual HRESULT STDMETHODCALLTYPE SetSize(
        ULARGE_INTEGER libNewSize);

    virtual HRESULT STDMETHODCALLTYPE CopyTo(
        IStream		*pstm,
        ULARGE_INTEGER cb,
        ULARGE_INTEGER *pcbRead,
        ULARGE_INTEGER *pcbWritten);

    virtual HRESULT STDMETHODCALLTYPE Commit(
        DWORD		grfCommitFlags);

    virtual HRESULT STDMETHODCALLTYPE Revert( void);

    virtual HRESULT STDMETHODCALLTYPE LockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD		dwLockType);

    virtual HRESULT STDMETHODCALLTYPE UnlockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD		dwLockType);

    virtual HRESULT STDMETHODCALLTYPE Stat(
        STATSTG		*pstatstg,
        DWORD		grfStatFlag);

    virtual HRESULT STDMETHODCALLTYPE Clone(
        IStream		**ppstm);


	HRESULT Init(							// Return code.
		TiggerStorage *pStorage,			// Parent storage.
		LPCSTR		szStream);				// Stream name.

	ULONG GetStreamSize();

private:
	TiggerStorage	*m_pStorage;		// Our parent storage.
	char			m_rcStream[MAXSTREAMNAME]; // Name of the stream.
	LONG			m_cRef;				// Ref count.
};

#endif // __StgTiggerStream_h__
