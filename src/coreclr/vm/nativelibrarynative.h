// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: NativeLibraryNative.h
//
//
// QCall's for the NativeLibrary class
//

#ifndef __NATIVELIBRARYNATIVE_H__
#define __NATIVELIBRARYNATIVE_H__

extern "C" INT_PTR QCALLTYPE NativeLibrary_LoadFromPath(LPCWSTR path, BOOL throwOnError);
extern "C" INT_PTR QCALLTYPE NativeLibrary_LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                            BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                            BOOL throwOnError);
extern "C" void QCALLTYPE NativeLibrary_FreeLib(INT_PTR handle);
extern "C" INT_PTR QCALLTYPE NativeLibrary_GetSymbol(INT_PTR handle, LPCWSTR symbolName, BOOL throwOnError);

#endif // __NATIVELIBRARYNATIVE_H__
