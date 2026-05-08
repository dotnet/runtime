// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//-----------------------------------------------------------------------------
// @File: slist.h
//
// Unified singly linked list template shared between CoreCLR VM and
// NativeAOT Runtime.
//
//-----------------------------------------------------------------------------

#ifndef _H_SLIST_
#define _H_SLIST_

// ---------------------------------------------------------------------------
// Environment compatibility — define CoreCLR macros as no-ops for NativeAOT.
// ---------------------------------------------------------------------------
#if defined(FEATURE_NATIVEAOT)
  #define LIMITED_METHOD_CONTRACT
  #define LIMITED_METHOD_DAC_CONTRACT
  #define WRAPPER_NO_CONTRACT
#endif

#include "cdacdata.h"
#include <utility> // std::forward (used by SListElem)

// ---------------------------------------------------------------------------
// SListMode — controls which SList operations are permitted.
// ---------------------------------------------------------------------------
enum class SListMode
{
    Thin,         // Head-only: InsertHead, RemoveHead, no tail.
    Tail,         // Adds InsertTail/GetTail for O(1) tail insertion.
};

// ---------------------------------------------------------------------------
// SListTraits
//
// T must have a pointer-sized m_pNext field.
// ---------------------------------------------------------------------------
template <typename T, SListMode Mode = SListMode::Thin>
struct SListTraits
{
    typedef DPTR(T) PTR_T;
    typedef DPTR(PTR_T) PTR_PTR_T;

    static constexpr bool HasTail = (Mode == SListMode::Tail);

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

// ---------------------------------------------------------------------------
// SListTailBase — conditionally holds m_pTail when Traits::HasTail is true.
// ---------------------------------------------------------------------------
template <typename PTR_T, bool hasTail>
struct SListTailBase { };

template <typename PTR_T>
struct SListTailBase<PTR_T, true>
{
    PTR_T m_pTail = NULL;
};

// ---------------------------------------------------------------------------
// SList
//
// Intrusive singly linked list. Elements must have a pointer-sized m_pNext
// field. Use SListTraits<T, SListMode::Tail> for O(1) tail insertion.
// ---------------------------------------------------------------------------
template <typename T, typename Traits = SListTraits<T>>
struct SList : public Traits, private SListTailBase<typename Traits::PTR_T, Traits::HasTail>
{
    typedef typename Traits::PTR_T PTR_T;
    typedef typename Traits::PTR_PTR_T PTR_PTR_T;

    // as a generic data structure, friend to all specializations of cdac_data
    template<typename U> friend struct ::cdac_data;

    PTR_T m_pHead = NULL;

public:

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

    // ---------------------------------------------------------------------------
    // Pointer-to-pointer iterator — supports Insert and Remove at the current
    // position. Used via Begin()/End().
    // ---------------------------------------------------------------------------
    class Iterator
    {
        friend struct SList;

    public:
        Iterator(Iterator const &it)
            : m_ppCur(it.m_ppCur)
#ifdef _DEBUG
            , m_fIsValid(it.m_fIsValid)
#endif
        { }

        Iterator& operator=(Iterator const &it)
        {
            m_ppCur = it.m_ppCur;
#ifdef _DEBUG
            m_fIsValid = it.m_fIsValid;
#endif
            return *this;
        }

        PTR_T operator->()
        { _Validate(e_HasValue); return _Value(); }

        PTR_T operator*()
        { _Validate(e_HasValue); return _Value(); }

        Iterator & operator++()
        {
            _Validate(e_HasValue);
            m_ppCur = Traits::GetNextPtr(_Value());
            return *this;
        }

        Iterator operator++(int)
        {
            _Validate(e_HasValue);
            PTR_PTR_T ppRet = m_ppCur;
            ++(*this);
            return Iterator(ppRet);
        }

        bool operator==(Iterator const &rhs)
        {
            _Validate(e_CanCompare);
            rhs._Validate(e_CanCompare);
            return Traits::Equals(_Value(), rhs._Value());
        }

        bool operator==(PTR_T pT)
        {
            _Validate(e_CanCompare);
            return Traits::Equals(_Value(), pT);
        }

        bool operator!=(Iterator const &rhs)
        { return !operator==(rhs); }

    private:
        Iterator(PTR_PTR_T ppItem)
            : m_ppCur(ppItem)
#ifdef _DEBUG
            , m_fIsValid(true)
#endif
        { }

        Iterator Insert(PTR_T pItem)
        {
            _Validate(e_CanInsert);
            *Traits::GetNextPtr(pItem) = *m_ppCur;
            *m_ppCur = pItem;
            Iterator itRet(m_ppCur);
            ++(*this);
            return itRet;
        }

        Iterator Remove()
        {
            _Validate(e_HasValue);
            *m_ppCur = *Traits::GetNextPtr(*m_ppCur);
            PTR_PTR_T ppRet = m_ppCur;
            *this = End();
            return Iterator(ppRet);
        }

        static Iterator End()
        { return Iterator(NULL); }

        PTR_PTR_T m_ppCur;
#ifdef _DEBUG
        mutable bool m_fIsValid;
#endif

        PTR_T _Value() const
        {
#ifdef _DEBUG
            _ASSERTE(m_fIsValid);
#endif
            return dac_cast<PTR_T>(m_ppCur == NULL ? NULL : *m_ppCur);
        }

        enum e_ValidateOperation
        {
            e_CanCompare,
            e_CanInsert,
            e_HasValue,
        };

        void _Validate(e_ValidateOperation op) const
        {
#ifdef _DEBUG
            _ASSERTE(m_fIsValid);
#endif
            if ((op != e_CanCompare && m_ppCur == NULL) ||
                (op == e_HasValue && *m_ppCur == NULL))
            {
                _ASSERTE(!"Invalid SList::Iterator use.");
#ifdef _DEBUG
                m_fIsValid = false;
#endif
            }
        }
    };

    Iterator Begin()
    {
        typedef SList<T, Traits> T_THIS;
        return Iterator(dac_cast<PTR_PTR_T>(
            dac_cast<TADDR>(this) + offsetof(T_THIS, m_pHead)));
    }

    Iterator End()
    { return Iterator::End(); }

    Iterator FindFirst(PTR_T pItem)
    {
        Iterator it = Begin();
        for (; it != End(); ++it)
        {
            if (Traits::Equals(*it, pItem))
                break;
        }
        return it;
    }

    // Inserts pItem *before* it. Returns iterator pointing to inserted item.
    Iterator Insert(Iterator & it, PTR_T pItem)
    {
        static_assert(!Traits::HasTail, "Iterator Insert cannot maintain m_pTail");
        return it.Insert(pItem);
    }

    // Removes item pointed to by it. Returns iterator to following item.
    Iterator Remove(Iterator & it)
    {
        static_assert(!Traits::HasTail, "Iterator Remove cannot maintain m_pTail");
        return it.Remove();
    }

#ifndef DACCESS_COMPILE

    void InsertHead(PTR_T pItem)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pItem != NULL);
        if constexpr (Traits::HasTail)
        {
            if (m_pHead == NULL)
                this->m_pTail = pItem;
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
            if constexpr (Traits::HasTail)
            {
                if (m_pHead == NULL)
                    this->m_pTail = NULL;
            }
        }

        return pRet;
    }

    void InsertTail(PTR_T pItem)
    {
        static_assert(Traits::HasTail, "InsertTail requires Traits::HasTail to be true");
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pItem != NULL);
        *Traits::GetNextPtr(pItem) = NULL;
        if (this->m_pTail != NULL)
            *Traits::GetNextPtr(this->m_pTail) = pItem;
        else
            m_pHead = pItem;
        this->m_pTail = pItem;
    }

    PTR_T GetTail()
    {
        static_assert(Traits::HasTail, "GetTail requires Traits::HasTail to be true");
        LIMITED_METHOD_DAC_CONTRACT;

        return this->m_pTail;
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

                if constexpr (Traits::HasTail)
                {
                    if (cur == this->m_pTail)
                        this->m_pTail = prev;
                }

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

    // Inserts pNewItem immediately after pAfter in the list.
    static void InsertAfter(PTR_T pAfter, PTR_T pNewItem)
    {
        static_assert(!Traits::HasTail, "InsertAfter cannot maintain m_pTail");
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pAfter != NULL && pNewItem != NULL);
        _ASSERTE(*Traits::GetNextPtr(pNewItem) == NULL);
        *Traits::GetNextPtr(pNewItem) = *Traits::GetNextPtr(pAfter);
        *Traits::GetNextPtr(pAfter) = pNewItem;
    }

#endif // !DACCESS_COMPILE
};

// ---------------------------------------------------------------------------
// Convenience alias for tail-tracking SList.
// ---------------------------------------------------------------------------
template <typename T> using SListTail = SList<T, SListTraits<T, SListMode::Tail>>;

// ---------------------------------------------------------------------------
// SListElem — non-intrusive list element wrapper.
// ---------------------------------------------------------------------------
template <typename ElemT>
struct SListElem
{
    typedef DPTR(SListElem) PTR_SListElem;
    PTR_SListElem m_pNext;
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

#endif // _H_SLIST_
