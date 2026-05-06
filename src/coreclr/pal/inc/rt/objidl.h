// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: objidl.h
//
// ===========================================================================
// simplified objidl.h for PAL

#include "rpc.h"
#include "rpcndr.h"

#include "unknwn.h"

#ifndef __IEnumUnknown_INTERFACE_DEFINED__
#define __IEnumUnknown_INTERFACE_DEFINED__

// 00000100-0000-0000-C000-000000000046
EXTERN_C const IID IID_IEnumUnknown;

interface IEnumUnknown : public IUnknown
{
public:
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next(
        /* [annotation][in] */
        _In_  ULONG celt,
        /* [annotation][out] */
        _Out_writes_to_(celt,*pceltFetched)  IUnknown **rgelt,
        /* [annotation][out] */
        _Out_opt_  ULONG *pceltFetched) = 0;

    virtual HRESULT STDMETHODCALLTYPE Skip(
        /* [in] */ ULONG celt) = 0;

    virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;

    virtual HRESULT STDMETHODCALLTYPE Clone(
        /* [out] */ __RPC__deref_out_opt IEnumUnknown **ppenum) = 0;

};

#endif 	/* __IEnumUnknown_INTERFACE_DEFINED__ */

#ifndef __ISequentialStream_INTERFACE_DEFINED__
#define __ISequentialStream_INTERFACE_DEFINED__

// 0c733a30-2a1c-11ce-ade5-00aa0044773d
EXTERN_C const IID IID_ISequentialStream;

interface ISequentialStream : public IUnknown
{
public:
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Read(
        /* [length_is][size_is][out] */ void *pv,
        /* [in] */ ULONG cb,
        /* [out] */ ULONG *pcbRead) = 0;

    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Write(
        /* [size_is][in] */ const void *pv,
        /* [in] */ ULONG cb,
        /* [out] */ ULONG *pcbWritten) = 0;

};

#endif // __ISequentialStream_INTERFACE_DEFINED__


#ifndef __IStream_INTERFACE_DEFINED__
#define __IStream_INTERFACE_DEFINED__

typedef struct tagSTATSTG
    {
    LPOLESTR pwcsName;
    DWORD type;
    ULARGE_INTEGER cbSize;
    FILETIME mtime;
    FILETIME ctime;
    FILETIME atime;
    DWORD grfMode;
    DWORD grfLocksSupported;
    CLSID clsid;
    DWORD grfStateBits;
    DWORD reserved;
    } 	STATSTG;

typedef
enum tagSTGTY
    {	STGTY_STORAGE	= 1,
	STGTY_STREAM	= 2,
	STGTY_LOCKBYTES	= 3,
	STGTY_PROPERTY	= 4
    } 	STGTY;

typedef
enum tagSTREAM_SEEK
    {	STREAM_SEEK_SET	= 0,
	STREAM_SEEK_CUR	= 1,
	STREAM_SEEK_END	= 2
    } 	STREAM_SEEK;

typedef
enum tagSTATFLAG
    {	STATFLAG_DEFAULT	= 0,
	STATFLAG_NONAME	= 1,
	STATFLAG_NOOPEN	= 2
    } 	STATFLAG;

// 0000000c-0000-0000-C000-000000000046
EXTERN_C const IID IID_IStream;

interface DECLSPEC_UUID("0000000c-0000-0000-C000-000000000046")
IStream : public ISequentialStream
{
public:
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE Seek(
        /* [in] */ LARGE_INTEGER dlibMove,
        /* [in] */ DWORD dwOrigin,
        /* [out] */ ULARGE_INTEGER *plibNewPosition) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetSize(
        /* [in] */ ULARGE_INTEGER libNewSize) = 0;

    virtual /* [local] */ HRESULT STDMETHODCALLTYPE CopyTo(
        /* [unique][in] */ IStream *pstm,
        /* [in] */ ULARGE_INTEGER cb,
        /* [out] */ ULARGE_INTEGER *pcbRead,
        /* [out] */ ULARGE_INTEGER *pcbWritten) = 0;

    virtual HRESULT STDMETHODCALLTYPE Commit(
        /* [in] */ DWORD grfCommitFlags) = 0;

    virtual HRESULT STDMETHODCALLTYPE Revert( void) = 0;

    virtual HRESULT STDMETHODCALLTYPE LockRegion(
        /* [in] */ ULARGE_INTEGER libOffset,
        /* [in] */ ULARGE_INTEGER cb,
        /* [in] */ DWORD dwLockType) = 0;

    virtual HRESULT STDMETHODCALLTYPE UnlockRegion(
        /* [in] */ ULARGE_INTEGER libOffset,
        /* [in] */ ULARGE_INTEGER cb,
        /* [in] */ DWORD dwLockType) = 0;

    virtual HRESULT STDMETHODCALLTYPE Stat(
        /* [out] */ STATSTG *pstatstg,
        /* [in] */ DWORD grfStatFlag) = 0;

    virtual HRESULT STDMETHODCALLTYPE Clone(
        /* [out] */ IStream **ppstm) = 0;

};

#endif // __IStream_INTERFACE_DEFINED__


#ifndef __IStorage_INTERFACE_DEFINED__
#define __IStorage_INTERFACE_DEFINED__

typedef OLECHAR **SNB;

interface IEnumSTATSTG;

// 0000000b-0000-0000-C000-000000000046

interface IStorage : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE CreateStream(
        /* [string][in] */ const OLECHAR *pwcsName,
        /* [in] */ DWORD grfMode,
        /* [in] */ DWORD reserved1,
        /* [in] */ DWORD reserved2,
        /* [out] */ IStream **ppstm) = 0;

    virtual /* [local] */ HRESULT STDMETHODCALLTYPE OpenStream(
        /* [string][in] */ const OLECHAR *pwcsName,
        /* [unique][in] */ void *reserved1,
        /* [in] */ DWORD grfMode,
        /* [in] */ DWORD reserved2,
        /* [out] */ IStream **ppstm) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateStorage(
        /* [string][in] */ const OLECHAR *pwcsName,
        /* [in] */ DWORD grfMode,
        /* [in] */ DWORD reserved1,
        /* [in] */ DWORD reserved2,
        /* [out] */ IStorage **ppstg) = 0;

    virtual HRESULT STDMETHODCALLTYPE OpenStorage(
        /* [string][unique][in] */ const OLECHAR *pwcsName,
        /* [unique][in] */ IStorage *pstgPriority,
        /* [in] */ DWORD grfMode,
        /* [unique][in] */ SNB snbExclude,
        /* [in] */ DWORD reserved,
        /* [out] */ IStorage **ppstg) = 0;

    virtual HRESULT STDMETHODCALLTYPE CopyTo(
        /* [in] */ DWORD ciidExclude,
        /* [size_is][unique][in] */ const IID *rgiidExclude,
        /* [unique][in] */ SNB snbExclude,
        /* [unique][in] */ IStorage *pstgDest) = 0;

    virtual HRESULT STDMETHODCALLTYPE MoveElementTo(
        /* [string][in] */ const OLECHAR *pwcsName,
        /* [unique][in] */ IStorage *pstgDest,
        /* [string][in] */ const OLECHAR *pwcsNewName,
        /* [in] */ DWORD grfFlags) = 0;

    virtual HRESULT STDMETHODCALLTYPE Commit(
        /* [in] */ DWORD grfCommitFlags) = 0;

    virtual HRESULT STDMETHODCALLTYPE Revert( void) = 0;

    virtual /* [local] */ HRESULT STDMETHODCALLTYPE EnumElements(
        /* [in] */ DWORD reserved1,
        /* [size_is][unique][in] */ void *reserved2,
        /* [in] */ DWORD reserved3,
        /* [out] */ IEnumSTATSTG **ppenum) = 0;

    virtual HRESULT STDMETHODCALLTYPE DestroyElement(
        /* [string][in] */ const OLECHAR *pwcsName) = 0;

    virtual HRESULT STDMETHODCALLTYPE RenameElement(
        /* [string][in] */ const OLECHAR *pwcsOldName,
        /* [string][in] */ const OLECHAR *pwcsNewName) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetElementTimes(
        /* [string][unique][in] */ const OLECHAR *pwcsName,
        /* [unique][in] */ const FILETIME *pctime,
        /* [unique][in] */ const FILETIME *patime,
        /* [unique][in] */ const FILETIME *pmtime) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetClass(
        /* [in] */ REFCLSID clsid) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetStateBits(
        /* [in] */ DWORD grfStateBits,
        /* [in] */ DWORD grfMask) = 0;

    virtual HRESULT STDMETHODCALLTYPE Stat(
        /* [out] */ STATSTG *pstatstg,
        /* [in] */ DWORD grfStatFlag) = 0;

};

#endif // __IStorage_INTERFACE_DEFINED__


#ifndef __IMalloc_INTERFACE_DEFINED__
#define __IMalloc_INTERFACE_DEFINED__

/* interface IMalloc */
/* [uuid][object][local] */

// 0000001d-0000-0000-C000-000000000046
EXTERN_C const IID IID_IMalloc;

interface IMalloc : public IUnknown
{
public:
    virtual void *STDMETHODCALLTYPE Alloc(
        /* [in] */ SIZE_T cb) = 0;

    virtual void *STDMETHODCALLTYPE Realloc(
        /* [in] */ void *pv,
        /* [in] */ SIZE_T cb) = 0;

    virtual void STDMETHODCALLTYPE Free(
        /* [in] */ void *pv) = 0;

    virtual SIZE_T STDMETHODCALLTYPE GetSize(
        /* [in] */ void *pv) = 0;

    virtual int STDMETHODCALLTYPE DidAlloc(
        void *pv) = 0;

    virtual void STDMETHODCALLTYPE HeapMinimize( void) = 0;

};

typedef /* [unique] */ IMalloc *LPMALLOC;

#endif // __IMalloc_INTERFACE_DEFINED__
