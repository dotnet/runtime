// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Hash table associated with each module that records for all types defined in that module the mapping
// between type name and token (or TypeHandle).
//


#ifndef CLASSHASH_INL
#define CLASSHASH_INL

//  Low bit is discriminator between unresolved and resolved.
//     Low bit == 0:  Resolved:   data == TypeHandle
//     Low bit == 1:  Unresolved: data encodes either a typeDef or exportTypeDef. Use bit 31 as discriminator.
//
//  If not resolved, bit 31 (64-bit: yes, it's bit31, not the high bit!) is discriminator between regular typeDef and exportedType
//
//     Bit31   == 0:  mdTypeDef:      000t tttt tttt tttt tttt tttt tttt ttt1
//     Bit31   == 1:  mdExportedType: 100e eeee eeee eeee eeee eeee eeee eee1
//
//

/* static */
inline PTR_VOID EEClassHashTable::CompressClassDef(mdToken cl)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(TypeFromToken(cl) == mdtTypeDef || TypeFromToken(cl) == mdtExportedType);

    switch (TypeFromToken(cl))
    {
        case mdtTypeDef:      return (PTR_VOID)(                         0 | (((ULONG_PTR)cl & 0x00ffffff) << 1) | EECLASSHASH_TYPEHANDLE_DISCR);
        case mdtExportedType: return (PTR_VOID)(EECLASSHASH_MDEXPORT_DISCR | (((ULONG_PTR)cl & 0x00ffffff) << 1) | EECLASSHASH_TYPEHANDLE_DISCR);
        default:
            _ASSERTE(!"Can't get here.");
            return 0;
    }
}

static DWORD HashName(LPCUTF8 pszNamespace, LPCUTF8 pszClassName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    DWORD dwHash = 5381;
    DWORD dwChar;

    while ((dwChar = *pszNamespace++) != 0)
        dwHash = ((dwHash << 5) + dwHash) ^ dwChar;

    while ((dwChar = *pszClassName++) != 0)
        dwHash = ((dwHash << 5) + dwHash) ^ dwChar;

    // reseve 0 hash so that we could pass 0 in no-encloser case, 31 bit is still good for our needs
    return  dwHash | 1;
}

static DWORD Combine(DWORD hash, DWORD encloserHash)
{
    LIMITED_METHOD_CONTRACT;

    // NB: there are better ways to combine hashes, but here we need a commutative operator,
    //     since we are not always combining hashes starting from the innermost name
    hash = hash ^ encloserHash;

    // reseve 0 hash so that we could pass 0 in no-encloser case, 31 bit is still good for our needs
    return hash | 1;
}

inline DWORD EEClassHashTable::Hash(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, DWORD encloserHash)
{
    LIMITED_METHOD_CONTRACT;

    return Combine(HashName(pszNamespace, pszClassName), encloserHash);
}

// hashes the name specified by the token and names of all its containers
inline DWORD EEClassHashTable::Hash(IMDInternalImport* pMDImport, mdToken token)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
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
    DWORD hashcode = 0;

    while (hasTypeToken)
    {
        if (IsNilToken(token))
            return 0;

        switch (TypeFromToken(token))
        {
        case mdtTypeDef:
            IfFailThrow(pMDImport->GetNameOfTypeDef(token, &szName, &szNamespace));
            hr = pMDImport->GetNestedClassProps(token, &token);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hasTypeToken = false;
            else
                IfFailThrow(hr);
            break;

        case mdtTypeRef:
            IfFailThrow(pMDImport->GetNameOfTypeRef(token, &szNamespace, &szName));
            IfFailThrow(pMDImport->GetResolutionScopeOfTypeRef(token, &token));
            hasTypeToken = (TypeFromToken(token) == mdtTypeRef);
            break;

        case mdtExportedType:
            IfFailThrow(pMDImport->GetExportedTypeProps(token, &szNamespace, &szName, &token, NULL, NULL));
            hasTypeToken = (TypeFromToken(token) == mdtExportedType);
            break;

        default:
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
        }

        DWORD nameHash = HashName(szNamespace, szName);
        hashcode = Combine(nameHash, hashcode);
    }

    return hashcode;
}

#endif // CLASSHASH_INL
