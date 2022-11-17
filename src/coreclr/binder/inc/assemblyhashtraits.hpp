// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyHashTraits.hpp
//


//
// Defines the AssemblyHashTraits template class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_HASH_TRAITS_HPP__
#define __BINDER__ASSEMBLY_HASH_TRAITS_HPP__

#include "bindertypes.hpp"
#include "contextentry.hpp"
#include "shash.h"

namespace BINDER_SPACE
{
    class AssemblyHashTraits : public DeleteElementsOnDestructSHashTraits<NoRemoveSHashTraits<DefaultSHashTraits<ContextEntry*>>>
    {
    public:
        typedef AssemblyName* key_t;

        // GetKey, Equals and Hash can throw due to SString
        static const bool s_NoThrow = false;

        static key_t GetKey(const element_t& pAssemblyEntry)
        {
            return pAssemblyEntry->GetAssemblyName();
        }
        static BOOL Equals(key_t pAssemblyName1, key_t pAssemblyName2)
        {
            return pAssemblyName1->Equals(pAssemblyName2, AssemblyName::INCLUDE_DEFAULT);
        }
        static count_t Hash(key_t pAssemblyName)
        {
            return pAssemblyName->Hash(AssemblyName::INCLUDE_DEFAULT);
        }
        static element_t Null()
        {
            return NULL;
        }
        static bool IsNull(const element_t &assemblyEntry)
        {
            return (assemblyEntry == NULL);
        }
    };
};

#endif
