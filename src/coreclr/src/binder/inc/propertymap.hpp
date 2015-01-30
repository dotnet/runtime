//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ============================================================
//
// PropertyMap.hpp
//


//
// Defines the PropertyMap class
//
// ============================================================


#ifndef __BINDER__PROPERTY_MAP_HPP__
#define __BINDER__PROPERTY_MAP_HPP__

#include "propertyhashtraits.hpp"

namespace BINDER_SPACE
{
    class PropertyMap : protected SHash<PropertyHashTraits>
    {
    public:
        PropertyMap();
        ~PropertyMap();

        HRESULT Add(/* in */ SString *pPropertyName,
                    /* in */ SBuffer *pPropertyValue);
        SBuffer *Lookup(/* in */ SString *pPropertyName);
    };
};

#endif
