// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//-----------------------------------------------------------------------------
// @File: slist.h
//

//
// @commn: Bunch of utility classes
//
// HISTORY:
//   02/03/98:  	created helper classes
//						SLink, link node for singly linked list, every class that is intrusively
//								linked should have a data member of this type
//						SList, template linked list class, contains only inline
//								methods for fast list operations, with proper type checking
//
//						see below for futher info. on how to use these template classes
//
//-----------------------------------------------------------------------------

//#ifndef _H_UTIL
//#error "I am a part of util.hpp Please don't include me alone !"
//#endif


#ifndef _H_SLIST_
#define _H_SLIST_

//------------------------------------------------------------------
// struct SLink, to use a singly linked list
// have a data member m_Link of type SLink in your class
// and instantiate the template SList class
//--------------------------------------------------------------------

struct SLink;
typedef DPTR(struct SLink) PTR_SLink;

struct SLink
{
    PTR_SLink m_pNext;
    SLink()
    {
        LIMITED_METHOD_CONTRACT;

        m_pNext = NULL;
    }

    void InsertAfter(SLink* pLinkToInsert)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION_MSG(NULL == pLinkToInsert->m_pNext, "This method does not support inserting lists");

        PTR_SLink pTemp = m_pNext;

        m_pNext = PTR_SLink(pLinkToInsert);
        pLinkToInsert->m_pNext = pTemp;
    }

    // find pLink within the list starting at pHead
    // if found remove the link from the list and return the link
    // otherwise return NULL
    static SLink* FindAndRemove(SLink *pHead, SLink* pLink, SLink ** ppPrior)
    {
        LIMITED_METHOD_CONTRACT;

	    _ASSERTE(pHead != NULL);
	    _ASSERTE(pLink != NULL);

	    SLink* pFreeLink = NULL;
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

//------------------------------------------------------------------
// class SList. Intrusive singly linked list.
//
// To use SList with the default instantiation, your class should
// define a data member of type SLink and named 'm_Link'. To use a
// different field name, you need to provide an explicit LinkPtr
// template argument. For example:
//   'SList<MyClass, false, MyClass*, &MyClass::m_FieldName>'
//
// SList has two different behaviours depending on boolean
// fHead variable,
//
// if fHead is true, then the list allows only InsertHead  operations
// if fHead is false, then the list allows only InsertTail operations
// the code is optimized to perform these operations
// all methods are inline, and conditional compiled based on template
// argument 'fHead'
// so there is no actual code size increase
//--------------------------------------------------------------
template <class T, bool fHead = false, typename __PTR = T*, SLink T::*LinkPtr = &T::m_Link>
class SList
{
public:
    // typedef used by the Queue class below
    typedef T ENTRY_TYPE;

protected:

    // used as sentinel
    SLink  m_link; // slink.m_pNext == Null
    PTR_SLink m_pHead;
    PTR_SLink m_pTail;

    // get the list node within the object
    static SLink* GetLink (T* pLink)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return &(pLink->*LinkPtr);
    }

    // move to the beginning of the object given the pointer within the object
    static T* GetObject (SLink* pLink)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (pLink == NULL)
        {
            return NULL;
        }
        else
        {
#if 1
            // Newer compilers define offsetof to be __builtin_offsetof, which doesn't use the
            // old-school memory model trick to determine offset.
            const UINT_PTR offset = (((UINT_PTR)&(((T *)0x1000)->*LinkPtr))-0x1000);
            return (T*)__PTR(dac_cast<TADDR>(pLink) - offset);
#else
            return (T*)__PTR(dac_cast<TADDR>(pLink) - offsetof(T, *LinkPtr));
#endif
        }
    }

public:

    SList()
    {
        WRAPPER_NO_CONTRACT;
#ifndef DACCESS_COMPILE
        Init();
#endif // !defined(DACCESS_COMPILE)
    }

    void Init()
    {
        LIMITED_METHOD_CONTRACT;
        m_pHead = PTR_SLink(&m_link);
        // NOTE :: fHead variable is template argument
        // the following code is a compiled in, only if the fHead flag
        // is set to false,
        if (!fHead)
        {
            m_pTail = PTR_SLink(&m_link);
        }
    }

    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHead->m_pNext == NULL;
    }

#ifndef DACCESS_COMPILE

    void InsertTail(T *pObj)
    {
        LIMITED_METHOD_CONTRACT;
        // NOTE : conditional compilation on fHead template variable
        if (!fHead)
        {
            _ASSERTE(pObj != NULL);
            SLink *pLink = GetLink(pObj);

            m_pTail->m_pNext = pLink;
            m_pTail = pLink;
        }
        else
        {// you instantiated this class asking only for InsertHead operations
            _ASSERTE(0);
        }
    }

    void InsertHead(T *pObj)
    {
        LIMITED_METHOD_CONTRACT;
        // NOTE : conditional compilation on fHead template variable
        if (fHead)
        {
            _ASSERTE(pObj != NULL);
            SLink *pLink = GetLink(pObj);

            pLink->m_pNext = m_pHead->m_pNext;
            m_pHead->m_pNext = pLink;
        }
        else
        {// you instantiated this class asking only for InsertTail operations
            _ASSERTE(0);
        }
    }

    T*	RemoveHead()
    {
        LIMITED_METHOD_CONTRACT;
        SLink* pLink = m_pHead->m_pNext;
        if (pLink != NULL)
        {
            m_pHead->m_pNext = pLink->m_pNext;
        }
        // conditionally compiled, if the instantiated class
        // uses Insert Tail operations
        if (!fHead)
        {
            if(m_pTail == pLink)
            {
                m_pTail = m_pHead;
            }
        }

        return GetObject(pLink);
    }

#endif // !DACCESS_COMPILE

    T*	GetHead()
    {
        WRAPPER_NO_CONTRACT;
        return GetObject(m_pHead->m_pNext);
    }

    T*	GetTail()
    {
        WRAPPER_NO_CONTRACT;

        // conditional compile
        if (fHead)
        {	// you instantiated this class asking only for InsertHead operations
            // you need to walk the list yourself to find the tail
            _ASSERTE(0);
        }
        return (m_pHead != m_pTail) ? GetObject(m_pTail) : NULL;
    }

    static T *GetNext(T *pObj)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(pObj != NULL);
        return GetObject(GetLink(pObj)->m_pNext);
    }

    T* FindAndRemove(T *pObj)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(pObj != NULL);

        SLink   *prior;
        SLink   *ret = SLink::FindAndRemove(m_pHead, GetLink(pObj), &prior);

        if (ret == m_pTail)
            m_pTail = PTR_SLink(prior);

        return GetObject(ret);
    }

    class Iterator
    {
        friend class SList;

    public:
        Iterator & operator++()
        { _ASSERTE(m_cur != NULL); m_cur = SList::GetNext(m_cur); return *this; }

        Iterator operator++(int)
        { Iterator it(m_cur); ++(*this); return it; }

        bool operator==(Iterator const & other) const
        {
            return m_cur == other.m_cur ||
                   (m_cur != NULL && other.m_cur != NULL && *m_cur == *other.m_cur);
        }

        bool operator!=(Iterator const & other) const
        { return !(*this == other); }

        T & operator*()
        { _ASSERTE(m_cur != NULL); return *m_cur; }

        T * operator->() const
        { return m_cur; }

    private:
        Iterator(SList * pList)
            : m_cur(pList->GetHead())
        { }

        Iterator(T* pObj)
            : m_cur(pObj)
        { }

        Iterator()
            : m_cur(NULL)
        { }

        T* m_cur;
    };

    Iterator begin()
    { return Iterator(GetHead()); }

    Iterator end()
    { return Iterator(); }
};

template <typename ElemT>
struct SListElem
{
    SLink m_Link;
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
        : m_Link()
        , m_Value()
    { }

    template <typename T1>
    SListElem(T1&& val)
        : m_Link()
        , m_Value(std::forward<T1>(val))
    { }

    template <typename T1, typename T2>
    SListElem(T1&& val1, T2&& val2)
        : m_Link()
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2))
    { }

    template <typename T1, typename T2, typename T3>
    SListElem(T1&& val1, T2&& val2, T3&& val3)
        : m_Link()
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2), std::forward<T3>(val3))
    { }

    template <typename T1, typename T2, typename T3, typename T4>
    SListElem(T1&& val1, T2&& val2, T3&& val3, T4&& val4)
        : m_Link()
        , m_Value(std::forward<T1>(val1), std::forward<T2>(val2), std::forward<T3>(val3), std::forward<T4>(val4))
    { }
};

#endif // _H_SLIST_

// End of file: list.h
