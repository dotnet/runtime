// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EmptyContainers_h__
#define __EmptyContainers_h__

// This header file will contains minimal containers that are needed for EventPipe library implementation
// @TODO - this will likely be replaced by the common container classes to be added to the EventPipe library
// Hence initially, the bare boned implementation focus is on unblocking HW/Simple Trace bring up

#include "EmptyContainers2.h"

struct SLink_EP
{
    SLink_EP* m_pNext;

    SLink_EP()
    {
        m_pNext = NULL;
    }

    // find pLink within the list starting at pHead
    // if found remove the link from the list and return the link
    // otherwise return NULL
    static SLink_EP* FindAndRemove(SLink_EP *pHead, SLink_EP* pLink, SLink_EP ** ppPrior)
    {

	    _ASSERTE(pHead != NULL);
	    _ASSERTE(pLink != NULL);

	    SLink_EP* pFreeLink = NULL;
        *ppPrior = NULL;

	    while (pHead->m_pNext != NULL)
	    {
		    if (pHead->m_pNext == pLink)
		    {
			    pFreeLink = pLink;
			    pHead->m_pNext = pLink->m_pNext;
                *ppPrior = pHead;
                break;
		    }
            pHead = pHead->m_pNext;
	    }

	    return pFreeLink;
    }
};


// template <class T, bool fHead = false, typename __PTR = T*, SLink_EP T::*LinkPtr = &T::m_Link>
// Assumes fHead to be false
template <class T>
class SList_EP
{
protected:
    // used as sentinel
    SLink_EP  m_link;
    SLink_EP* m_pHead;
    SLink_EP* m_pTail;

    // get the list node within the object
    static SLink_EP* GetLink (T* pLink)
    {
        return &(pLink->m_Link);
    }

    static T* GetObject (SLink_EP* pLink)
    {
        if (pLink == NULL)
            return NULL;
        return reinterpret_cast<T*>(reinterpret_cast<ptrdiff_t>(pLink) - offsetof(T, m_Link));
     }

public:

    SList_EP()
    {
        m_pHead = &m_link;
        // NOTE :: fHead variable is template argument
        // the following code is a compiled in, only if the fHead flag
        // is set to false,
        m_pTail = &m_link;
        // TODO: Likely not needed now since SLink_EP has a ctor
        m_link.m_pNext = NULL;
    }

    bool IsEmpty()
    {
        return m_pHead->m_pNext == NULL;
    }

    void InsertTail(T *pObj)
    {
        _ASSERTE(pObj != NULL);
        SLink_EP *pLink = GetLink(pObj);

        m_pTail->m_pNext = pLink;
        m_pTail = pLink;
    }

    T*	RemoveHead()
    {
        SLink_EP* pLink = m_pHead->m_pNext;
        if (pLink != NULL)
        {
            m_pHead->m_pNext = pLink->m_pNext;
        }

        if(m_pTail == pLink)
        {
            m_pTail = m_pHead;
        }

        return GetObject(pLink);
    }

    T*	GetHead()
    {
        return GetObject(m_pHead->m_pNext);
    }

    static T *GetNext(T *pObj)
    {
        _ASSERTE(pObj != NULL);
        return GetObject(GetLink(pObj)->m_pNext);
    }


    T* FindAndRemove(T *pObj)
    {
        _ASSERTE(pObj != NULL);

        SLink_EP   *prior;
        SLink_EP   *ret = SLink_EP::FindAndRemove(m_pHead, GetLink(pObj), &prior);

        if (ret == m_pTail)
            m_pTail = prior;

        return GetObject(ret);
    }

    void InsertHead(T *pObj)
    {
        PalDebugBreak();
    }


    class Iterator
    {
        friend class SList_EP;
        //T* _t;

    public:
        Iterator & operator++()
        { 
            _ASSERTE(m_cur != NULL); 
            m_cur = SList_EP::GetNext(m_cur); 
            return *this; 
        }

        Iterator operator++(int)
        { 
            Iterator it(m_cur); 
            ++(*this); 
            return it; 
        }

        bool operator==(Iterator const & other) const
        {
            return m_cur == other.m_cur ||
                   (m_cur != NULL && other.m_cur != NULL && *m_cur == *other.m_cur);
        }

        T & operator*() 
        {
            _ASSERTE(m_cur != NULL);
            return *m_cur;
        }

        T * operator->() const 
        {
            PalDebugBreak();
            return m_cur;
        }

    private:
        // Iterator(SList * pList)
        //     : m_cur(pList->GetHead())
        // { }

        Iterator(T* pObj)
            : m_cur(pObj)
        { }

        Iterator()
            : m_cur(NULL)
        { }

        T* m_cur;

    };

    Iterator begin()
    { 
        return Iterator(GetHead()); 
    }

    Iterator end()
    { 
        return Iterator(); 
    }

};

template <typename ElemT>
struct SListElem_EP
{
    SLink_EP m_Link;
    ElemT m_Value;

    operator ElemT const &() const
    { 
        return m_Value; 
    }

    operator ElemT &()
    { 
        return m_Value; 
    }

    ElemT const & operator*() const
    { 
        PalDebugBreak();
        return m_Value; 
    }

    ElemT & operator*()
    { 
        PalDebugBreak();
        return m_Value; 
    }

    SListElem_EP()
        : m_Link()
        , m_Value()
    { 
    }

    template <typename T1>
    SListElem_EP(T1&& val)
        : m_Link()
        , m_Value(std::forward<T1>(val))
    { 
    }

    ElemT & GetValue()
    { 
        return m_Value; 
    }    

};

// Bare boned implementation to unblock HW
template <class T>
class CQuickArrayList_EP
{
private:
    size_t m_curSize;
    T *m_array;
    size_t maxSize;
public:
    CQuickArrayList_EP()
        : m_curSize(0), maxSize(100)
    {
        m_array = new T[maxSize];
    }


    T* AllocNoThrow(size_t iItems)
    {
        return new T[iItems];
    }

    bool PushNoThrow(const T & value)
    {
        if(m_curSize >= maxSize)
            PalDebugBreak();
        m_array[m_curSize++] = value;
        return true;
    }

    size_t Size() const
    {
        return m_curSize;
    }
    
    T Pop()
    {
        T t = m_array[m_curSize];
        m_curSize--;
        return t;
    }

    void Shrink()
    {
    }
    
    T& operator[] (size_t ix)
    {
        return m_array[ix];
    }

    T* Ptr()
    {
        return m_array;
    }
};

#endif // __EmptyContainers_h__
