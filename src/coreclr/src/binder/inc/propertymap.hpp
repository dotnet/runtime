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
