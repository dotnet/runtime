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

inline DWORD EEClassHashTable::Hash(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, DWORD hashEncloser)
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

    if (hashEncloser != 0)
    {
        dwHash = ((dwHash << 5) + dwHash) ^ hashEncloser;
    }

    return  dwHash;
}

#endif // CLASSHASH_INL
