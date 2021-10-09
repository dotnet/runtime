// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CODE.H
//

#ifndef __MONO_DEBUGGER_CORDB_CODE_H__
#define __MONO_DEBUGGER_CORDB_CODE_H__

#include <cordb.h>

class CordbCode : public CordbBaseMono, public ICorDebugCode
{
    CordbFunction* m_pFunction;
    ULONG32 m_nSize;
public:
    CordbCode(Connection* conn, CordbFunction* func);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbCode";
    }
    ULONG32 GetSize();
    HRESULT STDMETHODCALLTYPE IsIL(BOOL* pbIL);
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS* pStart);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pcBytes);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ULONG32 offset, ICorDebugFunctionBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE GetCode(ULONG32 startOffset, ULONG32 endOffset, ULONG32 cBufferAlloc, BYTE buffer[], ULONG32* pcBufferSize);
    HRESULT STDMETHODCALLTYPE GetVersionNumber(ULONG32* nVersion);
    HRESULT STDMETHODCALLTYPE GetILToNativeMapping(ULONG32  cMap,
                         ULONG32* pcMap,

                         COR_DEBUG_IL_TO_NATIVE_MAP map[]);
    HRESULT STDMETHODCALLTYPE GetEnCRemapSequencePoints(ULONG32 cMap, ULONG32* pcMap, ULONG32 offsets[]);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    CordbFunction* GetFunction() const
    {
        return m_pFunction;
    }
};

#endif
