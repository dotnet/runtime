// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// PropertyMap.cpp
//


//
// Implements the PropertyMap class
//
// ============================================================

#include "propertymap.hpp"

namespace BINDER_SPACE
{
    PropertyMap::PropertyMap() : SHash<PropertyHashTraits>::SHash() {
        // Nothing to do here
    }

    PropertyMap::~PropertyMap() {
        // Nothing to do here
    }

    HRESULT PropertyMap::Add(SString *pPropertyName,
                             SBuffer *pPropertyValue)
    {
        _ASSERTE(pPropertyName != NULL);
        _ASSERTE(pPropertyValue != NULL);

        HRESULT hr = S_OK;

        NewHolder<PropertyEntry> pPropertyEntry;
        SAFE_NEW(pPropertyEntry, PropertyEntry);

        pPropertyEntry->SetPropertyName(pPropertyName);
        pPropertyEntry->SetPropertyValue(pPropertyValue);
        
        SHash<PropertyHashTraits>::Add(pPropertyEntry);
        pPropertyEntry.SuppressRelease();
        
    Exit:
        return hr;
    }

    SBuffer *PropertyMap::Lookup(SString *pPropertyName)
    {
        _ASSERTE(pPropertyName != NULL);

        SBuffer *pPropertyValue = NULL;
        PropertyEntry *pPropertyEntry = SHash<PropertyHashTraits>::Lookup(pPropertyName);

        if (pPropertyEntry != NULL)
        {
            pPropertyValue = pPropertyEntry->GetPropertyValue();
        }

        return pPropertyValue;
    }
};
