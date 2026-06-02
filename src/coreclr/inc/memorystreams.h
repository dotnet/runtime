// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// memorystreams.h
//
// IStream implementations for in-memory streams
//
//*****************************************************************************

#ifndef __MemoryStreams_h__
#define __MemoryStreams_h__

#include <objidl.h>

#include "daccess.h"
#include "memoryrange.h"

// Forward declarations
template<typename T> struct cdac_data;

//*****************************************************************************
// CInMemoryStream is a simple IStream implementation that provides
// read/write access to a fixed-size memory buffer.
//*****************************************************************************
class CInMemoryStream : public IStream
{
public:
    CInMemoryStream() :
        m_pMem(0),
        m_cbSize(0),
        m_cbCurrent(0),
        m_cRef(1),
        m_dataCopy(NULL)
    { }

    virtual ~CInMemoryStream() {}

    void InitNew(
        void        *pMem,
        ULONG       cbSize)
    {
        m_pMem = pMem;
        m_cbSize = cbSize;
        m_cbCurrent = 0;
    }

    ULONG STDMETHODCALLTYPE AddRef() {
        return InterlockedIncrement(&m_cRef);
    }


    ULONG STDMETHODCALLTYPE Release();

    __checkReturn
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *ppOut);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Read(void *pv, ULONG cb, ULONG *pcbRead);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Write(const void  *pv, ULONG cb, ULONG *pcbWritten);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Seek(LARGE_INTEGER dlibMove,DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition);

    __checkReturn
    HRESULT STDMETHODCALLTYPE SetSize(ULARGE_INTEGER libNewSize)
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE CopyTo(
        IStream     *pstm,
        ULARGE_INTEGER cb,
        ULARGE_INTEGER *pcbRead,
        ULARGE_INTEGER *pcbWritten);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Commit(
        DWORD       grfCommitFlags)
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Revert()
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE LockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD       dwLockType)
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE UnlockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD       dwLockType)
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Stat(
        STATSTG     *pstatstg,
        DWORD       grfStatFlag)
    {
        pstatstg->cbSize.QuadPart = m_cbSize;
        return (S_OK);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Clone(
        IStream     **ppstm)
    {
        return (E_NOTIMPL);
    }

    __checkReturn
    static HRESULT CreateStreamOnMemory(           // Return code.
                                 void        *pMem,                  // Memory to create stream on.
                                 ULONG       cbSize,                 // Size of data.
                                 IStream     **ppIStream,            // Return stream object here.
                                 BOOL        fDeleteMemoryOnRelease = FALSE
                                 );

    __checkReturn
    static HRESULT CreateStreamOnMemoryCopy(
                                 void        *pMem,
                                 ULONG       cbSize,
                                 IStream     **ppIStream);

private:
    void        *m_pMem;                // Memory for the read.
    ULONG       m_cbSize;               // Size of the memory.
    ULONG       m_cbCurrent;            // Current offset.
    LONG        m_cRef;                 // Ref count.
    BYTE       *m_dataCopy;             // Optional copy of the data.
};  // class CInMemoryStream

//*****************************************************************************
// CGrowableStream is a simple IStream implementation that grows as
// its written to. All the memory is contiguous, so read access is
// fast. A grow does a realloc, so be aware of that if you're going to
// use this.
//*****************************************************************************

// DPTR instead of VPTR because we don't actually call any of the virtuals.
typedef DPTR(class CGrowableStream) PTR_CGrowableStream;

class CGrowableStream : public IStream
{
public:
    //Constructs a new GrowableStream
    // multiplicativeGrowthRate - when the stream grows it will be at least this
    //   multiple of its old size. Values greater than 1 ensure O(N) amortized
    //   performance growing the stream to size N, 1 ensures O(N^2) amortized perf
    //   but gives the tightest memory usage. Valid range is [1.0, 2.0].
    // additiveGrowthRate - when the stream grows it will increase in size by at least
    //   this number of bytes. Larger numbers cause fewer re-allocations at the cost of
    //   increased memory usage.
    CGrowableStream(float multiplicativeGrowthRate = 2.0, DWORD additiveGrowthRate = 4096);

#ifndef DACCESS_COMPILE
    virtual ~CGrowableStream();
#endif

    // Expose the total raw buffer.
    // This can be used by DAC to get the raw contents.
    // This becomes potentially invalid on the next call on the class, because the underlying storage can be
    // reallocated.
    MemoryRange GetRawBuffer() const
    {
        PTR_VOID p = m_swBuffer;
        return MemoryRange(p, m_dwBufferSize);
    }

private:
    // Raw pointer to buffer. This may change as the buffer grows and gets reallocated.
    PTR_BYTE m_swBuffer;

    // Total size of the buffer in bytes.
    DWORD   m_dwBufferSize;

    // Current index in the buffer. This can be moved around by Seek.
    DWORD   m_dwBufferIndex;

    // Logical length of the stream
    DWORD   m_dwStreamLength;

    // Reference count
    LONG    m_cRef;

    // growth rate parameters determine new stream size when it must grow
    float   m_multiplicativeGrowthRate;
    int     m_additiveGrowthRate;

    // Ensures the stream is physically and logically at least newLogicalSize
    // in size
    HRESULT EnsureCapacity(DWORD newLogicalSize);

    // IStream methods
public:

#ifndef DACCESS_COMPILE
    ULONG STDMETHODCALLTYPE AddRef() {
        return InterlockedIncrement(&m_cRef);
    }


    ULONG STDMETHODCALLTYPE Release();

    __checkReturn
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *ppOut);

    STDMETHOD(Read)(
         void * pv,
         ULONG cb,
         ULONG * pcbRead);

    STDMETHOD(Write)(
         const void * pv,
         ULONG cb,
         ULONG * pcbWritten);

    STDMETHOD(Seek)(
         LARGE_INTEGER dlibMove,
         DWORD dwOrigin,
         ULARGE_INTEGER * plibNewPosition);

    STDMETHOD(SetSize)(ULARGE_INTEGER libNewSize);

    STDMETHOD(CopyTo)(
         IStream * pstm,
         ULARGE_INTEGER cb,
         ULARGE_INTEGER * pcbRead,
         ULARGE_INTEGER * pcbWritten) { return E_NOTIMPL; }

    STDMETHOD(Commit)(
         DWORD grfCommitFlags) { return NOERROR; }

    STDMETHOD(Revert)( void) { return E_NOTIMPL; }

    STDMETHOD(LockRegion)(
         ULARGE_INTEGER libOffset,
         ULARGE_INTEGER cb,
         DWORD dwLockType) { return E_NOTIMPL; }

    STDMETHOD(UnlockRegion)(
         ULARGE_INTEGER libOffset,
         ULARGE_INTEGER cb,
         DWORD dwLockType) { return E_NOTIMPL; }

    STDMETHOD(Stat)(
         STATSTG * pstatstg,
         DWORD grfStatFlag);

    // Make a deep copy of the stream into a new CGrowableStream instance
    STDMETHOD(Clone)(
         IStream ** ppstm);

#endif // DACCESS_COMPILE

    friend struct cdac_data<CGrowableStream>;
}; // class CGrowableStream


template<>
struct cdac_data<CGrowableStream>
{
    static constexpr size_t Buffer = offsetof(CGrowableStream, m_swBuffer);
    static constexpr size_t Size = offsetof(CGrowableStream, m_dwBufferSize);
};

#endif // __MemoryStreams_h__
