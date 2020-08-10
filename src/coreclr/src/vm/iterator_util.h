// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: iterator_util.h
//

//

//
// ============================================================================

#ifndef _ITERATOR_UTIL_H_
#define _ITERATOR_UTIL_H_

namespace IteratorUtil
{

// **************************************************************************************
template <typename ElementType>
class ArrayIteratorBase
{
public:
    typedef DPTR(ElementType) PTR_ElementType;
    typedef ArrayIteratorBase<ElementType> MyType;

    // ----------------------------------------------------------------------------------
    ArrayIteratorBase(
        PTR_ElementType pStart,
        size_t cEntries)
        : m_pCur(pStart),
          m_pStart(pStart),
          m_pEnd(pStart + cEntries)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIteratorBase(
        PTR_ElementType pStart,
        PTR_ElementType pEnd)
        : m_pCur(pStart),
          m_pStart(pStart),
          m_pEnd(pEnd)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIteratorBase(
        const MyType &it)
        : m_pCur(it.m_pCur),
          m_pStart(it.m_pStart),
          m_pEnd(it.m_pEnd)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    bool
    AtStart() const
        { LIMITED_METHOD_CONTRACT; return m_pCur == m_pStart; }

    // ----------------------------------------------------------------------------------
    bool
    AtEnd() const
        { LIMITED_METHOD_CONTRACT; return m_pCur == m_pEnd; }

    // ----------------------------------------------------------------------------------
    void
    ResetToStart()
        { LIMITED_METHOD_CONTRACT; m_pCur = m_pStart; }

    // ----------------------------------------------------------------------------------
    void
    ResetToEnd()
        { LIMITED_METHOD_CONTRACT; m_pCur = m_pEnd; }

    // ----------------------------------------------------------------------------------
    ElementType &
    Value()
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(!AtEnd()); return *m_pCur; }

    // ----------------------------------------------------------------------------------
    ElementType &
    operator*()
        { WRAPPER_NO_CONTRACT; return Value(); }

    // ----------------------------------------------------------------------------------
    ElementType &
    operator[](size_t idx)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(m_pStart + idx < m_pEnd);
        return m_pStart[idx];
    }

    // ----------------------------------------------------------------------------------
    size_t
    CurrentIndex()
        { LIMITED_METHOD_CONTRACT; return m_pCur - m_pStart; }

    // ----------------------------------------------------------------------------------
    void
    MoveTo(size_t idx)
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(m_pStart + idx < m_pEnd); m_pCur = m_pStart + idx; }

    // ----------------------------------------------------------------------------------
    bool
    Next()
    {
        WRAPPER_NO_CONTRACT;
        if (AtEnd())
        {
            return false;
        }

        m_pCur++;

        return !AtEnd();
    }

    // ----------------------------------------------------------------------------------
    bool
    Prev()
    {
        if (AtStart())
        {
            return false;
        }

        m_pCur--;

        return true;
    }

    MyType &
    operator++()
        { WRAPPER_NO_CONTRACT; CONSISTENCY_CHECK(!AtEnd()); Next(); return *this; }

protected:
    // ----------------------------------------------------------------------------------
    PTR_ElementType  m_pCur;
    PTR_ElementType  m_pStart;
    PTR_ElementType  m_pEnd;

    // ----------------------------------------------------------------------------------
    // Do not allow address to be taken of what should be a by-val or by-ref.
    ArrayIteratorBase<ElementType> *
    operator&()
        { LIMITED_METHOD_CONTRACT; }
};

// **************************************************************************************
template <typename ElementType>
class ArrayIterator
    : public ArrayIteratorBase<ElementType>
{
public:
    typedef ArrayIteratorBase<ElementType> _BaseTy;
    typedef typename _BaseTy::PTR_ElementType PTR_ElementType;

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        PTR_ElementType pStart,
        size_t          cEntries)
        : _BaseTy(pStart, cEntries)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        PTR_ElementType pStart,
        PTR_ElementType pEnd)
        : _BaseTy(pStart, pEnd)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        const ArrayIterator &it)
        : _BaseTy(it)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    PTR_ElementType
    operator->()
        { WRAPPER_NO_CONTRACT; return &this->Value(); }

#ifdef DACCESS_COMPILE
private:
    // ----------------------------------------------------------------------------------
    // You are trying to instantiate the iterator over a non DACized array.
    // Make sure you pass in "DPTR(ElementType)" or "PTR_ElementType" and
    // not "ElementType *" as the argument type.
    ArrayIterator(
        ElementType *   pStart,
        size_t          cEntries);

    // ----------------------------------------------------------------------------------
    // You are trying to instantiate the iterator over a non DACized array.
    // Make sure you pass in "DPTR(ElementType)" or "PTR_ElementType" and
    // not "ElementType *" as the argument type.
    ArrayIterator(
        ElementType *   pStart,
        ElementType *   pEnd);
#endif
};

// **************************************************************************************
template <typename ValueType>
class ArrayIterator<ValueType *>
    : public ArrayIteratorBase<DPTR(ValueType)>
{
public:
    typedef ArrayIteratorBase<DPTR(ValueType)> _BaseTy;
    typedef typename _BaseTy::PTR_ElementType PTR_ElementType;

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        PTR_ElementType pStart,
        size_t          cEntries)
        : _BaseTy(pStart, cEntries)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        PTR_ElementType pStart,
        PTR_ElementType pEnd)
        : _BaseTy(pStart, pEnd)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    ArrayIterator(
        const ArrayIterator &it)
        : _BaseTy(it)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    DPTR(ValueType)
    operator->()
        { WRAPPER_NO_CONTRACT; return this->Value(); }

#ifdef DACCESS_COMPILE
private:
    // ----------------------------------------------------------------------------------
    // You are trying to instantiate the iterator over a non DACized array.
    // Make sure you pass in "DPTR(ElementType)" or "PTR_ElementType" and
    // not "ElementType *" as the argument type.
    ArrayIterator(
        ValueType **    pStart,
        size_t          cEntries);

    // ----------------------------------------------------------------------------------
    // You are trying to instantiate the iterator over a non DACized array.
    // Make sure you pass in "DPTR(ElementType)" or "PTR_ElementType" and
    // not "ElementType *" as the argument type.
    ArrayIterator(
        ValueType **   pStart,
        ValueType **   pEnd);
#endif
};

#if 0
// **************************************************************************************
// It's important to note that ElemType is expected to have a public instance method:
//      ElemType * GetNext();

template <typename ElemType>
class SListIterator
{
public:
    // ----------------------------------------------------------------------------------
    SListIterator(
        ElemType * pHead)
        : m_pCur(pHead),
          m_pHead(pHead)
        { LIMITED_METHOD_CONTRACT; }

    // ----------------------------------------------------------------------------------
    bool
    AtStart() const
        { LIMITED_METHOD_CONTRACT; return m_pCur == m_pHead; }

    // ----------------------------------------------------------------------------------
    bool
    AtEnd() const
        { LIMITED_METHOD_CONTRACT; return m_pCur == NULL; }

    // ----------------------------------------------------------------------------------
    void
    ResetToStart()
        { LIMITED_METHOD_CONTRACT; m_pCur = m_pHead; }

    // ----------------------------------------------------------------------------------
    ElemType &
    Value()
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(!AtEnd()); return *m_pCur; }

    // ----------------------------------------------------------------------------------
    ElemType &
    operator*()
        { WRAPPER_NO_CONTRACT; return Value(); }

    // ----------------------------------------------------------------------------------
    ElemType *
    operator->()
        { WRAPPER_NO_CONTRACT; return &Value(); }

    // ----------------------------------------------------------------------------------
    bool
    Next()
    {
        WRAPPER_NO_CONTRACT;
        if (AtEnd())
        {
            return false;
        }

        m_pCur = m_pCur->GetNext();

        return !AtEnd();
    }

protected:
    // ----------------------------------------------------------------------------------
    ElemType *  m_pCur;
    ElemType *  m_pHead;

    // ----------------------------------------------------------------------------------
    // Do not allow address to be taken of what should be a by-val or by-ref.
    SListIterator<ElemType> *
    operator&()
        { LIMITED_METHOD_CONTRACT; }
};
#endif

} // IteratorUtil


#endif // _ITERATOR_UTIL_H_
