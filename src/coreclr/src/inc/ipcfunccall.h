// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: IPCFuncCall.h
//
// Define class to support a cross process function call. 
//
//*****************************************************************************


#ifndef _IPCFUNCCALLIMPL_H_
#define _IPCFUNCCALLIMPL_H_

//-----------------------------------------------------------------------------
// 1. Handler creates a IPCFuncCallHandler object and inits it with
// a callback function.
// 2. Source calls IPCFuncCallSource::DoThreadSafeCall(). This will pause the
// thread and trigger the callback on the handlers side.
// 
// This mechanism is very robust. See the error return codes on 
// DoThreadSafeCall() for more details.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Send the call
//-----------------------------------------------------------------------------
class IPCFuncCallSource
{
public:
//.............................................................................
// Error return codes for members.
// Our biggest error concerns are timeouts and no handlers. HRESULTS won't
// help us with these, so we'll have to use our own codes.
//.............................................................................
	enum EError
	{
	// (Common) the function was called, and we waited for the full duration.
		Ok,

	// (Common) The function MAY have been called, but we timed out before it 
	// finished This means either: The function was called, but took too long 
	// to finish or The handler died on us right after we hooked up to it and 
	// so the function never even got called.
		Fail_Timeout_Call,

	// (Common) There was no handler for us to call
		Fail_NoHandler,

	// (rare) The function was never called. We successfully connected to the handler,
	// but we timed out waiting for the mutex.
		Fail_Timeout_Lock,	
			
	// (very rare) We were unable to create the mutex to serialize
		Fail_CreateMutex,

	// (very rare) Catch-all General Failure. 
		Failed
		
	};


// Make a call, wrapped in a mutex
	static EError DoThreadSafeCall();


protected:
	
};


//-----------------------------------------------------------------------------
// AuxThread Callback
//-----------------------------------------------------------------------------
DWORD WINAPI HandlerAuxThreadProc(LPVOID lpParameter);


//-----------------------------------------------------------------------------
// Callback for handler. AuxThread will call this.
//-----------------------------------------------------------------------------
typedef void (*HANDLER_CALLBACK)();

//-----------------------------------------------------------------------------
// Receieves the call. This should be in a different process than the source
//-----------------------------------------------------------------------------
class IPCFuncCallHandler
{
public:
    HRESULT InitFCHandler(HANDLER_CALLBACK pfnCallback, HANDLER_CALLBACK pfnCleanupCallback);
    void TerminateFCHandler();
    void WaitForShutdown();

    IPCFuncCallHandler();
    ~IPCFuncCallHandler();

protected:
    BOOL IsShutdownComplete();
    void SafeCleanup();
    HANDLE m_hStartEnum;	// event to notify start call
    HANDLE m_hDoneEnum;		// event to notify end call

    Volatile<HANDLE> m_hAuxThread;	// thread to listen for m_hStartEnum

    HANDLER_CALLBACK m_pfnCallback;
    HANDLER_CALLBACK m_pfnCleanupCallback;
    
    Volatile<BOOL> m_fShutdownAuxThread; // flag the Aux thread to finish up gracefully
    HANDLE m_hShutdownThread; // Event to signal the Aux thread to finish up gracefully

    HMODULE m_hCallbackModule; // Hold the module's ref to make sure that the
                               // aux thread's code doesn't get unmapped.
// Make auxthread our friend so he can access all our eventing objects
	friend DWORD WINAPI HandlerAuxThreadProc(LPVOID);
};


#endif // _IPCFUNCCALLIMPL_H_
