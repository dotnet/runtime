// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttachClient.h
// 

// 
// Definition of ProfilingAPIAttachClient, which houses the prime portion of the
// implementation of the AttachProfiler() API, exported by mscoree.dll, and consumed by
// trigger processes in order to force the runtime of a target process to load a
// profiler. This handles opening a client connection to the pipe created by the target
// profilee, and sending requests across that pipe to force the target profilee (which
// acts as the pipe server) to attach a profiler.
// 

// ======================================================================================

#ifndef __PROF_ATTACH_CLIENT_H__
#define __PROF_ATTACH_CLIENT_H__

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
extern "C" HRESULT STDMETHODCALLTYPE AttachProfiler(
    DWORD dwProfileeProcessID,
    DWORD dwMillisecondsMax,
    const CLSID * pClsidProfiler,
    LPCWSTR wszProfilerPath,
    void * pvClientData,
    UINT cbClientData,
    LPCWSTR wszRuntimeVersion);
#endif // FEATURE_PROFAPI_ATTACH_DETACH
// ---------------------------------------------------------------------------------------
// Here's the beef. All the pipe client stuff running in the trigger process (via call to
// AttachProfiler()) is housed in this class. Note that these functions cannot assume a
// fully initialized runtime (e.g., it would be nonsensical for these functions to
// reference ProfilingAPIAttachDetach::s_hAttachEvent). These functions operate solely by
// finding the attach event & pipes by name, and using them to communicate with the
// target profilee app.

class ProfilingAPIAttachClient
{
public:
    HRESULT AttachProfiler(
        DWORD dwProfileeProcessID,
        DWORD dwMillisecondsMax,
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        void * pvClientData,
        UINT cbClientData,
        LPCWSTR wszRuntimeVersion);

protected:
    // Client connection to the pipe that connects to the target profilee (server)
    HandleHolder m_hPipeClient;

    BOOL MightProcessExist(DWORD dwProcessID);
    HRESULT SignalAttachEvent(LPCWSTR wszEventName);
    HRESULT OpenPipeClient(
        LPCWSTR wszPipeName,
        DWORD dwMillisecondsMax);
    HRESULT VerifyVersionIsCompatible(DWORD dwMillisecondsMax);
    HRESULT SendAttachRequest(
        DWORD dwMillisecondsMax, 
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        void * pvClientData,
        UINT cbClientData,
        HRESULT * phrAttach);
    HRESULT SendAndReceive(
        DWORD dwMillisecondsMax,
        LPVOID pvInBuffer,
        DWORD cbInBuffer,
        LPVOID pvOutBuffer,
        DWORD cbOutBuffer,
        DWORD * pcbReceived);
};

#endif //__PROF_ATTACH_CLIENT_H__
