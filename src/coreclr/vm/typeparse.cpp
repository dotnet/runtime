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

    OBJECTREF objRequestingAssembly = NULL;
    GCPROTECT_BEGIN(objRequestingAssembly);

    if (pRequestingAssembly != NULL)
    {
        objRequestingAssembly = pRequestingAssembly->GetExposedObject();
    }

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__TYPE_NAME_RESOLVER__GET_TYPE_HELPER);
    DECLARE_ARGHOLDER_ARRAY(args, 5);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(szTypeName);
    args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(objRequestingAssembly);
    args[ARGNUM_2] = BOOL_TO_ARGHOLDER(bThrowIfNotFound);
    args[ARGNUM_3] = BOOL_TO_ARGHOLDER(bRequireAssemblyQualifiedName);
    args[ARGNUM_4] = PTR_TO_ARGHOLDER(unsafeAccessorMethod);

    REFLECTCLASSBASEREF objType = NULL;
    CALL_MANAGED_METHOD_RETREF(objType, REFLECTCLASSBASEREF, args);

    if (objType != NULL)
    {
        type = objType->GetType();
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
