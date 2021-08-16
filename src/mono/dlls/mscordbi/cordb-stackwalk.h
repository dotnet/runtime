// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-STACKWALK.H
//

#ifndef __MONO_DEBUGGER_CORDB_STACKWALK_H__
#define __MONO_DEBUGGER_CORDB_STACKWALK_H__

#include <cordb.h>

class CordbStackWalk : public CordbBaseMono,
                    public ICorDebugStackWalk 
{
    CordbThread* m_pThread;
    int                m_nCurrentFrame;
    int                m_nFrames;
    CordbNativeFrame** m_ppFrames;

public:
    CordbStackWalk(Connection* conn, CordbThread* ppThread);
    ~CordbStackWalk();
    HRESULT PopulateStackWalk();
    void Reset();
    HRESULT STDMETHODCALLTYPE GetContext(ULONG32 contextFlags, ULONG32 contextBufSize, ULONG32 *contextSize, BYTE contextBuf[  ]);
    HRESULT STDMETHODCALLTYPE SetContext(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[  ]);
    HRESULT STDMETHODCALLTYPE Next(void);
    HRESULT STDMETHODCALLTYPE GetFrame(ICorDebugFrame **pFrame);
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
        return "CordbThread";
    }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);
};

#endif
