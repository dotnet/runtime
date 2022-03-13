// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* This app writes out the data for a special resource which is embedded in clr.dll
 * The resource serves two purposes, to differentiate a random dll named clr.dll from
 * an official microsoft clr dll (it isn't foolproof but it should prevent anyone from
 * accidentally appearing to be a clr) and to provide file information about DAC and DBI
 * which correspond to this build of the CLR.
 */


#include <stdlib.h>
#include <stdio.h>
#include <windows.h>
#include <clrinternal.h>

char* g_appName;


struct ClrDebugResource
{
    DWORD dwVersion;
    GUID signature;
    DWORD dwDacTimeStamp;
    DWORD dwDacSizeOfImage;
    DWORD dwDbiTimeStamp;
    DWORD dwDbiSizeOfImage;
};

BOOL
GetBinFileData(_In_z_ char* binFileName, DWORD* pTimeStamp, DWORD* pSizeOfImage)
{
    HANDLE binFileHandle;
    DWORD peHeaderOffset;
    ULONG done;

    binFileHandle = CreateFileA(binFileName, GENERIC_READ, FILE_SHARE_READ,
                                NULL, OPEN_EXISTING, 0, NULL);
    if (!binFileHandle || binFileHandle == INVALID_HANDLE_VALUE)
    {
        printf("%s: Unable to open '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    // Read the 4 byte value at 0x3c, which is the offset to PE header
    if (INVALID_SET_FILE_POINTER == SetFilePointer(binFileHandle, 0x3c, NULL, FILE_BEGIN))
    {
        printf("%s: Unable to move file pointer '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    if (!ReadFile(binFileHandle, &peHeaderOffset, 4, &done, NULL) ||
        done != 4)
    {
        printf("%s: Unable to read '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    // Read the 4 byte value at 8 bytes after peHeader, that is the timestamp
    if (INVALID_SET_FILE_POINTER == SetFilePointer(binFileHandle, peHeaderOffset + 8, NULL, FILE_BEGIN))
    {
        printf("%s: Unable to move file pointer '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    if (!ReadFile(binFileHandle, pTimeStamp, 4, &done, NULL) ||
        done != 4)
    {
        printf("%s: Unable to read '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    // Read the 4 byte value at 80 bytes after peHeader, that is the sizeOfImage
    if (INVALID_SET_FILE_POINTER == SetFilePointer(binFileHandle, peHeaderOffset + 80, NULL, FILE_BEGIN))
    {
        printf("%s: Unable to move file pointer '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    if (!ReadFile(binFileHandle, pSizeOfImage, 4, &done, NULL) ||
        done != 4)
    {
        printf("%s: Unable to read '%s', %d\n", g_appName,
               binFileName, GetLastError());
        goto error;
    }

    CloseHandle(binFileHandle);
    return TRUE;

error:
    CloseHandle(binFileHandle);
    return FALSE;
}

void
Usage(void)
{
    printf("Usage: %s [options]\n", g_appName);
    printf("Options are:\n");
    printf("  /out:<file>     - output binary file that contains the raw resource data\n");
    printf("  /dac:<file>     - path to mscordacwks that should be referenced\n");
    printf("  /dbi:<file>     - path to mscordbi that should be referenced\n");
    printf("  /sku:<sku_name> - Either clr, coreclr, or phoneclr indicating the CLR sku\n");
}

void __cdecl
main(int argc, _In_z_ char** argv)
{
    char* outFile = NULL;
    char* dacFile = NULL;
    char* dbiFile = NULL;
    char* sku = NULL;

    g_appName = argv[0];

    while (--argc)
    {
        argv++;

        if (!strncmp(*argv, "/out:", 5))
        {
            outFile = *argv + 5;
        }
        else if (!strncmp(*argv, "/dac:", 5))
        {
            dacFile = *argv + 5;
        }
        else if (!strncmp(*argv, "/dbi:", 5))
        {
            dbiFile = *argv + 5;
        }
        else if (!strncmp(*argv, "/sku:", 5))
        {
            sku = *argv + 5;
        }
        else
        {
            Usage();
            exit(1);
        }
    }

    if (!outFile || !dacFile || !dbiFile || !sku)
    {
        Usage();
        exit(1);
    }

    ClrDebugResource res;
    res.dwVersion = 0;

    if(strcmp(sku, "clr")==0)
    {
        res.signature = CLR_ID_V4_DESKTOP;
    }
    else if(strcmp(sku, "coreclr")==0)
    {
        res.signature = CLR_ID_CORECLR;
    }
    else if(strcmp(sku, "phoneclr")==0)
    {
        res.signature = CLR_ID_PHONE_CLR;
    }
    else if (strcmp(sku, "onecoreclr") == 0)
    {
        res.signature = CLR_ID_ONECORE_CLR;
    }
    else
    {
        printf("Error: Unrecognized sku option: %s\n", sku);
        Usage();
        exit(1);
    }

    printf("%s: Reading data from DAC: %s\n", g_appName, dacFile);
    if(!GetBinFileData(dacFile, &(res.dwDacTimeStamp), &(res.dwDacSizeOfImage)))
        exit(1);
    printf("%s: DAC timeStamp = 0x%x sizeOfImage = 0x%x\n", g_appName, res.dwDacTimeStamp, res.dwDacSizeOfImage);

    printf("%s: Reading data from DBI: %s\n", g_appName, dbiFile);
    if(!GetBinFileData(dbiFile, &(res.dwDbiTimeStamp), &(res.dwDbiSizeOfImage)))
        exit(1);
    printf("%s: DBI timeStamp = 0x%x sizeOfImage = 0x%x\n", g_appName, res.dwDbiTimeStamp, res.dwDbiSizeOfImage);

    printf("%s: Writing binary resource file: %s\n", g_appName, outFile);
    HANDLE outFileHandle = CreateFileA(outFile, GENERIC_WRITE, 0,
                                                   NULL, CREATE_ALWAYS, 0, NULL);
    if (!outFileHandle || outFileHandle == INVALID_HANDLE_VALUE)
    {
        printf("%s: Unable to create '%s', %d\n",
               g_appName, outFile, GetLastError());
        goto error;
    }


    DWORD done = 0;
    if(!WriteFile(outFileHandle, &res, sizeof(res), &done, NULL) || done != sizeof(res))
    {
        printf("%s: Unable to write file data '%s', %d\n",
               g_appName, outFile, GetLastError());
        goto error;
    }

    CloseHandle(outFileHandle);
    printf("%s: Success. Returning 0\n", g_appName);
    exit(0);

error:
    CloseHandle(outFileHandle);
    exit(1);
}
