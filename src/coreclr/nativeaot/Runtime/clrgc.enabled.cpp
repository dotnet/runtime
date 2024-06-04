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

//  Validate that the name used to load the GC is just a simple file name
//  and does not contain something that could be used in a non-qualified path.
//  For example, using the string "..\..\..\clrgc.dll" we might attempt to
//  load a GC from the root of the drive.
//
//  The minimal set of characters that we must check for and exclude are:
//  On all platforms:
//     '/'  - (forward slash)
//  On Windows:
//     '\\' - (backslash)
//     ':'  - (colon)
//
//  Returns false if we find any of these characters in 'pwzModuleName'
//  Returns true if we reach the null terminator without encountering
//  any of these characters.
//
bool ValidateModuleName(const char* pwzModuleName)
{
    const char* pCurChar = pwzModuleName;
    char curChar;
    do {
        curChar = *pCurChar;
        if (curChar == '/'
#ifdef TARGET_WINDOWS
            || (curChar == '\\') || (curChar == ':')
#endif
        )
        {
            //  Return false if we find any of these character in 'pwzJitName'
            return false;
        }
        pCurChar++;
    } while (curChar != 0);

    //  Return true; we have reached the null terminator
    //
    return true;
}

HRESULT GCHeapUtilities::InitializeStandaloneGC()
{
    char* moduleName;
    char* modulePath;

    if (!RhConfig::Environment::TryGetStringValue("GCPath", &modulePath))
    {
        modulePath = nullptr;
    }

    if (!RhConfig::Environment::TryGetStringValue("GCName", &moduleName))
    {
        moduleName = nullptr;
    }

    if (!(moduleName || modulePath))
    {
        return GCHeapUtilities::InitializeDefaultGC();
    }

    NewArrayHolder<char> moduleNameHolder(moduleName);
    if (!modulePath)
    {
        //
        // This is not a security feature.
        // The libFileName originates either from an environment variable or from the runtimeconfig.json
        // These are trusted locations, and therefore even if it is a relative path, there is no security risk.
        //
        // However, users often don't know the absolute path to their coreclr module, especially on production. 
        // Therefore we allow referencing it from an arbitrary location through libFilePath instead. Users, however
        // are warned that they should keep the file in a secure location such that it cannot be tampered. 
        //
        if (!ValidateModuleName(moduleName))
        {
            LOG((LF_GC, LL_FATALERROR, "GC initialization failed to load the Standalone GC library.\n"));
            return E_FAIL;
        }

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
        modulePath = new (nothrow) char[folderLength + nameLength + 1];
        if (!modulePath)
        {
            return E_OUTOFMEMORY;
        }
        strcpy(modulePath, convertedExecutableModulePath);
        strcpy(modulePath + folderLength, moduleName);
    }

    NewArrayHolder<char> modulePathHolder(modulePath);
    HANDLE hMod = PalLoadLibrary(modulePath);

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
