// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "EventCallbacks.h"

EventCallbacks::EventCallbacks(IDebugClient* pDebugClient) : m_refCount(1), m_pDebugClient(pDebugClient)
{
}

EventCallbacks::~EventCallbacks()
{
    if(m_pDebugClient != NULL)
        m_pDebugClient->Release();
}

    // IUnknown implementation
HRESULT __stdcall EventCallbacks::QueryInterface(REFIID riid, VOID** ppInterface)
{
    if(riid == __uuidof(IDebugEventCallbacks))
    {
        *ppInterface = static_cast<IDebugEventCallbacks*>(this);
        AddRef();
        return S_OK;
    }
    else if(riid == __uuidof(IUnknown))
    {
        *ppInterface = static_cast<IUnknown*>(this);
        AddRef();
        return S_OK;
    }
    else
    {
        return E_NOINTERFACE;
    }
}

ULONG __stdcall EventCallbacks::AddRef()
{
    return InterlockedIncrement((volatile LONG *) &m_refCount);
}

ULONG __stdcall EventCallbacks::Release()
{
    ULONG count = InterlockedDecrement((volatile LONG *) &m_refCount);
    if(count == 0)
    {
        delete this;
    }
    return count;
}
    
// IDebugEventCallbacks implementation
HRESULT __stdcall EventCallbacks::Breakpoint(PDEBUG_BREAKPOINT bp)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::ChangeDebuggeeState(ULONG Flags, ULONG64 Argument)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::ChangeEngineState(ULONG Flags, ULONG64 Argument)
{
    return DEBUG_STATUS_NO_CHANGE;
}
HRESULT __stdcall EventCallbacks::ChangeSymbolState(ULONG Flags, ULONG64 Argument)
{
    return DEBUG_STATUS_NO_CHANGE;
}
HRESULT __stdcall EventCallbacks::CreateProcess(ULONG64 ImageFileHandle,
    ULONG64 Handle,
    ULONG64 BaseOffset,
    ULONG ModuleSize,
    PCSTR ModuleName,
    PCSTR ImageName,
    ULONG CheckSum,
    ULONG TimeDateStamp,
    ULONG64 InitialThreadHandle,
    ULONG64 ThreadDataOffset,
    ULONG64 StartOffset)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::CreateThread(ULONG64 Handle,
    ULONG64 DataOffset,
    ULONG64 StartOffset)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::Exception(PEXCEPTION_RECORD64 Exception, ULONG FirstChance)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::ExitProcess(ULONG ExitCode)
{
    UninitCorDebugInterface();
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::ExitThread(ULONG ExitCode)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::GetInterestMask(PULONG Mask)
{
    *Mask = DEBUG_EVENT_LOAD_MODULE | DEBUG_EVENT_EXIT_PROCESS;
    return S_OK;
}

extern BOOL g_fAllowJitOptimization;

HRESULT __stdcall EventCallbacks::LoadModule(ULONG64 ImageFileHandle,
    ULONG64 BaseOffset,
    ULONG ModuleSize,
    PCSTR ModuleName,
    PCSTR ImageName,
    ULONG CheckSum,
    ULONG TimeDateStamp)
{
    HRESULT handleEventStatus = DEBUG_STATUS_NO_CHANGE;
    ExtQuery(m_pDebugClient);

    if (ModuleName != NULL && _stricmp(ModuleName, MAIN_CLR_MODULE_NAME_A) == 0)
    {
        // if we don't want the JIT to optimize, we should also disable optimized NGEN images
        if(!g_fAllowJitOptimization)
        {
            // if we aren't succesful SetNGENCompilerFlags will print relevant error messages
            // and then we need to stop the debugger so the user can intervene if desired
            if(FAILED(SetNGENCompilerFlags(CORDEBUG_JIT_DISABLE_OPTIMIZATION)))
            {
                handleEventStatus = DEBUG_STATUS_BREAK;
            }
        }
    }

    ExtRelease();
    return handleEventStatus;
}

HRESULT __stdcall EventCallbacks::SessionStatus(ULONG Status)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::SystemError(ULONG Error, ULONG Level)
{
    return DEBUG_STATUS_NO_CHANGE;
}

HRESULT __stdcall EventCallbacks::UnloadModule(PCSTR ImageBaseName, ULONG64 BaseOffset)
{
    return DEBUG_STATUS_NO_CHANGE;
}
