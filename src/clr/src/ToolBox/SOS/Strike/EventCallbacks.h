// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EventCallbacks__
#define __EventCallbacks__

#include "exts.h"

// A set of callbacks that are registered with windbg whenever SOS is loaded
// Right now these callbacks only act on the module load event for CLR, but
// feel free to add other event hooks as needed
//
// TODO: we should probably be using these callbacks to hook clrnotify exceptions
// rather than attaching a user handler on the clrn event. That handler is both
// visible to the user and could be accidentally erased by them.
class EventCallbacks : IDebugEventCallbacks
{
public:
    EventCallbacks(IDebugClient* pDebugClient);
    ~EventCallbacks();

    // IUnknown implementation
    HRESULT __stdcall QueryInterface(REFIID riid, VOID** ppInterface);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();
    
    // IDebugEventCallbacks implementation
    HRESULT __stdcall Breakpoint(PDEBUG_BREAKPOINT bp);
        HRESULT __stdcall ChangeDebuggeeState(ULONG Flags, ULONG64 Argument);
        HRESULT __stdcall ChangeEngineState(ULONG Flags, ULONG64 Argument);
        HRESULT __stdcall ChangeSymbolState(ULONG Flags, ULONG64 Argument);
        HRESULT __stdcall CreateProcess(ULONG64 ImageFileHandle,
                                        ULONG64 Handle,
                                        ULONG64 BaseOffset,
                                        ULONG ModuleSize,
                                        PCSTR ModuleName,
                                        PCSTR ImageName,
                                        ULONG CheckSum,
                                        ULONG TimeDateStamp,
                                        ULONG64 InitialThreadHandle,
                                        ULONG64 ThreadDataOffset,
                                        ULONG64 StartOffset);
        HRESULT __stdcall CreateThread(ULONG64 Handle,
                                       ULONG64 DataOffset,
                                       ULONG64 StartOffset);
        HRESULT __stdcall Exception(PEXCEPTION_RECORD64 Exception, ULONG FirstChance);
        HRESULT __stdcall ExitProcess(ULONG ExitCode);
        HRESULT __stdcall ExitThread(ULONG ExitCode);
        HRESULT __stdcall GetInterestMask(PULONG Mask);
        HRESULT __stdcall LoadModule(ULONG64 ImageFileHandle,
                                     ULONG64 BaseOffset,
                                     ULONG ModuleSize,
                                     PCSTR ModuleName,
                                     PCSTR ImageName,
                                     ULONG CheckSum,
                                     ULONG TimeDateStamp);
        HRESULT __stdcall SessionStatus(ULONG Status);
        HRESULT __stdcall SystemError(ULONG Error, ULONG Level);
        HRESULT __stdcall UnloadModule(PCSTR ImageBaseName, ULONG64 BaseOffset);


private:
    volatile ULONG m_refCount;
    IDebugClient* m_pDebugClient;
};

#endif
