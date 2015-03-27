//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// debugshim.h
// 

//
//*****************************************************************************

#ifndef _DEBUG_SHIM_
#define _DEBUG_SHIM_

#include "cor.h"
#include "cordebug.h"
#include <wchar.h>
#include <metahost.h>

#define CORECLR_DAC_MODULE_NAME_W W("mscordaccore")
#define CLR_DAC_MODULE_NAME_W W("mscordacwks")
#define MAIN_DBI_MODULE_NAME_W W("mscordbi")

// forward declaration
struct ICorDebugDataTarget;

// ICLRDebugging implementation.
class CLRDebuggingImpl : public ICLRDebugging
{

public:
    CLRDebuggingImpl(GUID skuId) : m_cRef(0), m_skuId(skuId)
    {
    }

public:
    // ICLRDebugging methods:
    STDMETHOD(OpenVirtualProcess(
        ULONG64 moduleBaseAddress,
        IUnknown * pDataTarget,
        ICLRDebuggingLibraryProvider * pLibraryProvider,
        CLR_DEBUGGING_VERSION * pMaxDebuggerSupportedVersion,
        REFIID riidProcess,
        IUnknown ** ppProcess,
        CLR_DEBUGGING_VERSION * pVersion,
        CLR_DEBUGGING_PROCESS_FLAGS * pFlags));
    
    STDMETHOD(CanUnloadNow(HMODULE hModule));

	//IUnknown methods:
	STDMETHOD(QueryInterface(
                REFIID riid,
                void **ppvObject));

	// Standard AddRef implementation
	STDMETHOD_(ULONG, AddRef());

	// Standard Release implementation.
	STDMETHOD_(ULONG, Release());



private:
    VOID RetargetDacIfNeeded(DWORD* pdwTimeStamp,
                             DWORD* pdwSizeOfImage);

    HRESULT GetCLRInfo(ICorDebugDataTarget * pDataTarget,
                       ULONG64 moduleBaseAddress,
                       CLR_DEBUGGING_VERSION * pVersion,
                       DWORD * pdwDbiTimeStamp,
                       DWORD * pdwDbiSizeOfImage,
                       __out_z __inout_ecount(dwDbiNameCharCount) WCHAR * pDbiName,
                       DWORD   dwDbiNameCharCount,
                       DWORD * pdwDacTimeStamp,
                       DWORD * pdwDacSizeOfImage,
                       __out_z __inout_ecount(dwDacNameCharCount) WCHAR * pDacName,
                       DWORD   dwDacNameCharCount);

    HRESULT FormatLongDacModuleName(__out_z __inout_ecount(cchBuffer) WCHAR * pBuffer,
                                    DWORD cchBuffer,
                                    DWORD targetImageFileMachine,
                                    VS_FIXEDFILEINFO * pVersion);

	volatile LONG m_cRef;
    GUID m_skuId;

};  // class CLRDebuggingImpl

#endif
