// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CEEMAIN.H
//

//
//

// CEEMAIN.H defines the entrypoints into the Virtual Execution Engine and
// gets the load/run process going.
// ===========================================================================

#ifndef CEEMain_H
#define CEEMain_H

#include <windef.h> // for HFILE, HANDLE, HMODULE

class EEDbgInterfaceImpl;

// Ensure the EE is started up.
HRESULT EnsureEEStarted();

// Enum to control what happens at the end of EE shutdown. There are two options:
// 1. Call ::ExitProcess to cause the process to terminate gracefully. This is how
//    shutdown normally ends. "Shutdown" methods that take this action as an argument
//    do not return when SCA_ExitProcessWhenShutdownComplete is passed.
//
// 2. Terminate process and generate a dump if enabled.
//
// 3. Return after performing all shutdown processing. This is a special case used
//    by a shutdown initiated via the Shim, and is used to ensure that all runtimes
//    loaded SxS are shutdown gracefully. "Shutdown" methods that take this action
//    as an argument return when SCA_ReturnWhenShutdownComplete is passed.
enum ShutdownCompleteAction
{
    SCA_ExitProcessWhenShutdownComplete,
    SCA_TerminateProcessWhenShutdownComplete,
    SCA_ReturnWhenShutdownComplete
};

// Force shutdown of the EE
void ForceEEShutdown(ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete);

// Notification of a DLL_THREAD_DETACH or a Thread Terminate.
void ThreadDetaching();

void EnsureTlsDestructionMonitor();

void DeleteThreadLocalMemory();

void SetLatchedExitCode (INT32 code);
INT32 GetLatchedExitCode (void);

// Tells whether the garbage collector is fully initialized
// Stronger than IsGCHeapInitialized
BOOL IsGarbageCollectorFullyInitialized();

// Specifies whether coreclr is embedded or standalone
extern bool g_coreclr_embedded;

// Specifies whether hostpolicy is embedded in executable or standalone
extern bool g_hostpolicy_embedded;

#endif
