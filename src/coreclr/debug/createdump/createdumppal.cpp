// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#define INITGUID
#include <guiddef.h>

// Used by the ICLRDataTarget and ICLRDataEnumMemoryRegionsCallback implementations.
DEFINE_GUID(IID_IUnknown, 0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

#define TEMP_DIRECTORY_PATH "/tmp/"

// Used to construct the default dump path.
DWORD
PALAPI
GetTempPathA(
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer)
{
    DWORD dwPathLen = 0;
    const char* tempDir = getenv("TMPDIR");
    if (tempDir == nullptr)
        tempDir = TEMP_DIRECTORY_PATH;

    size_t tempDirLen = strlen(tempDir);
    if (tempDirLen < nBufferLength)
    {
        dwPathLen = tempDirLen;
        strcpy_s(lpBuffer, nBufferLength, tempDir);
    }
    else
    {
        dwPathLen = tempDirLen + 1;
    }
    return dwPathLen;
}

// Used by the invalid parameter handler in pal/inc/rt/safecrt.h.
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

#ifdef _DEBUG

// Used by _ASSERTE in the PAL headers.
DWORD
PALAPI
GetCurrentProcessId()
{
    return getpid();
}

// Used by _ASSERTE in the PAL headers.
VOID
PALAPI
DebugBreak()
{
    abort();
}

#endif // _DEBUG

// createdump_static registers this callback when embedded in a host that owns a PAL.
// The standalone createdump executable calls createdump_main directly.
PALIMPORT
VOID
PALAPI
PAL_SetCreateDumpCallback(
    IN PCREATEDUMP_CALLBACK callback)
{
}
