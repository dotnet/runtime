// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// PropertyMap.hpp
//


// 
// Defines the PropertyMap class
//
// ============================================================

#ifndef __BINDER__PROPERTY_HASH_TRAITS_HPP__
#define __BINDER__PROPERTY_HASH_TRAITS_HPP__

#include "bindertypes.hpp"
#include "sstring.h"
#include "shash.h"

namespace BINDER_SPACE
{
    class PropertyEntry
    {
    public:
        inline PropertyEntry()
        {
            m_pPropertyName = NULL;
            m_pPropertyValue = NULL;
        }
        inline ~PropertyEntry()
        {
            SAFE_DELETE(m_pPropertyName);
            SAFE_DELETE(m_pPropertyValue);
        }

        // Getters/Setters
        inline SString *GetPropertyName()
        {
            return m_pPropertyName;
        }
        inline void SetPropertyName(SString *pPropertyName)
        {
            SAFE_DELETE(m_pPropertyName);
            m_pPropertyName = pPropertyName;
        }
        inline SBuffer *GetPropertyValue()
        {
            return m_pPropertyValue;
        }
        inline void SetPropertyValue(SBuffer *pPropertyValue)
        {
            SAFE_DELETE(m_pPropertyValue);
            m_pPropertyValue = pPropertyValue;
        }

    protected:
        SString *m_pPropertyName;
        SBuffer *m_pPropertyValue;
    };

    class PropertyHashTraits : public DefaultSHashTraits<PropertyEntry *>
    {
    public:
        typedef SString* key_t;
 
        // GetKey, Equals, and Hash can throw due to SString
        static const bool s_NoThrow = false;

        static key_t GetKey(element_t pPropertyEntry)
        {
            return pPropertyEntry->GetPropertyName();
        }
        static BOOL Equals(key_t pPropertyName1, key_t pPropertyName2)
        {
            return pPropertyName1->Equals(*pPropertyName2);
        }
        static count_t Hash(key_t pPropertyName)
        {
            return pPropertyName->Hash();
        }
        static element_t Null()
        {
            return NULL;
        }
        static bool IsNull(const element_t &propertyEntry)
        {
            return (propertyEntry == NULL);
        }

    };
};

#endif
