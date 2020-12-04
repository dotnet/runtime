// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FACTORY_INL_
#define _FACTORY_INL_

#include "factory.h"

template<typename PRODUCT, DWORD MAX_FACTORY_PRODUCT>
PRODUCT* InlineFactory<PRODUCT, MAX_FACTORY_PRODUCT>::Create()
{
    WRAPPER_NO_CONTRACT;

    if (m_cProduct == MAX_FACTORY_PRODUCT)
    {
        InlineFactory* pNext = GetNext();
        if (pNext)
        {
            return pNext->Create();
        }
        else
        {
            return NULL;
        }
    }

    return &m_product[m_cProduct++];
}

#endif

