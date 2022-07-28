// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __slist_h__
#define __slist_h__

#include "forward_declarations.h"

MSVC_SAVE_WARNING_STATE()
MSVC_DISABLE_WARNING(4127)  // conditional expression is constant -- it's intentionally constant

struct DoNothingFailFastPolicy
{
    static inline void FailFast();
};

template <typename T, typename FailFastPolicy = DoNothingFailFastPolicy>
struct DefaultSListTraits : public FailFastPolicy
{
    typedef DPTR(T) PTR_T;
    typedef DPTR(PTR_T) PTR_PTR_T;

    static inline PTR_PTR_T GetNextPtr(PTR_T pT);
    static inline bool Equals(PTR_T pA, PTR_T pB);
};

//------------------------------------------------------------------------------------------------------------
// class SList, to use a singly linked list.
//
// To use, either expose a field DPTR(T) m_pNext by adding DefaultSListTraits as a friend class, or
// define a new Traits class derived from DefaultSListTraits<T> and override the GetNextPtr function.
//
// SList supports lockless head insert and Remove methods. However, PushHeadInterlocked and
// PopHeadInterlocked must be used very carefully, as the rest of the mutating methods are not
// interlocked. In general, code must be careful to ensure that it will never use more than one
// synchronization mechanism at any given time to control access to a resource, and this is no
// exception. In particular, if synchronized access to other SList operations (such as FindAndRemove)
// are required, than a separate synchronization mechanism (such as a critical section) must be used.
//------------------------------------------------------------------------------------------------------------
template <typename T, typename Traits = DefaultSListTraits<T> >
class SList : public Traits
{
protected:
    typedef typename Traits::PTR_T PTR_T;
    typedef typename Traits::PTR_PTR_T PTR_PTR_T;

public:
    SList();

    // Returns true if there are no entries in the list.
    bool IsEmpty();

    // Returns the value of (but does not remove) the first element in the list.
    PTR_T GetHead();

    // Inserts pItem at the front of the list. See class header for more information.
    void PushHead(PTR_T pItem);
    void PushHeadInterlocked(PTR_T pItem);

    // Removes and returns the first entry in the list. See class header for more information.
    PTR_T PopHead();

    class Iterator
    {
        friend SList<T, Traits>;

      public:
        Iterator(Iterator const &it);
        Iterator& operator=(Iterator const &it);

        PTR_T operator->();
        PTR_T operator*();

        Iterator & operator++();
        Iterator operator++(int);

        bool operator==(Iterator const &rhs);
        bool operator==(PTR_T pT);
        bool operator!=(Iterator const &rhs);

      private:
        Iterator(PTR_PTR_T ppItem);

        Iterator Insert(PTR_T pItem);
        Iterator Remove();

        static Iterator End();
        PTR_PTR_T m_ppCur;
#ifdef _DEBUG
        mutable bool m_fIsValid;
#endif

        PTR_T _Value() const;

        enum e_ValidateOperation
        {
            e_CanCompare,   // Will assert in debug if m_fIsValid == false.
            e_CanInsert,    // i.e., not the fake End() value of m_ppCur == NULL
            e_HasValue,     // i.e., m_ppCur != NULL && *m_ppCur != NULL
        };
        void _Validate(e_ValidateOperation op) const;
    };

    Iterator Begin();
    Iterator End();

    // Returns iterator to first list item matching pItem
    Iterator FindFirst(PTR_T pItem);
    bool     RemoveFirst(PTR_T pItem);

    // Inserts pItem *before* it. Returns iterator pointing to inserted item.
    Iterator Insert(Iterator & it, PTR_T pItem);

    // Removes item pointed to by it from the list. Returns iterator pointing
    // to following item.
    Iterator Remove(Iterator & it);

private:
    PTR_T m_pHead;
};

MSVC_RESTORE_WARNING_STATE()

#endif // __slist_h__
