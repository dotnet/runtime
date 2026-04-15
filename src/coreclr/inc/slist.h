// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//-----------------------------------------------------------------------------
// @File: slist.h
//
// Unified singly linked list template.
//
// HISTORY:
//   02/03/98:  Created helper classes SLink / SList for CoreCLR.
//              Sentinel-based design with pointer-to-member customisation.
//
//   04/14/26:  Replaced with NativeAOT-style design: no sentinel node,
//              NULL means empty, traits-based next-pointer access.
//              Added tail tracking (HasTail in Traits), SListElem<T> wrapper,
//              FindAndRemove, GetNext, DAC guards, cdac_data friend.
//              Migrated all CoreCLR consumers from SLink to m_pNext.
//
//-----------------------------------------------------------------------------

#ifndef _H_SLIST_
#define _H_SLIST_

#include "cdacdata.h"

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4127) // conditional expression is constant
#endif

// ---------------------------------------------------------------------------
// DefaultSListTraits
//
// T must have a field DPTR(T) m_pNext. Grant access by adding:
//   friend struct DefaultSListTraits<T>;
// ---------------------------------------------------------------------------
template <typename T>
struct DefaultSListTraits
{
    typedef DPTR(T) PTR_T;
    typedef DPTR(PTR_T) PTR_PTR_T;

    static constexpr bool HasTail = false;

    static inline PTR_PTR_T GetNextPtr(PTR_T pT)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(pT != NULL);
        return dac_cast<PTR_PTR_T>(dac_cast<TADDR>(pT) + offsetof(T, m_pNext));
    }

    static inline bool Equals(PTR_T pA, PTR_T pB)
    {
        LIMITED_METHOD_CONTRACT;
        return pA == pB;
    }
};

// Convenience traits class that enables tail tracking on SList.
template <typename T>
struct DefaultSListTraitsWithTail : public DefaultSListTraits<T>
{
    static constexpr bool HasTail = true;
};

// ---------------------------------------------------------------------------
// SList
//
// Intrusive singly linked list. Elements must expose a DPTR(T) m_pNext field
// accessible to the Traits class. Use DefaultSListTraitsWithTail<T> as the
// Traits parameter for O(1) tail insertion.
// ---------------------------------------------------------------------------
template <typename T, typename Traits = DefaultSListTraits<T> >
class SList : public Traits
{
protected:
    typedef typename Traits::PTR_T PTR_T;
    typedef typename Traits::PTR_PTR_T PTR_PTR_T;

    // as a generic data structure, friend to all specializations of cdac_data
    template<typename U> friend struct ::cdac_data;

public:
    SList()
    {
        m_pHead = NULL;
        m_pTail = NULL;
    }

    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHead == NULL;
    }

    PTR_T GetHead()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pHead;
    }

    static T* GetNext(T* pObj)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (pObj == NULL)
            return NULL;

        return *Traits::GetNextPtr(dac_cast<PTR_T>(pObj));
    }

#ifndef DACCESS_COMPILE

    void InsertHead(PTR_T pItem)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pItem != NULL);
        if (Traits::HasTail)
        {
            if (m_pHead == NULL)
                m_pTail = pItem;
        }
        *Traits::GetNextPtr(pItem) = m_pHead;
        m_pHead = pItem;
    }

    PTR_T RemoveHead()
    {
        LIMITED_METHOD_CONTRACT;
        PTR_T pRet = m_pHead;
        if (pRet != NULL)
        {
            m_pHead = *Traits::GetNextPtr(pRet);
            if (Traits::HasTail)
            {
                if (m_pHead == NULL)
                    m_pTail = NULL;
            }
        }

        return pRet;
    }

    void InsertTail(PTR_T pItem)
    {
        static_assert(Traits::HasTail, "PushTail requires Traits::HasTail to be true");
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pItem != NULL);
        *Traits::GetNextPtr(pItem) = NULL;
        if (m_pTail != NULL)
            *Traits::GetNextPtr(m_pTail) = pItem;
        else
            m_pHead = pItem;
        m_pTail = pItem;
    }

    PTR_T GetTail()
    {
        static_assert(Traits::HasTail, "GetTail requires Traits::HasTail to be true");
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pTail;
    }

    bool RemoveFirst(PTR_T pItem)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pItem != NULL);

        PTR_T prev = NULL;
        PTR_T cur = m_pHead;
        while (cur != NULL)
        {
            if (Traits::Equals(cur, pItem))
            {
                PTR_T next = *Traits::GetNextPtr(cur);
                if (prev == NULL)
                    m_pHead = next;
                else
                    *Traits::GetNextPtr(prev) = next;

                if (Traits::HasTail && cur == m_pTail)
                    m_pTail = prev;

                return true;
            }
            prev = cur;
            cur = *Traits::GetNextPtr(cur);
        }

        return false;
    }

    // Alias for RemoveFirst, for API compatibility.
    bool FindAndRemove(PTR_T pItem)
    {
        WRAPPER_NO_CONTRACT;

        return RemoveFirst(pItem);
    }

#endif // !DACCESS_COMPILE

    class Iterator
    {
    public:
        Iterator()
            : m_cur(NULL)
        { }

        Iterator(PTR_T p)
            : m_cur(p)
        { }

        Iterator & operator++()
        { m_cur = GetNext(m_cur); return *this; }

        Iterator operator++(int)
        { Iterator t(m_cur); ++(*this); return t; }

        bool operator==(Iterator const & other) const
        { return m_cur == other.m_cur; }

        bool operator!=(Iterator const & other) const
        { return m_cur != other.m_cur; }

        PTR_T operator*() const
        { return m_cur; }

        PTR_T operator->() const
        { return m_cur; }

    private:
        PTR_T m_cur;
    };

    Iterator begin()
    { return Iterator(m_pHead); }

    Iterator end()
    { return Iterator(); }

protected:
    PTR_T m_pHead;
    PTR_T m_pTail;  // Only meaningful when Traits::HasTail is true.
};

// ---------------------------------------------------------------------------
// SListElem — non-intrusive list element wrapper.
// ---------------------------------------------------------------------------
template <typename ElemT>
struct SListElem
{
    SListElem<ElemT>* m_pNext;
    ElemT m_Value;

    operator ElemT const &() const
    { return m_Value; }

    operator ElemT &()
    { return m_Value; }

    ElemT const & operator*() const
    { return m_Value; }

    ElemT & operator*()
    { return m_Value; }

    ElemT const & GetValue() const
    { return m_Value; }

    ElemT & GetValue()
    { return m_Value; }

    SListElem()
        : m_pNext(NULL)
        , m_Value()
    { }

    template <typename T1>
    SListElem(T1&& val)
        : m_pNext(NULL)
        , m_Value(std::forward<T1>(val))
    { }

    template <typename T1, typename T2>
    SListElem(T1&& val1, T2&& val2)
        : m_pNext(NULL)
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2))
    { }

    template <typename T1, typename T2, typename T3>
    SListElem(T1&& val1, T2&& val2, T3&& val3)
        : m_pNext(NULL)
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2), std::forward<T3>(val3))
    { }

    template <typename T1, typename T2, typename T3, typename T4>
    SListElem(T1&& val1, T2&& val2, T3&& val3, T4&& val4)
        : m_pNext(NULL)
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2), std::forward<T3>(val3), std::forward<T4>(val4))
    { }
};

#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif // _H_SLIST_