// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"

int GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token)
{
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
            ThrowHR(COR_E_BADIMAGEFORMAT);

        switch (TypeFromToken(token))
        {
        case mdtTypeDef:
            if (FAILED(pMDImport->GetNameOfTypeDef(token, &szName, &szNamespace)))
                ThrowHR(COR_E_BADIMAGEFORMAT);
            hr = pMDImport->GetNestedClassProps(token, &token);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hasTypeToken = false;
            else if (FAILED(hr))
                ThrowHR(COR_E_BADIMAGEFORMAT);
            break;

        case mdtTypeRef:
            if (FAILED(pMDImport->GetNameOfTypeRef(token, &szNamespace, &szName)))
                ThrowHR(COR_E_BADIMAGEFORMAT);
            if (FAILED(pMDImport->GetResolutionScopeOfTypeRef(token, &token)))
                ThrowHR(COR_E_BADIMAGEFORMAT);
            hasTypeToken = (TypeFromToken(token) == mdtTypeRef);
            break;

        case mdtExportedType:
            if (FAILED(pMDImport->GetExportedTypeProps(token, &szNamespace, &szName, &token, NULL, NULL)))
                ThrowHR(COR_E_BADIMAGEFORMAT);
            hasTypeToken = (TypeFromToken(token) == mdtExportedType);
            break;

        default:
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        hashcode ^= ComputeNameHashCode(szNamespace, szName);
    }

    return hashcode;
}

#ifndef DACCESS_COMPILE
int GetVersionResilientTypeHashCode(TypeHandle type)
{
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
    if (type.IsArray())
    {
        ArrayTypeDesc *pArray = type.AsArray();
        return ComputeArrayTypeHashCode(GetVersionResilientTypeHashCode(pArray->GetArrayElementTypeHandle()), pArray->GetRank());
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
#endif // DACCESS_COMPILE
