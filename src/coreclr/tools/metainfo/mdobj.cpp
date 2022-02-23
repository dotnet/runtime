// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <ctype.h>
#include <crtdbg.h>
#include "mdinfo.h"

#ifndef STRING_BUFFER_LEN
#define STRING_BUFFER_LEN 4096
#endif

#define OBJ_EXT         ".obj"
#define OBJ_EXT_W       W(".obj")
#define OBJ_EXT_LEN     4
#define LIB_EXT         ".lib"
#define LIB_EXT_W       W(".lib")
#define LIB_EXT_LEN     4

extern IMetaDataDispenserEx *g_pDisp;
extern DWORD g_ValModuleType;

// This function is copied from peparse.c file.  Making this static, so we won't end up with
// duplicate definitions causing confusion.
static const char g_szCORMETA[] = ".cormeta";
static HRESULT FindObjMetaData(PVOID pImage, PVOID *ppMetaData, long *pcbMetaData)
{
    IMAGE_FILE_HEADER *pImageHdr;       // Header for the .obj file.
    IMAGE_SECTION_HEADER *pSectionHdr;  // Section header.
    WORD        i;                      // Loop control.

    // Get a pointer to the header and the first section.
    pImageHdr = (IMAGE_FILE_HEADER *) pImage;
    pSectionHdr = (IMAGE_SECTION_HEADER *)(pImageHdr + 1);

    // Avoid confusion.
    *ppMetaData = NULL;
    *pcbMetaData = 0;

    // Walk each section looking for .cormeta.
    for (i=0;  i<VAL16(pImageHdr->NumberOfSections);  i++, pSectionHdr++)
    {
        // Simple comparison to section name.
        if (strcmp((const char *) pSectionHdr->Name, g_szCORMETA) == 0)
        {
            *pcbMetaData = VAL32(pSectionHdr->SizeOfRawData);
            *ppMetaData = (void *) ((UINT_PTR)pImage + VAL32(pSectionHdr->PointerToRawData));
            break;
        }
    }

    // Check for errors.
    if (*ppMetaData == NULL || *pcbMetaData == 0)
        return (E_FAIL);
    return (S_OK);
}


// This function returns the address to the MapView of file and file size.
void GetMapViewOfFile(_In_ WCHAR *szFile, PBYTE *ppbMap, DWORD *pdwFileSize)
{
    HANDLE      hMapFile;
    DWORD       dwHighSize;

    HANDLE hFile = WszCreateFile(szFile,
                               GENERIC_READ,
                               FILE_SHARE_READ,
                               NULL,
                               OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                               NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        MDInfo::Error("CreateFileA failed!");

    *pdwFileSize = GetFileSize(hFile, &dwHighSize);

    if ((*pdwFileSize == 0xFFFFFFFF) && (GetLastError() != NO_ERROR))
    {
        CloseHandle(hFile);
        MDInfo::Error("GetFileSize failed!");
    }
    _ASSERTE(dwHighSize == 0);

    hMapFile = CreateFileMappingW(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    CloseHandle(hFile);
    if (!hMapFile)
        MDInfo::Error("CreateFileMappingW failed!");

    *ppbMap = (PBYTE) MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
    CloseHandle(hMapFile);

    if (!*ppbMap)
        MDInfo::Error("MapViewOfFile failed!");
} // void GetMapViewOfFile()

// This function skips a member given the pointer to the member header
// and returns a pointer to the next header.
PBYTE SkipMember(PBYTE pbMapAddress)
{
    PIMAGE_ARCHIVE_MEMBER_HEADER pMemHdr;
    ULONG       ulMemSize;
    int         j;

    pMemHdr = (PIMAGE_ARCHIVE_MEMBER_HEADER)pbMapAddress;

    // Get size of the member.
    ulMemSize = 0;
    for (j = 0; j < 10; j++)
    {
        if (pMemHdr->Size[j] < '0' || pMemHdr->Size[j] > '9')
            break;
        else
            ulMemSize = ulMemSize * 10 + pMemHdr->Size[j] - '0';
    }

    // Skip past the header.
    pbMapAddress += IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR + ulMemSize;
    // Find the next even address if the current one is not even.
    if ((ULONG_PTR)pbMapAddress % 2)
        pbMapAddress++;

    return pbMapAddress;
} // void SkipMember()

// This function returns the name of the given Obj.  If the name fits in the header,
// szBuf will be filled in and returned from the function.  Else an offset into the long
// names section will be returned.
char *GetNameOfObj(PBYTE pbLongNames, PIMAGE_ARCHIVE_MEMBER_HEADER pMemHdr, char szBuf[17])
{
    if (pMemHdr->Name[0] == '/')
    {
        ULONG   ulOffset = 0;

        // Long Names section must exist if the .obj file name starts with '/'.
        _ASSERTE(pbLongNames &&
            "Corrupt archive file - .obj file name in the header starts with "
            "'/' but no long names section present in the archive file.");

        // Calculate the offset into the long names section.
        for (int j = 1; j < 16; j++)
        {
            if (pMemHdr->Name[j] < '0' || pMemHdr->Name[j] > '9')
                break;
            else
                ulOffset = ulOffset * 10 + pMemHdr->Name[j] - '0';
        }
        return (char *)(pbLongNames + ulOffset);
    }
    else
    {
        int j;
        for (j = 0; j < 16; j++)
            if ((szBuf[j] = pMemHdr->Name[j]) == '/')
                break;
        szBuf[j] = '\0';
        return szBuf;
    }
} // char *GetNameOfObj()

// DisplayArchive() function
//
// Opens the .LIB file, and displays the metadata in the specified object files.

void DisplayArchive(_In_z_ WCHAR* szFile, ULONG DumpFilter, _In_opt_z_ WCHAR* szObjName, strPassBackFn pDisplayString)
{
    PBYTE       pbMapAddress;
    PBYTE       pbStartAddress;
    PBYTE       pbLongNameAddress;
    PIMAGE_ARCHIVE_MEMBER_HEADER pMemHdr;
    DWORD       dwFileSize;
    PVOID       pvMetaData;
    char        *szName;
    WCHAR     wzName[1024];
    char        szBuf[17];
    long        cbMetaData;
    int         i;
    HRESULT     hr;
	char		szString[1024];

    GetMapViewOfFile(szFile, &pbMapAddress, &dwFileSize);
    pbStartAddress = pbMapAddress;

    // Verify and skip archive signature.
    if (dwFileSize < IMAGE_ARCHIVE_START_SIZE ||
        strncmp((char *)pbMapAddress, IMAGE_ARCHIVE_START, IMAGE_ARCHIVE_START_SIZE))
    {
        MDInfo::Error("Bad file format - archive signature mis-match!");
    }
    pbMapAddress += IMAGE_ARCHIVE_START_SIZE;

    // Skip linker member 1, linker member 2.
    for (i = 0; i < 2; i++)
        pbMapAddress = SkipMember(pbMapAddress);

    // Save address of the long name member and skip it if there exists one.
    pMemHdr = (PIMAGE_ARCHIVE_MEMBER_HEADER)pbMapAddress;
    if (pMemHdr->Name[0] == '/' && pMemHdr->Name[1] == '/')
    {
        pbLongNameAddress = pbMapAddress + IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR;
        pbMapAddress = SkipMember(pbMapAddress);
    }
    else
        pbLongNameAddress = 0;

    pDisplayString ("\n");
    // Get the MetaData for each object file and display it.
    while (DWORD(pbMapAddress - pbStartAddress) < dwFileSize)
    {
        if((szName = GetNameOfObj(pbLongNameAddress, (PIMAGE_ARCHIVE_MEMBER_HEADER)pbMapAddress, szBuf))!=NULL)
        {
            if (Wsz_mbstowcs(wzName, szName, 1024) == -1)
                MDInfo::Error("Conversion from Multi-Byte to Wide-Char failed.");

            // Display metadata only for object files.
            // If szObjName is specified, display metadata only for that one object file.
            if (!_stricmp(&szName[strlen(szName) - OBJ_EXT_LEN], OBJ_EXT) &&
                (!szObjName || !_wcsicmp(szObjName, wzName)))
            {
                // Try to find the MetaData section in the current object file.
                hr = FindObjMetaData(pbMapAddress+IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR, &pvMetaData, &cbMetaData);
                if (SUCCEEDED(hr))
                {
                    sprintf_s (szString,1024,"MetaData for object file %s:\n", szName);
                    pDisplayString(szString);
                    MDInfo archiveInfo(g_pDisp,
                                    (PBYTE)pvMetaData,
                                    cbMetaData,
                                    pDisplayString,
                                    DumpFilter);
                    archiveInfo.DisplayMD();
                }
                else
                {
                    sprintf_s(szString,1024,"MetaData not found for object file %s!\n\n", szName);
                    pDisplayString(szString);
                }
            }
        }

        // Skip past the object file.
        pbMapAddress = SkipMember(pbMapAddress);
    }

    UnmapViewOfFile(pbStartAddress);
} // void DisplayArchive()

// DisplayFile() function
//
// Opens the meta data content of a .EXE, .CLB, .CLASS, .TLB, .DLL or .LIB file, and
// calls RawDisplay()

void DisplayFile(_In_z_ WCHAR* szFile, BOOL isFile, ULONG DumpFilter, _In_opt_z_ WCHAR* szObjName, strPassBackFn pDisplayString)
{
    // Open the emit scope

    // We need to make sure this file isn't too long. Checking _MAX_PATH is probably safe, but since we have a much
    // larger buffer, we might as well use it all.
    if (wcslen(szFile) > 1000)
        return;


    WCHAR szScope[1024];
	char szString[1024];

    if (isFile)
    {
        wcscpy_s(szScope, 1024, W("file:"));
        wcscat_s(szScope, 1024, szFile);
    }
    else
        wcscpy_s(szScope, 1024, szFile);

    // print bar that separates different files
    pDisplayString("////////////////////////////////////////////////////////////////\n");
    WCHAR rcFname[_MAX_FNAME], rcExt[_MAX_EXT];

    _wsplitpath_s(szFile, NULL, 0, NULL, 0, rcFname, _MAX_FNAME, rcExt, _MAX_EXT);
    sprintf_s(szString,1024,"\nFile %S%S: \n",rcFname, rcExt);
    pDisplayString(szString);

    if (DumpFilter & MDInfo::dumpValidate)
    {
        if (!_wcsicmp(rcExt, OBJ_EXT_W) || !_wcsicmp(rcExt, LIB_EXT_W))
            g_ValModuleType = ValidatorModuleTypeObj;
        else
            g_ValModuleType = ValidatorModuleTypePE;
    }

    if (!_wcsicmp(rcExt, LIB_EXT_W))
        DisplayArchive(szFile, DumpFilter, szObjName, pDisplayString);
    else
    {
        MDInfo metaDataInfo(g_pDisp, szScope, pDisplayString, DumpFilter);
        metaDataInfo.DisplayMD();
    }
} // void DisplayFile()

