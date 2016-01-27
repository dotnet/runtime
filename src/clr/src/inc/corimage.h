// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

/*============================================================
**
** CorImage.h
**
** IMAGEHLP routines so we can avoid early binding to that DLL.
**
===========================================================*/

#ifndef _CORIMAGE_H_
#define _CORIMAGE_H_

#include <daccess.h>

#ifdef  __cplusplus
extern "C" {
#endif

IMAGE_NT_HEADERS *Cor_RtlImageNtHeader(VOID *pvBase,
                                       ULONG FileLength);

PIMAGE_SECTION_HEADER
Cor_RtlImageRvaToSection(PTR_IMAGE_NT_HEADERS NtHeaders,
                         ULONG Rva,
                         ULONG FileLength);
    
PIMAGE_SECTION_HEADER
Cor_RtlImageRvaRangeToSection(PTR_IMAGE_NT_HEADERS NtHeaders,
                              ULONG Rva,
                              ULONG Range,
                              ULONG FileLength);

DWORD Cor_RtlImageRvaToOffset(PTR_IMAGE_NT_HEADERS NtHeaders,
                              ULONG Rva,
                              ULONG FileLength);

PBYTE Cor_RtlImageRvaToVa(PTR_IMAGE_NT_HEADERS NtHeaders,
                          PBYTE Base,
                          ULONG Rva,
                          ULONG FileLength);

PBYTE Cor_RtlImageDirToVa(PTR_IMAGE_NT_HEADERS NtHeaders,
                          PBYTE Base,
                          UINT  DirIndex,
                          ULONG FileLength);

PBYTE Cor_RtlImageRvaToVa32(PTR_IMAGE_NT_HEADERS32 NtHeaders,
                            PBYTE Base,
                            ULONG Rva,
                            ULONG FileLength);

PBYTE Cor_RtlImageRvaToVa64(PTR_IMAGE_NT_HEADERS64 NtHeaders,
                            PBYTE Base,
                            ULONG Rva,
                            ULONG FileLength);

#ifdef __cplusplus
}
#endif

#endif // _CORIMAGE_H_
