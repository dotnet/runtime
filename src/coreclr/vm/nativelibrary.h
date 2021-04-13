// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NATIVELIBRARY_H_
#define _NATIVELIBRARY_H_

#include <clrtypes.h>

class LoadLibErrorTracker;

class NativeLibrary
{
public:
    static NATIVE_LIBRARY_HANDLE LoadLibraryFromPath(LPCWSTR libraryPath, BOOL throwOnError);
    static NATIVE_LIBRARY_HANDLE LoadLibraryByName(LPCWSTR name, Assembly *callingAssembly,
                                                   BOOL hasDllImportSearchPathFlags, DWORD dllImportSearchPathFlags,
                                                   BOOL throwOnError);
    static NATIVE_LIBRARY_HANDLE LoadNativeLibrary(NDirectMethodDesc * pMD, LoadLibErrorTracker *pErrorTracker);
    static void FreeNativeLibrary(NATIVE_LIBRARY_HANDLE handle);
    static INT_PTR GetNativeLibraryExport(NATIVE_LIBRARY_HANDLE handle, LPCWSTR symbolName, BOOL throwOnError);

    static NATIVE_LIBRARY_HANDLE LoadNativeLibrary(NDirectMethodDesc *pMD);
};

#endif // _NATIVELIBRARY_H_
