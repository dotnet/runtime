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

extern "C" QCallExceptionStatus QCALLTYPE NativeLibrary_LoadByName(LPCWSTR name, QCall::AssemblyHandle callingAssembly,
                                            BOOL hasDllImportSearchPathFlag, DWORD dllImportSearchPathFlag,
                                            BOOL throwOnError, INT_PTR* pReturnValue);

#endif // __NATIVELIBRARYNATIVE_H__
