// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// DataTargetAdapter.h
//

//
// header for compatibility adapter for ICLRDataTarget
//*****************************************************************************

#ifndef DATATARGETADAPTER_H_
#define DATATARGETADAPTER_H_

#include <cordebug.h>

// Forward decl to avoid including clrdata.h here (it's use is being deprecated)
interface ICLRDataTarget;

/*
 * DataTargetAdapter - implements the new ICorDebugDataTarget interfaces
 * by wrapping legacy ICLRDataTarget implementations.  New code should use
 * ICorDebugDataTarget, but we must continue to support ICLRDataTarget
 * for dbgeng (watson, windbg, etc.) and for any other 3rd parties since
 * it is a documented API for dump generation.
 */
class DataTargetAdapter : public ICorDebugMutableDataTarget
{
public:
    // Create an adapter over the supplied legacy data target interface
    DataTargetAdapter(ICLRDataTarget * pLegacyTarget);
    virtual ~DataTargetAdapter();

    //
    // IUnknown.
    //
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** ppInterface);

    virtual ULONG STDMETHODCALLTYPE AddRef();

    virtual ULONG STDMETHODCALLTYPE Release();

    //
    // ICorDebugMutableDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE GetPlatform(
        CorDebugPlatform *pPlatform);

    virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
        CORDB_ADDRESS address,
        PBYTE pBuffer,
        ULONG32 request,
        ULONG32 *pcbRead);

    virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
        CORDB_ADDRESS address,
        const BYTE * pBuffer,
        ULONG32 request);

    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        PBYTE context);

    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextSize,
        const BYTE * context);

    virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
        DWORD dwThreadId,
        CORDB_CONTINUE_STATUS continueStatus);

private:
    LONG m_ref;                         // Reference count.
    ICLRDataTarget * m_pLegacyTarget;   // underlying data target
};


#endif //DATATARGETADAPTER_H_
