// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class EventPipeWritingProfiler : public Profiler
{
public:
    EventPipeWritingProfiler() : Profiler(),
        _failures(0),
        _provider(0),
        _allTypesEvent(0),
        _arrayTypeEvent(0),
        _emptyEvent(0),
        _simpleEvent(0)
    {}

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock);
    virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result);

private:
    std::atomic<int> _failures;
    ICorProfilerInfo12 *_pCorProfilerInfo12;
    EVENTPIPE_PROVIDER _provider;
    EVENTPIPE_EVENT _allTypesEvent;
    EVENTPIPE_EVENT _arrayTypeEvent;
    EVENTPIPE_EVENT _emptyEvent;
    EVENTPIPE_EVENT _simpleEvent;

    HRESULT FunctionSeen(FunctionID functionId);

    template<typename T>
    static void WriteToBuffer(BYTE *pBuffer, size_t bufferLength, size_t *pOffset, T value)
    {
        _ASSERTE(bufferLength >= (*pOffset + sizeof(T)));

        *(T*)(pBuffer + *pOffset) = value;
        *pOffset += sizeof(T);
    }
};
