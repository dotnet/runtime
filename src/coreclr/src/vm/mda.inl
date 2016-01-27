// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

template<typename PRODUCT>
PRODUCT* MdaFactory<PRODUCT>::Create()
{
    WRAPPER_NO_CONTRACT;

    if (m_cProduct == MDA_MAX_FACTORY_PRODUCT) 
        return GetNext()->Create();

    return &m_product[m_cProduct++];
}
