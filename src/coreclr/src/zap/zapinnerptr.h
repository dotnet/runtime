// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapInnerPtr.h
//

//
// ZapNode that points into middle of other ZapNode. It is used to create
// pointers into datastructures that are not convenient to split into smaller zap nodes.
//
// ======================================================================================

#ifndef __ZAPINNERPTR_H__
#define __ZAPINNERPTR_H__

class ZapInnerPtr : public ZapNode
{
    ZapNode * m_pBase;

public:
    ZapInnerPtr(ZapNode * pBase)
        : m_pBase(pBase)
    {
    }

    ZapNode * GetBase()
    {
        return m_pBase;
    }

    virtual int GetOffset() = 0;

    void Resolve()
    {
        if (m_pBase->IsPlaced())
        {
            SetRVA(m_pBase->GetRVA() + GetOffset());
        }
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_InnerPtr;
    }
};

class ZapInnerPtrTable
{
    // Create more space efficient nodes for a few common constant offsets
    template <DWORD offset>
    class InnerPtrConst : public ZapInnerPtr
    {
    public:
        InnerPtrConst(ZapNode * pBase)
            : ZapInnerPtr(pBase)
        {
        }

        virtual int GetOffset()
        {
            return offset;
        }
    };

    // The generic node for arbitrary offsets
    class InnerPtrVar : public ZapInnerPtr
    {
        int m_offset;

    public:
        InnerPtrVar(ZapNode * pBase, SSIZE_T offset)
            : ZapInnerPtr(pBase), m_offset((int)offset)
        {
            if (offset != (int)offset)
                ThrowHR(COR_E_OVERFLOW);
        }

        virtual int GetOffset()
        {
            return m_offset;
        }
    };

    struct InnerPtrKey
    {
        InnerPtrKey(ZapNode * pBase, SSIZE_T offset)
            : m_pBase(pBase), m_offset(offset)
        {
        }

        ZapNode * m_pBase;
        SSIZE_T m_offset;
    };

    class InnerPtrTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapInnerPtr *> >
    {
    public:
        typedef const InnerPtrKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return InnerPtrKey(e->GetBase(), e->GetOffset());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return (k1.m_pBase == k2.m_pBase) && (k1.m_offset == k2.m_offset);
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k.m_pBase ^ (count_t)k.m_offset;
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash< InnerPtrTraits > InnerPtrTable;

    InnerPtrTable m_entries;
    ZapWriter * m_pWriter;

public:
    ZapInnerPtrTable(ZapWriter * pWriter)
        : m_pWriter(pWriter)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE_NOT_NEEDED(ZapInnerPtrTable::m_entries, cbILImage);
    }

    // Returns ZapNode that points at given offset in base ZapNode
    ZapNode * Get(ZapNode * pBase, SSIZE_T offset);

    // Assign offsets to the inner-ptr ZapNodes
    void Resolve();
};

#endif // __ZAPINNERPTR_H__
