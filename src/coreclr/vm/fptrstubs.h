// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _FPTRSTUBS_H
#define _FPTRSTUBS_H

#include "common.h"

// FuncPtrStubs contains stubs that is used by GetMultiCallableAddrOfCode() if
// the function has not been jitted. Using a stub decouples ldftn from
// the prestub, so prestub does not need to be backpatched.
//
// This stub is also used in other places which need a function pointer

class FuncPtrStubs
{
public :
    FuncPtrStubs();

    Precode*            Lookup(MethodDesc * pMD, PrecodeType type);
    PCODE               GetFuncPtrStub(MethodDesc * pMD, PrecodeType type);

    Precode*            Lookup(MethodDesc * pMD)
    {
        return Lookup(pMD, GetDefaultType(pMD));
    }

    PCODE               GetFuncPtrStub(MethodDesc * pMD)
    {
        return GetFuncPtrStub(pMD, GetDefaultType(pMD));
    }

    static PrecodeType GetDefaultType(MethodDesc* pMD);

private:
    Crst                m_hashTableCrst;

    struct PrecodeKey
    {
        PrecodeKey(MethodDesc* pMD, PrecodeType type)
            : m_pMD(pMD), m_type(type)
        {
        }

        MethodDesc*     m_pMD;
        PrecodeType     m_type;
    };

    class PrecodeTraits : public NoRemoveSHashTraits< DefaultSHashTraits<Precode*> >
    {
    public:
        typedef PrecodeKey key_t;

        static key_t GetKey(element_t e)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;
            return PrecodeKey(e->GetMethodDesc(), e->GetType());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return (k1.m_pMD == k2.m_pMD) && (k1.m_type == k2.m_type);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k.m_pMD ^ k.m_type;
        }
    };

    SHash<PrecodeTraits>    m_hashTable;    // To find a existing stub for a method
};

#endif // _FPTRSTUBS_H
