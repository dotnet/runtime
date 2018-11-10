// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ProfAttach.inl
// 

//
// Implementation of inlineable functions that help with attaching and detaching 
// profilers
//

// ======================================================================================

#ifndef __PROF_ATTACH_INL__
#define __PROF_ATTACH_INL__

// ----------------------------------------------------------------------------
// BaseRequestMessage::BaseRequestMessage
// 
// Description:
//    Constructor for base class of all request messages sent from trigger (client) to
//    profilee (server).
//    
// Arguments:
//    * cbMessage - Size, in bytes, of the entire request message (including size of
//        derived type, client data, etc., if present in the message)
//    * requestMessageType - Enum representing type of request this constitutes
//

inline BaseRequestMessage::BaseRequestMessage(
    DWORD cbMessage, 
    RequestMessageType requestMessageType) :
        m_cbMessage(cbMessage),
        m_requestMessageType(requestMessageType)
{
    LIMITED_METHOD_CONTRACT;
}

// ----------------------------------------------------------------------------
// GetVersionRequestMessage::GetVersionRequestMessage
//
// Description: 
//    Constructor to create a fully initialized GetVersionRequestMessage
//

inline GetVersionRequestMessage::GetVersionRequestMessage()
    : BaseRequestMessage(sizeof(GetVersionRequestMessage), kMsgGetVersion)
{
    LIMITED_METHOD_CONTRACT;
}

// ----------------------------------------------------------------------------
// AttachRequestMessage::AttachRequestMessage
//
// Description: 
//    Constructor for request message of type kMsgAttach sent from trigger (client) to
//    profilee (server)
//
// Arguments:
//    * cbMessage - Size, in bytes, of the entire request message (including size of
//        derived type, client data, etc., if present in the message)
//    * triggerVersion - Uint representing profiler attach interface version used by trigger
//    * pClsidProfiler - CLSID of profiler to attach
//    * wszProfilerPath - path to profiler DLL 
//    * dwClientDataStartOffset - see code:AttachRequestMessage
//    * cbClientDataLength - see code:AttachRequestMessage
//

inline AttachRequestMessage::AttachRequestMessage(
    DWORD cbMessage, 
    const UINT & triggerVersion,
    const CLSID * pClsidProfiler,
    LPCWSTR wszProfilerPath,
    DWORD dwClientDataStartOffset,
    DWORD cbClientDataLength) :
        BaseRequestMessage(cbMessage, kMsgAttach),
        m_dwClientDataStartOffset(dwClientDataStartOffset),
        m_cbClientDataLength(cbClientDataLength)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERT(cbMessage >= sizeof(AttachRequestMessage) + cbClientDataLength);
    memcpy(&m_clsidProfiler, pClsidProfiler, sizeof(m_clsidProfiler));
    m_triggerVersion = triggerVersion;
    if (wszProfilerPath != NULL)
    {
        _ASSERTE(wcslen(wszProfilerPath) < _countof(m_wszProfilerPath));
        wcscpy_s(m_wszProfilerPath, _countof(m_wszProfilerPath), wszProfilerPath);
    }
    else
    {
        m_wszProfilerPath[0] = L'\0';
    }
}

// ----------------------------------------------------------------------------
// AttachRequestMessageV2::AttachRequestMessageV2
//
// Description: 
//    Constructor for request message V2 of type kMsgAttach sent from trigger (client) to
//    profilee (server)
//
// Arguments:
//    * cbMessage - Size, in bytes, of the entire request message (including size of
//        derived type, client data, etc., if present in the message)
//    * triggerVersion - Uint representing profiler attach interface version used by trigger
//    * pClsidProfiler - CLSID of profiler to attach
//    * wszProfilerPath - path to profiler DLL 
//    * dwClientDataStartOffset - see code:AttachRequestMessage
//    * cbClientDataLength - see code:AttachRequestMessage
//    * dwConcurrentGCWaitTimeoutInMs - the time out for wait operation on concurrent GC to finish. 
//                                      Attach scenario only.
//
inline AttachRequestMessageV2::AttachRequestMessageV2(
    DWORD cbMessage, 
    const UINT & triggerVersion,
    const CLSID * pClsidProfiler,
    LPCWSTR wszProfilerPath,
    DWORD dwClientDataStartOffset,
    DWORD cbClientDataLength,
    DWORD dwConcurrentGCWaitTimeoutInMs)
    :AttachRequestMessage(
        cbMessage,
        triggerVersion,
        pClsidProfiler,
        wszProfilerPath,
        dwClientDataStartOffset,
        cbClientDataLength),
    m_dwConcurrentGCWaitTimeoutInMs(dwConcurrentGCWaitTimeoutInMs)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERT(cbMessage >= sizeof(AttachRequestMessageV2) + cbClientDataLength);
}

// ----------------------------------------------------------------------------
// AttachRequestMessageV2::CanCastTo
inline BOOL AttachRequestMessageV2::CanCastTo(const AttachRequestMessage *pMsg)
{
    LIMITED_METHOD_CONTRACT;

    // We already have checks that the client data doesn't go beyond the message body. 
    // If someone creates a bad message that pretends to be a V2 message, the worst scenario 
    // is we got a bad time out.
    if (pMsg->m_cbMessage >= sizeof(AttachRequestMessageV2) + pMsg->m_cbClientDataLength)
        return TRUE;
    
    return FALSE;
}

// ----------------------------------------------------------------------------
// BaseResponseMessage::BaseResponseMessage
//
// Description: 
//    Constructor for base class of all response messages returned by profilee (server)
//    to trigger (client)
//
// Arguments:
//    * hr - HRESULT indicating success or failure of executing the request that the
//        trigger had made to the profilee
//

inline BaseResponseMessage::BaseResponseMessage(HRESULT hr) :
    m_hr(hr)
{
    LIMITED_METHOD_CONTRACT;
}

// ----------------------------------------------------------------------------
// BaseResponseMessage::BaseResponseMessage
//
// Description: 
//    Zero-parameter constructor for BaseResponseMessage for use when hr is not yet
//    known.
//

inline BaseResponseMessage::BaseResponseMessage() :
    m_hr(E_UNEXPECTED)
{
    LIMITED_METHOD_CONTRACT;
}

// ----------------------------------------------------------------------------
// GetVersionResponseMessage::GetVersionResponseMessage
//
// Description: 
//    Constructor to create a fully initialized GetVersionResponseMessage
//
// Arguments:
//    * hr - Success / failure of carrying out the GetVersion request
//    * profileeVersion - Version of the target profilee app's runtime (server)
//    * minimumAllowableTriggerVersion - Oldest version of a trigger process that this
//        target profilee app is willing to talk to.
//

inline GetVersionResponseMessage::GetVersionResponseMessage(
    HRESULT hr,
    const UINT & profileeVersion,
    const UINT & minimumAllowableTriggerVersion) :
        BaseResponseMessage(hr)
{
    LIMITED_METHOD_CONTRACT;
    m_profileeVersion = profileeVersion;
    m_minimumAllowableTriggerVersion = minimumAllowableTriggerVersion;
}

// ----------------------------------------------------------------------------
// GetVersionResponseMessage::GetVersionResponseMessage
//
// Description: 
//    Constructor to use for GetVersionResponseMessage when the data is not known yet. 
//    The trigger will typically use this constructor to create an empty
//    GetVersionResponseMessage as storage to receive the GetVersionResponseMessage data
//    that will come in over the pipe from the target profilee app.
//

inline GetVersionResponseMessage::GetVersionResponseMessage()
{
    LIMITED_METHOD_CONTRACT;
    memset(this, 0, sizeof(*this));
    m_hr = E_UNEXPECTED;
}

// ----------------------------------------------------------------------------
// AttachResponseMessage::AttachResponseMessage
//
// Description: 
//    Constructor for AttachResponseMessage
//
// Arguments:
//    * hr - Success / failure of carrying out the attach request
//

inline AttachResponseMessage::AttachResponseMessage(HRESULT hr)
    : BaseResponseMessage(hr)
{
    LIMITED_METHOD_CONTRACT;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::GetAttachThreadingMode
// 
// Description:
//    Returns the profiling attach threading mode for this runtime instance. See
//    code:ProfilingAPIAttachDetach::AttachThreadingMode.
//    
// Return Value:
//    The profiling attach threading mode 
//    
// Assumptions:
//    * code:ProfilingAPIAttachDetach::Initialize must be called before this function.
//        

// static
inline ProfilingAPIAttachDetach::AttachThreadingMode ProfilingAPIAttachDetach::GetAttachThreadingMode()
{
    LIMITED_METHOD_CONTRACT;

    // ProfilingAPIAttachDetach::Initialize must be called before this function.
    _ASSERTE(s_fInitializeCalled);
    return s_attachThreadingMode; 
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::GetAttachEventNameForPidAndVersion
//
// Description: 
//    Generates name for Globally Named Attach Event, based on PID and the runtime version
//    Name looks like this:
//         CPFATE_nnnn_RuntimeVersion
//    CPFATE stands for CLR Profiling API attach trigger event
//    nnnn is decimal process ID
//    RuntimeVersion is the string of the runtime version
//
// Arguments:
//    * hProfileeProcess - The profilee process we want to attach to
//    * wszRuntimeVersion - runtime version string
//    * pAttachEventName - [in/out] SString to hold the generated name
//

// static
inline void ProfilingAPIAttachDetach::GetAttachEventNameForPidAndVersion(HANDLE hProfileeProcess, LPCWSTR wszRuntimeVersion, SString * pAttachEventName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Convert to lower case using invariant culture
    SString strRuntimeVersion(wszRuntimeVersion);
    strRuntimeVersion.LowerCase();
    
    DWORD dwProfileeProcessPid = ::GetProcessId(hProfileeProcess);
    
    if (IsAppContainerProcess(hProfileeProcess))
    {
        HANDLE hCurrentProcess = ::GetCurrentProcess();
        if (hProfileeProcess == hCurrentProcess || IsAppContainerProcess(::GetCurrentProcess()))
        {
            // App container to app container or the current process is the profilee process
            // In any case, use a local name
            pAttachEventName->Printf(L"NCPFATE_%d_%s", dwProfileeProcessPid, strRuntimeVersion.GetUnicode());        
        }
        else
        {
            // Otherwise, we'll assume it is full-trust to lowbox, and in this case we need to prefix the name with app container path
            WCHAR wszObjectPath[MAX_PATH];
            HRESULT hr = GetAppContainerNamedObjectPath(hProfileeProcess, wszObjectPath, sizeof(wszObjectPath)/sizeof(WCHAR));
            IfFailThrow(hr);

            //
            // Retrieve the session ID
            //
            DWORD dwSessionId;
            if (!ProcessIdToSessionId(dwProfileeProcessPid, &dwSessionId))
            {
                COMPlusThrowHR(HRESULT_FROM_GetLastError());
            }

            pAttachEventName->Printf(L"Session\\%d\\%s\\NCPFATE_%d_%s", dwSessionId, wszObjectPath, dwProfileeProcessPid, strRuntimeVersion.GetUnicode());        
        }        
    }
    else
    {
        // Non-app conatiner scenario
        // Create in global namespace
        pAttachEventName->Printf(L"Global\\NCPFATE_%d_%s", dwProfileeProcessPid, strRuntimeVersion.GetUnicode());
    }
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::GetAttachPipeNameForPidAndVersion
//
// Description: 
//    Generates name for Globally Named Attach Pipe, based on PID and the runtime version
//    Name looks like this:
//         \\.\pipe\CPFATP_nnnn_RuntimeVersion
//    CPFATP stands for CLR Profiling API attach trigger pipe
//    nnnn is decimal process ID
//    RuntimeVersion is the string of the runtime version
//
// Arguments:
//    * hProfileeProcess - The profilee process we want to attach to
//    * wszRuntimeVersion - runtime version string
//    * pAttachPipeName - [in/out] SString to hold the generated name
//

// static
inline void ProfilingAPIAttachDetach::GetAttachPipeNameForPidAndVersion(HANDLE hProfileeProcess, LPCWSTR wszRuntimeVersion, SString * pAttachPipeName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Convert to lower case using invariant culture
    SString strRuntimeVersion(wszRuntimeVersion);
    strRuntimeVersion.LowerCase();
    
    DWORD dwProfileeProcessPid = ::GetProcessId(hProfileeProcess);
    
    if (IsAppContainerProcess(hProfileeProcess))
    {
    
        //
        // Retrieve low object path
        //
        WCHAR wszObjectPath[MAX_PATH];
        HRESULT hr = GetAppContainerNamedObjectPath(hProfileeProcess, wszObjectPath, sizeof(wszObjectPath)/sizeof(WCHAR));
        IfFailThrow(hr);

        //
        // Retrieve the session ID
        //
        DWORD dwSessionId;
        if (!ProcessIdToSessionId(dwProfileeProcessPid, &dwSessionId))
        {
            COMPlusThrowHR(HRESULT_FROM_GetLastError());
        }
            
        pAttachPipeName->Printf(L"\\\\.\\pipe\\Sessions\\%d\\%s\\NCPFATP_%d_%s", dwSessionId, wszObjectPath, dwProfileeProcessPid, strRuntimeVersion.GetUnicode());    
    }
    else
    {
        pAttachPipeName->Printf(L"\\\\.\\pipe\\NCPFATP_%d_%s", dwProfileeProcessPid, strRuntimeVersion.GetUnicode());    
    }    
}

// Simple wrapper around code:ProfilingAPIAttachDetach::GetAttachEventNameForPidAndVersion using
// current process's PID and current runtime directory
// static
inline HRESULT ProfilingAPIAttachDetach::GetAttachEventName(HANDLE hProfileeProcess, SString * pAttachEventName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    WCHAR wszRuntimeVersion[MAX_PATH];
    wszRuntimeVersion[0] = L'\0';

    // Note: CoreCLR can have the same version as Desktop CLR.  And it's possible to have mutilple 
    // instances of the same version of the CoreCLR in the process.  We need to come up with 
    // something other than version When Attach is enabled for CoreCLR.
    DWORD dwSize = _countof(wszRuntimeVersion); 
    HRESULT hr = GetCORVersionInternal(wszRuntimeVersion, dwSize, &dwSize);
    if (FAILED(hr))
    {
        return hr;
    }

    GetAttachEventNameForPidAndVersion(hProfileeProcess, wszRuntimeVersion, pAttachEventName);
    return S_OK;
}

// Simple wrapper around code:ProfilingAPIAttachDetach::GetAttachPipeNameForPidAndVersion using
// current process's PID and current runtime directory
// static
inline HRESULT ProfilingAPIAttachDetach::GetAttachPipeName(HANDLE hProfileeProcess, SString * pAttachPipeName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    WCHAR wszRuntimeVersion[MAX_PATH];
    wszRuntimeVersion[0] = L'\0';

    // Note: CoreCLR can have the same version as Desktop CLR.  And it's possible to have mutilple 
    // instances of the same version of the CoreCLR in the process.  We need to come up with 
    // something other than version When Attach is enabled for CoreCLR.
    DWORD dwSize = _countof(wszRuntimeVersion); 
    HRESULT hr = GetCORVersionInternal(wszRuntimeVersion, dwSize, &dwSize);
    if (FAILED(hr))
    {
        return hr;
    }

    GetAttachPipeNameForPidAndVersion(hProfileeProcess, wszRuntimeVersion, pAttachPipeName);
    return S_OK;
}

#endif // __PROF_ATTACH_INL__
