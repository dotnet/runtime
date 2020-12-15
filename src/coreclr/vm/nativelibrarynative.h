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

class NativeLibraryNative
{
public:
    static INT_PTR QCALLTYPE LoadFromPath(LPCWSTR path, BOOL throwOnError);
    static INT_PTR QCALLTYPE LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                               BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                               BOOL throwOnError);
    static void QCALLTYPE FreeLib(INT_PTR handle);
    static INT_PTR QCALLTYPE GetSymbol(INT_PTR handle, LPCWSTR symbolName, BOOL throwOnError);

};

#endif // __NATIVELIBRARYNATIVE_H__
