// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <windows.h>
#include <daccess.h>

char* g_appName;

#define MAX(x,y) ((x) > (y) ? (x) : (y))

void
AddBinaryResourceToDll(_In_z_ char* dllName,
                       _In_z_ const char* resName,
                       PVOID resData,
                       ULONG resDataSize)
{
    // a retry loop in case we get transient file access issues
    // does exponential backoff with retries after 1,2,4,8,16, and 32 seconds
    for(int seconds = 0; seconds < 60; seconds = MAX(seconds*2,1))
    {
        if(seconds != 0)
            Sleep(seconds * 1000);

        HANDLE dllUpdate = BeginUpdateResourceA(dllName, FALSE);
        if (!dllUpdate)
        {
            printf("Unable to open '%s' for update, error=%d\n",
                dllName, GetLastError());
            continue;
        }

        if (!UpdateResourceA(dllUpdate,
            (LPCSTR)RT_RCDATA,
            resName,
            MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL),
            resData,
            resDataSize))
        {
            printf("Unable to update '%s', error=%d\n",
                dllName, GetLastError());
            continue;
        }

        if(!EndUpdateResource(dllUpdate, FALSE))
        {
            printf("Unable to write updates to '%s', error=%d\n",
                dllName, GetLastError());
            continue;
        }

        return;
    }

    printf("Stopping after excessive failures\n");
    exit(1);
}

void
GetBinFileData(_In_z_ char* binFileName, PVOID* binData, PULONG binDataSize)
{
    HANDLE binFileHandle;
    PVOID data;
    ULONG size, done;

    binFileHandle = CreateFileA(binFileName, GENERIC_READ, FILE_SHARE_READ,
                                NULL, OPEN_EXISTING, 0, NULL);
    if (!binFileHandle || binFileHandle == INVALID_HANDLE_VALUE)
    {
        printf("Unable to open '%s', %d\n",
               binFileName, GetLastError());
        exit(1);
    }

    size = GetFileSize(binFileHandle, NULL);
    data = malloc(size);
    if (!data)
    {
        printf("Out of memory\n");
        exit(1);
    }

    if (!ReadFile(binFileHandle, data, size, &done, NULL) ||
        done != size)
    {
        printf("Unable to read '%s', %d\n",
               binFileName, GetLastError());
        exit(1);
    }

    CloseHandle(binFileHandle);

    *binData = data;
    *binDataSize = size;
}

void
Usage(void)
{
    printf("Usage: %s [options]\n", g_appName);
    printf("Options are:\n");
    printf("  /bin:<file>   - Binary data to attach to DLL\n");
    printf("  /dll:<file>   - DLL to modify\n");
    printf("  /name:<name>  - resource name [Optional]\n");
    exit(1);
}

void __cdecl
main(int argc, _In_z_ char** argv)
{
    char* binFile = NULL;
    char* dllFile = NULL;
    char* resName = NULL;

    g_appName = argv[0];

    while (--argc)
    {
        argv++;

        if (!strncmp(*argv, "/bin:", 5))
        {
            binFile = *argv + 5;
        }
        else if (!strncmp(*argv, "/dll:", 5))
        {
            dllFile = *argv + 5;
        }
        else if (!strncmp(*argv, "/name:", 6))
        {
            resName = *argv + 6;
        }
        else
        {
            Usage();
        }
    }

    if (!binFile || !dllFile)
    {
        Usage();
    }

    PVOID resData;
    ULONG resDataSize;

    GetBinFileData(binFile, &resData, &resDataSize);

    AddBinaryResourceToDll(dllFile, resName?resName:DACCESS_TABLE_RESOURCE,
                           resData, resDataSize);

    free(resData);

    printf("Updated %s\n", dllFile);
}
