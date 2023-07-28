// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <assert.h>
#include "../profiler.h"

class EventPipeWritingProfiler : public Profiler
{
public:
    static EventPipeWritingProfiler *GetSingleton()
    {
        return s_singleton;
    }

    EventPipeWritingProfiler() : Profiler(),
        _failures(0),
        _enables(0),
        _disables(0),
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

    void ProviderCallback(
        const UINT8 *source_id,
        UINT32 is_enabled,
        UINT8 level,
        UINT64 match_any_keywords,
        UINT64 match_all_keywords,
        COR_PRF_FILTER_DATA *filter_data,
        void *callback_data);
private:
    static EventPipeWritingProfiler *s_singleton;

    std::atomic<int> _failures;
    std::atomic<int> _enables;
    std::atomic<int> _disables;
    ICorProfilerInfo14 *_pCorProfilerInfo;
    EVENTPIPE_PROVIDER _provider;
    EVENTPIPE_EVENT _allTypesEvent;
    EVENTPIPE_EVENT _arrayTypeEvent;
    EVENTPIPE_EVENT _emptyEvent;
    EVENTPIPE_EVENT _simpleEvent;

    HRESULT FunctionSeen(FunctionID functionId);

    template<typename T>
    static void WriteToBuffer(BYTE *pBuffer, size_t bufferLength, size_t *pOffset, T value)
    {
        assert(bufferLength >= (*pOffset + sizeof(T)));

        *(T*)(pBuffer + *pOffset) = value;
        *pOffset += sizeof(T);
    }
};
