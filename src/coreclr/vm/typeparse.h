// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// typeparse.h
// ---------------------------------------------------------------------------

#ifndef TYPEPARSE_H
#define TYPEPARSE_H

#include "common.h"
#include "class.h"
#include "typehandle.h"

bool inline IsTypeNameReservedChar(WCHAR ch)
{
    LIMITED_METHOD_CONTRACT;

    switch (ch)
    {
    case W(','):
    case W('['):
    case W(']'):
    case W('&'):
    case W('*'):
    case W('+'):
    case W('\\'):
        return true;

    default:
        return false;
    }
}

class TypeName
{
public:
    //-------------------------------------------------------------------------------------------
    // Retrieves a type in the default context. Requires assembly qualified type name.
    //-------------------------------------------------------------------------------------------
    static TypeHandle GetTypeFromAsmQualifiedName(LPCWSTR szFullyQualifiedName, BOOL bThrowIfNotFound = FALSE);


    //-------------------------------------------------------------------------------------------
    // This version is used for resolving types named in custom attributes such as those used
    // for interop. Thus, it follows a well-known multistage set of rules for determining which
    // assembly the type is in. It will also enforce that the requesting assembly has access
    // rights to the type being loaded.
    //
    // The search logic is:
    //
    //    if szTypeName is ASM-qualified, only that assembly will be searched.
    //    if szTypeName is not ASM-qualified, we will search for the types in the following order:
    //       - in pRequestingAssembly (if not NULL). pRequestingAssembly is the assembly that contained
    //         the custom attribute from which the typename was derived.
    //       - in CoreLib
    //       - raise an AssemblyResolveEvent() in the current appdomain
    //
    //--------------------------------------------------------------------------------------------
    static TypeHandle GetTypeReferencedByCustomAttribute(LPCUTF8 szTypeName, Assembly *pRequestingAssembly);
    static TypeHandle GetTypeReferencedByCustomAttribute(LPCWSTR szTypeName, Assembly *pRequestingAssembly);
};

#endif
