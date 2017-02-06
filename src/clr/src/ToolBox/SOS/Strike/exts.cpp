// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#include "exts.h"
#include "disasm.h"
#ifndef FEATURE_PAL
#include "EventCallbacks.h"

#define VER_PRODUCTVERSION_W        (0x0100)

//
// globals
//
EXT_API_VERSION         ApiVersion = { (VER_PRODUCTVERSION_W >> 8), (VER_PRODUCTVERSION_W & 0xff), EXT_API_VERSION_NUMBER64, 0 };
WINDBG_EXTENSION_APIS   ExtensionApis;

ULONG PageSize;

OnUnloadTask *OnUnloadTask::s_pUnloadTaskList = NULL;

//
// Valid for the lifetime of the debug session.
//

ULONG   TargetMachine;
BOOL    Connected;
ULONG   g_TargetClass;
DWORD_PTR g_filterHint = 0;

PDEBUG_CLIENT         g_ExtClient;    
PDEBUG_DATA_SPACES2   g_ExtData2;
PDEBUG_SYMBOLS2       g_ExtSymbols2;
PDEBUG_ADVANCED3      g_ExtAdvanced3;
PDEBUG_CLIENT         g_pCallbacksClient;

#else

DebugClient*          g_DebugClient;
ILLDBServices*        g_ExtServices;    

#endif // FEATURE_PAL

IMachine* g_targetMachine = NULL;
BOOL      g_bDacBroken = FALSE;

PDEBUG_CONTROL2       g_ExtControl;
PDEBUG_DATA_SPACES    g_ExtData;
PDEBUG_REGISTERS      g_ExtRegisters;
PDEBUG_SYMBOLS        g_ExtSymbols;
PDEBUG_SYSTEM_OBJECTS g_ExtSystem;

#define SOS_ExtQueryFailGo(var, riid)                       \
    var = NULL;                                             \
    if ((Status = client->QueryInterface(__uuidof(riid),    \
                                 (void **)&var)) != S_OK)   \
    {                                                       \
        goto Fail;                                          \
    }

// Queries for all debugger interfaces.
#ifndef FEATURE_PAL    
extern "C" HRESULT
ExtQuery(PDEBUG_CLIENT client)
{
    g_ExtClient = client;
#else
extern "C" HRESULT
ExtQuery(ILLDBServices* services)
{
    g_ExtServices = services;
    DebugClient* client = new DebugClient(services);
    g_DebugClient = client;
#endif
    HRESULT Status;
    SOS_ExtQueryFailGo(g_ExtControl, IDebugControl2);
    SOS_ExtQueryFailGo(g_ExtData, IDebugDataSpaces);
    SOS_ExtQueryFailGo(g_ExtRegisters, IDebugRegisters);
    SOS_ExtQueryFailGo(g_ExtSymbols, IDebugSymbols);
    SOS_ExtQueryFailGo(g_ExtSystem, IDebugSystemObjects);
#ifndef FEATURE_PAL
    SOS_ExtQueryFailGo(g_ExtData2, IDebugDataSpaces2);
    SOS_ExtQueryFailGo(g_ExtSymbols2, IDebugSymbols2);
    SOS_ExtQueryFailGo(g_ExtAdvanced3, IDebugAdvanced3);
#endif // FEATURE_PAL
    return S_OK;

 Fail:
    if (Status == E_OUTOFMEMORY)
        ReportOOM();
    
    ExtRelease();
    return Status;
}

extern "C" HRESULT
ArchQuery(void)
{
    ULONG targetArchitecture;
    IMachine* targetMachine = NULL;

    g_ExtControl->GetExecutingProcessorType(&targetArchitecture);

#ifdef SOS_TARGET_AMD64
    if(targetArchitecture == IMAGE_FILE_MACHINE_AMD64)
    {
        targetMachine = AMD64Machine::GetInstance();
    }
#elif defined(SOS_TARGET_X86)
    if (targetArchitecture == IMAGE_FILE_MACHINE_I386)
    {
        targetMachine = X86Machine::GetInstance();
    }
#elif defined(SOS_TARGET_ARM)
    if (targetArchitecture == IMAGE_FILE_MACHINE_ARMNT)
    {
        targetMachine = ARMMachine::GetInstance();
    }
#elif defined(SOS_TARGET_ARM64)
    if (targetArchitecture == IMAGE_FILE_MACHINE_ARM64)
    {
        targetMachine = ARM64Machine::GetInstance();
    }
#else
#error "Undefined target architecture"
#endif

    if (targetMachine == NULL)
    {
        g_targetMachine = NULL;
        ExtErr("SOS does not support the current target architecture 0x%llx.\n", targetArchitecture);
        return E_FAIL;
    }

    g_targetMachine = targetMachine;
    return S_OK;
}

// Cleans up all debugger interfaces.
void
ExtRelease(void)
{
    EXT_RELEASE(g_ExtControl);
    EXT_RELEASE(g_ExtData);
    EXT_RELEASE(g_ExtRegisters);
    EXT_RELEASE(g_ExtSymbols);
    EXT_RELEASE(g_ExtSystem);
#ifndef FEATURE_PAL
    EXT_RELEASE(g_ExtData2);
    EXT_RELEASE(g_ExtSymbols2);
    EXT_RELEASE(g_ExtAdvanced3);
    g_ExtClient = NULL;
#else 
    EXT_RELEASE(g_DebugClient);
    g_ExtServices = NULL;
#endif // FEATURE_PAL
}

#ifndef FEATURE_PAL

BOOL IsMiniDumpFileNODAC();
extern HMODULE g_hInstance;

// This function throws an exception that can be caught by the debugger,
// instead of allowing the default CRT behavior of invoking Watson to failfast.
void __cdecl _SOS_invalid_parameter(
   const WCHAR * expression,
   const WCHAR * function, 
   const WCHAR * file, 
   unsigned int line,
   uintptr_t pReserved
)
{
    ExtErr("\nSOS failure!\n");
    throw "SOS failure";
}

// Unregisters our windbg event callbacks and releases the client, event callback objects
void CleanupEventCallbacks()
{
    if(g_pCallbacksClient != NULL)
    {
        g_pCallbacksClient->Release();
        g_pCallbacksClient = NULL;
    }
}

bool g_Initialized = false;

extern "C"
HRESULT
CALLBACK
DebugExtensionInitialize(PULONG Version, PULONG Flags)
{
    IDebugClient *DebugClient;
    PDEBUG_CONTROL DebugControl;
    HRESULT Hr;

    *Version = DEBUG_EXTENSION_VERSION(1, 0);
    *Flags = 0;

    if (g_Initialized)
    {
        return S_OK;
    }
    g_Initialized = true;

    if ((Hr = DebugCreate(__uuidof(IDebugClient),
                          (void **)&DebugClient)) != S_OK)
    {
        return Hr;
    }
    if ((Hr = DebugClient->QueryInterface(__uuidof(IDebugControl),
                                              (void **)&DebugControl)) != S_OK)
    {
        return Hr;
    }

    ExtensionApis.nSize = sizeof (ExtensionApis);
    if ((Hr = DebugControl->GetWindbgExtensionApis64(&ExtensionApis)) != S_OK)
    {
        return Hr;
    }
    
    // Fixes the "Unable to read dynamic function table entries" error messages by disabling the WinDbg security
    // feature that prevents the loading of unknown out of proc tack walkers.
    DebugControl->Execute(DEBUG_OUTCTL_IGNORE, ".settings set EngineInitialization.VerifyFunctionTableCallbacks=false", 
        DEBUG_EXECUTE_NOT_LOGGED | DEBUG_EXECUTE_NO_REPEAT);

    ExtQuery(DebugClient);
    if (IsMiniDumpFileNODAC())
    {
        ExtOut (
            "----------------------------------------------------------------------------\n"
            "The user dump currently examined is a minidump. Consequently, only a subset\n"
            "of sos.dll functionality will be available. If needed, attaching to the live\n"
            "process or debugging a full dump will allow access to sos.dll's full feature\n"
            "set.\n"
            "To create a full user dump use the command: .dump /ma <filename>\n"
            "----------------------------------------------------------------------------\n");
    }
    ExtRelease();
    
    OnUnloadTask::Register(CleanupEventCallbacks);
    g_pCallbacksClient = DebugClient;
    EventCallbacks* pCallbacksObj = new EventCallbacks(DebugClient);
    IDebugEventCallbacks* pCallbacks = NULL;
    pCallbacksObj->QueryInterface(__uuidof(IDebugEventCallbacks), (void**)&pCallbacks);
    pCallbacksObj->Release();

    if(FAILED(Hr = g_pCallbacksClient->SetEventCallbacks(pCallbacks)))
    {
        ExtOut ("SOS: Failed to register callback events\n");
        pCallbacks->Release();
        return Hr;
    }
    pCallbacks->Release();

#ifndef _ARM_
    // Make sure we do not tear down the debugger when a security function fails
    // Since we link statically against CRT this will only affect the SOS module.
    _set_invalid_parameter_handler(_SOS_invalid_parameter);
#endif
    
    DebugControl->Release();
    return S_OK;
}

extern "C"
void
CALLBACK
DebugExtensionNotify(ULONG Notify, ULONG64 /*Argument*/)
{
    //
    // The first time we actually connect to a target, get the page size
    //

    if ((Notify == DEBUG_NOTIFY_SESSION_ACCESSIBLE) && (!Connected))
    {
        IDebugClient *DebugClient;
        PDEBUG_DATA_SPACES DebugDataSpaces;
        PDEBUG_CONTROL DebugControl;
        HRESULT Hr;
        ULONG64 Page;

        if ((Hr = DebugCreate(__uuidof(IDebugClient),
                              (void **)&DebugClient)) == S_OK)
        {
            //
            // Get the page size and PAE enable flag
            //

            if ((Hr = DebugClient->QueryInterface(__uuidof(IDebugDataSpaces),
                                       (void **)&DebugDataSpaces)) == S_OK)
            {
                if ((Hr = DebugDataSpaces->ReadDebuggerData(
                    DEBUG_DATA_MmPageSize, &Page,
                    sizeof(Page), NULL)) == S_OK)
                {
                    PageSize = (ULONG)(ULONG_PTR)Page;
                }

                DebugDataSpaces->Release();
            }
            //
            // Get the architecture type.
            //

            if ((Hr = DebugClient->QueryInterface(__uuidof(IDebugControl),
                                                  (void **)&DebugControl)) == S_OK)
            {
                if ((Hr = DebugControl->GetActualProcessorType(
                    &TargetMachine)) == S_OK)
                {
                    Connected = TRUE;
                }
                ULONG Qualifier;
                if ((Hr = DebugControl->GetDebuggeeType(&g_TargetClass, &Qualifier)) == S_OK)
                {
                }

                DebugControl->Release();
            }

            DebugClient->Release();
        }
    }


    if (Notify == DEBUG_NOTIFY_SESSION_INACTIVE)
    {
        Connected = FALSE;
        PageSize = 0;
        TargetMachine = 0;
    }

    return;
}

extern "C"
void
CALLBACK
DebugExtensionUninitialize(void)
{
    // execute all registered cleanup tasks
    OnUnloadTask::Run();
    return;
}


BOOL DllInit(
    HANDLE /*hModule*/,
    DWORD  dwReason,
    DWORD  /*dwReserved*/
    )
{
    switch (dwReason) {
        case DLL_THREAD_ATTACH:
            break;

        case DLL_THREAD_DETACH:
            break;

        case DLL_PROCESS_DETACH:
            break;

        case DLL_PROCESS_ATTACH:
            break;
    }

    return TRUE;
}

BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        g_hInstance = (HMODULE) hInstance;
    }
    return true;
}

#else // FEATURE_PAL

HRESULT
DebugClient::QueryInterface(
    REFIID InterfaceId,
    PVOID* Interface
    )
{
    if (InterfaceId == __uuidof(IUnknown) ||
        InterfaceId == __uuidof(IDebugControl2) ||
        InterfaceId == __uuidof(IDebugControl4) ||
        InterfaceId == __uuidof(IDebugDataSpaces) ||
        InterfaceId == __uuidof(IDebugSymbols) ||
        InterfaceId == __uuidof(IDebugSystemObjects) ||
        InterfaceId == __uuidof(IDebugRegisters))
    {
        *Interface = this;
        AddRef();
        return S_OK;
    }
    else
    {
        *Interface = NULL;
        return E_NOINTERFACE;
    }
}

ULONG
DebugClient::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);    
    return ref;
}

ULONG
DebugClient::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        m_lldbservices->Release();
        delete this;
    }
    return ref;
}

#endif // FEATURE_PAL
