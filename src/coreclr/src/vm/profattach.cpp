// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttach.cpp
// 

// 
// Definitions of functions that help with attaching and detaching profilers
// 

// ======================================================================================

#include "common.h"

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 

#include <sddl.h>                   // Windows security descriptor language
#include <SecurityUtil.h>
#include "eeprofinterfaces.h"
#include "eetoprofinterfaceimpl.h"
#include "corprof.h"
#include "proftoeeinterfaceimpl.h"
#include "proftoeeinterfaceimpl.inl"
#include "profilinghelper.h"
#include "profilinghelper.inl"
#include "profattach.h"
#include "profattach.inl"
#include "securitywrapper.h"
#include "profattachserver.h"
#include "profattachserver.inl"
#include "profattachclient.h"
#include "profdetach.h"

PSECURITY_DESCRIPTOR ProfilingAPIAttachDetach::s_pSecurityDescriptor = NULL;
HANDLE ProfilingAPIAttachDetach::s_hAttachEvent = NULL;
ProfilingAPIAttachDetach::AttachThreadingMode ProfilingAPIAttachDetach::s_attachThreadingMode = 
    ProfilingAPIAttachDetach::kUninitialized;
BOOL ProfilingAPIAttachDetach::s_fInitializeCalled = FALSE;

// Both the trigger (via code:ProfilingAPIAttachClient) and the target profilee (via
// code:ProfilingAPIAttachServer) use this constant to identify their own version.
const VersionBlock ProfilingAPIAttachDetach::kCurrentProcessVersion(
    VER_MAJORVERSION,
    VER_MINORVERSION,
    VER_PRODUCTBUILD,
    VER_PRODUCTBUILD_QFE);

// Note that the following two VersionBlocks are initialized with static numerals rather
// than using the VER_* preproc defines, as we don't want these VersionBlocks to change
// on us from version to version unless we explicitly make a choice to begin breaking
// compatibility between triggers and profilees (and hopefully we won't need to do this
// ever!).

// A profilee compiled into this mscorwks.dll states that it can only interoperate with
// triggers (i.e., AttachProfiler() implementations (pipe clients)) whose runtime version
// is >= this constant.
// 
// This value should not change as new runtimes are released unless
// code:ProfilingAPIAttachServer is modified to accept newer requests or send newer
// response messages in a way incompatible with older code:ProfilingAPIAttachClient
// objects implementing AttachProfiler(). And that is generally discouraged anyway.
const VersionBlock ProfilingAPIAttachDetach::kMinimumAllowableTriggerVersion(
    4,
    0,
    0,
    0);

// An AttachProfiler() implementation compiled into this mscorwks.dll, and called within
// a trigger process, can only interoperate with target profilee apps (pipe servers)
// whose runtime version is >= this constant.
// 
// This value should not change as new runtimes are released unless
// code:ProfilingAPIAttachClient is modified to send newer request or interpret newer
// response messages in a way incompatible with older code:ProfilingAPIAttachServer
// objects implementing the pipe server. And that is generally discouraged anyway.
const VersionBlock ProfilingAPIAttachDetach::kMinimumAllowableProfileeVersion(
    4,
    0,
    0,
    0);


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::OverlappedResultHolder implementation.  See 
// code:ProfilingAPIAttachDetach::OverlappedResultHolder for more information
//

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::OverlappedResultHolder::Initialize
//
// Description: 
//    Call this first!  This initializes the contained OVERLAPPED structure
//
// Return Value:
//    Returns E_OUTOFMEMORY if OVERLAPPED structure could not be allocated.
//    Else S_OK.
//

HRESULT ProfilingAPIAttachDetach::OverlappedResultHolder::Initialize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    Assign(new (nothrow) OVERLAPPED);
    if (m_value == NULL)
    {
        return E_OUTOFMEMORY;
    }

    memset(m_value, 0, sizeof(OVERLAPPED));
    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::OverlappedResultHolder::Wait
// 
// Description:
//    Uses the contained OVERLAPPED structure (pointed to by m_value) to call
//    WaitForSingleObject to wait for an overlapped read or write on the pipe to complete
//    (or timeout).
//    
// Arguments:
//    * dwMillisecondsMax - [in] Timeout for the wait
//    * hPipe - [in] Handle to the pipe object carrying out the request (may be either a
//        server or client pipe handle).
//    * pcbReceived - [out] Number of bytes received from the overlapped request
//        
// Return Value:
//    HRESULT indicating success or failure
//    
// Assumptions:
//    * Must call code:ProfilingAPIAttachDetach::OverlappedResultHolder::Initialize first

HRESULT ProfilingAPIAttachDetach::OverlappedResultHolder::Wait(
    DWORD dwMillisecondsMax,
    HANDLE hPipe,
    DWORD * pcbReceived)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(hPipe));
    _ASSERTE(m_value != NULL);
    _ASSERTE(pcbReceived != NULL);

    HRESULT hr = E_UNEXPECTED;

    // Since the OVERLAPPED structure referenced by m_value contains a NULL event, the OS
    // will signal hPipe itself when the operation is complete
    switch (WaitForSingleObject(hPipe, dwMillisecondsMax))
    {
    default:
        _ASSERTE(!"Unexpected return from WaitForSingleObject()");
        hr = E_UNEXPECTED;
        break;

    case WAIT_FAILED:
        hr = HRESULT_FROM_GetLastError();
        break;

    case WAIT_TIMEOUT:
        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        break;

    case WAIT_OBJECT_0:
        // Operation finished in time.  Get the results
        if (!GetOverlappedResult(
            hPipe,
            m_value,
            pcbReceived,
            TRUE))        // bWait: operation is done, so this returns immediately anyway
        {
            hr = HRESULT_FROM_GetLastError();
        }
        else
        {
            hr = S_OK;
        }
        break;
    }

    // The gymnastics below are to ensure that Windows is done with the overlapped
    // structure, so we know it's safe to allow the base class (NewHolder) to free it
    // when the destructor is called.

    if (SUCCEEDED(hr))
    {
        // Operation successful, so we're done with the OVERLAPPED structure pointed to
        // by m_value and may return
        return hr;
    }

    _ASSERTE(FAILED(hr));

    // There was a failure waiting for or retrieving the result. Cancel the operation and
    // wait again for verification that the operation is completed or canceled.

    // Note that we're ignoring whether CancelIo succeeds or fails, as our action is the
    // same either way:  Wait on the pipe again to verify that no active operation remains.
    CancelIo(hPipe);
    
    if (WaitForSingleObject(hPipe, dwMillisecondsMax) == WAIT_OBJECT_0)
    {
        // Typical case: The wait returns successfully and quickly, so we have
        // verification that the OVERLAPPED structured pointed to by m_value is done
        // being used.
        return hr;
    }

    // Atypical case: For all our trying, we're unable to force this request to end
    // before returning. Therefore, we're intentionally leaking the OVERLAPPED structured
    // pointed to by m_value, as Windows may write to it at a later time.
    SuppressRelease();
    return hr;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::ProfilingAPIAttachThreadStart
//
// Description: 
//    Thread proc for AttachThread.  Serves as simple try/catch wrapper around
//    ProfilingAPIAttachThreadMain
//
// Arguments:
//    * LPVOID thread proc param is ignored
//
// Return Value:
//    Just returns 0 always.
//

// static
DWORD WINAPI ProfilingAPIAttachDetach::ProfilingAPIAttachThreadStart(LPVOID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // At start of this thread, set its type so SOS !threads and anyone else knows who we
    // are.
    ClrFlsSetThreadType(ThreadType_ProfAPI_Attach);
    
    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: AttachThread created and executing.\n"));

    // This try block is a last-ditch stop-gap to prevent an unhandled exception on the
    // AttachThread from bringing down the process.  Note that if the unhandled
    // exception is a terminal one, then hey, sure, let's tear everything down.  Also
    // note that any naughtiness in the profiler (e.g., throwing an exception from its
    // Initialize callback) should already be handled before we pop back to here, so this
    // is just being super paranoid.
    EX_TRY
    {
        // Don't care about return value, thread proc will just return 0 regardless
        ProfilingAPIAttachThreadMain();
    }
    EX_CATCH
    {
        _ASSERTE(!"Unhandled exception on profiling API attach / detach thread");
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: AttachThread exiting.\n"));

    return 0;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::ProfilingAPIAttachThreadMain
//
// Description: 
//    Main code for AttachThread.  Includes all attach functionality.
//
// Return Value:
//    S_OK if a profiler ever attached, error HRESULT otherwise
//

// static
HRESULT ProfilingAPIAttachDetach::ProfilingAPIAttachThreadMain()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr;

    ProfilingAPIAttachServer attachServer;
    hr = attachServer.ExecutePipeRequests();
    if (FAILED(hr))
    {
        // No profiler got attached, so we're done
        return hr;
    }

    // If we made it here, a profiler was successfully attached. It would be nice to be
    // able to assert g_profControlBlock.curProfStatus.Get() == kProfStatusActive, but
    // that's prone to a theoretical race: the profiler might have attached and detached
    // by the time we get here.

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::InitSecurityAttributes
//
// Description: 
//    Initializes a SECURITY_ATTRIBUTES struct using the result of
//    code:ProfilingAPIAttachDetach::GetSecurityDescriptor
//
// Arguments:
//    * pSecAttrs - [in/out] SECURITY_ATTRIBUTES struct to initialize
//    * cbSecAttrs - Size in bytes of *pSecAttrs
//
// Return Value:
//    HRESULT indicating success or failure
//

// static
HRESULT ProfilingAPIAttachDetach::InitSecurityAttributes(
    SECURITY_ATTRIBUTES * pSecAttrs,
    DWORD cbSecAttrs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    PSECURITY_DESCRIPTOR psd = NULL;
    HRESULT hr = GetSecurityDescriptor(&psd);
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(psd != NULL);
    memset(pSecAttrs, 0, cbSecAttrs);
    pSecAttrs->nLength = cbSecAttrs;
    pSecAttrs->lpSecurityDescriptor = psd;
    pSecAttrs->bInheritHandle = FALSE;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper function that gets the string (SDDL) form of the mandatory SID for this
// process. This encodes the integrity level of the process for use in security
// descriptors. The integrity level is capped at "high". See code:#HighGoodEnough.
//
// Arguments:
//      * pwszIntegritySidString - [out] On return will point to a buffer allocated by
//          Windows that contains the string representation of the SID. If
//          GetIntegritySidString succeeds, the caller is responsible for freeing
//          *pwszIntegritySidString via LocalFree().
//
// Return Value:
//      HRESULT indicating success or failure.
//
//

static HRESULT GetIntegritySidString(__out LPWSTR * pwszIntegritySidString)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr;
    _ASSERTE(pwszIntegritySidString != NULL);

    NewArrayHolder<BYTE> pbLabel;

    // This grabs the mandatory label SID of the current process.  We will write this
    // SID into the security descriptor, to ensure that triggers of lower integrity
    // levels may NOT access the object... with one exception.  See code:#HighGoodEnough
    hr = SecurityUtil::GetMandatoryLabelFromProcess(GetCurrentProcess(), &pbLabel);
    if (FAILED(hr))
    { 
        return hr;
    }

    TOKEN_MANDATORY_LABEL * ptml = (TOKEN_MANDATORY_LABEL *) pbLabel.GetValue();

    // #HighGoodEnough:
    // The mandatory label SID we write into the security descriptor is the same as that
    // of the current process, with one exception. If the current process's integrity
    // level > high (e.g., ASP.NET running at "system" integrity level), then write
    // "high" into the security descriptor instead of the current process's actual
    // integrity level. This allows a high integrity trigger to access the object. This
    // implements the policy that a high integrity level is "good enough" to profile any
    // process, even if the target process is at an even higher integrity level than
    // "high". Why have this policy:
    //     * A high integrity process represents an elevated admin, which morally equates
    //         to a principal that should have complete control over the machine. This
    //         includes debugging or profiling any process.
    //     * According to a security expert dev on Windows, integrity level is not a
    //         "security feature". It's mainly useful as defense-in-depth or to protect
    //         IE users and admins from themselves in most cases.
    //     * It's impossible to spawn a system integrity trigger process outside of
    //         session 0 services. So profiling ASP.NET would be crazy hard without this
    //         policy.
    DWORD * pdwIntegrityLevel = SecurityUtil::GetIntegrityLevelFromMandatorySID(ptml->Label.Sid);
    if (*pdwIntegrityLevel > SECURITY_MANDATORY_HIGH_RID)
    {
        *pdwIntegrityLevel = SECURITY_MANDATORY_HIGH_RID;
    }
    
    if (!ConvertSidToStringSid(ptml->Label.Sid, pwszIntegritySidString))
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::GetSecurityDescriptor
// 
// Description:
//    Generates a security descriptor based on an ACL containing (1) an ACE that allows
//    the current user read / write and (2) an ACE that allows admins read / write.
//    Resulting security descriptor is returned in an [out] param, and is also cached for
//    future use.
//    
// Arguments:
//    * ppsd - [out] Generated (or cached) security descriptor
//        
// Return Value:
//    HRESULT indicating success or failure.
//    

// static
HRESULT ProfilingAPIAttachDetach::GetSecurityDescriptor(PSECURITY_DESCRIPTOR * ppsd)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(ppsd != NULL);

    if (s_pSecurityDescriptor != NULL)
    {
        *ppsd = s_pSecurityDescriptor;
        return S_OK;
    }

    // Get the user SID for the DACL

    PSID psidUser = NULL;
    HRESULT hr = ProfilingAPIUtility::GetCurrentProcessUserSid(&psidUser);
    if (FAILED(hr))
    {
        return hr;
    }

    WinAllocatedBlockHolder pvCurrentUserSidString;

    if (!ConvertSidToStringSid(psidUser, (LPWSTR *)(LPVOID *) &pvCurrentUserSidString))
    {
        return HRESULT_FROM_GetLastError();
    }

    // Get the integrity / mandatory SID for the SACL, if Vista+

    LPCWSTR pwszIntegritySid = NULL;
    WinAllocatedBlockHolder pvIntegritySidString;

    hr = GetIntegritySidString((LPWSTR *) (LPVOID *) &pvIntegritySidString);
    if (FAILED(hr))
    {
        return hr;
    }
    pwszIntegritySid = (LPCWSTR) pvIntegritySidString.GetValue();

    ULONG cbsd;
    StackSString sddlSecurityDescriptor;
    WinAllocatedBlockHolder pvSecurityDescriptor;

    // The following API (ConvertStringSecurityDescriptorToSecurityDescriptorW) takes a
    // string representation of a security descriptor (using the SDDL language), and
    // returns back the security descriptor object to be used when defining the globally
    // named event or pipe object. For a description of this language, go to the help on
    // the API, and click on "string-format security descriptor":
    // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthz/security/security_descriptor_string_format.asp
    // or look through sddl.h.
    
    // Cheat sheet for the subset of the format that we're using:
    //
    // Security Descriptor string:
    //     D:dacl_flags(string_ace1)(string_ace2)... (string_acen)
    // Security SACL string:
    //     S:sacl_flags(string_ace1)(string_ace2)... (string_acen)
    // Each string_ace: 
    //     ace_type;ace_flags;rights;object_guid;inherit_object_guid;account_sid
    //
    // The following portions of the security descriptor string are NOT used:
    //     O:owner_sid (b/c we want current user to be the owner)
    //     G:group_sid (b/c not setting the primary group of the object)

    // This reusable chunk defines the "(string_ace)" portion of the DACL.  Given
    // a SID, this makes an ACE for the SID with GENERIC_READ | GENERIC_WRITE access
    #define ACE_STRING(AccountSidString)                                                    \
                                                                                            \
        SDDL_ACE_BEGIN                                                                      \
                                                                                            \
            /* ace_type: "A;" An "allow" DACL (not "deny")                            */    \
            SDDL_ACCESS_ALLOWED SDDL_SEPERATOR                                              \
                                                                                            \
            /* (skipping ace_flags, so that no child auto-inherits from this object)  */    \
            SDDL_SEPERATOR                                                                  \
                                                                                            \
            /* rights: "GRGW": GENERIC_READ | GENERIC_WRITE access allowed            */    \
            SDDL_GENERIC_READ SDDL_GENERIC_WRITE SDDL_SEPERATOR                             \
                                                                                            \
            /* (skipping object_guid)                                                 */    \
            SDDL_SEPERATOR                                                                  \
                                                                                            \
            /* (skipping inherit_object_guid)                                         */    \
            SDDL_SEPERATOR                                                                  \
                                                                                            \
            /* account_sid (filled in by macro user)                                  */    \
            AccountSidString                                                                \
                                                                                            \
        SDDL_ACE_END


    // First, construct the DACL

    sddlSecurityDescriptor.Printf(
        // "D:" This is a DACL
        SDDL_DACL SDDL_DELIMINATOR
        
        // dacl_flags:

        // "P" This is protected (i.e., don't allow security descriptor to be modified
        // by inheritable ACEs)
        SDDL_PROTECTED                                  

        // (string_ace1)
        // account_sid: "BA" built-in local administrators group
        ACE_STRING(SDDL_BUILTIN_ADMINISTRATORS)

        // (string_ace2)
        // account_sid: to be filled in with the current process token's primary SID
        ACE_STRING(W("%s")),

        // current process token's primary SID
        (LPCWSTR) (LPVOID) pvCurrentUserSidString);

    // Next, add the SACL (Vista+ only)

    if (pwszIntegritySid != NULL)
    {
        sddlSecurityDescriptor.AppendPrintf(
            // "S:" This is a SACL -- for the integrity level of the current process
            SDDL_SACL SDDL_DELIMINATOR

            // The SACL ACE begins here
            SDDL_ACE_BEGIN                                                                
                             
                // ace_type: "ML;"  A Mandatory Label ACE (i.e., integrity level)
                SDDL_MANDATORY_LABEL SDDL_SEPERATOR
                                                                                          
                // (skipping ace_flags, so that no child auto-inherits from this object)
                SDDL_SEPERATOR                                                            
                                                 
                // rights: "NWNR;" If the trigger's integrity level is lower than the
                // integrity level we're writing into this security descriptor, then that
                // trigger may not read or write to this object.
                SDDL_NO_WRITE_UP SDDL_NO_READ_UP SDDL_SEPERATOR                       
                                                                                          
                // (skipping object_guid)
                SDDL_SEPERATOR                                                            
                     
                // (skipping inherit_object_guid)
                SDDL_SEPERATOR

                // To be filled in with the current process's mandatory label SID (which
                // describes the current process's integrity level, capped at "high integrity")
                W("%s")
                                                                                          
            SDDL_ACE_END,
        
            // current process's mandatory label SID
            pwszIntegritySid);
    }
        
    if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(
        sddlSecurityDescriptor.GetUnicode(),
        SDDL_REVISION_1,
        (PSECURITY_DESCRIPTOR *) (LPVOID *) &pvSecurityDescriptor,
        &cbsd))
    {
        return HRESULT_FROM_GetLastError();
    }

    if (FastInterlockCompareExchangePointer(
        &s_pSecurityDescriptor, 
        (PSECURITY_DESCRIPTOR) pvSecurityDescriptor, 
        NULL) == NULL)
    {
        // Ownership transferred to s_pSecurityDescriptor, so don't free it here
        pvSecurityDescriptor.SuppressRelease();
    }

    _ASSERTE(s_pSecurityDescriptor != NULL);
    *ppsd = s_pSecurityDescriptor;
    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::Initialize
// 
// Description:
//    Perform startup (one-time-only) initialization for attach / detach infrastructure.
//    This includes the Global Attach Event, but does NOT include the Global Attach Pipe
//    (which is created only on demand).  This is lazily called the first time the
//    finalizer asks for the attach event.
//    
// Return Value:
//    S_OK:    Attach / detach infrastructure initialized ok
//    S_FALSE: Attach / detach infrastructure not initialized, but for an acceptable reason
//             (e.g., executing memory- or sync- hosted)
//    else:    error HRESULT indicating an unacceptable failure that prevented attach /
//             detach infrastructure from initializing (e.g., security problem, OOM, etc.)
//    
// Assumptions:
//    * By the time this is called:
//         * Configuration must have been read from the registry
//         * If there is a host, it has already initialized its state, including its
//             intent to memory-host or sync-host.
//         * Finalizer thread is initializing and is first asking for the attach event.
//             

// static
HRESULT ProfilingAPIAttachDetach::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // This one assert verifies two things:
    //     * 1. Configuration has been read from the registry, AND
    //     * 2. If there is a host, it has already initialized its state.
    // #2 is implied by this assert, because the host initializes its state before
    // EEStartup is even called: Host directly calls CorHost2::SetHostControl to
    // initialize itself, announce whether the CLR will be memory hosted, sync hosted,
    // etc., and then host calls CorHost2::Start, which calls EEStartup, which
    // initializes configuration information. So if configuration information is
    // available, the host must have already initialized itself.
    // 
    // The reason we care is that, for profiling API attach to be enabled during this
    // run, we need to have the finalizer thread wait on multiple sync objects. And
    // waiting on multiple objects is disallowed if we're memory / sync-hosted. So we
    // need to know now whether waiting on multiple objects is allowed, so we know
    // whether we can initialize the Attach support objects.
    _ASSERTE(g_pConfig != NULL);

    // Even if we fail to create the event, this BOOL indicates we at least
    // tried to.
    _ASSERTE(!s_fInitializeCalled);
    s_fInitializeCalled = TRUE;

    INDEBUG(VerifyMessageStructureLayout());

    // If the CLR is being memory- or sync-hosted, then attach is not supported
    // (see comments above)
    if (CLRMemoryHosted() || CLRSyncHosted())
    {
        LOG((
            LF_CORPROF, 
            LL_INFO10, 
            "**PROF: Process is running with a host that implements custom memory or "
                "synchronization management.  So it will not be possible to attach a "
                "profiler to this process.\n"));

        // NOTE: Intentionally not logging this to the event log, as it would be
        // obnoxious to see such a message every time SQL started up

        return S_FALSE;
    }

    InitializeAttachThreadingMode();

    if (s_attachThreadingMode == kOnDemand)
    {
        return InitializeForOnDemandMode();
    }

    _ASSERTE(s_attachThreadingMode == kAlwaysOn);
    return InitializeForAlwaysOnMode();
}

#ifdef _DEBUG

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::VerifyMessageStructureLayout
//
// Description: 
//    Debug-only function that asserts if there appear to be changes to structures that
//    are not allowed to change (for backward-compatibility reasons).  In particular:
//         * VersionBlock must not change
//         * BaseRequestMessage must not change
//

// static
void ProfilingAPIAttachDetach::VerifyMessageStructureLayout()
{
    LIMITED_METHOD_CONTRACT;

    // If any of these asserts fire, then VersionBlock is changing its binary
    // layout in an incompatible way. Bad!
    _ASSERTE(sizeof(VersionBlock) == 16);
    _ASSERTE(offsetof(VersionBlock, m_dwMajor) == 0);
    _ASSERTE(offsetof(VersionBlock, m_dwMinor) == 4);
    _ASSERTE(offsetof(VersionBlock, m_dwBuild) == 8);
    _ASSERTE(offsetof(VersionBlock, m_dwQFE) == 12);

    // If any of these asserts fire, then GetVersionRequestMessage is changing its binary
    // layout in an incompatible way. Bad!
    _ASSERTE(sizeof(GetVersionRequestMessage) == 8);
    _ASSERTE(offsetof(GetVersionRequestMessage, m_cbMessage) == 0);
    _ASSERTE(offsetof(GetVersionRequestMessage, m_requestMessageType) == 4);

    // If any of these asserts fire, then GetVersionResponseMessage is changing its binary
    // layout in an incompatible way. Bad!
    _ASSERTE(sizeof(GetVersionResponseMessage) == 36);
    _ASSERTE(offsetof(GetVersionResponseMessage, m_hr) == 0);
    _ASSERTE(offsetof(GetVersionResponseMessage, m_profileeVersion) == 4);
    _ASSERTE(offsetof(GetVersionResponseMessage, m_minimumAllowableTriggerVersion) == 20);
}

#endif //_DEBUG

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::InitializeAttachThreadingMode
//
// Description: 
//    Looks at environment and GC mode to determine whether the AttachThread should
//    always be around, or created only on demand.  See
//    code:ProfilingAPIAttachDetach::AttachThreadingMode.
//

// static
void ProfilingAPIAttachDetach::InitializeAttachThreadingMode()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(s_attachThreadingMode == kUninitialized);

    // Environment variable trumps all, so check it first
    DWORD dwAlwaysOn = g_pConfig->GetConfigDWORD_DontUse_(
        CLRConfig::EXTERNAL_AttachThreadAlwaysOn,
        GCHeap::IsServerHeap() ? 1 : 0);      // Default depends on GC server mode

    if (dwAlwaysOn == 0)
    {
        s_attachThreadingMode = kOnDemand;
    }
    else
    {
        s_attachThreadingMode = kAlwaysOn;
    }
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::InitializeForAlwaysOnMode
//
// Description: 
//    Performs initialization specific to running in Always On mode.  Specifically, this
//    means creating the AttachThread.  The attach event is not created in this case.
//    
// Return Value:
//    HRESULT indicating success or failure.
//

// static
HRESULT ProfilingAPIAttachDetach::InitializeForAlwaysOnMode()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(s_attachThreadingMode == kAlwaysOn);

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Attach AlwaysOn mode invoked; creating new AttachThread.\n"));

    CreateAttachThread();

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::InitializeForOnDemandMode
//
// Description:
//    Performs initialization specific to running in On Demand mode. Specifically, this
//    means creating the attach event. (The AttachThread will only be created when this
//    event is signaled by a trigger process.)
//
// Return Value:
//    HRESULT indicating success or failure.
//

// static
HRESULT ProfilingAPIAttachDetach::InitializeForOnDemandMode()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(s_attachThreadingMode == kOnDemand);

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Attach OnDemand mode invoked; creating attach event.\n"));

    // The only part of attach that gets initialized before a profiler has
    // actually requested to attach is the single global event that gets
    // signaled from out-of-process.

    StackSString attachEventName;
    HRESULT hr;
    hr = GetAttachEventName(::GetCurrentProcess(), &attachEventName);
    if (FAILED(hr))
    {
        return hr;
    }

    // Deliberately NOT using CLREvent, as it does not have support for a global name.
    // It's ok not to use CLREvent, as we're assured above that we're not sync-hosted,
    // which means CLREvent would just use raw Windows events anyway.

    SECURITY_ATTRIBUTES *psa = NULL;
    
    SECURITY_ATTRIBUTES sa;

    // Only assign security attributes for non-app container scenario
    // We are assuming the default (blocking everything for app container scenario is good enough
    if (!IsAppContainerProcess(::GetCurrentProcess()))
    {
        hr = InitSecurityAttributes(&sa, sizeof(sa));
        if (FAILED(hr))
        {
            return hr;
        }

        psa = &sa;    
    }
    
    _ASSERTE(s_hAttachEvent == NULL);
    s_hAttachEvent = WszCreateEvent(
        psa,                            // security attributes
        FALSE,                          // bManualReset = FALSE: autoreset after waiting thread is unblocked
        FALSE,                          // initial state = FALSE, i.e., unsignaled
        attachEventName.GetUnicode()    // Global name seen out-of-proc
        );
    if (s_hAttachEvent == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::GetAttachEvent
// 
// Description:
//    Used by finalizer thread to get the profiling API attach event. First time this is
//    called, the event and other supporting objects will be created.
//
// Return Value:
//    The attach event or NULL if attach event creation failed during startup. In either
//    case, do NOT call CloseHandle on the returned event handle.
//    
// Assumptions:
//    * ProfilingAPIUtility::InitializeProfiling should already have been called before
//        this is called.  That ensures that, if a profiler was configured to load on
//        startup, then that load has already occurred by now.
//    * The event's HANDLE refcount is managed solely by ProfilingAPIAttachDetach. So do
//        not call CloseHandle() on the HANDLE returned.
//        
// Notes:
//    * If the attach event was not created on startup, then this will return NULL.
//        Possible reasons why this can occur:
//        * The current process is the NGEN service, OR
//        * The process is sync- or memory- hosted, OR
//        * Attach is running in "always on" mode, meaning we always have an AttachThread
//            with a pipe, so there's no need for an event.
//        

// static
HANDLE ProfilingAPIAttachDetach::GetAttachEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (IsCompilationProcess())
    {
        // No profiler attach on NGEN!
        return NULL;
    }

    if (!s_fInitializeCalled)
    {
        // If a profiler was supposed to load on startup, it's already happened
        // now.  So it's safe to set up the attach support objects, and allow
        // an attaching profiler to make an attempt (which can now gracefully fail
        // if a startup profiler has loaded).
        
        HRESULT hr = Initialize();
        if (FAILED(hr))
        {
            LOG((
                LF_CORPROF, 
                LL_ERROR, 
                "**PROF: ProfilingAPIAttachDetach::Initialize failed, so this process will not "
                    "be able to attach a profiler. hr=0x%x.\n",
                hr));
            ProfilingAPIUtility::LogProfError(IDS_E_PROF_ATTACH_INIT, hr);

            return NULL;
        }
    }

    if (s_attachThreadingMode == kAlwaysOn)
    {
        // In always-on mode, we always have an AttachThread listening on the pipe, so
        // there's no need for an event.
        _ASSERTE(s_hAttachEvent == NULL);
    }

    return s_hAttachEvent;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::ProcessSignaledAttachEvent
//
// Description: 
//    Called by finalizer thread when the finalizer thread detects that the globally
//    named Profiler Attach Event is signaled.  This simply spins up the AttachThread
//    (starting in ProfilingAPIAttachThreadStart) and returns.
//

// static
void ProfilingAPIAttachDetach::ProcessSignaledAttachEvent()
{
    // This function is practically a leaf (though not quite), and is called from the
    // finalizer thread at various points, so keeping the contract strict to allow for
    // maximum flexibility on when this may called.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Attach event signaled; creating new AttachThread.\n"));

    CreateAttachThread();
}

typedef BOOL
(WINAPI *PFN_GetAppContainerNamedObjectPath)(
    HANDLE Token,
    PSID AppContainerSid,
    ULONG ObjectPathLength,
    WCHAR * ObjectPath,
    PULONG ReturnLength
    ); 

static Volatile<PFN_GetAppContainerNamedObjectPath> g_pfnGetAppContainerNamedObjectPath = NULL;

// ----------------------------------------------------------------------------
// GetAppContainerNamedObjectPath
//
// Description: 
//    Retrieve named object path for the specified app container process
//    The name looks something like the following:
//        LowBoxNamedObjects\<AppContainer_SID>
//    AppContainer_SID is the SID for the app container, for example: S-1-15-2-3-4-5-6-7-8
//
// Arguments:
//    * hProcess - handle of the app container proces
//    * wszObjectPath - [out] Buffer to fill in
//    * dwObjectPathSizeInChar - Size of buffer
//
HRESULT ProfilingAPIAttachDetach::GetAppContainerNamedObjectPath(HANDLE hProcess, __out_ecount(dwObjectPathSizeInChar) WCHAR * wszObjectPath, DWORD dwObjectPathSizeInChar)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(wszObjectPath != NULL);
    
    HandleHolder hToken;
    
    if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        return HRESULT_FROM_GetLastError();
    }
    
    if (g_pfnGetAppContainerNamedObjectPath.Load() == NULL)
    {
        HMODULE hMod = WszGetModuleHandle(W("kernel32.dll"));
        if (hMod == NULL)
        {
            // This should never happen but I'm checking it anyway
            return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }
        
        PFN_GetAppContainerNamedObjectPath pfnGetAppContainerNamedObjectPath = (PFN_GetAppContainerNamedObjectPath) 
            ::GetProcAddress(
                hMod,
                "GetAppContainerNamedObjectPath");
        
        if (!pfnGetAppContainerNamedObjectPath)
        {            

            return HRESULT_FROM_GetLastError();
        }

        // We should always get the same address back from GetProcAddress so there is no concern for race condition
        g_pfnGetAppContainerNamedObjectPath = pfnGetAppContainerNamedObjectPath;
    }
    
    DWORD dwBufferLength;
    if (!g_pfnGetAppContainerNamedObjectPath(        
        hToken,                                 // Process token
        NULL,                                   // AppContainer package SID optional.        
        dwObjectPathSizeInChar,                 // Object path length        
        wszObjectPath,                          // Object path        
        &dwBufferLength                         // return length
        )) 
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}


// @TODO: Update this once Windows header file is updated to Win8
#ifndef TokenIsAppContainer
    #define TokenIsAppContainer ((TOKEN_INFORMATION_CLASS) 29)
#endif

// ----------------------------------------------------------------------------
// ProfilingAPIAttachDetach::IsAppContainerProcess
//
// Description: 
//    Return whether the specified process is a app container process
//

// static
BOOL ProfilingAPIAttachDetach::IsAppContainerProcess(HANDLE hProcess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    HandleHolder hToken;
    
    if(!::OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        return FALSE;
    }

    BOOL fIsAppContainerProcess;
    DWORD dwReturnLength;
    if (!::GetTokenInformation(
            hToken, 
            TokenIsAppContainer, 
            &fIsAppContainerProcess, 
            sizeof(BOOL), 
            &dwReturnLength) ||
        dwReturnLength != sizeof(BOOL))
    {
        return FALSE;
    }
    else
    {
        return fIsAppContainerProcess;
    }
}

//---------------------------------------------------------------------------------------
//
// Called by other points in the runtime (e.g., finalizer thread) to create a new thread
// to fill the role of the AttachThread.
//

// static
void ProfilingAPIAttachDetach::CreateAttachThread()
{
    // This function is practically a leaf (though not quite), and is called from the
    // finalizer thread at various points, so keeping the contract strict to allow for
    // maximum flexibility on when this may called.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    HandleHolder hAttachThread;

    // The AttachThread is intentionally not an EE Thread-object thread
    hAttachThread = ::CreateThread(
        NULL,       // lpThreadAttributes; don't want child processes inheriting this handle
        0,          // dwStackSize (0 = use default)
        ProfilingAPIAttachThreadStart,
        NULL,       // lpParameter (none to pass)
        0,          // dwCreationFlags (0 = use default flags, start thread immediately)
        NULL        // lpThreadId (don't need therad ID)
        );
    if (hAttachThread == NULL)
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Failed to create AttachThread.  GetLastError=%d.\n",
            GetLastError()));

        // No other error-specific code really makes much sense here. An error here is
        // probably due to serious OOM issues which would also probably prevent logging
        // an event. A trigger process will report that it waited for the pipe to be
        // created, and timed out during the wait. That should be enough for the user.
    }
}

// ----------------------------------------------------------------------------
// CLRProfilingClassFactoryImpl::CreateInstance
// 
// Description:
//    A standard IClassFactory interface function to allow a profiling trigger 
//    to query for IID_ICLRProfiling interface
// 
HRESULT CLRProfilingClassFactoryImpl::CreateInstance(IUnknown * pUnkOuter, REFIID riid, void ** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    if (ppv == NULL)
        return E_POINTER;

    *ppv = NULL;

    NewHolder<CLRProfilingImpl> pProfilingImpl = new (nothrow) CLRProfilingImpl();
    if (pProfilingImpl == NULL)
        return E_OUTOFMEMORY;

    HRESULT hr = pProfilingImpl->QueryInterface(riid, ppv);
    if (SUCCEEDED(hr))
    {
        pProfilingImpl.SuppressRelease();
    }

    return hr;
}

// ----------------------------------------------------------------------------
// CLRProfilingClassFactoryImpl::LockServer
// 
// Description:
//    A standard IClassFactory interface function that doesn't do anything interesting here
// 
HRESULT CLRProfilingClassFactoryImpl::LockServer(BOOL fLock)
{
    LIMITED_METHOD_CONTRACT;

    return S_OK;
}

// ----------------------------------------------------------------------------
// CLRProfilingImpl::AttachProfiler
// 
// Description:
//    A wrapper COM function to invoke AttachProfiler with parameters from 
//    profiling trigger along with a runtime version string
// 
HRESULT CLRProfilingImpl::AttachProfiler(DWORD dwProfileeProcessID, 
                                         DWORD dwMillisecondsMax, 
                                         const CLSID *pClsidProfiler, 
                                         LPCWSTR wszProfilerPath, 
                                         void *pvClientData, 
                                         UINT cbClientData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    WCHAR wszRuntimeVersion[MAX_PATH_FNAME];
    DWORD dwSize = _countof(wszRuntimeVersion); 
    HRESULT hr = GetCORVersionInternal(wszRuntimeVersion, dwSize, &dwSize);
    if (FAILED(hr))
        return hr;

    return ::AttachProfiler(dwProfileeProcessID,
                            dwMillisecondsMax,
                            pClsidProfiler,
                            wszProfilerPath,
                            pvClientData,
                            cbClientData,
                            wszRuntimeVersion);
}

// ----------------------------------------------------------------------------
// ICLRProfilingGetClassObject
// 
// Description:
//    A wrapper to create a CLRProfilingImpl object and to QueryInterface on the CLRProfilingImpl object
// 
HRESULT ICLRProfilingGetClassObject(REFCLSID rclsid, REFIID riid, void **ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        SO_NOT_MAINLINE;
        PRECONDITION(rclsid == CLSID_CLRProfiling);
    }
    CONTRACTL_END;

    if (ppv == NULL)
        return E_POINTER;

    *ppv = NULL;

    NewHolder<CLRProfilingClassFactoryImpl> pCLRProfilingClassFactoryImpl = new (nothrow) CLRProfilingClassFactoryImpl();
    if (pCLRProfilingClassFactoryImpl == NULL)
        return E_OUTOFMEMORY;

    HRESULT hr = pCLRProfilingClassFactoryImpl->QueryInterface(riid, ppv);
    if (SUCCEEDED(hr))
    {
        pCLRProfilingClassFactoryImpl.SuppressRelease();
    }

    return hr;
}


#endif // FEATURE_PROFAPI_ATTACH_DETACH 
