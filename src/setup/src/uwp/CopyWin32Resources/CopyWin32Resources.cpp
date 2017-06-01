//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <stdlib.h>
#include <stdio.h>
#include <windows.h>
#include <Strsafe.h>
#include <String.h>
#include <wchar.h>
#include <Imagehlp.h>
#include <new>

// Declare callback functions.
BOOL EnumTypesFunc(
    HMODULE hModule,
    _In_z_ LPTSTR lpType,
    LONG_PTR lParam);

BOOL EnumNamesFunc(
    HMODULE hModule,
    _In_z_ LPCTSTR lpType,
    _In_z_ LPTSTR lpName,
    LONG_PTR lParam);

BOOL EnumLangsFunc(
    HMODULE hModule,
    _In_z_ LPCTSTR lpType,
    _In_z_ LPCTSTR lpName,
    WORD wLang,
    LONG_PTR lParam);

const wchar_t* input_format  = L"/input:";
const wchar_t* output_format = L"/output:";
const size_t input_format_size  = wcslen(input_format);
const size_t output_format_size = wcslen(output_format);

enum ACTION {
    eInsert,
    eRemove,
};

typedef struct {
    HANDLE handle;
    ACTION action;
} EnumResourceCallbackParam, *pEnumResourceCallbackParam;


int CopyWin32Resources(_In_z_ LPWSTR inputFile, _In_z_ LPWSTR destFile);


int __cdecl wmain(int argc, _In_reads_(argc) wchar_t* argv[])
{
    wprintf(L"Copyright (c) Microsoft Corporation.  All rights reserved.");
    wchar_t *copyFrom = 0, *copyTo = 0;

    while (--argc)
    {
        argv++;
        if (!wcsncmp(*argv, input_format, input_format_size))
        {
            copyFrom = *argv + input_format_size;
        }
        else if (!wcsncmp(*argv, output_format, output_format_size))
        {
            copyTo = *argv + output_format_size;
        }
        else
        {
            return 90;
        }
    }

    return CopyWin32Resources(copyFrom, copyTo);
}

int DeleteCertificates(_In_z_ LPWSTR fileName)
{
    HANDLE hFile = CreateFile(fileName,
            FILE_GENERIC_READ | FILE_GENERIC_WRITE,
            0, 
            NULL,
            OPEN_EXISTING,
            0, 
            NULL
            );

    if (hFile == INVALID_HANDLE_VALUE)
    {
        return 701;
    }

    DWORD certificateCount;
    DWORD *certificateIndices;

    if (!ImageEnumerateCertificates(hFile,
                                    CERT_SECTION_TYPE_ANY,
                                    &certificateCount,
                                    NULL,
                                    0
                                    )) 
    {
        CloseHandle(hFile);
        return 702;
    }

    if (certificateCount == 0)
    {
        CloseHandle(hFile);
        return 0;
    }
    certificateIndices = new (std::nothrow) DWORD[certificateCount];

    if (certificateIndices == NULL)
    {
        CloseHandle(hFile);
        return 703;
    }
    
    ImageEnumerateCertificates(hFile,
                               CERT_SECTION_TYPE_ANY,
                               &certificateCount,
                               certificateIndices,
                               certificateCount
                               );

    if (certificateCount == 0)
    {
        CloseHandle(hFile);
        return 704;
    }

    for (DWORD i=0; i<certificateCount; i++)
    {
        if (!ImageRemoveCertificate(hFile, certificateIndices[i])) 
        {
            CloseHandle(hFile);
            return 705;
        }
    }

    CloseHandle(hFile);

    return 0;
}

int CopyWin32Resources(_In_z_ LPWSTR inputFile, _In_z_ LPWSTR destFile)
{
    if (!inputFile)
    {
        return 100;
    }

    if (!destFile)
    {
        return 100;
    }

    int deleteCertificatesResult = DeleteCertificates(destFile);
    if (deleteCertificatesResult != 0)
    {
        return deleteCertificatesResult;
    }

    PVOID resData;
    ULONG resDataSize;

    HMODULE hModuleToReadResourcesFrom = LoadLibraryEx(inputFile, NULL, LOAD_LIBRARY_AS_DATAFILE);
    if (hModuleToReadResourcesFrom == NULL)
    {
        return 200;
    }

    HMODULE hModuleToInsertResourcesInto = LoadLibraryEx(destFile, NULL, LOAD_LIBRARY_AS_DATAFILE);
    if (hModuleToInsertResourcesInto == NULL)
    {
        return 201;
    }

    // Open the file to which you want to add the  resource.
    HANDLE hUpdateRes = BeginUpdateResource(destFile, FALSE);
    if (hUpdateRes == NULL)
    {
        return 300;
    }

    EnumResourceCallbackParam insertionParam;
    insertionParam.handle = hUpdateRes;
    insertionParam.action = eInsert;

    EnumResourceCallbackParam cleanupParam;
    cleanupParam.handle = hUpdateRes;
    cleanupParam.action = eRemove;

    if (!EnumResourceTypes(hModuleToInsertResourcesInto, // module handle
            (ENUMRESTYPEPROC)EnumTypesFunc,  // callback function
            (LONG_PTR)&cleanupParam) // callback function parameter
        )
    {
        return 400;
    }

    if (!EnumResourceTypes(hModuleToReadResourcesFrom,  // module handle
            (ENUMRESTYPEPROC)EnumTypesFunc,  // callback function
            (LONG_PTR)&insertionParam) // callback function parameter
        )
    {
        return 401;
    }

    if (!FreeLibrary(hModuleToInsertResourcesInto))
    {
        return 601;
    }

    if (!EndUpdateResource(hUpdateRes, FALSE))
    {
        return 500;
    }

    // Clean up.
    if (!FreeLibrary(hModuleToReadResourcesFrom))
    {
        return 600;
    }

    return 0;
}

BOOL EnumTypesFunc(
    HMODULE hModule,  // module handle
    _In_z_ LPTSTR lpType,    // address of resource type
    LONG_PTR lParam)      // extra parameter, could be
    // used for error checking
{

    // Find the names of all resources of type lpType.
    return EnumResourceNames(hModule,
        lpType,
        (ENUMRESNAMEPROC)EnumNamesFunc,
        lParam);

}

//    PURPOSE:  Resource name callback
BOOL EnumNamesFunc(
    HMODULE hModule,  // module handle
    _In_z_ LPCTSTR lpType,   // address of resource type
    _In_z_ LPTSTR lpName,    // address of resource name
    LONG_PTR lParam)      // extra parameter, could be
    // used for error checking
{

    // Find the languages of all resources of type
    // lpType and name lpName.
    return EnumResourceLanguages(hModule,
        lpType,
        lpName,
        (ENUMRESLANGPROC)EnumLangsFunc,
        lParam);

}

//    PURPOSE:  Resource language callback
BOOL EnumLangsFunc(
    HMODULE hModule, // module handle
    _In_z_ LPCTSTR lpType,  // address of resource type
    _In_z_ LPCTSTR lpName,  // address of resource name
    WORD wLang,      // resource language
    LONG_PTR lParam)     // extra parameter, could be
    // used for error checking
{


    HRSRC hRes;         // handle/ptr. to res. info. in hModule
    LPVOID lpResLock = NULL;   // pointer to resource data
    BOOL result;

    pEnumResourceCallbackParam pCallbackParam = (pEnumResourceCallbackParam) lParam;

    hRes = FindResourceEx(hModule, lpType, lpName, wLang);
    HGLOBAL hResLoad = LoadResource(hModule, hRes);
    if (hResLoad == NULL)
    {
        return FALSE;
    }

    if (pCallbackParam->action == eInsert)
    {
        // Lock the resource box into global memory.
        lpResLock = LockResource(hResLoad);
        if (lpResLock == NULL)
        {
            return FALSE;
        }
    }


    result = UpdateResource(pCallbackParam->handle, // update resource handle
        lpType,
        lpName,
        wLang,
        lpResLock, // ptr to resource info
        (pCallbackParam->action == eRemove) ? 0 : SizeofResource(hModule, hRes) // size of resource info
        );       

    if (result == FALSE)
    {
        return FALSE;
    }
    return TRUE;
}
