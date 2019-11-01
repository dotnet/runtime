// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// ShimDataTarget.h
// 

// 
// header for liveproc data targets
//*****************************************************************************

#ifndef SHIMDATATARGET_H_
#define SHIMDATATARGET_H_


// Function to invoke for 
typedef HRESULT (*FPContinueStatusChanged)(void * pUserData, DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus);


//---------------------------------------------------------------------------------------
// Data target for a live process. This is used by Shim. 
// 
class ShimDataTarget : public ICorDebugMutableDataTarget, ICorDebugDataTarget4
{
public:
    virtual ~ShimDataTarget() {}

    // Allow hooking an implementation for ContinueStatusChanged.
    void HookContinueStatusChanged(FPContinueStatusChanged fpContinueStatusChanged, void * pUserData);

    // Release any resources. Also called by destructor.
    virtual void Dispose() = 0;

    // Set data-target into an error mode. This can be used to mark that the process
    // is unavailable because it's running
    void SetError(HRESULT hr);

    // Get the OS Process ID that this DataTarget is for.
    DWORD GetPid();

    //
    // IUnknown.
    //
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID InterfaceId,
        PVOID* Interface);
    
    virtual ULONG STDMETHODCALLTYPE AddRef();

    virtual ULONG STDMETHODCALLTYPE Release();

    //
    // ICorDebugMutableDataTarget.
    //

    virtual HRESULT STDMETHODCALLTYPE GetPlatform( 
        CorDebugPlatform * pPlatform) = 0;

    virtual HRESULT STDMETHODCALLTYPE ReadVirtual( 
        CORDB_ADDRESS address,
        BYTE * pBuffer,
        ULONG32 request,
        ULONG32 * pcbRead) = 0;

    virtual HRESULT STDMETHODCALLTYPE WriteVirtual( 
        CORDB_ADDRESS address,
        const BYTE * pBuffer,
        ULONG32 request) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextFlags,
        ULONG32 contextSize,
        BYTE * context) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        DWORD dwThreadID,
        ULONG32 contextSize,
        const BYTE * context) = 0;

    virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
        DWORD dwThreadId,
        CORDB_CONTINUE_STATUS dwContinueStatus) = 0;

    // @dbgtodo - add Native Patch Table support

    //
    // ICorDebugDataTarget4
    //    

    // Unwind to the next stack frame
    virtual HRESULT STDMETHODCALLTYPE VirtualUnwind(
        DWORD threadId, ULONG32 contextSize, PBYTE context) = 0;

protected:
    // Pid of the target process.
    DWORD m_processId;

    // If this HRESULT != S_OK, then all interface methods will return this.
    // This provides a way to mark the debugggee as stopped / dead.
    HRESULT m_hr;

    FPContinueStatusChanged m_fpContinueStatusChanged;
    void * m_pContinueStatusChangedUserData;

    // Reference count.
    LONG m_ref;
};

//---------------------------------------------------------------------------------------
//
// Construction method for data-target
//
// Arguments:
//      machineInfo - used for Mac debugging; uniquely identifies the debugger proxy on the remote machine
//      processId - (input) live OS process ID to build a data-target for.
//      ppDataTarget - (output) new data-target instance. This gets addreffed.
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    pid must be for local, same architecture, process.
//    Caller must have security permissions for OpenProcess()
//    Caller must release *ppDataTarget.
//

HRESULT BuildPlatformSpecificDataTarget(MachineInfo machineInfo,
                                        const ProcessDescriptor * pProcessDescriptor,
                                        ShimDataTarget ** ppDataTarget);

#endif //  SHIMDATATARGET_H_

