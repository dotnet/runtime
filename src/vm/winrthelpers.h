// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// WinRtHelpers.h
// 

// 
// Helpers to fetch the first WinRT Type def from metadata import
// 
// ======================================================================================

#pragma once

#ifdef FEATURE_COMINTEROP

// --------------------------------------------------------------------------------------
// Return the first public WinRT type's namespace and typename - the names have the lifetime of the MetaData scope.
HRESULT GetFirstWinRTTypeDef(
    IMDInternalImport * pMDInternalImport, 
    LPCSTR *            pszNameSpace,       // Tight to the lifetime of pssFakeNameSpaceAllocationBuffer when the WinMD file is empty
    LPCSTR *            pszTypeName, 
    LPCWSTR             wszAssemblyPath,    // Used for creating fake binding type name in case the WinMD file is empty
    SString *           pssFakeNameSpaceAllocationBuffer);  // Used as allocation buffer for fake namespace

HRESULT GetBindableWinRTName(
    IMDInternalImport * pMDInternalImport, 
    IAssemblyName *     pIAssemblyName);

#endif //FEATURE_COMINTEROP
