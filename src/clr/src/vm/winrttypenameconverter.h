// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: WinRTTypeNameConverter.cpp
//

//

//
// ============================================================================

#ifndef FEATURE_COMINTEROP
#error This file should only be included when FEATURE_COMINTEROP is defined
#endif 

#pragma once

#include "..\md\winmd\inc\adapter.h"
#include "clrprivbinding.h"

struct WinRTTypeNameInfo;

//
// Converts between a WinRT type name and TypeHandle
//
class WinRTTypeNameConverter
{
public :    
    //==============================================================================================
    // Managed -> WinRT
    //==============================================================================================

    //
    // Append WinRT type name for the specified type handle
    //
    static bool AppendWinRTTypeNameForManagedType(
        TypeHandle      thManagedType,
        SString         &strWinRTTypeName,
        bool            bForGetRuntimeClassName,
        bool            *pbIsPrimitive);

    //
    // Return the redirection index and type kind if the MethodTable* is a redirected type
    //
    static bool ResolveRedirectedType(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex * pIndex, WinMDAdapter::WinMDTypeKind * pKind = NULL);

    //
    // Append the WinRT type name for the method table, if it is a WinRT primitive type
    //
    static bool AppendWinRTNameForPrimitiveType(MethodTable *pMT, SString &strName);
    
    //
    // Is the specified MethodTable a WinRT primitive type
    //
    static bool IsWinRTPrimitiveType(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        return GetWinRTNameForPrimitiveType(pMT, NULL);
    }

    //
    // Is the specified MethodTable a redirected CLR type?
    //
    static bool IsRedirectedType(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        return ResolveRedirectedType(pMT, NULL);        
    }

    static bool IsRedirectedType(MethodTable *pMT, WinMDAdapter::WinMDTypeKind kind);

    //
    // Determine if the given type redirected only by doing name comparisons.  This is used to
    // calculate the redirected type index at EEClass creation time.
    //
    static WinMDAdapter::RedirectedTypeIndex GetRedirectedTypeIndexByName(
        Module *pModule,
        mdTypeDef token);
public :
    //==============================================================================================
    // WinRT -> Managed
    //==============================================================================================

    //
    // Is the specified MethodTable a redirected WinRT type?
    //
    static bool IsRedirectedWinRTSourceType(MethodTable *pMT);

    //
    // Get TypeHandle from a WinRT type name
    // Parse the WinRT type name in the form of WinRTType=TypeName[<WinRTType[, WinRTType, ...]>]
    //
    static TypeHandle LoadManagedTypeForWinRTTypeName(LPCWSTR wszWinRTTypeName, ICLRPrivBinder * loadBinder, bool *pbIsPrimitive);
    
private :
    
    //
    // Get predefined WinRT name for a primitive type
    //
    static bool GetWinRTNameForPrimitiveType(MethodTable *pMT, SString *pName);

    //
    // Return MethodTable* for the specified WinRT primitive type name
    //
    static bool GetMethodTableFromWinRTPrimitiveType(LPCWSTR wszTypeName, UINT32 uTypeNameLen, MethodTable **ppMT);
    
    //
    // Return TypeHandle for the specified WinRT type name (supports generic type)
    // Updates wszWinRTTypeName pointer as it parse the string    
    //
    static TypeHandle LoadManagedTypeForWinRTTypeNameInternal(SString *ssTypeName, ICLRPrivBinder* loadBinder, bool *pbIsPrimitive);
    
    //
    // Return MethodTable* for the specified WinRT primitive type name (non-generic type)
    // Updates wszWinRTTypeName pointer as it parse the string
    //
    static TypeHandle GetManagedTypeFromSimpleWinRTNameInternal(SString *ssTypeName, ICLRPrivBinder* loadBinder, bool *pbIsPrimitive);

    static bool AppendWinRTTypeNameForManagedType(
        TypeHandle          thManagedType,
        SString            &strWinRTTypeName,
        bool                bForGetRuntimeClassName,
        bool               *pbIsPrimitive,
        WinRTTypeNameInfo  *pCurrentTypeInfo);
};
