//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    imagehlp.h

Abstract:

    This module defines the prototypes and constants required for the image
    help routines.




--*/

#ifndef _IMAGEHLP_
#define _IMAGEHLP_

#if _MSC_VER > 1020
#pragma once
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define IMAGEAPI __stdcall

PIMAGE_NT_HEADERS
IMAGEAPI
CheckSumMappedFile (
    PVOID BaseAddress,
    DWORD FileLength,
    PDWORD HeaderSum,
    PDWORD CheckSum
    );

PIMAGE_NT_HEADERS
IMAGEAPI
ImageNtHeader (
    IN PVOID Base
    );

PVOID
IMAGEAPI
ImageRvaToVa(
    IN PIMAGE_NT_HEADERS NtHeaders,
    IN PVOID Base,
    IN ULONG Rva,
    IN OUT PIMAGE_SECTION_HEADER *LastRvaSection
    );

#ifdef __cplusplus
}
#endif

#endif // _IMAGEHLP_
