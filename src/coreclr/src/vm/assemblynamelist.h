// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// assemblynamelist.h
//

//
// 
/// provides class to implement lookups by assemby name.
/// always checks the simple name
/// never checks culture
///
/// ALSO: it leaks the stored assembly names, so currently can be used only in globals
///
/// checks version/pk/pa only if present in the one being looked up


#ifndef ASSEMBLYNAMELISTHASHTRAITS_H
#define ASSEMBLYNAMELISTHASHTRAITS_H

#include "naming.h"

class AssemblyNameListHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<IAssemblyName*> >
{
public:
    typedef IAssemblyName* key_t;

    static key_t GetKey(element_t pName)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        return pName;
    }
    static const element_t Null()
    {
        LIMITED_METHOD_CONTRACT;

        return NULL;
    }
    static bool IsNull(const element_t &name)
    {
        LIMITED_METHOD_CONTRACT;
        
        return (name == NULL);
    }
    static BOOL Equals(key_t pIAssemblyNameInMap, key_t pIAssemblyNameToCheck)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(pIAssemblyNameInMap));
            PRECONDITION(CheckPointer(pIAssemblyNameToCheck));
        }
        CONTRACTL_END;

        DWORD dwMask = ASM_CMPF_NAME;
        if (CAssemblyName::IsStronglyNamed(pIAssemblyNameInMap))
            dwMask |= ASM_CMPF_PUBLIC_KEY_TOKEN;

        DWORD cbSize = 0;
        HRESULT hr = pIAssemblyNameInMap->GetProperty(ASM_NAME_MAJOR_VERSION, static_cast<PBYTE>(nullptr), &cbSize);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            dwMask |= ASM_CMPF_VERSION;

        cbSize = 0;
        hr = pIAssemblyNameInMap->GetProperty(ASM_NAME_ARCHITECTURE, static_cast<PBYTE>(nullptr), &cbSize);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            dwMask |= ASM_CMPF_ARCHITECTURE;


        hr = pIAssemblyNameToCheck->IsEqual(pIAssemblyNameInMap,
                                              dwMask);
        return (hr == S_OK);
    }

    static count_t Hash(key_t pIAssemblyName)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(pIAssemblyName));
        }
        CONTRACTL_END;

        DWORD dwHash = 0;

        // use only simple name for hashing
        if (FAILED(CAssemblyName::GetHash(pIAssemblyName,0,
                                          0xffffffff,
                                          &dwHash)))
            {
                // Returning bogus hash is safe; it will cause Equals to be called more often
                dwHash = 0;
            }

        return static_cast<count_t>(dwHash);
    }
};


typedef SHash<AssemblyNameListHashTraits> AssemblyNameList;

#endif // ASSEMBLYNAMELISTHASHTRAITS_H
