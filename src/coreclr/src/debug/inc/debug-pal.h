//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef Debug_PAL_H
#define Debug_PAL_H

#if defined(FEATURE_PAL)
// This function looks for a dynamic module (libraryName) loaded into the process specified (pId)
// and returns its load address. NULL is module is not loaded.
void *GetDynamicLibraryAddressInProcess(DWORD pid, const char *libraryName);
#endif

#endif //Debug_PAL_H