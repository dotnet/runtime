// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#define SKIP_TRACING_DEFINITIONS
#include "gcenv.h"
#undef SKIP_TRACING_DEFINITIONS
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gceventstatus.h"
#include "holder.h"
#include "RhConfig.h"

#include "gctoeeinterface.standalone.inl"

enum GC_LOAD_STATUS {
    GC_LOAD_STATUS_BEFORE_START,
    GC_LOAD_STATUS_START,
    GC_LOAD_STATUS_DONE_LOAD,
    GC_LOAD_STATUS_GET_VERSIONINFO,
    GC_LOAD_STATUS_CALL_VERSIONINFO,
    GC_LOAD_STATUS_DONE_VERSION_CHECK,
    GC_LOAD_STATUS_GET_INITIALIZE,
    GC_LOAD_STATUS_LOAD_COMPLETE
};

// Load status of the GC. If GC loading fails, the value of this
// global indicates where the failure occurred.
GC_LOAD_STATUS g_gc_load_status = GC_LOAD_STATUS_BEFORE_START;

// The version of the GC that we have loaded.
VersionInfo g_gc_version_info;

CrstStatic g_eventStashLock;

GCEventLevel g_stashedLevel = GCEventLevel_None;
GCEventKeyword g_stashedKeyword = GCEventKeyword_None;
GCEventLevel g_stashedPrivateLevel = GCEventLevel_None;
GCEventKeyword g_stashedPrivateKeyword = GCEventKeyword_None;

BOOL g_gcEventTracingInitialized = FALSE;

void InitializeGCEventLock()
{
    g_eventStashLock.InitNoThrow(CrstGcEvent);
}

HRESULT InitializeStandaloneGC();

HRESULT InitializeGCSelector()
{
    return InitializeStandaloneGC();
}

HRESULT InitializeStandaloneGC()
{
    return GCHeapUtilities::InitializeStandaloneGC();
}

HRESULT GCHeapUtilities::InitializeStandaloneGC()
{
    char* moduleName;

    if (!RhConfig::Environment::TryGetStringValue("GCName", &moduleName))
    {
        return GCHeapUtilities::InitializeDefaultGC();
    }

    NewArrayHolder<char> moduleNameHolder(moduleName);
    HANDLE executableModule = PalGetModuleHandleFromPointer((void*)&PalGetModuleHandleFromPointer);
    const TCHAR * executableModulePath = NULL;
    PalGetModuleFileName(&executableModulePath, executableModule);
    char* convertedExecutableModulePath = PalCopyTCharAsChar(executableModulePath);
    if (!convertedExecutableModulePath)
    {
        return E_OUTOFMEMORY;
    }
    NewArrayHolder<char> convertedExecutableModulePathHolder(convertedExecutableModulePath);
    {
        char* p = convertedExecutableModulePath;
        char* q = nullptr;
        while (*p != '\0')
        {
            if (*p == DIRECTORY_SEPARATOR_CHAR)
            {
                q = p;
            }
            p++;
        }
        assert(q != nullptr);
        q++;
        *q = '\0';
    }
    size_t folderLength = strlen(convertedExecutableModulePath);
    size_t nameLength = strlen(moduleName);
    char* moduleFullPath = new (nothrow) char[folderLength + nameLength + 1];
    if (!moduleFullPath)
    {
        return E_OUTOFMEMORY;
    }
    NewArrayHolder<char> moduleFullPathHolder(moduleFullPath);
    strcpy(moduleFullPath, convertedExecutableModulePath);
    strcpy(moduleFullPath + folderLength, moduleName);

    HANDLE hMod = PalLoadLibrary(moduleFullPath);

    if (!hMod)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed to load the Standalone GC library.\n"));
        return E_FAIL;
    }

    IGCToCLR* gcToClr = new (nothrow) standalone::GCToEEInterface();
    if (!gcToClr)
    {
        return E_OUTOFMEMORY;
    }

    GC_VersionInfoFunction versionInfo = (GC_VersionInfoFunction)PalGetProcAddress(hMod, "GC_VersionInfo");
    if (!versionInfo)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the GC_VersionInfo function not found.\n"));
        return E_FAIL;
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_VERSIONINFO;
    g_gc_version_info.MajorVersion = EE_INTERFACE_MAJOR_VERSION;
    g_gc_version_info.MinorVersion = 0;
    g_gc_version_info.BuildVersion = 0;
    versionInfo(&g_gc_version_info);
    g_gc_load_status = GC_LOAD_STATUS_CALL_VERSIONINFO;

    if (g_gc_version_info.MajorVersion < GC_INTERFACE_MAJOR_VERSION)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the Standalone GC reported a major version lower than what the runtime requires.\n"));
        return E_FAIL;
    }

    GC_InitializeFunction initFunc = (GC_InitializeFunction)PalGetProcAddress(hMod, "GC_Initialize");
    if (!initFunc)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the GC_Initialize function not found.\n"));
        return E_FAIL;
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_INITIALIZE;
    IGCHeap* heap;
    IGCHandleManager* manager;
    HRESULT initResult = initFunc(gcToClr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        g_pGCHeap = heap;
        {
            CrstHolder lh(&g_eventStashLock);
            g_pGCHeap->ControlEvents(g_stashedKeyword, g_stashedLevel);
            g_pGCHeap->ControlPrivateEvents(g_stashedPrivateKeyword, g_stashedPrivateLevel);
            g_gcEventTracingInitialized = TRUE;
        }
        g_pGCHandleManager = manager;
        g_gcDacGlobals = &g_gc_dac_vars;
        LOG((LF_GC, LL_INFO100, "GC load successful\n"));
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
}

void GCHeapUtilities::RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level)
{
    CrstHolder lh(&g_eventStashLock);
    if (g_gcEventTracingInitialized)
    {
        if (isPublicProvider)
        {
            g_pGCHeap->ControlEvents(keywords, level);
        }
        else
        {
            g_pGCHeap->ControlPrivateEvents(keywords, level);
        }
    }
    else
    {
        if (isPublicProvider)
        {
            g_stashedKeyword = keywords;
            g_stashedLevel = level;
        }
        else
        {
            g_stashedPrivateKeyword = keywords;
            g_stashedPrivateLevel = level;
        }
    }
}
