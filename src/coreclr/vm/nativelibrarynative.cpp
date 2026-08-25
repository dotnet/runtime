// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: NativeLibraryNative.cpp
//

#include "common.h"
#include "nativelibrary.h"
#include "nativelibrarynative.h"

// static
extern "C" QCallExceptionStatus QCALLTYPE NativeLibrary_LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                                         BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                                         BOOL throwOnError, INT_PTR* pReturnValue)
{
    QCALL_CONTRACT;

    NATIVE_LIBRARY_HANDLE handle = nullptr;

    BEGIN_QCALL;

    handle = NativeLibrary::LoadLibraryByName(name, callingAssembly, hasDllImportSearchPathFlag, dllImportSearchPathFlag, throwOnError);

    *pReturnValue = reinterpret_cast<INT_PTR>(handle);

    END_QCALL;
}
