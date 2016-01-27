// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// SArray.h
// --------------------------------------------------------------------------------


#ifndef _SARRAY_H_
#define _SARRAY_H_

#include "sbuffer.h"

// --------------------------------------------------------------------------------
// SArray is a typed array wrapper around an SBuffer.  It manages individual 
// constructors and destructors of array elements if avaiable, as well as providing
// typed access.
// --------------------------------------------------------------------------------

template <typename ELEMENT, BOOL BITWISE_COPY = TRUE>
class SArray 
{
  private:

    SBuffer m_buffer;

    static COUNT_T VerifySizeRange(ELEMENT * begin, ELEMENT * end);

  public:

    class Iterator;
    friend class Iterator;

    SArray();
    SArray(COUNT_T count);
    SArray(ELEMENT * begin, ELEMENT * end);
    ~SArray();

    void Clear();
    void Set(const SArray<ELEMENT, BITWISE_COPY> &array);

    COUNT_T GetCount() const;
    BOOL IsEmpty() const;

    void SetCount(COUNT_T count);

    COUNT_T GetAllocation() const;

    void Preallocate(int count) const;
    void Trim() const;

    void Copy(const Iterator &to, const Iterator &from, COUNT_T size);
    void Move(const Iterator &to, const Iterator &from, COUNT_T size);

    void Copy(const Iterator &i, const ELEMENT *source, COUNT_T size);
    void Copy(void *dest, const Iterator &i, COUNT_T size);

    Iterator Append()
    {
        WRAPPER_NO_CONTRACT;

        COUNT_T count = GetCount();
        if ( GetAllocation() == count )
            Preallocate( 2 * count );

        Iterator i = End();
        Insert(i);
        return i;
    }

    void Append(ELEMENT elem)
    {
        WRAPPER_NO_CONTRACT;
        *Append() = elem;
    }

    ELEMENT AppendEx(ELEMENT elem)
    {
        WRAPPER_NO_CONTRACT;

        *Append() = elem;
        return elem;
    }
    
    void Insert(const Iterator &i);
    void Delete(const Iterator &i);

    void Insert(const Iterator &i, COUNT_T count);
    void Delete(const Iterator &i, COUNT_T count);

    void Replace(const Iterator &i, COUNT_T deleteCount, COUNT_T insertCount);

    ELEMENT *OpenRawBuffer(COUNT_T maxElementCount);
    ELEMENT *OpenRawBuffer();
    void CloseRawBuffer(COUNT_T actualElementCount);
    void CloseRawBuffer();

    Iterator Begin()
    {
        WRAPPER_NO_CONTRACT;
        return Iterator(this, 0);
    }

    Iterator End()
    {
        WRAPPER_NO_CONTRACT;
        return Iterator(this, GetCount());
    }

    Iterator operator+(COUNT_T index)
    {
        return Iterator(this, index);
    }

    ELEMENT & operator[] (int index);
    const ELEMENT & operator[] (int index) const;

    ELEMENT & operator[] (COUNT_T index);
    const ELEMENT & operator[] (COUNT_T index) const;

 protected:
    SArray(void *prealloc, COUNT_T size);

 public:

    class Iterator : public CheckedIteratorBase<SArray<ELEMENT, BITWISE_COPY> >, 
                     public Indexer<ELEMENT, Iterator>
    {
        friend class SArray;
        friend class Indexer<ELEMENT, Iterator>;

        SBuffer::Iterator m_i;

      public:
        
        Iterator(SArray *array, SCOUNT_T index)
          : CheckedIteratorBase<SArray<ELEMENT, BITWISE_COPY> >(array)
        {
            WRAPPER_NO_CONTRACT;
            m_i = array->m_buffer.Begin() + index*sizeof(ELEMENT);
        }

    protected:

        ELEMENT &GetAt(SCOUNT_T delta) const
        {
            LIMITED_METHOD_CONTRACT;
            return * (ELEMENT *) &m_i[delta*sizeof(ELEMENT)];
        }

        void Skip(SCOUNT_T delta)
        {
            LIMITED_METHOD_CONTRACT;
            m_i += delta*sizeof(ELEMENT);
        }

        COUNT_T Subtract(const Iterator &i) const
        {
            LIMITED_METHOD_CONTRACT;
            return (m_i - i.m_i)/sizeof(ELEMENT);
        }

        CHECK DoCheck(SCOUNT_T delta) const
        {
            WRAPPER_NO_CONTRACT;
            return m_i.CheckIndex(delta*sizeof(ELEMENT));
        }

      public:

        CHECK Check() const
        {
            WRAPPER_NO_CONTRACT;
            return m_i.Check();
        }
    };

    ELEMENT *GetElements() const;

  private:

    //--------------------------------------------------------------------
    // Routines for managing the buffer content.  
    //--------------------------------------------------------------------

    void ConstructBuffer(const Iterator &i, COUNT_T size);
    void CopyConstructBuffer(const Iterator &i, COUNT_T size, const ELEMENT *from);
    void DestructBuffer(const Iterator &i, COUNT_T size);
};

// ================================================================================
// InlineSArray : Tempate for an SArray with preallocated element space
// ================================================================================

template <typename ELEMENT, COUNT_T SIZE, BOOL BITWISE_COPY = TRUE>
class InlineSArray : public SArray<ELEMENT, BITWISE_COPY>
{
 private:
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4200) // zero sized array
#pragma warning(disable:4324) // don't complain if DECLSPEC_ALIGN actually pads
    DECLSPEC_ALIGN(BUFFER_ALIGNMENT) BYTE m_prealloc[SIZE*sizeof(ELEMENT)];
#pragma warning(pop)
#else
     // use UINT64 to get maximum alignment of the memory
     UINT64 m_prealloc[ALIGN(SIZE*sizeof(ELEMENT),sizeof(UINT64))/sizeof(UINT64)];
#endif  // _MSC_VER

 public:
    InlineSArray();
};

// ================================================================================
// StackSArray : SArray with relatively large preallocated buffer for stack use
// ================================================================================

template <typename ELEMENT, BOOL BITWISE_COPY = TRUE>
class StackSArray : public InlineSArray<ELEMENT, STACK_ALLOC/sizeof(ELEMENT), BITWISE_COPY>
{
}; 

// ================================================================================
// Inline definitions
// ================================================================================

#include "sarray.inl"

#endif  // _SARRAY_H_
