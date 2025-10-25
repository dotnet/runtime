// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NATIVELIBRARY_H_
#define _NATIVELIBRARY_H_

#include <clrtypes.h>

class NativeLibrary
{
public:
    static NATIVE_LIBRARY_HANDLE LoadFromAssemblyDirectory(LPCWSTR libraryName, Assembly *callingAssembly);
    static NATIVE_LIBRARY_HANDLE LoadLibraryFromMethodDesc(PInvokeMethodDesc *pMD);
};

#endif // _NATIVELIBRARY_H_
