// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** CorImage.h
**
** IMAGEHLP routines so we can avoid early binding to that DLL.
===========================================================*/

#ifndef _CORIMAGE_H_
#define _CORIMAGE_H_

#include <daccess.h>

#ifdef  __cplusplus
extern "C" {
#endif

IMAGE_NT_HEADERS *Cor_RtlImageNtHeader(VOID *pvBase,
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
