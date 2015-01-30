//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

template<typename PRODUCT>
PRODUCT* MdaFactory<PRODUCT>::Create()
{
    WRAPPER_NO_CONTRACT;

    if (m_cProduct == MDA_MAX_FACTORY_PRODUCT) 
        return GetNext()->Create();

    return &m_product[m_cProduct++];
}
