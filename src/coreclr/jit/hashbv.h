// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HASHBV_H
#define HASHBV_H

#if defined(HOST_AMD64) || defined(HOST_X86)
#include <xmmintrin.h>
#endif

#include <stdlib.h>
#include <stdio.h>
#include <memory.h>
#include <windows.h>

//#define TESTING 1

#define LOG2_BITS_PER_ELEMENT 5
#define LOG2_ELEMENTS_PER_NODE 2
#define LOG2_BITS_PER_NODE (LOG2_BITS_PER_ELEMENT + LOG2_ELEMENTS_PER_NODE)

#define BITS_PER_ELEMENT (1 << LOG2_BITS_PER_ELEMENT)
#define ELEMENTS_PER_NODE (1 << LOG2_ELEMENTS_PER_NODE)
#define BITS_PER_NODE (1 << LOG2_BITS_PER_NODE)

#ifdef TARGET_AMD64
typedef unsigned __int64 elemType;
typedef unsigned __int64 indexType;
#else
typedef unsigned int elemType;
typedef unsigned int indexType;
#endif

class hashBvNode;
class hashBv;
class hashBvIterator;
class hashBvGlobalData;

typedef void bitAction(indexType);
typedef void nodeAction(hashBvNode*);
typedef void dualNodeAction(hashBv* left, hashBv* right, hashBvNode* a, hashBvNode* b);

#define NOMOREBITS -1

#ifdef DEBUG
inline void pBit(indexType i)
{
    printf("%d ", i);
}
#endif // DEBUG

// ------------------------------------------------------------
//  this is essentially a hashtable of small fixed bitvectors.
//  for any index, bits select position as follows:
//   32                                                      0
// ------------------------------------------------------------
//  | ... ... ... | hash | element in node | index in element |
// ------------------------------------------------------------
//
//
// hashBv
// | // hashtable
// v
// []->node->node->node
// []->node
// []
// []->node->node
//
//

#if TESTING
inline int log2(int number)
{
    int result = 0;
    number >>= 1;
    while (number)
    {
        result++;
        number >>= 1;
    }
    return result;
}
#endif

// return greatest power of 2 that is less than or equal
inline int nearest_pow2(unsigned number)
{
    int result = 0;

    if (number > 0xffff)
    {
        number >>= 16;
        result += 16;
    }
    if (number > 0xff)
    {
        number >>= 8;
        result += 8;
    }
    if (number > 0xf)
    {
        number >>= 4;
        result += 4;
    }
    if (number > 0x3)
    {
        number >>= 2;
        result += 2;
    }
    if (number > 0x1)
    {
        number >>= 1;
        result += 1;
    }
    return 1 << result;
}

class hashBvNode
{
public:
    hashBvNode* next;
    indexType   baseIndex;
    elemType    elements[ELEMENTS_PER_NODE];

public:
    hashBvNode(indexType base);
    hashBvNode()
    {
    }
    static hashBvNode* Create(indexType base, Compiler* comp);
    void Reconstruct(indexType base);
    int numElements()
    {
        return ELEMENTS_PER_NODE;
    }
    void setBit(indexType base);
    void setLowest(indexType numToSet);
    bool getBit(indexType base);
    void clrBit(indexType base);
    bool anySet();
    bool belongsIn(indexType index);
    int  countBits();
    bool anyBits();
    void foreachBit(bitAction x);
    void freeNode(hashBvGlobalData* glob);
    bool sameAs(hashBvNode* other);
    void copyFrom(hashBvNode* other);

    void AndWith(hashBvNode* other);
    void OrWith(hashBvNode* other);
    void XorWith(hashBvNode* other);
    void Subtract(hashBvNode* other);

    elemType AndWithChange(hashBvNode* other);
    elemType OrWithChange(hashBvNode* other);
    elemType XorWithChange(hashBvNode* other);
    elemType SubtractWithChange(hashBvNode* other);

    bool Intersects(hashBvNode* other);

#ifdef DEBUG
    void dump();
#endif // DEBUG
};

class hashBv
{
public:
    // --------------------------------------
    // data
    // --------------------------------------
    hashBvNode** nodeArr;
    hashBvNode*  initialVector[1];

    union {
        Compiler* compiler;
        // for freelist
        hashBv* next;
    };

    unsigned short log2_hashSize;
    // used for heuristic resizing... could be overflowed in rare circumstances
    // but should not affect correctness
    unsigned short numNodes;

public:
    hashBv(Compiler* comp);
    static hashBv* Create(Compiler* comp);
    static void Init(Compiler* comp);
    static hashBv* CreateFrom(hashBv* other, Compiler* comp);
    void hbvFree();
#ifdef DEBUG
    void dump();
    void dumpFancy();
#endif // DEBUG
    __forceinline int hashtable_size() const
    {
        return 1 << this->log2_hashSize;
    }

    hashBvGlobalData* globalData();

    static hashBvNode*& nodeFreeList(hashBvGlobalData* globalData);
    static hashBv*& hbvFreeList(hashBvGlobalData* data);

    hashBvNode** getInsertionPointForIndex(indexType index);

private:
    hashBvNode* getNodeForIndexHelper(indexType index, bool canAdd);
    int getHashForIndex(indexType index, int table_size);
    int getRehashForIndex(indexType thisIndex, int thisTableSize, int newTableSize);

    // maintain free lists for vectors
    hashBvNode** getNewVector(int vectorLength);
    int getNodeCount();

public:
    inline hashBvNode* getOrAddNodeForIndex(indexType index)
    {
        hashBvNode* temp = getNodeForIndexHelper(index, true);
        return temp;
    }
    hashBvNode* getNodeForIndex(indexType index);
    void removeNodeAtBase(indexType index);

public:
    void setBit(indexType index);
    void setAll(indexType numToSet);
    bool testBit(indexType index);
    void clearBit(indexType index);
    int  countBits();
    bool anySet();
    void copyFrom(hashBv* other, Compiler* comp);
    void ZeroAll();
    bool CompareWith(hashBv* other);

    void AndWith(hashBv* other);
    void OrWith(hashBv* other);
    void XorWith(hashBv* other);
    void Subtract(hashBv* other);
    void Subtract3(hashBv* other, hashBv* other2);

    void UnionMinus(hashBv* a, hashBv* b, hashBv* c);

    bool AndWithChange(hashBv* other);
    bool OrWithChange(hashBv* other);
    bool OrWithChangeRight(hashBv* other);
    bool OrWithChangeLeft(hashBv* other);
    bool XorWithChange(hashBv* other);
    bool SubtractWithChange(hashBv* other);

    bool Intersects(hashBv* other);

    template <class Action>
    bool MultiTraverseLHSBigger(hashBv* other);
    template <class Action>
    bool MultiTraverseRHSBigger(hashBv* other);
    template <class Action>
    bool MultiTraverseEqual(hashBv* other);
    template <class Action>
    bool MultiTraverse(hashBv* other);

    void InorderTraverse(nodeAction a);
    void InorderTraverseTwo(hashBv* other, dualNodeAction a);

    void Resize(int newSize);
    void Resize();
    void MergeLists(hashBvNode** a, hashBvNode** b);

    bool TooSmall();
    bool TooBig();
    bool IsValid();
};

// --------------------------------------------------------------------
// --------------------------------------------------------------------

class hashBvIterator
{
public:
    unsigned    hashtable_size;
    unsigned    hashtable_index;
    hashBv*     bv;
    hashBvNode* currNode;
    indexType   current_element;
    // base index of current node
    indexType current_base;
    // working data of current element
    elemType current_data;

    hashBvIterator(hashBv* bv);
    void initFrom(hashBv* bv);
    hashBvIterator();
    indexType nextBit();

private:
    void nextNode();
};

class hashBvGlobalData
{
    friend class hashBv;
    friend class hashBvNode;

    hashBvNode* hbvNodeFreeList;
    hashBv*     hbvFreeList;
};

enum class HbvWalk
{
    Continue,
    Abort,
};

template <typename TFunctor>
HbvWalk ForEachHbvBitSet(const hashBv& bv, TFunctor func)
{
    for (int hashNum = 0; hashNum < bv.hashtable_size(); hashNum++)
    {
        hashBvNode* node = bv.nodeArr[hashNum];
        while (node)
        {
            indexType base = node->baseIndex;
            for (int el = 0; el < node->numElements(); el++)
            {
                elemType e = node->elements[el];
                while (e)
                {
                    unsigned  i     = BitOperations::BitScanForward(e);
                    indexType index = base + (el * BITS_PER_ELEMENT) + i;
                    e ^= (elemType(1) << i);

                    if (func(index) == HbvWalk::Abort)
                    {
                        return HbvWalk::Abort;
                    }
                }
            }
            node = node->next;
        }
    }

    return HbvWalk::Continue;
}

#ifdef DEBUG
void SimpleDumpNode(hashBvNode* n);
void DumpNode(hashBvNode* n);
void SimpleDumpDualNode(hashBv* a, hashBv* b, hashBvNode* n, hashBvNode* m);
#endif // DEBUG

#endif
