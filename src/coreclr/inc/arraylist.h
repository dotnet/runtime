// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef ARRAYLIST_H_
#define ARRAYLIST_H_

#include <daccess.h>
#include <contract.h>
#include <stddef.h> // offsetof

// Forward Declarations
template<typename T> struct cdac_data;

//
// ArrayList is a simple class which is used to contain a growable
// list of pointers, stored in chunks.  Modification is by appending
// only currently.  Access is by index (efficient if the number of
// elements stays small) and iteration (efficient in all cases).
//
// An important property of an ArrayList is that the list remains
// coherent while it is being modified. This means that readers
// never need to lock when accessing it.
//

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4200) // Disable zero-sized array warning
#endif

class ArrayListBase
{
 public:

    enum
    {
        ARRAY_BLOCK_SIZE_START = 5,
    };

  private:

    struct ArrayListBlock
    {
        SPTR(ArrayListBlock)    m_next;
        DWORD                   m_blockSize;
#ifdef HOST_64BIT
        DWORD                   m_padding;
#endif
        PTR_VOID                m_array[0];

#ifdef DACCESS_COMPILE
        static ULONG32 DacSize(TADDR addr)
        {
            LIMITED_METHOD_CONTRACT;
            return offsetof(ArrayListBlock, m_array) +
                (*PTR_DWORD(addr + offsetof(ArrayListBlock, m_blockSize)) * sizeof(void*));
        }
#endif
    };
    typedef SPTR(ArrayListBlock) PTR_ArrayListBlock;

    struct FirstArrayListBlock
    {
        PTR_ArrayListBlock      m_next;
        DWORD                   m_blockSize;
#ifdef HOST_64BIT
        DWORD                   m_padding;
#endif
        void *                  m_array[ARRAY_BLOCK_SIZE_START];
    };

    typedef DPTR(FirstArrayListBlock) PTR_FirstArrayListBlock;

    DWORD               m_count;
    FirstArrayListBlock m_firstBlock;

  public:

    PTR_VOID *GetPtr(DWORD index) const;
    PTR_VOID Get(DWORD index) const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return *GetPtr(index);
    }

    void Set(DWORD index, PTR_VOID element)
    {
        WRAPPER_NO_CONTRACT;
        *GetPtr(index) = element;
    }

    DWORD GetCount() const { LIMITED_METHOD_DAC_CONTRACT; return m_count; }

    HRESULT Append(void *element);

    enum { NOT_FOUND = -1 };
    DWORD FindElement(DWORD start, PTR_VOID element) const;

    void Clear();

    void Init()
    {
        LIMITED_METHOD_CONTRACT;

        m_count = 0;
        m_firstBlock.m_next = NULL;
        m_firstBlock.m_blockSize = ARRAY_BLOCK_SIZE_START;
    }

    void Destroy()
    {
        WRAPPER_NO_CONTRACT;
        Clear();
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    class ConstIterator;

    class Iterator
    {
        friend class ArrayListBase;
        friend class ConstIterator;

      public:
        BOOL Next();

        void SetEmpty()
        {
            LIMITED_METHOD_CONTRACT;

            m_block = NULL;
            m_index = (DWORD)-1;
            m_remaining = 0;
            m_total = 0;
        }

        PTR_VOID GetElement() {LIMITED_METHOD_DAC_CONTRACT; return m_block->m_array[m_index]; }
        PTR_VOID * GetElementPtr() {LIMITED_METHOD_CONTRACT; return m_block->m_array + m_index; }
        DWORD GetIndex() {LIMITED_METHOD_CONTRACT; return m_index + m_total; }
        void *GetBlock() { return m_block; }

      private:
        ArrayListBlock*     m_block;
        DWORD               m_index;
        DWORD               m_remaining;
        DWORD               m_total;
        static Iterator Create(ArrayListBlock* block, DWORD remaining)
        {
            LIMITED_METHOD_DAC_CONTRACT;
            Iterator i;
            i.m_block = block;
            i.m_index = (DWORD) -1;
            i.m_remaining = remaining;
            i.m_total = 0;
            return i;
        }
    };

    class ConstIterator
    {
    public:
        ConstIterator(ArrayListBlock *pBlock, DWORD dwRemaining) : m_iterator(Iterator::Create(pBlock, dwRemaining))
        {
        }

        BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            return m_iterator.Next();
        }

        PTR_VOID GetElement()
        {
            WRAPPER_NO_CONTRACT;
            return m_iterator.GetElement();
        }

    private:
        Iterator m_iterator;
    };

    Iterator Iterate()
    {
        WRAPPER_NO_CONTRACT;
        return Iterator::Create((ArrayListBlock*)&m_firstBlock, m_count);
    }

    ConstIterator Iterate() const
    {
        // Const cast is safe because ConstIterator does not expose any way to modify the block
        ArrayListBlock *pFirstBlock = const_cast<ArrayListBlock *>(reinterpret_cast<const ArrayListBlock *>(&m_firstBlock));
        return ConstIterator(pFirstBlock, m_count);
    }

    // BlockIterator is used for only memory walking, such as prejit save/fixup.
    // It is not appropriate for other more typical ArrayList use.
    class BlockIterator
    {
      private:

        ArrayListBlock *m_block;
        DWORD           m_remaining;

        friend class ArrayListBase;
        BlockIterator(ArrayListBlock *block, DWORD remaining)
          : m_block(block), m_remaining(remaining)
        {
        }

      public:

        BOOL Next()
        {
            if (m_block != NULL)
            {
                // Prevent m_remaining from underflowing - we can have completely empty block at the end.
                if (m_remaining > m_block->m_blockSize)
                    m_remaining -= m_block->m_blockSize;
                else
                    m_remaining = 0;

                m_block = m_block->m_next;
            }
            return m_block != NULL;
        }

#ifndef DACCESS_COMPILE
        void ClearUnusedMemory()
        {
            if (m_remaining < m_block->m_blockSize)
                ZeroMemory(m_block->m_array + m_remaining, (m_block->m_blockSize - m_remaining) * sizeof(void*));
#ifdef HOST_64BIT
            m_block->m_padding = 0;
#endif // HOST_64BIT
        }
#endif // DACCESS_COMPILE

        void **GetNextPtr()
        {
            return (void **) &m_block->m_next;
        }

        void *GetBlock()
        {
            return m_block;
        }

        SIZE_T GetBlockSize()
        {
            return offsetof(ArrayListBlock, m_array) + (m_block->m_blockSize * sizeof(void*));
        }
    };

    void **GetInitialNextPtr()
    {
        return (void **) &m_firstBlock.m_next;
    }

    BlockIterator IterateBlocks()
    {
        return BlockIterator((ArrayListBlock *) &m_firstBlock, m_count);
    }

    friend struct cdac_data<ArrayListBase>;
};

template<>
struct cdac_data<ArrayListBase>
{
    static constexpr size_t Count = offsetof(ArrayListBase, m_count);
    static constexpr size_t FirstBlock = offsetof(ArrayListBase, m_firstBlock);

    static constexpr size_t Next = offsetof(ArrayListBase::ArrayListBlock, m_next);
    static constexpr size_t Size = offsetof(ArrayListBase::ArrayListBlock, m_blockSize);
    static constexpr size_t ArrayStart = offsetof(ArrayListBase::ArrayListBlock, m_array);
};


class ArrayList : public ArrayListBase
{
public:
#ifndef DACCESS_COMPILE
    ArrayList()
    {
        WRAPPER_NO_CONTRACT;
        Init();
    }

    ~ArrayList()
    {
        WRAPPER_NO_CONTRACT;
        Destroy();
    }
#endif
};

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
class ArrayListStatic : public ArrayListBase
{
};

typedef DPTR(ArrayListStatic) PTR_ArrayListStatic;
#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif
