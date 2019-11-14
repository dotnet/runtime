// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// ReadOnlyDataTargetFacade.h
//

//
//*****************************************************************************

#ifndef READONLYDATATARGETFACADE_H_
#define READONLYDATATARGETFACADE_H_

#include <cordebug.h>

//---------------------------------------------------------------------------------------
//  ReadOnlyDataTargetFacade
//
//  This class is designed to be used as an ICorDebugMutableDataTarget when none is
//  supplied.  All of the write APIs will fail with CORDBG_E_TARGET_READONLY as required
//  by the data target spec when a write operation is invoked on a read-only data target.
//  The desire here is to merge the error code paths for the case when a write fails,
//  and the case when a write is requested but the data target supplied doesn't
//  implement ICorDebugMutableDataTarget.
//
//  Note that this is intended to be used only for the additional APIs defined by
//  ICorDebugMutableDataTarget.  Calling any of the base ICorDebugDataTarget APIs
//  will ASSERT and fail.  An alternative design would be to make this class a wrapper
//  class (similar to DataTargetAdapter) over an existing ICorDebugDataTarget interface.
//  In general, we'd like callers of the data target to differentiate between when they're
//  using read-only APIs and mutation APIs since they need to be aware that the latter often
//  won't be supported by the data target.  Also, that design would have the draw-back
//  of incuring an extra virtual dispatch on every read API call (makaing debugging more
//  complex and possibly having a performance impact).
//
class ReadOnlyDataTargetFacade : public ICorDebugMutableDataTarget
{
public:
    ReadOnlyDataTargetFacade();
    virtual ~ReadOnlyDataTargetFacade() {}

    //
    // IUnknown.
    //
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID InterfaceId,
        PVOID* Interface);

    virtual ULONG STDMETHODCALLTYPE AddRef();

    virtual ULONG STDMETHODCALLTYPE Release();

    //
    // ICorDebugDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE GetPlatform(
        CorDebugPlatform *pPlatform);

    virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
        CORDB_ADDRESS address,
        BYTE * pBuffer,
        ULONG32 request,
        ULONG32 * pcbRead);

    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        BYTE * context);

    //
    // ICorDebugMutableDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
        CORDB_ADDRESS address,
        const BYTE * pBuffer,
        ULONG32 request);

    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextSize,
        const BYTE * context);

    virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
        DWORD dwThreadId,
        CORDB_CONTINUE_STATUS dwContinueStatus);

private:
    // Reference count.
    LONG m_ref;
};

#include "readonlydatatargetfacade.inl"

#endif //  READONLYDATATARGETFACADE_H_

