// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: NativeLibraryNative.cpp
//

#include "common.h"
#include "dllimport.h"
#include "nativelibrarynative.h"

// static
INT_PTR QCALLTYPE NativeLibraryNative::LoadFromPath(LPCWSTR path, BOOL throwOnError)
{
    QCALL_CONTRACT;

    NATIVE_LIBRARY_HANDLE handle = nullptr;

    BEGIN_QCALL;

    handle = NDirect::LoadLibraryFromPath(path, throwOnError);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(handle);
}

// static
INT_PTR QCALLTYPE NativeLibraryNative::LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                                         BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                                         BOOL throwOnError)
{
    QCALL_CONTRACT;

    NATIVE_LIBRARY_HANDLE handle = nullptr;
    Assembly *pAssembly = callingAssembly->GetAssembly();

    BEGIN_QCALL;

    handle = NDirect::LoadLibraryByName(name, pAssembly, hasDllImportSearchPathFlag, dllImportSearchPathFlag, throwOnError);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(handle);
}

// static
void QCALLTYPE NativeLibraryNative::FreeLib(INT_PTR handle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    NDirect::FreeNativeLibrary((NATIVE_LIBRARY_HANDLE) handle);

    END_QCALL;
}

//static
INT_PTR QCALLTYPE NativeLibraryNative::GetSymbol(INT_PTR handle, LPCWSTR symbolName, BOOL throwOnError)
{
    QCALL_CONTRACT;

    INT_PTR address = NULL;

    BEGIN_QCALL;

    address = NDirect::GetNativeLibraryExport((NATIVE_LIBRARY_HANDLE)handle, symbolName, throwOnError);

    END_QCALL;

    return address;
}

