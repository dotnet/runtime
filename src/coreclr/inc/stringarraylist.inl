// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ex.h"

template<typename TEncoding>
inline SString<TEncoding>& StringArrayList<TEncoding>::operator[] (DWORD idx) const
{
    WRAPPER_NO_CONTRACT;
    return Get(idx);
}

template<typename TEncoding>
inline SString<TEncoding>& StringArrayList<TEncoding>::Get (DWORD idx) const
{
    WRAPPER_NO_CONTRACT;
    PTR_SString<TEncoding> ppRet=(PTR_SString<TEncoding>)m_Elements.Get(idx);
    return *ppRet;
}

template<typename TEncoding>
inline DWORD StringArrayList<TEncoding>::GetCount() const
{
    WRAPPER_NO_CONTRACT;
    return m_Elements.GetCount();
}

#ifndef DACCESS_COMPILE
template<typename TEncoding>
inline void StringArrayList<TEncoding>::Append(const SString<TEncoding>& string)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    NewHolder<SString<TEncoding>> pAdd = new SString<TEncoding>(string);
    IfFailThrow(m_Elements.Append(pAdd));
    pAdd.SuppressRelease();
}

template<typename TEncoding>
inline void StringArrayList<TEncoding>::AppendIfNotThere(const SString<TEncoding>& string)
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
inline StringArrayList<TEncoding>::~StringArrayList()
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
        delete (SString<TEncoding>*)m_Elements.Get(i);
    }
#endif
}

