// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _FACTORY_H_
#define _FACTORY_H_

template<typename PRODUCT>
class Factory
{
public:
    virtual PRODUCT* Create() = 0;
    virtual ~Factory() {}
};

template<typename PRODUCT, DWORD MAX_FACTORY_PRODUCT = 64>
class InlineFactory : public Factory<PRODUCT>
{
public:
    InlineFactory() : m_next(NULL), m_cProduct(0) { WRAPPER_NO_CONTRACT; }
    ~InlineFactory() { WRAPPER_NO_CONTRACT; if (m_next) delete m_next; } 
    PRODUCT* Create();

private:
    InlineFactory* GetNext()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        if (m_next == NULL)
        {
            m_next = new (nothrow) InlineFactory<PRODUCT, MAX_FACTORY_PRODUCT>();
        }
              
        return m_next;
    }

    InlineFactory* m_next;
    PRODUCT m_product[MAX_FACTORY_PRODUCT];
    INT32 m_cProduct;
};

#include "factory.inl"

#endif

