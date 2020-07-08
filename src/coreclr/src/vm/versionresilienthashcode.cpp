// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"

bool GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pdwHashCode));
    }
    CONTRACTL_END

    _ASSERTE(TypeFromToken(token) == mdtTypeDef ||
        TypeFromToken(token) == mdtTypeRef ||
        TypeFromToken(token) == mdtExportedType);
    _ASSERTE(!IsNilToken(token));

    HRESULT hr;
    LPCUTF8 szNamespace;
    LPCUTF8 szName;
    bool hasTypeToken = true;
    int hashcode = 0;

    while (hasTypeToken)
    {
        if (IsNilToken(token))
            return false;

        switch (TypeFromToken(token))
        {
        case mdtTypeDef:
            if (FAILED(pMDImport->GetNameOfTypeDef(token, &szName, &szNamespace)))
                return false;
            hr = pMDImport->GetNestedClassProps(token, &token);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hasTypeToken = false;
            else if (FAILED(hr))
                return false;
            break;

        case mdtTypeRef:
            if (FAILED(pMDImport->GetNameOfTypeRef(token, &szNamespace, &szName)))
                return false;
            if (FAILED(pMDImport->GetResolutionScopeOfTypeRef(token, &token)))
                return false;
            hasTypeToken = (TypeFromToken(token) == mdtTypeRef);
            break;

        case mdtExportedType:
            if (FAILED(pMDImport->GetExportedTypeProps(token, &szNamespace, &szName, &token, NULL, NULL)))
                return false;
            hasTypeToken = (TypeFromToken(token) == mdtExportedType);
            break;

        default:
            return false;
        }

        hashcode ^= ComputeNameHashCode(szNamespace, szName);
    }

    *pdwHashCode = hashcode;

    return true;
}

#ifndef DACCESS_COMPILE
int GetVersionResilientTypeHashCode(TypeHandle type)
{
    STANDARD_VM_CONTRACT;

    if (type.IsArray())
    {
        return ComputeArrayTypeHashCode(GetVersionResilientTypeHashCode(type.GetArrayElementTypeHandle()), type.GetRank());
    }
    else
    if (!type.IsTypeDesc())
    {
        MethodTable *pMT = type.AsMethodTable();

        _ASSERTE(!pMT->IsArray());
        _ASSERTE(!IsNilToken(pMT->GetCl()));

        LPCUTF8 szNamespace;
        LPCUTF8 szName;
        IfFailThrow(pMT->GetMDImport()->GetNameOfTypeDef(pMT->GetCl(), &szName, &szNamespace));
        int hashcode = ComputeNameHashCode(szNamespace, szName);

        MethodTable *pMTEnclosing = pMT->LoadEnclosingMethodTable(CLASS_LOAD_UNRESTOREDTYPEKEY);
        if (pMTEnclosing != NULL)
        {
            hashcode = ComputeNestedTypeHashCode(GetVersionResilientTypeHashCode(TypeHandle(pMTEnclosing)), hashcode);
        }

        if (!pMT->IsGenericTypeDefinition() && pMT->HasInstantiation())
        {
            return ComputeGenericInstanceHashCode(hashcode,
                pMT->GetInstantiation().GetNumArgs(), pMT->GetInstantiation(), GetVersionResilientTypeHashCode);
        }
        else
        {
            return hashcode;
        }
    }
    else
    if (type.IsPointer())
    {
        return ComputePointerTypeHashCode(GetVersionResilientTypeHashCode(type.AsTypeDesc()->GetTypeParam()));
    }
    else
    if (type.IsByRef())
    {
        return ComputeByrefTypeHashCode(GetVersionResilientTypeHashCode(type.AsTypeDesc()->GetTypeParam()));
    }

    assert(false);
    return 0;
}

int GetVersionResilientMethodHashCode(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    int hashCode = GetVersionResilientTypeHashCode(TypeHandle(pMD->GetMethodTable()));

    // Todo: Add signature to hash.
    if (pMD->GetNumGenericMethodArgs() > 0)
    {
        hashCode ^= ComputeGenericInstanceHashCode(ComputeNameHashCode(pMD->GetName()), pMD->GetNumGenericMethodArgs(), pMD->GetMethodInstantiation(), GetVersionResilientTypeHashCode);
    }
    else
    {
        hashCode ^= ComputeNameHashCode(pMD->GetName());
    }

    return hashCode;
}

int GetVersionResilientModuleHashCode(Module* pModule)
{
    return ComputeNameHashCode(pModule->GetSimpleName());
}
#endif // DACCESS_COMPILE
