// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ex.h"

template<typename TEncoding>
inline EString<TEncoding>& EStringArrayList<TEncoding>::operator[] (DWORD idx) const
{
    WRAPPER_NO_CONTRACT;
    return Get(idx);
}

template<typename TEncoding>
inline EString<TEncoding>& EStringArrayList<TEncoding>::Get (DWORD idx) const
{
    WRAPPER_NO_CONTRACT;
    PTR_EString<TEncoding> ppRet=(PTR_EString<TEncoding>)m_Elements.Get(idx);
    return *ppRet;
}

template<typename TEncoding>
inline DWORD EStringArrayList<TEncoding>::GetCount() const
{
    WRAPPER_NO_CONTRACT;
    return m_Elements.GetCount();
}

#ifndef DACCESS_COMPILE
template<typename TEncoding>
inline void EStringArrayList<TEncoding>::Append(const EString<TEncoding>& string)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    NewHolder<EString<TEncoding>> pAdd = new EString<TEncoding>(string);
    IfFailThrow(m_Elements.Append(pAdd));
    pAdd.SuppressRelease();
}

template<typename TEncoding>
inline void EStringArrayList<TEncoding>::AppendIfNotThere(const EString<TEncoding>& string)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    for (DWORD i=0;i<GetCount();i++)
    {
        if(Get(i).Equals(string))
            return;
    }
    Append(string);
}

#endif


template<typename TEncoding>
inline EStringArrayList<TEncoding>::~EStringArrayList()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
#ifndef DACCESS_COMPILE
    for (DWORD i=0;i< GetCount() ;i++)
    {
        delete (EString<TEncoding>*)m_Elements.Get(i);
    }
#endif
}

