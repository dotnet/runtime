// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// memorystreams.cpp
//
// IStream implementations for in-memory streams
//
//*****************************************************************************

#include "stdafx.h"
#include "memorystreams.h"
#include "utilcode.h"
#include "posterror.h"

//
// CInMemoryStream
//

ULONG
STDMETHODCALLTYPE CInMemoryStream::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (cRef == 0)
    {
        if (m_dataCopy != NULL)
            delete [] m_dataCopy;

        delete this;
    }
    return (cRef);
} // CInMemoryStream::Release

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::QueryInterface(REFIID riid, PVOID *ppOut)
{
    if (!ppOut)
    {
        return E_POINTER;
    }

    *ppOut = NULL;
    if (riid == IID_IStream || riid == IID_ISequentialStream || riid == IID_IUnknown)
    {
        *ppOut = this;
        AddRef();
        return (S_OK);
    }

    return E_NOINTERFACE;

} // CInMemoryStream::QueryInterface

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Read(
    void  *pv,
    ULONG  cb,
    ULONG *pcbRead)
{
    ULONG       cbRead = min(cb, m_cbSize - m_cbCurrent);

    if (cbRead == 0)
        return (S_FALSE);
    memcpy(pv, (void *) ((ULONG_PTR) m_pMem + m_cbCurrent), cbRead);
    if (pcbRead)
        *pcbRead = cbRead;
    m_cbCurrent += cbRead;
    return (S_OK);
} // CInMemoryStream::Read

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Write(
    const void *pv,
    ULONG       cb,
    ULONG      *pcbWritten)
{
    if (ovadd_gt(m_cbCurrent, cb, m_cbSize))
        return (PostError(OutOfMemory()));

    memcpy((BYTE *) m_pMem + m_cbCurrent, pv, cb);
    m_cbCurrent += cb;
    if (pcbWritten) *pcbWritten = cb;
    return (S_OK);
} // CInMemoryStream::Write

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Seek(
    LARGE_INTEGER   dlibMove,
    DWORD           dwOrigin,
    ULARGE_INTEGER *plibNewPosition)
{
    _ASSERTE(dwOrigin == STREAM_SEEK_SET || dwOrigin == STREAM_SEEK_CUR);
    _ASSERTE(dlibMove.QuadPart <= static_cast<LONGLONG>(UINT32_MAX));

    if (dwOrigin == STREAM_SEEK_SET)
    {
        m_cbCurrent = (ULONG) dlibMove.QuadPart;
    }
    else
    if (dwOrigin == STREAM_SEEK_CUR)
    {
        m_cbCurrent+= (ULONG)dlibMove.QuadPart;
    }

    if (plibNewPosition)
    {
            plibNewPosition->QuadPart = m_cbCurrent;
    }

    return (m_cbCurrent < m_cbSize) ? (S_OK) : E_FAIL;
} // CInMemoryStream::Seek

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::CopyTo(
    IStream        *pstm,
    ULARGE_INTEGER  cb,
    ULARGE_INTEGER *pcbRead,
    ULARGE_INTEGER *pcbWritten)
{
    HRESULT     hr;
    // We don't handle pcbRead or pcbWritten.
    _ASSERTE(pcbRead == 0);
    _ASSERTE(pcbWritten == 0);

    _ASSERTE(cb.QuadPart <= UINT32_MAX);
    ULONG       cbTotal = min(static_cast<ULONG>(cb.QuadPart), m_cbSize - m_cbCurrent);
    ULONG       cbRead=min((ULONG)1024, cbTotal);
    CQuickBytes rBuf;
    void        *pBuf = rBuf.AllocNoThrow(cbRead);
    if (pBuf == 0)
        return (PostError(OutOfMemory()));

    while (cbTotal)
        {
            if (cbRead > cbTotal)
                cbRead = cbTotal;
            if (FAILED(hr=Read(pBuf, cbRead, 0)))
                return (hr);
            if (FAILED(hr=pstm->Write(pBuf, cbRead, 0)))
                return (hr);
            cbTotal -= cbRead;
        }

    // Adjust seek pointer to the end.
    m_cbCurrent = m_cbSize;

    return (S_OK);
} // CInMemoryStream::CopyTo

HRESULT
CInMemoryStream::CreateStreamOnMemory(
    void     *pMem,                     // Memory to create stream on.
    ULONG     cbSize,                   // Size of data.
    IStream **ppIStream,                // Return stream object here.
    BOOL      fDeleteMemoryOnRelease)
{
    CInMemoryStream *pIStream;          // New stream object.
    if ((pIStream = new (nothrow) CInMemoryStream) == 0)
        return (PostError(OutOfMemory()));
    pIStream->InitNew(pMem, cbSize);
    if (fDeleteMemoryOnRelease)
    {
        // make sure this memory is allocated using new
        pIStream->m_dataCopy = (BYTE *)pMem;
    }
    *ppIStream = pIStream;
    return (S_OK);
} // CInMemoryStream::CreateStreamOnMemory

HRESULT
CInMemoryStream::CreateStreamOnMemoryCopy(
    void     *pMem,
    ULONG     cbSize,
    IStream **ppIStream)
{
    CInMemoryStream *pIStream;          // New stream object.
    if ((pIStream = new (nothrow) CInMemoryStream) == 0)
        return (PostError(OutOfMemory()));

    // Init the stream.
    pIStream->m_cbCurrent = 0;
    pIStream->m_cbSize = cbSize;

    // Copy the data.
    pIStream->m_dataCopy = new (nothrow) BYTE[cbSize];

    if (pIStream->m_dataCopy == NULL)
    {
        delete pIStream;
        return (PostError(OutOfMemory()));
    }

    pIStream->m_pMem = pIStream->m_dataCopy;
    memcpy(pIStream->m_dataCopy, pMem, cbSize);

    *ppIStream = pIStream;
    return (S_OK);
} // CInMemoryStream::CreateStreamOnMemoryCopy

//---------------------------------------------------------------------------
// CGrowableStream is a simple IStream implementation that grows as
// its written to. All the memory is contiguous, so read access is
// fast. A grow does a realloc, so be aware of that if you're going to
// use this.
//---------------------------------------------------------------------------

//Constructs a new GrowableStream
// multiplicativeGrowthRate - when the stream grows it will be at least this
//   multiple of its old size. Values greater than 1 ensure O(N) amortized
//   performance growing the stream to size N, 1 ensures O(N^2) amortized perf
//   but gives the tightest memory usage. Valid range is [1.0, 2.0].
// additiveGrowthRate - when the stream grows it will increase in size by at least
//   this number of bytes. Larger numbers cause fewer re-allocations at the cost of
//   increased memory usage.
CGrowableStream::CGrowableStream(float multiplicativeGrowthRate, DWORD additiveGrowthRate)
{
    m_swBuffer = NULL;
    m_dwBufferSize = 0;
    m_dwBufferIndex = 0;
    m_dwStreamLength = 0;
    m_cRef = 1;

    // Lets make sure these values stay somewhat sane... if you adjust the limits
    // make sure you also write correct overflow checking code in EnsureCapcity
    _ASSERTE(multiplicativeGrowthRate >= 1.0F && multiplicativeGrowthRate <= 2.0F);
    m_multiplicativeGrowthRate = min(max(1.0F, multiplicativeGrowthRate), 2.0F);

    _ASSERTE(additiveGrowthRate >= 1);
    m_additiveGrowthRate = max((DWORD)1, additiveGrowthRate);
} // CGrowableStream::CGrowableStream

#ifndef DACCESS_COMPILE

CGrowableStream::~CGrowableStream()
{
    // Destroy the buffer.
    if (m_swBuffer != NULL)
        delete [] m_swBuffer;

    m_swBuffer = NULL;
    m_dwBufferSize = 0;
} // CGrowableStream::~CGrowableStream

// Grows the stream and optionally the internal buffer to ensure it is at least
// newLogicalSize
HRESULT CGrowableStream::EnsureCapacity(DWORD newLogicalSize)
{
    _ASSERTE(m_dwBufferSize >= m_dwStreamLength);

    // If there is no enough space left in the buffer, grow it
    if (newLogicalSize > m_dwBufferSize)
    {
        // Grow to max of newLogicalSize, m_dwBufferSize*multiplicativeGrowthRate, and
        // m_dwBufferSize+m_additiveGrowthRate
        S_UINT32 addSize = S_UINT32(m_dwBufferSize) + S_UINT32(m_additiveGrowthRate);
        if (addSize.IsOverflow())
        {
            addSize = S_UINT32(UINT_MAX);
        }

        // this should have been enforced in the constructor too
        _ASSERTE(m_multiplicativeGrowthRate <= 2.0 && m_multiplicativeGrowthRate >= 1.0);

        // 2*UINT_MAX doesn't overflow a float so this certain to be safe
        float multSizeF = (float)m_dwBufferSize * m_multiplicativeGrowthRate;
        DWORD multSize;
        if(multSizeF > (float)UINT_MAX)
        {
            multSize = UINT_MAX;
        }
        else
        {
            multSize = (DWORD)multSizeF;
        }

        DWORD newBufferSize = max(max(newLogicalSize, multSize), (DWORD)addSize.Value());

        char *tmp = new (nothrow) char[newBufferSize];
        if(tmp == NULL)
        {
            return E_OUTOFMEMORY;
        }

        if (m_swBuffer) {
            memcpy (tmp, m_swBuffer, m_dwBufferSize);
            delete [] m_swBuffer;
        }
        m_swBuffer = (BYTE *)tmp;
        m_dwBufferSize = newBufferSize;
    }

    _ASSERTE(m_dwBufferSize >= newLogicalSize);
    // the internal buffer is big enough, might have to increase logical size
    // though
    if(newLogicalSize > m_dwStreamLength)
    {
        m_dwStreamLength = newLogicalSize;
    }

    _ASSERTE(m_dwBufferSize >= m_dwStreamLength);
    return S_OK;
}

ULONG
STDMETHODCALLTYPE
CGrowableStream::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
        delete this;

    return cRef;
} // CGrowableStream::Release

HRESULT
STDMETHODCALLTYPE
CGrowableStream::QueryInterface(
    REFIID riid,
    PVOID *ppOut)
{
    if (riid != IID_IUnknown && riid!=IID_ISequentialStream && riid!=IID_IStream)
        return E_NOINTERFACE;

    *ppOut = this;
    AddRef();
    return (S_OK);
} // CGrowableStream::QueryInterface

HRESULT
CGrowableStream::Read(
    void  *pv,
    ULONG  cb,
    ULONG *pcbRead)
{
    HRESULT hr = S_OK;
    DWORD dwCanReadBytes = 0;

    if (NULL == pv)
        return E_POINTER;

    // short-circuit a zero-length read or see if we are at the end
    if (cb == 0 || m_dwBufferIndex >= m_dwStreamLength)
    {
        if (pcbRead != NULL)
            *pcbRead = 0;

        return S_OK;
    }

    // Figure out if we have enough room in the stream (excluding any
    // unused space at the end of the buffer)
    dwCanReadBytes = cb;

    S_UINT32 dwNewIndex = S_UINT32(dwCanReadBytes) + S_UINT32(m_dwBufferIndex);
    if (dwNewIndex.IsOverflow() || (dwNewIndex.Value() > m_dwStreamLength))
    {
        // Only read whatever is left in the buffer (if any)
        dwCanReadBytes = (m_dwStreamLength - m_dwBufferIndex);
    }

    // copy from our buffer to caller's buffer
    memcpy(pv, &m_swBuffer[m_dwBufferIndex], dwCanReadBytes);

    // adjust our current position
    m_dwBufferIndex += dwCanReadBytes;

    // if they want the info, tell them how many byte we read for them
    if (pcbRead != NULL)
        *pcbRead = dwCanReadBytes;

    return hr;
} // CGrowableStream::Read

HRESULT
CGrowableStream::Write(
    const void *pv,
    ULONG       cb,
    ULONG      *pcbWritten)
{
    HRESULT hr = S_OK;
    DWORD dwActualWrite = 0;

    // avoid NULL write
    if (cb == 0)
    {
        hr = S_OK;
        goto Error;
    }

    // Check if our buffer is large enough
    _ASSERTE(m_dwBufferIndex <= m_dwStreamLength);
    _ASSERTE(m_dwStreamLength <= m_dwBufferSize);

    // If there is no enough space left in the buffer, grow it
    if (cb > (m_dwStreamLength - m_dwBufferIndex))
    {
        // Determine the new size needed
        S_UINT32 size = S_UINT32(m_dwBufferSize) + S_UINT32(cb);
        if (size.IsOverflow())
        {
            hr = HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
            goto Error;
        }

        hr = EnsureCapacity(size.Value());
        if(FAILED(hr))
        {
            goto Error;
        }
    }

    if ((pv != NULL) && (cb > 0))
    {
        // write to current position in the buffer
        memcpy(&m_swBuffer[m_dwBufferIndex], pv, cb);

        // now update our current index
        m_dwBufferIndex += cb;

        // in case they want to know the number of bytes written
        dwActualWrite = cb;
    }

Error:
    if (pcbWritten)
        *pcbWritten = dwActualWrite;

    return hr;
} // CGrowableStream::Write

STDMETHODIMP
CGrowableStream::Seek(
    LARGE_INTEGER   dlibMove,
    DWORD           dwOrigin,
    ULARGE_INTEGER *plibNewPosition)
{
    // a Seek() call on STREAM_SEEK_CUR and a dlibMove == 0 is a
    // request to get the current seek position.
    if ((dwOrigin == STREAM_SEEK_CUR && dlibMove.u.LowPart == 0) &&
        (dlibMove.u.HighPart == 0) &&
        (NULL != plibNewPosition))
    {
        goto Error;
    }

    // we only support STREAM_SEEK_SET (beginning of buffer)
    if (dwOrigin != STREAM_SEEK_SET)
        return E_NOTIMPL;

    // did they ask to seek past end of stream?  If so we're supposed to
    // extend with zeros.  But we've never supported that.
    if (dlibMove.u.LowPart > m_dwStreamLength)
        return E_UNEXPECTED;

    // we ignore the high part of the large integer
    SIMPLIFYING_ASSUMPTION(dlibMove.u.HighPart == 0);
    m_dwBufferIndex = dlibMove.u.LowPart;

Error:
    if (NULL != plibNewPosition)
    {
        plibNewPosition->u.HighPart = 0;
        plibNewPosition->u.LowPart = m_dwBufferIndex;
    }

    return S_OK;
} // CGrowableStream::Seek

STDMETHODIMP
CGrowableStream::SetSize(
    ULARGE_INTEGER libNewSize)
{
    DWORD dwNewSize = libNewSize.u.LowPart;

    _ASSERTE(libNewSize.u.HighPart == 0);

    // we don't support large allocations
    if (libNewSize.u.HighPart > 0)
        return E_OUTOFMEMORY;

    HRESULT hr = EnsureCapacity(dwNewSize);
    if(FAILED(hr))
    {
        return hr;
    }

    // EnsureCapacity doesn't shrink the logicalSize if dwNewSize is smaller
    // and SetSize is allowed to shrink the stream too. Note that we won't
    // release physical memory here, we just appear to get smaller
    m_dwStreamLength = dwNewSize;

    return S_OK;
} // CGrowableStream::SetSize

STDMETHODIMP
CGrowableStream::Stat(
    STATSTG *pstatstg,
    DWORD    grfStatFlag)
{
    if (NULL == pstatstg)
        return E_POINTER;

    // this is the only useful information we hand out - the length of the stream
    pstatstg->cbSize.u.HighPart = 0;
    pstatstg->cbSize.u.LowPart = m_dwStreamLength;
    pstatstg->type = STGTY_STREAM;

    // we ignore the grfStatFlag - we always assume STATFLAG_NONAME
    pstatstg->pwcsName = NULL;

    pstatstg->grfMode = 0;
    pstatstg->grfLocksSupported = 0;
    pstatstg->clsid = CLSID_NULL;
    pstatstg->grfStateBits = 0;

    return S_OK;
} // CGrowableStream::Stat

//
// Clone - Make a deep copy of the stream into a new cGrowableStream instance
//
// Arguments:
//   ppStream - required output parameter for the new stream instance
//
// Returns:
//   S_OK on success, or an error code on failure.
//
HRESULT
CGrowableStream::Clone(
    IStream **ppStream)
{
    if (NULL == ppStream)
        return E_POINTER;

    // Copy our entire buffer into the new stream
    CGrowableStream * newStream = new (nothrow) CGrowableStream();
    if (newStream == NULL)
    {
        return E_OUTOFMEMORY;
    }

    HRESULT hr = newStream->Write(m_swBuffer, m_dwStreamLength, NULL);
    if (FAILED(hr))
    {
        delete newStream;
        return hr;
    }

    *ppStream = newStream;
    return S_OK;
} // CGrowableStream::Clone

#endif // !DACCESS_COMPILE
