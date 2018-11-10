// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ProfAttach.h
// 

//
// Declaration of functions that help with attaching and detaching profilers, including
// message structures that are passed back and forth between the trigger (client) and
// the target profilee (server).  For code specific to triggers and profilees, see
// code:ProfilingAPIAttachClient and code:ProfilingAPIAttachServer, respectively.
//

// ======================================================================================

#ifndef __PROF_ATTACH_H__
#define __PROF_ATTACH_H__

#include "internalunknownimpl.h"
#include "corprof.h"


//---------------------------------------------------------------------------------------
// Types of request messages that may be sent from trigger across the pipe
// 
enum RequestMessageType
{
    // Client (trigger) asks server (profilee) for server's version information. 
    // The message type must be code:ProfilingAPIAttachDetach::BaseRequestMessage
    kMsgGetVersion,
    
    // Client (trigger) asks server (profilee) to attach the profiler.  The message
    // type must be code:ProfilingAPIAttachDetach::AttachRequestMessage or AttachRequestMessageV2
    kMsgAttach,

    kMsgCount
};


// ---------------------------------------------------------------------------------------
// Base request message format. All request messages sent by trigger across pipe derive
// from this.
// 
// **** COMPATIBILITY WARNING ***
//     
// You are not allowed to change this structure in such a way as would modify the binary
// layout of derived type GetVersionRequestMessage, or else the trigger & profilee will
// be unable to negotiate version information. Asserts in
// code:ProfilingAPIAttachDetach::VerifyMessageStructureLayout attempt to enforce this.
// 
// **** COMPATIBILITY WARNING ***
//     
struct BaseRequestMessage
{
public:
    // Total size of the message (including size of derived type, client data, etc., if
    // present in the message)
    DWORD m_cbMessage;
    
    // What kind of message is this?
    RequestMessageType m_requestMessageType;
    
    BaseRequestMessage(DWORD cbMessage, RequestMessageType requestMessageType);

private:
    // Use parameterized constructor above to initialize this struct
    BaseRequestMessage();
};


// ---------------------------------------------------------------------------------------
// Message format for requesting version information from the target profilee
// 
// **** COMPATIBILITY WARNING ***
//     
// You are not allowed to change the binary layout of this structure, or else the trigger
// & profilee will be unable to negotiate version information. Asserts in
// code:ProfilingAPIAttachDetach::VerifyMessageStructureLayout attempt to enforce this.
// 
// **** COMPATIBILITY WARNING ***
//     
struct GetVersionRequestMessage : public BaseRequestMessage
{
public:
    GetVersionRequestMessage();
};


//---------------------------------------------------------------------------------------
// Attach request message format.  A kMsgAttach message sent by trigger must be of this
// type.
struct AttachRequestMessage : public BaseRequestMessage
{
public:
    // Trigger sends its version info here.  This allows the target profilee to
    // customize its response for the format expected by the trigger.
    UINT    m_triggerVersion;

    // The GUID of the profiler's COM object to load
    CLSID           m_clsidProfiler;

    // The path to the profiler's COM object to load
    WCHAR           m_wszProfilerPath[MAX_LONGPATH];

    // Client data is custom data that the profiler's
    // trigger-process wishes to copy into this process.
    // Profiler authors will typically use this as a way to
    // communicate to the profiler DLL what options the profiler
    // user has chosen.  This will help the profiler DLL configure
    // itself (e.g., to determine which callbacks to request).
    //
    // Since the client data is variable length, and we may
    // want to tail-extend this structure in the future, we use
    // an offset to point to the client data.  Client data
    // begins at this + m_dwClientDataStartOffset bytes.
    DWORD           m_dwClientDataStartOffset;
    DWORD           m_cbClientDataLength;

    AttachRequestMessage(
        DWORD cbMessage,
        const UINT & triggerVersion,
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        DWORD dwClientDataStartOffset,
        DWORD cbClientDataLength);

private:
    // Use parameterized constructor above to initialize this struct
    AttachRequestMessage();
};

//---------------------------------------------------------------------------------------
// Attach request message V2
// Pass the timeout information from client (the trigger process) to server (the profilee)
struct AttachRequestMessageV2 : public AttachRequestMessage
{

public :
    // Timeout for the wait operation for concurrent GC in server side
    // Basically time out passed from AttachProfiler API minus the amount of time already
    // elapsed in client side
    DWORD   m_dwConcurrentGCWaitTimeoutInMs;

public :
    AttachRequestMessageV2(
        DWORD cbMessage,
        const UINT & triggerVersion,
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        DWORD dwClientDataStartOffset,
        DWORD cbClientDataLength,
        DWORD dwConcurrentGCWaitTimeoutInMs);

    // Whether the attach request message is a V2 message (including V2+)
    static BOOL CanCastTo(const AttachRequestMessage * pMsg);
    
private:
    // Use parameterized constructor above to initialize this struct
    AttachRequestMessageV2();
};

// ---------------------------------------------------------------------------------------
// Base response message format. All response messages returned by profilee across the
// pipe to the trigger derive from this.
// 
// **** COMPATIBILITY WARNING ***
//     
// You are not allowed to change this structure in such a way as would change the binary
// layout of derived type GetVersionResponseMessage, or else the trigger & profilee will
// be unable to negotiate version information. Asserts in
// code:ProfilingAPIAttachDetach::VerifyMessageStructureLayout attempt to enforce this.
// 
// **** COMPATIBILITY WARNING ***
//     
struct BaseResponseMessage
{
public:
    // HRESULT indicating success or failure of carrying out the request
    HRESULT m_hr;

    BaseResponseMessage(HRESULT hr);

protected:
    // Use parameterized constructor above to initialize this struct
    BaseResponseMessage();
};

// ---------------------------------------------------------------------------------------
// GetVersion response message format. The server responds to a kMsgGetVersion message
// request with a message of this type.
// 
// **** COMPATIBILITY WARNING ***
//     
// You are not allowed to change the binary layout of this structure, or else the trigger
// & profilee will be unable to negotiate version information. Asserts in
// code:ProfilingAPIAttachDetach::VerifyMessageStructureLayout attempt to enforce this.
// 
// **** COMPATIBILITY WARNING ***
//     
struct GetVersionResponseMessage : public BaseResponseMessage
{
public:
    // The target profilee constructs this response by filling out the following two
    // values. The trigger process uses these values to determine whether it's compatible
    // with the target profilee.
    
    // Target profilee provides its version info here.  If trigger determines that
    // this number is too small, then trigger refuses the profilee as being too old.
    UINT m_profileeVersion;

    // Target profilee provides here the oldest version of a trigger process that it
    // can communicate with.  If trigger determines that this number is too big,
    // then trigger refuses the profilee as being too new.
    UINT m_minimumAllowableTriggerVersion;

    GetVersionResponseMessage(
        HRESULT hr,
        const UINT & profileeVersion,
        const UINT & minimumAllowableTriggerVersion);

    GetVersionResponseMessage();
};
    

// ---------------------------------------------------------------------------------------
// Attach response message format. The server responds to a kMsgAttach message
// request with a message of this type.
// 
struct AttachResponseMessage : public BaseResponseMessage
{
public:
    AttachResponseMessage(HRESULT hr);
};

// ---------------------------------------------------------------------------------------
// Static-only class to handle attach request communication and detach functionality
// 
// The target profilee app generally calls functions in ProfilingAPIAttachServer, while
// the trigger process (by way of the AttachProfiler API) generally calls functions in
// ProfilingAPIAttachClient. ProfilingAPIAttachDetach contains functionality common to
// target profilees and triggers, as well as initialization and other routines exposed to
// other parts of the EE.
// 
class ProfilingAPIAttachDetach
{
public:
    // ---------------------------------------------------------------------------------------
    // Indicates whether AttachThread is always available without the need for an event
    // (that the finalizer thread listens to), or whether the AttachThread is only
    // available on demand (when finalizer thread detects the attach event has been
    // signaled).  The mode used by default is determined by the gc mode (server vs.
    // workstation).  But this can be overridden in either case by setting
    // COMPlus_AttachThreadAlwaysOn: 0=kOnDemand, nonzero=kAlwaysOn.
    enum AttachThreadingMode
    {
        // Too early in startup to know the mode yet
        kUninitialized,
        
        // Default GC-workstation mode: AttachThread is only created when the attach
        // event is signaled. AttachThread automatically exits when pipe requests quiet
        // down.
        kOnDemand,
        
        // Default GC-server mode: AttachThread and attach pipe are created on startup,
        // and they never go away. There is no need for an attach event in this mode, so
        // the attach event is never created.
        kAlwaysOn,
    };

    // ---------------------------------------------------------------------------------------
    // Helper class used by both the target profilee app (server) and the trigger process
    // (client) to create and dispose of an OVERLAPPED structure and to use it in a call
    // to the OS API GetOverlappedResult (wrapped via
    // code:ProfilingAPIAttachDetach::OverlappedResultHolder::Wait). The point of having
    // this holder is to encapsulate the code that verifies when the OS is finished with
    // the OVERLAPPED structure (usually when OverlappedResultHolder goes out of scope).
    // See code:ProfilingAPIAttachDetach::OverlappedResultHolder::Wait for details. Since
    // this class derives from NewHolder<OVERLAPPED>, users may automagically cast
    // instances to OVERLAPPED* for use in passing to Windows OS APIs
    class OverlappedResultHolder : public NewHolder<OVERLAPPED>
    {
    public:
        HRESULT Initialize();
        HRESULT Wait(
            DWORD dwMillisecondsMax,
            HANDLE hPipe,
            DWORD * pcbReceived);
    };

    static const UINT kCurrentProcessVersion = 1;
    static const UINT kMinimumAllowableTriggerVersion = 1;
    static const UINT kMinimumAllowableProfileeVersion = 1;

    static DWORD WINAPI ProfilingAPIAttachThreadStart(LPVOID lpParameter);
    static void ProcessSignaledAttachEvent();
    static HANDLE GetAttachEvent();
    static HRESULT Initialize();
    static HRESULT InitSecurityAttributes(SECURITY_ATTRIBUTES * pSecAttrs, DWORD cbSecAttrs);
    static AttachThreadingMode GetAttachThreadingMode();

    static HRESULT GetAttachPipeName(HANDLE hProfileeProcess, SString * pAttachPipeName);
    static void GetAttachPipeNameForPidAndVersion(HANDLE hProfileeProcess, LPCWSTR wszRuntimeVersion, SString * pAttachPipeName);
    static HRESULT GetAttachEventName(HANDLE hProfileeProcess, SString * pAttachEventName);
    static void GetAttachEventNameForPidAndVersion(HANDLE hProfileeProcess, LPCWSTR wszRuntimeVersion, SString * pAttachEventName);
    static HRESULT GetAppContainerNamedObjectPath(HANDLE hProcess, __out_ecount(dwObjectPathSizeInChar) WCHAR * wszObjectPath, DWORD dwObjectPathSizeInChar);
    static BOOL IsAppContainerProcess(HANDLE hProcess);
    
private:
    // This caches the security descriptor to be used when generating the
    // SECURITY_ATTRIBUTES structure for the event and pipe objects.
    // 
    // Technically, this should be freed via LocalFree() or HeapFree (with the current
    // process heap), but there's only one of these per runtime, and it is used
    // throughout the process's lifetime, and there isn't much point to freeing it when
    // the process shuts down since the OS does that automatically.
    static PSECURITY_DESCRIPTOR s_pSecurityDescriptor;

    // HANDLE to event object created on startup and listened to by finalizer thread
    // (when running in code:ProfilingAPIAttachDetach::kOnDemand mode)
    // 
    // Technically, this should be freed via CloseHandle(), but there's only one of these
    // per runtime, and it is used throughout the process's lifetime, and there isn't
    // much point to freeing it when the process shuts down since the OS does that
    // automatically.
    static HANDLE s_hAttachEvent;

    // See code:ProfilingAPIAttachDetach::AttachThreadingMode
    static AttachThreadingMode s_attachThreadingMode;

    static BOOL s_fInitializeCalled;

    // Static-only class.  Private constructor enforces you don't try to make an instance
    ProfilingAPIAttachDetach() {}

    INDEBUG(static void VerifyMessageStructureLayout());
    static void InitializeAttachThreadingMode();
    static HRESULT InitializeForOnDemandMode();
    static HRESULT InitializeForAlwaysOnMode();
    static HRESULT ProfilingAPIAttachThreadMain();
    static void CreateAttachThread();

    static HRESULT GetSecurityDescriptor(PSECURITY_DESCRIPTOR * ppsd);
};

// IClassFactory implementation for ICLRProfiling inteface.
class CLRProfilingClassFactoryImpl : public IUnknownCommon<IClassFactory>
{
public:
    CLRProfilingClassFactoryImpl()
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~CLRProfilingClassFactoryImpl()
    {
        LIMITED_METHOD_CONTRACT;
    }

    //
    // IClassFactory methods
    //
    STDMETHOD(CreateInstance( 
    IUnknown    *pUnkOuter,
    REFIID      riid,
    void        **ppv));

    STDMETHOD(LockServer( 
    BOOL fLock));
};

// CLRProfiling implementation.
class CLRProfilingImpl : public IUnknownCommon<ICLRProfiling>
{
public:
    CLRProfilingImpl()
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~CLRProfilingImpl()
    {
        LIMITED_METHOD_CONTRACT;
    }

    //
    // ICLRProfiling method
    //
    STDMETHOD(AttachProfiler(
        DWORD dwProfileeProcessID,
        DWORD dwMillisecondsMax,
        const CLSID * pClsidProfiler,
        LPCWSTR wszProfilerPath,
        void * pvClientData,
        UINT cbClientData));
};


#endif // __PROF_ATTACH_H__
