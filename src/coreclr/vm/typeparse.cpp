// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// typeparse.cpp
// ---------------------------------------------------------------------------

#include "common.h"
#include "typeparse.h"

static TypeHandle GetTypeHelper(LPCWSTR szTypeName, Assembly* pRequestingAssembly, BOOL bThrowIfNotFound, BOOL bRequireAssemblyQualifiedName, MethodDesc* unsafeAccessorMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeHandle type;

    GCX_COOP();

    struct {
        OBJECTREF objRequestingAssembly;
        OBJECTREF objType;
    } gc;
    gc.objRequestingAssembly = NULL;
    gc.objType = NULL;

    GCPROTECT_BEGIN(gc);

    if (pRequestingAssembly != NULL)
    {
        gc.objRequestingAssembly = pRequestingAssembly->GetExposedObject();
    }

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    UnmanagedCallersOnlyCaller getTypeHelper(METHOD__TYPE_NAME_RESOLVER__GET_TYPE_HELPER);
    getTypeHelper.InvokeThrowing(szTypeName, &gc.objRequestingAssembly, CLR_BOOL_ARG(bThrowIfNotFound), CLR_BOOL_ARG(bRequireAssemblyQualifiedName), (INT_PTR)unsafeAccessorMethod, &gc.objType);

    if (gc.objType != NULL)
    {
        type = ((REFLECTCLASSBASEREF)gc.objType)->GetType();
    }

    GCPROTECT_END();

    return type;
}

TypeHandle TypeName::GetTypeReferencedByCustomAttribute(LPCUTF8 szTypeName, Assembly* pRequestingAssembly)
{
    WRAPPER_NO_CONTRACT;
    StackSString sszAssemblyQualifiedName(SString::Utf8, szTypeName);
    return GetTypeHelper(sszAssemblyQualifiedName.GetUnicode(), pRequestingAssembly, TRUE /* bThrowIfNotFound */, FALSE /* bRequireAssemblyQualifiedName */, NULL /* unsafeAccessorMethod */);
}

TypeHandle TypeName::GetTypeReferencedByCustomAttribute(LPCWSTR szTypeName, Assembly* pRequestingAssembly, MethodDesc* unsafeAccessorMethod)
{
    WRAPPER_NO_CONTRACT;
    return GetTypeHelper(szTypeName, pRequestingAssembly, TRUE /* bThrowIfNotFound */, FALSE /* bRequireAssemblyQualifiedName */, unsafeAccessorMethod);
}

TypeHandle TypeName::GetTypeFromAsmQualifiedName(LPCWSTR szFullyQualifiedName, BOOL bThrowIfNotFound)
{
    WRAPPER_NO_CONTRACT;
    return GetTypeHelper(szFullyQualifiedName, NULL, bThrowIfNotFound, TRUE /* bRequireAssemblyQualifiedName */, NULL /* unsafeAccessorMethod */);
}
