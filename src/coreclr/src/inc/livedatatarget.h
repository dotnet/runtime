// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// Define a Data-Target for a live process.
//
//*****************************************************************************

#ifndef _LIVEPROC_DATATARGET_H_
#define _LIVEPROC_DATATARGET_H_

// Defines the Data-Target and other public interfaces.
// Does not include IXClrData definitions.
#include <clrdata.h>

#ifndef TARGET_UNIX

//---------------------------------------------------------------------------------------
//
// Provides a simple legacy data-target implementation for a live, local, process.
// Note that in arrowhead, most debuggers use ICorDebugDataTarget, and we have
// implementations of this in MDbg.
//
class LiveProcDataTarget : public ICLRDataTarget
{
public:
    LiveProcDataTarget(HANDLE process,
                       DWORD processId,
                       CLRDATA_ADDRESS baseAddressOfEngine = NULL);

    //
    // IUnknown.
    //
    // This class is intended to be kept on the stack
    // or as a member and does not maintain a refcount.
    //

    STDMETHOD(QueryInterface)(
        THIS_
        IN REFIID InterfaceId,
        OUT PVOID* Interface
        );
    STDMETHOD_(ULONG, AddRef)(
        THIS
        );
    STDMETHOD_(ULONG, Release)(
        THIS
        );

    //
    // ICLRDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE GetMachineType(
        /* [out] */ ULONG32 *machine);
    virtual HRESULT STDMETHODCALLTYPE GetPointerSize(
        /* [out] */ ULONG32 *size);
    virtual HRESULT STDMETHODCALLTYPE GetImageBase(
        /* [string][in] */ LPCWSTR name,
        /* [out] */ CLRDATA_ADDRESS *base);
    virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [length_is][size_is][out] */ PBYTE buffer,
        /* [in] */ ULONG32 request,
        /* [optional][out] */ ULONG32 *done);
    virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
        /* [in] */ CLRDATA_ADDRESS address,
        /* [size_is][in] */ PBYTE buffer,
        /* [in] */ ULONG32 request,
        /* [optional][out] */ ULONG32 *done);
    virtual HRESULT STDMETHODCALLTYPE GetTLSValue(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 index,
        /* [out] */ CLRDATA_ADDRESS* value);
    virtual HRESULT STDMETHODCALLTYPE SetTLSValue(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 index,
        /* [in] */ CLRDATA_ADDRESS value);
    virtual HRESULT STDMETHODCALLTYPE GetCurrentThreadID(
        /* [out] */ ULONG32* threadID);
    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 contextFlags,
        /* [in] */ ULONG32 contextSize,
        /* [out, size_is(contextSize)] */ PBYTE context);
    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 contextSize,
        /* [in, size_is(contextSize)] */ PBYTE context);
    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in] */ ULONG32 reqCode,
        /* [in] */ ULONG32 inBufferSize,
        /* [size_is][in] */ BYTE *inBuffer,
        /* [in] */ ULONG32 outBufferSize,
        /* [size_is][out] */ BYTE *outBuffer);

private:
    HANDLE m_process;
    DWORD m_processId;
    CLRDATA_ADDRESS m_baseAddressOfEngine;
};

#endif // TARGET_UNIX

#endif // _LIVEPROC_DATATARGET_H_

