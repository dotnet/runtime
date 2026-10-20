// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#define INITGUID
#include <guiddef.h>

// Used by the ICLRDataTarget and ICLRDataEnumMemoryRegionsCallback implementations.
DEFINE_GUID(IID_IUnknown, 0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

#if defined(HOST_ARM64)
// Used by the PAL interlocked helpers.
bool g_arm64_atomics_present = false;
#endif

#define TEMP_DIRECTORY_PATH "/tmp/"

// Used to construct the default dump path.
DWORD
PALAPI
GetTempPathA(
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer)
{
    DWORD dwPathLen = 0;
    const char *tempDir = getenv("TMPDIR");
    if (tempDir == nullptr)
    {
        tempDir = TEMP_DIRECTORY_PATH;
    }
    size_t tempDirLen = strlen(tempDir);
    if (tempDirLen < nBufferLength)
    {
        dwPathLen = tempDirLen;
        strcpy_s(lpBuffer, nBufferLength, tempDir);
    }
    else
    {
        // Get the required length
        dwPathLen = tempDirLen + 1;
    }
    return dwPathLen;
}

//
// Used in pal\inc\rt\safecrt.h's _invalid_parameter handler
//

VOID
PALAPI
RaiseException(
    IN DWORD dwExceptionCode,
    IN DWORD dwExceptionFlags,
    IN DWORD nNumberOfArguments,
    IN CONST ULONG_PTR* lpArguments)
{
    throw;
}

//
// Used by _ASSERTE
//

#ifdef _DEBUG
DWORD
PALAPI
GetCurrentProcessId()
{
    return getpid();
}

VOID
PALAPI
DebugBreak()
{
    abort();
}

#endif // DEBUG

// createdump_static registers this callback when embedded in a host that owns a PAL.
// The standalone createdump executable calls createdump_main directly.
PALIMPORT
VOID
PALAPI
PAL_SetCreateDumpCallback(
    IN PCREATEDUMP_CALLBACK callback)
{
}
