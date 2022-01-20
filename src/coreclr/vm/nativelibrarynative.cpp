// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: NativeLibraryNative.cpp
//

#include "common.h"
#include "nativelibrary.h"
#include "nativelibrarynative.h"

// static
extern "C" INT_PTR QCALLTYPE NativeLibrary_LoadFromPath(LPCWSTR path, BOOL throwOnError)
{
    QCALL_CONTRACT;

    NATIVE_LIBRARY_HANDLE handle = nullptr;

    BEGIN_QCALL;

    handle = NativeLibrary::LoadLibraryFromPath(path, throwOnError);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(handle);
}

// static
extern "C" INT_PTR QCALLTYPE NativeLibrary_LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                                         BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                                         BOOL throwOnError)
{
    QCALL_CONTRACT;

    NATIVE_LIBRARY_HANDLE handle = nullptr;
    Assembly *pAssembly = callingAssembly->GetAssembly();

    BEGIN_QCALL;

    handle = NativeLibrary::LoadLibraryByName(name, pAssembly, hasDllImportSearchPathFlag, dllImportSearchPathFlag, throwOnError);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(handle);
}

// static
extern "C" void QCALLTYPE NativeLibrary_FreeLib(INT_PTR handle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    NativeLibrary::FreeNativeLibrary((NATIVE_LIBRARY_HANDLE) handle);

    END_QCALL;
}

//static
extern "C" INT_PTR QCALLTYPE NativeLibrary_GetSymbol(INT_PTR handle, LPCWSTR symbolName, BOOL throwOnError)
{
    QCALL_CONTRACT;

    INT_PTR address = NULL;

    BEGIN_QCALL;

    address = NativeLibrary::GetNativeLibraryExport((NATIVE_LIBRARY_HANDLE)handle, symbolName, throwOnError);

    END_QCALL;

    return address;
}
