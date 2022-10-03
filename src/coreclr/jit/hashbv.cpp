// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// --------------------------------------------------------------------
// --------------------------------------------------------------------

#ifdef DEBUG
void hashBvNode::dump()
{
    printf("base: %d { ", baseIndex);
    this->foreachBit(pBit);
    printf("}\n");
}
#endif // DEBUG

void hashBvNode::Reconstruct(indexType base)
{
    baseIndex = base;

    assert(!(baseIndex % BITS_PER_NODE));

    for (int i = 0; i < this->numElements(); i++)
    {
        elements[i] = 0;
    }
    next = nullptr;
}

hashBvNode::hashBvNode(indexType base)
{
    this->Reconstruct(base);
}

hashBvNode* hashBvNode::Create(indexType base, Compiler* compiler)
{
    hashBvNode* result = nullptr;

    if (compiler->hbvGlobalData.hbvNodeFreeList)
    {
        result                                  = compiler->hbvGlobalData.hbvNodeFreeList;
        compiler->hbvGlobalData.hbvNodeFreeList = result->next;
    }
    else
    {
        result = new (compiler, CMK_hashBv) hashBvNode;
    }
    result->Reconstruct(base);
    return result;
}

void hashBvNode::freeNode(hashBvGlobalData* glob)
{
    this->next            = glob->hbvNodeFreeList;
    glob->hbvNodeFreeList = this;
}

void hashBvNode::setBit(indexType base)
{
    assert(base >= baseIndex);
    assert(base - baseIndex < BITS_PER_NODE);

    base -= baseIndex;
    indexType elem = base / BITS_PER_ELEMENT;
    indexType posi = base % BITS_PER_ELEMENT;

    elements[elem] |= indexType(1) << posi;
}

void hashBvNode::setLowest(indexType numToSet)
{
    assert(numToSet <= BITS_PER_NODE);

    int elemIndex = 0;
    while (numToSet > BITS_PER_ELEMENT)
    {
        elements[elemIndex] = ~(elemType(0));
        numToSet -= BITS_PER_ELEMENT;
        elemIndex++;
    }
    if (numToSet)
    {
        elemType allOnes    = ~(elemType(0));
        int      numToShift = (int)(BITS_PER_ELEMENT - numToSet);
        elements[elemIndex] = allOnes >> numToShift;
    }
}

void hashBvNode::clrBit(indexType base)
{
    assert(base >= baseIndex);
    assert(base - baseIndex < BITS_PER_NODE);

    base -= baseIndex;
    indexType elem = base / BITS_PER_ELEMENT;
    indexType posi = base % BITS_PER_ELEMENT;

    elements[elem] &= ~(indexType(1) << posi);
}

bool hashBvNode::belongsIn(indexType index)
{
    if (index < baseIndex)
    {
        return false;
    }
    if (index >= baseIndex + BITS_PER_NODE)
    {
        return false;
    }
    return true;
}

int countBitsInWord(unsigned int bits)
{
    // In-place adder tree: perform 16 1-bit adds, 8 2-bit adds,
    // 4 4-bit adds, 2 8=bit adds, and 1 16-bit add.
    bits = ((bits >> 1) & 0x55555555) + (bits & 0x55555555);
    bits = ((bits >> 2) & 0x33333333) + (bits & 0x33333333);
    bits = ((bits >> 4) & 0x0F0F0F0F) + (bits & 0x0F0F0F0F);
    bits = ((bits >> 8) & 0x00FF00FF) + (bits & 0x00FF00FF);
    bits = ((bits >> 16) & 0x0000FFFF) + (bits & 0x0000FFFF);
    return (int)bits;
}

int countBitsInWord(unsigned __int64 bits)
{
    bits = ((bits >> 1) & 0x5555555555555555) + (bits & 0x5555555555555555);
    bits = ((bits >> 2) & 0x3333333333333333) + (bits & 0x3333333333333333);
    bits = ((bits >> 4) & 0x0F0F0F0F0F0F0F0F) + (bits & 0x0F0F0F0F0F0F0F0F);
    bits = ((bits >> 8) & 0x00FF00FF00FF00FF) + (bits & 0x00FF00FF00FF00FF);
    bits = ((bits >> 16) & 0x0000FFFF0000FFFF) + (bits & 0x0000FFFF0000FFFF);
    bits = ((bits >> 32) & 0x00000000FFFFFFFF) + (bits & 0x00000000FFFFFFFF);
    return (int)bits;
}

int hashBvNode::countBits()
{
    int result = 0;

    for (int i = 0; i < this->numElements(); i++)
    {
        elemType bits = elements[i];

        result += countBitsInWord(bits);

        result += (int)bits;
    }
    return result;
}

bool hashBvNode::anyBits()
{
    for (int i = 0; i < this->numElements(); i++)
    {
        if (elements[i])
        {
            return true;
        }
    }
    return false;
}

bool hashBvNode::getBit(indexType base)
{
    assert(base >= baseIndex);
    assert(base - baseIndex < BITS_PER_NODE);
    base -= baseIndex;

    indexType elem = base / BITS_PER_ELEMENT;
    indexType posi = base % BITS_PER_ELEMENT;

    if (elements[elem] & (indexType(1) << posi))
    {
        return true;
    }
    else
    {
        return false;
    }
}

bool hashBvNode::anySet()
{
    for (int i = 0; i < this->numElements(); i++)
    {
        if (elements[i])
        {
            return true;
        }
    }
    return false;
}

void hashBvNode::copyFrom(hashBvNode* other)
{
    this->baseIndex = other->baseIndex;
    for (int i = 0; i < this->numElements(); i++)
    {
        this->elements[i] = other->elements[i];
    }
}

void hashBvNode::foreachBit(bitAction a)
{
    indexType base;
    for (int i = 0; i < this->numElements(); i++)
    {
        base       = baseIndex + i * BITS_PER_ELEMENT;
        elemType e = elements[i];
        while (e)
        {
            if (e & 1)
            {
                a(base);
            }
            e >>= 1;
            base++;
        }
    }
}

elemType hashBvNode::AndWithChange(hashBvNode* other)
{
    elemType result = 0;

    for (int i = 0; i < this->numElements(); i++)
    {
        elemType src = this->elements[i];
        elemType dst;

        dst = src & other->elements[i];
        result |= src ^ dst;
        this->elements[i] = dst;
    }
    return result;
}

elemType hashBvNode::OrWithChange(hashBvNode* other)
{
    elemType result = 0;

    for (int i = 0; i < this->numElements(); i++)
    {
        elemType src = this->elements[i];
        elemType dst;

        dst = src | other->elements[i];
        result |= src ^ dst;
        this->elements[i] = dst;
    }
    return result;
}

elemType hashBvNode::XorWithChange(hashBvNode* other)
{
    elemType result = 0;

    for (int i = 0; i < this->numElements(); i++)
    {
        elemType src = this->elements[i];
        elemType dst;

        dst = src ^ other->elements[i];
        result |= src ^ dst;
        this->elements[i] = dst;
    }
    return result;
}

elemType hashBvNode::SubtractWithChange(hashBvNode* other)
{
    elemType result = 0;

    for (int i = 0; i < this->numElements(); i++)
    {
        elemType src = this->elements[i];
        elemType dst;

        dst = src & ~other->elements[i];
        result |= src ^ dst;
        this->elements[i] = dst;
    }
    return result;
}

bool hashBvNode::Intersects(hashBvNode* other)
{
    for (int i = 0; i < this->numElements(); i++)
    {
        if ((this->elements[i] & other->elements[i]) != 0)
        {
            return true;
        }
    }

    return false;
}

void hashBvNode::AndWith(hashBvNode* other)
{
    for (int i = 0; i < this->numElements(); i++)
    {
        this->elements[i] &= other->elements[i];
    }
}

void hashBvNode::OrWith(hashBvNode* other)
{
    for (int i = 0; i < this->numElements(); i++)
    {
        this->elements[i] |= other->elements[i];
    }
}

void hashBvNode::XorWith(hashBvNode* other)
{
    for (int i = 0; i < this->numElements(); i++)
    {
        this->elements[i] ^= other->elements[i];
    }
}

void hashBvNode::Subtract(hashBvNode* other)
{
    for (int i = 0; i < this->numElements(); i++)
    {
        this->elements[i] &= ~other->elements[i];
    }
}

bool hashBvNode::sameAs(hashBvNode* other)
{
    if (this->baseIndex != other->baseIndex)
    {
        return false;
    }

    for (int i = 0; i < this->numElements(); i++)
    {
        if (this->elements[i] != other->elements[i])
        {
            return false;
        }
    }

    return true;
}

// --------------------------------------------------------------------
// --------------------------------------------------------------------

hashBv::hashBv(Compiler* comp)
{
    this->compiler      = comp;
    this->log2_hashSize = 0;

    int hts = hashtable_size();
    nodeArr = getNewVector(hts);

    for (int i = 0; i < hts; i++)
    {
        nodeArr[i] = nullptr;
    }
    this->numNodes = 0;
}

hashBv* hashBv::Create(Compiler* compiler)
{
    hashBv*           result;
    hashBvGlobalData* gd = &compiler->hbvGlobalData;

    if (hbvFreeList(gd))
    {
        result          = hbvFreeList(gd);
        hbvFreeList(gd) = result->next;
        assert(result->nodeArr);
    }
    else
    {
        result = new (compiler, CMK_hashBv) hashBv(compiler);
        memset(result, 0, sizeof(hashBv));
        result->nodeArr = result->initialVector;
    }

    result->compiler      = compiler;
    result->log2_hashSize = 0;
    result->numNodes      = 0;

    return result;
}

void hashBv::Init(Compiler* compiler)
{
    memset(&compiler->hbvGlobalData, 0, sizeof(hashBvGlobalData));
}

hashBvGlobalData* hashBv::globalData()
{
    return &compiler->hbvGlobalData;
}

hashBvNode** hashBv::getNewVector(int vectorLength)
{
    assert(vectorLength > 0);
    assert(isPow2(vectorLength));

    hashBvNode** newVector = new (compiler, CMK_hashBv) hashBvNode*[vectorLength]();
    return newVector;
}

hashBvNode*& hashBv::nodeFreeList(hashBvGlobalData* data)
{
    return data->hbvNodeFreeList;
}

hashBv*& hashBv::hbvFreeList(hashBvGlobalData* data)
{
    return data->hbvFreeList;
}

void hashBv::hbvFree()
{
    int hts = hashtable_size();
    for (int i = 0; i < hts; i++)
    {
        while (nodeArr[i])
        {
            hashBvNode* curr = nodeArr[i];
            nodeArr[i]       = curr->next;
            curr->freeNode(globalData());
        }
    }
    // keep the vector attached because the whole thing is freelisted
    // plus you don't even know if it's freeable

    this->next                = hbvFreeList(globalData());
    hbvFreeList(globalData()) = this;
}

hashBv* hashBv::CreateFrom(hashBv* other, Compiler* comp)
{
    hashBv* result = hashBv::Create(comp);
    result->copyFrom(other, comp);
    return result;
}

void hashBv::MergeLists(hashBvNode** root1, hashBvNode** root2)
{
}

bool hashBv::TooSmall()
{
    return this->numNodes > this->hashtable_size() * 4;
}

bool hashBv::TooBig()
{
    return this->hashtable_size() > this->numNodes * 4;
}

int hashBv::getNodeCount()
{
    int size   = hashtable_size();
    int result = 0;

    for (int i = 0; i < size; i++)
    {
        hashBvNode* last = nodeArr[i];

        while (last)
        {
            last = last->next;
            result++;
        }
    }
    return result;
}

bool hashBv::IsValid()
{
    int size = hashtable_size();
    // is power of 2
    assert(((size - 1) & size) == 0);

    for (int i = 0; i < size; i++)
    {
        hashBvNode* last = nodeArr[i];
        hashBvNode* curr;
        int         lastIndex = -1;

        while (last)
        {
            // the node has been hashed correctly
            assert((int)last->baseIndex > lastIndex);
            lastIndex = (int)last->baseIndex;
            assert(i == getHashForIndex(last->baseIndex, size));
            curr = last->next;
            // the order is monotonically increasing bases
            if (curr)
            {
                assert(curr->baseIndex > last->baseIndex);
            }
            last = curr;
        }
    }
    return true;
}

void hashBv::Resize()
{
    // resize to 'optimal' size

    this->Resize(this->numNodes);
}

void hashBv::Resize(int newSize)
{
    assert(newSize > 0);
    newSize = nearest_pow2(newSize);

    int oldSize = hashtable_size();

    if (newSize == oldSize)
    {
        return;
    }

    int log2_newSize = genLog2((unsigned)newSize);

    hashBvNode** newNodes = this->getNewVector(newSize);

    hashBvNode*** insertionPoints = (hashBvNode***)_alloca(sizeof(hashBvNode*) * newSize);
    memset(insertionPoints, 0, sizeof(hashBvNode*) * newSize);

    for (int i = 0; i < newSize; i++)
    {
        insertionPoints[i] = &(newNodes[i]);
    }

    if (newSize > oldSize)
    {
        // for each src list, expand it into multiple dst lists
        for (int i = 0; i < oldSize; i++)
        {
            hashBvNode* next = nodeArr[i];

            while (next)
            {
                hashBvNode* curr = next;
                next             = curr->next;
                int destination  = getHashForIndex(curr->baseIndex, newSize);

                // ...

                // stick the current node on the end of the selected list
                *(insertionPoints[destination]) = curr;
                insertionPoints[destination]    = &(curr->next);
                curr->next                      = nullptr;
            }
        }
        nodeArr       = newNodes;
        log2_hashSize = (unsigned short)log2_newSize;
    }
    else if (oldSize > newSize)
    {
        // shrink multiple lists into one list
        // more efficient ways to do this but...
        // if the lists are long, you shouldn't be shrinking.
        for (int i = 0; i < oldSize; i++)
        {
            hashBvNode* next = nodeArr[i];

            if (next)
            {
                // all nodes in this list should have the same destination list
                int          destination    = getHashForIndex(next->baseIndex, newSize);
                hashBvNode** insertionPoint = &newNodes[destination];
                do
                {
                    hashBvNode* curr = next;
                    // figure out where to insert it
                    while (*insertionPoint && (*insertionPoint)->baseIndex < curr->baseIndex)
                    {
                        insertionPoint = &((*insertionPoint)->next);
                    }
                    next = curr->next;

                    hashBvNode* temp = *insertionPoint;
                    *insertionPoint  = curr;
                    curr->next       = temp;

                } while (next);
            }
        }
        nodeArr       = newNodes;
        log2_hashSize = (unsigned short)log2_newSize;
    }
    else
    {
        // same size
        assert(oldSize == newSize);
    }
    assert(this->IsValid());
}

#ifdef DEBUG
void hashBv::dump()
{
    bool      first = true;
    indexType index;

    // uncomment to print internal implementation details
    // DBEXEC(TRUE, printf("[%d(%d)(nodes:%d)]{ ", hashtable_size(), countBits(), this->numNodes));

    printf("{");
    FOREACH_HBV_BIT_SET(index, this)
    {
        if (!first)
        {
            printf(" ");
        }
        printf("%d", index);
        first = false;
    }
    NEXT_HBV_BIT_SET;
    printf("}\n");
}

void hashBv::dumpFancy()
{
    indexType index;
    indexType last_1 = -1;
    indexType last_0 = -1;

    printf("{");
    printf("count:%d", this->countBits());
    FOREACH_HBV_BIT_SET(index, this)
    {
        if (last_1 != index - 1)
        {
            if (last_0 + 1 != last_1)
            {
                printf(" %d-%d", last_0 + 1, last_1);
            }
            else
            {
                printf(" %d", last_1);
            }
            last_0 = index - 1;
        }
        last_1 = index;
    }
    NEXT_HBV_BIT_SET;

    // Print the last one
    if (last_0 + 1 != last_1)
    {
        printf(" %d-%d", last_0 + 1, last_1);
    }
    else
    {
        printf(" %d", last_1);
    }

    printf("}\n");
}
#endif // DEBUG

void hashBv::removeNodeAtBase(indexType index)
{
    hashBvNode** insertionPoint = this->getInsertionPointForIndex(index);

    hashBvNode* node = *insertionPoint;

    // make sure that we were called to remove something
    // that really was there
    assert(node);

    // splice it out
    *insertionPoint = node->next;
    this->numNodes--;
}

int hashBv::getHashForIndex(indexType index, int table_size)
{
    indexType hashIndex;

    hashIndex = index >> LOG2_BITS_PER_NODE;
    hashIndex &= (table_size - 1);

    return (int)hashIndex;
}

int hashBv::getRehashForIndex(indexType thisIndex, int thisTableSize, int newTableSize)
{
    assert(0);
    return 0;
}

hashBvNode** hashBv::getInsertionPointForIndex(indexType index)
{
    indexType indexInNode;
    indexType hashIndex;
    indexType baseIndex;

    hashBvNode* result;

    hashIndex = getHashForIndex(index, hashtable_size());

    baseIndex   = index & ~(BITS_PER_NODE - 1);
    indexInNode = index & (BITS_PER_NODE - 1);

    // printf("(%x) : hsh=%x, base=%x, index=%x\n", index,
    //      hashIndex, baseIndex, indexInNode);

    // find the node
    hashBvNode** prev = &nodeArr[hashIndex];
    result            = nodeArr[hashIndex];

    while (result)
    {
        if (result->baseIndex == baseIndex)
        {
            return prev;
        }
        else if (result->baseIndex > baseIndex)
        {
            return prev;
        }
        else
        {
            prev   = &(result->next);
            result = result->next;
        }
    }
    return prev;
}

hashBvNode* hashBv::getNodeForIndexHelper(indexType index, bool canAdd)
{
    // determine the base index of the node containing this index
    index = index & ~(BITS_PER_NODE - 1);

    hashBvNode** prev = getInsertionPointForIndex(index);

    hashBvNode* node = *prev;

    if (node && node->belongsIn(index))
    {
        return node;
    }
    else if (canAdd)
    {
        // missing node, insert it before the current one
        hashBvNode* temp = hashBvNode::Create(index, this->compiler);
        temp->next       = node;
        *prev            = temp;
        this->numNodes++;
        return temp;
    }
    else
    {
        return nullptr;
    }
}

hashBvNode* hashBv::getNodeForIndex(indexType index)
{
    // determine the base index of the node containing this index
    index = index & ~(BITS_PER_NODE - 1);

    hashBvNode** prev = getInsertionPointForIndex(index);

    hashBvNode* node = *prev;

    if (node && node->belongsIn(index))
    {
        return node;
    }
    else
    {
        return nullptr;
    }
}

void hashBv::setBit(indexType index)
{
    assert(index >= 0);
    assert(this->numNodes == this->getNodeCount());

    indexType baseIndex = index & ~(BITS_PER_NODE - 1);
    indexType base      = index - baseIndex;
    indexType elem      = base / BITS_PER_ELEMENT;
    indexType posi      = base % BITS_PER_ELEMENT;

    hashBvNode* result = nodeArr[0];

    // this should be the 99% case :  when there is only one node in the structure
    if ((result != nullptr) && (result->baseIndex == baseIndex))
    {
        result->elements[elem] |= indexType(1) << posi;
        return;
    }

    result = getOrAddNodeForIndex(index);
    result->setBit(index);

    assert(this->numNodes == this->getNodeCount());

    // if it's getting out of control resize it
    if (this->numNodes > this->hashtable_size() * 4)
    {
        this->Resize();
    }

    return;
}

void hashBv::setAll(indexType numToSet)
{
    // TODO-Throughput: this could be more efficient
    for (unsigned int i = 0; i < numToSet; i += BITS_PER_NODE)
    {
        hashBvNode* node        = getOrAddNodeForIndex(i);
        indexType   bits_to_set = min(BITS_PER_NODE, numToSet - i);
        node->setLowest(bits_to_set);
    }
}

void hashBv::clearBit(indexType index)
{
    assert(index >= 0);
    assert(this->numNodes == this->getNodeCount());
    hashBvNode* result = nullptr;

    indexType baseIndex = index & ~(BITS_PER_NODE - 1);
    indexType hashIndex = getHashForIndex(index, hashtable_size());

    hashBvNode** prev = &nodeArr[hashIndex];
    result            = nodeArr[hashIndex];

    while (result)
    {
        if (result->baseIndex == baseIndex)
        {
            result->clrBit(index);
            // if nothing left set free it
            if (!result->anySet())
            {
                *prev = result->next;
                result->freeNode(globalData());
                this->numNodes--;
            }
            return;
        }
        else if (result->baseIndex > baseIndex)
        {
            return;
        }
        else
        {
            prev   = &(result->next);
            result = result->next;
        }
    }
    assert(this->numNodes == this->getNodeCount());
    return;
}

bool hashBv::testBit(indexType index)
{
    // determine the base index of the node containing this index
    indexType baseIndex = index & ~(BITS_PER_NODE - 1);
    // 99% case
    if (nodeArr[0] && nodeArr[0]->baseIndex == baseIndex)
    {
        return nodeArr[0]->getBit(index);
    }

    indexType hashIndex = getHashForIndex(baseIndex, hashtable_size());

    hashBvNode* iter = nodeArr[hashIndex];

    while (iter)
    {
        if (iter->baseIndex == baseIndex)
        {
            return iter->getBit(index);
        }
        else
        {
            iter = iter->next;
        }
    }
    return false;
}

int hashBv::countBits()
{
    int result = 0;
    int hts    = this->hashtable_size();
    for (int hashNum = 0; hashNum < hts; hashNum++)
    {
        hashBvNode* node = nodeArr[hashNum];
        while (node)
        {
            result += node->countBits();
            node = node->next;
        }
    }
    return result;
}

bool hashBv::anySet()
{
    int hts = this->hashtable_size();
    for (int hashNum = 0; hashNum < hts; hashNum++)
    {
        hashBvNode* node = nodeArr[hashNum];
        while (node)
        {
            if (node->anySet())
            {
                return true;
            }
            node = node->next;
        }
    }
    return false;
}

class AndAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return false;
    }

    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in other, not this
        // so skip it
        r = r->next;
    }
    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in LHS, not RHS
        // so have to remove it
        hashBvNode* old = *l;
        *l              = (*l)->next;
        // splice it out
        old->freeNode(lhs->globalData());
        lhs->numNodes--;
        result = true;
    }
    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if ((*l)->AndWithChange(r))
        {
            r      = r->next;
            result = true;

            if ((*l)->anySet())
            {
                l = &((*l)->next);
            }
            else
            {
                hashBvNode* old = *l;
                *l              = (*l)->next;
                old->freeNode(lhs->globalData());
                lhs->numNodes--;
            }
        }
        else
        {
            r = r->next;
            l = &((*l)->next);
        }
    }
    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        r = r->next;
    }
};

class SubtractAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return false;
    }
    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in other, not this
        // so skip it
        r = r->next;
    }
    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // in lhs, not rhs
        // so skip lhs
        l = &((*l)->next);
    }
    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if ((*l)->SubtractWithChange(r))
        {
            r      = r->next;
            result = true;

            if ((*l)->anySet())
            {
                l = &((*l)->next);
            }
            else
            {
                hashBvNode* old = *l;
                *l              = (*l)->next;
                old->freeNode(lhs->globalData());
                lhs->numNodes--;
            }
        }
        else
        {
            r = r->next;
            l = &((*l)->next);
        }
    }
    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        r = r->next;
    }
};

class XorAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return false;
    }

    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in other, not this
        // so put one in
        result           = true;
        hashBvNode* temp = hashBvNode::Create(r->baseIndex, lhs->compiler);
        lhs->numNodes++;
        temp->XorWith(r);
        temp->next = (*l)->next;
        *l         = temp;
        l          = &(temp->next);

        r = r->next;
    }

    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in LHS, not RHS
        // so LHS remains the same
        l = &((*l)->next);
    }

    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if ((*l)->XorWithChange(r))
        {
            result = true;
        }
        l = &((*l)->next);
        r = r->next;
    }

    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in other, not this
        // so put one in
        result           = true;
        hashBvNode* temp = hashBvNode::Create(r->baseIndex, lhs->compiler);
        lhs->numNodes++;
        temp->XorWith(r);
        temp->next = nullptr;
        *l         = temp;
        l          = &(temp->next);

        r = r->next;
    }
};

class OrAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
        if (lhs->log2_hashSize + 2 < rhs->log2_hashSize)
        {
            lhs->Resize(rhs->numNodes);
        }
        if (rhs->numNodes > rhs->hashtable_size() * 4)
        {
            rhs->Resize(rhs->numNodes);
        }
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return false;
    }

    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // it's in other, not this
        // so put one in
        result           = true;
        hashBvNode* temp = hashBvNode::Create(r->baseIndex, lhs->compiler);
        lhs->numNodes++;
        temp->OrWith(r);
        temp->next = *l;
        *l         = temp;
        l          = &(temp->next);

        r = r->next;
    }
    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // in lhs, not rhs
        // so skip lhs
        l = &((*l)->next);
    }
    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if ((*l)->OrWithChange(r))
        {
            result = true;
        }
        l = &((*l)->next);
        r = r->next;
    }
    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // other contains something this does not
        // copy it
        // LeftGap(lhs, l, r, result, terminate);
        result           = true;
        hashBvNode* temp = hashBvNode::Create(r->baseIndex, lhs->compiler);
        lhs->numNodes++;
        temp->OrWith(r);
        temp->next = nullptr;
        *l         = temp;
        l          = &(temp->next);

        r = r->next;
    }
};

class CompareAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return true;
    }

    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        terminate = true;
        result    = false;
    }
    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // in lhs, not rhs
        // so skip lhs
        terminate = true;
        result    = false;
    }
    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if (!(*l)->sameAs(r))
        {
            terminate = true;
            result    = false;
        }
        l = &((*l)->next);
        r = r->next;
    }
    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        terminate = true;
        result    = false;
    }
};

class IntersectsAction
{
public:
    static inline void PreAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline void PostAction(hashBv* lhs, hashBv* rhs)
    {
    }
    static inline bool DefaultResult()
    {
        return false;
    }

    static inline void LeftGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // in rhs, not lhs
        // so skip rhs
        r = r->next;
    }
    static inline void RightGap(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        // in lhs, not rhs
        // so skip lhs
        l = &((*l)->next);
    }
    static inline void BothPresent(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        if ((*l)->Intersects(r))
        {
            terminate = true;
            result    = true;
        }
    }
    static inline void LeftEmpty(hashBv* lhs, hashBvNode**& l, hashBvNode*& r, bool& result, bool& terminate)
    {
        r = r->next;
    }
};

template <typename Action>
bool hashBv::MultiTraverseLHSBigger(hashBv* other)
{
    int hts = this->hashtable_size();
    int ots = other->hashtable_size();

    bool result    = Action::DefaultResult();
    bool terminate = false;

    // this is larger
    hashBvNode*** cursors;
    int           expansionFactor = hts / ots;
    cursors                       = (hashBvNode***)_alloca(expansionFactor * sizeof(void*));

    for (int h = 0; h < other->hashtable_size(); h++)
    {
        // set up cursors for the expansion of nodes
        for (int i = 0; i < expansionFactor; i++)
        {
            // ex: for [1024] &= [8]
            // for rhs in bin 0
            // cursors point to lhs: 0, 8, 16, 24, ...
            cursors[i] = &nodeArr[ots * i + h];
        }

        hashBvNode* o = other->nodeArr[h];
        while (o)
        {
            // figure out what dst list this goes to
            int          hash     = getHashForIndex(o->baseIndex, hts);
            int          dstIndex = (hash - h) >> other->log2_hashSize;
            hashBvNode** cursor   = cursors[dstIndex];
            hashBvNode*  c        = *cursor;

            // figure out where o fits in the cursor

            if (!c)
            {
                Action::LeftEmpty(this, cursors[dstIndex], o, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (c->baseIndex == o->baseIndex)
            {
                Action::BothPresent(this, cursors[dstIndex], o, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (c->baseIndex > o->baseIndex)
            {
                Action::LeftGap(this, cursors[dstIndex], o, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (c->baseIndex < o->baseIndex)
            {
                Action::RightGap(this, cursors[dstIndex], o, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
        }
        for (int i = 0; i < expansionFactor; i++)
        {
            while (*(cursors[i]))
            {
                Action::RightGap(this, cursors[i], o, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
        }
    }
    return result;
}

template <typename Action>
bool hashBv::MultiTraverseRHSBigger(hashBv* other)
{
    int ots = other->hashtable_size();

    bool result    = Action::DefaultResult();
    bool terminate = false;

    for (int hashNum = 0; hashNum < ots; hashNum++)
    {
        int destination = getHashForIndex(BITS_PER_NODE * hashNum, this->hashtable_size());
        assert(hashNum == getHashForIndex(BITS_PER_NODE * hashNum, other->hashtable_size()));

        hashBvNode** pa = &this->nodeArr[destination];
        hashBvNode** pb = &other->nodeArr[hashNum];
        hashBvNode*  b  = *pb;

        while (*pa && b)
        {
            hashBvNode* a = *pa;
            if (a->baseIndex < b->baseIndex)
            {
                // in a but not in b
                // but maybe it's someplace else in b
                if (getHashForIndex(a->baseIndex, ots) == hashNum)
                {
                    // this contains something other does not
                    // need to erase it
                    Action::RightGap(this, pa, b, result, terminate);
                    if (terminate)
                    {
                        return result;
                    }
                }
                else
                {
                    // other might contain this, we don't know yet
                    pa = &a->next;
                }
            }
            else if (a->baseIndex == b->baseIndex)
            {
                Action::BothPresent(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (a->baseIndex > b->baseIndex)
            {
                // other contains something this does not
                Action::LeftGap(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
        }
        while (*pa)
        {
            // if it's in the dest but not in src
            // then make sure it's expected to be in this list
            if (getHashForIndex((*pa)->baseIndex, ots) == hashNum)
            {
                Action::RightGap(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else
            {
                pa = &((*pa)->next);
            }
        }
        while (b)
        {
            Action::LeftEmpty(this, pa, b, result, terminate);
            if (terminate)
            {
                return result;
            }
        }
    }
    assert(this->numNodes == this->getNodeCount());
    return result;
}

// LHSBigger and RHSBigger algorithms both work for equal
// this is a specialized version of RHSBigger which is simpler (and faster)
// because equal sizes are the 99% case
template <typename Action>
bool hashBv::MultiTraverseEqual(hashBv* other)
{
    int hts = this->hashtable_size();
    assert(other->hashtable_size() == hts);

    bool result    = Action::DefaultResult();
    bool terminate = false;

    for (int hashNum = 0; hashNum < hts; hashNum++)
    {
        hashBvNode** pa = &this->nodeArr[hashNum];
        hashBvNode** pb = &other->nodeArr[hashNum];
        hashBvNode*  b  = *pb;

        while (*pa && b)
        {
            hashBvNode* a = *pa;
            if (a->baseIndex < b->baseIndex)
            {
                // in a but not in b
                Action::RightGap(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (a->baseIndex == b->baseIndex)
            {
                Action::BothPresent(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
            else if (a->baseIndex > b->baseIndex)
            {
                // other contains something this does not
                Action::LeftGap(this, pa, b, result, terminate);
                if (terminate)
                {
                    return result;
                }
            }
        }
        while (*pa)
        {
            // if it's in the dest but not in src
            Action::RightGap(this, pa, b, result, terminate);
            if (terminate)
            {
                return result;
            }
        }
        while (b)
        {
            Action::LeftEmpty(this, pa, b, result, terminate);
            if (terminate)
            {
                return result;
            }
        }
    }
    assert(this->numNodes == this->getNodeCount());
    return result;
}

template <class Action>
bool hashBv::MultiTraverse(hashBv* other)
{
    assert(this->numNodes == this->getNodeCount());

    Action::PreAction(this, other);

    int hts = this->log2_hashSize;
    int ots = other->log2_hashSize;

    if (hts == ots)
    {
        return MultiTraverseEqual<Action>(other);
    }
    else if (hts > ots)
    {
        return MultiTraverseLHSBigger<Action>(other);
    }
    else
    {
        return MultiTraverseRHSBigger<Action>(other);
    }
}

bool hashBv::Intersects(hashBv* other)
{
    return MultiTraverse<IntersectsAction>(other);
}

bool hashBv::AndWithChange(hashBv* other)
{
    return MultiTraverse<AndAction>(other);
}

// same as AND ~x
bool hashBv::SubtractWithChange(hashBv* other)
{
    return MultiTraverse<SubtractAction>(other);
}

void hashBv::Subtract(hashBv* other)
{
    this->SubtractWithChange(other);
}

void hashBv::Subtract3(hashBv* o1, hashBv* o2)
{
    this->copyFrom(o1, compiler);
    this->Subtract(o2);
}

void hashBv::UnionMinus(hashBv* src1, hashBv* src2, hashBv* src3)
{
    this->Subtract3(src1, src2);
    this->OrWithChange(src3);
}

void hashBv::ZeroAll()
{
    int hts = this->hashtable_size();

    for (int hashNum = 0; hashNum < hts; hashNum++)
    {
        while (nodeArr[hashNum])
        {
            hashBvNode* n    = nodeArr[hashNum];
            nodeArr[hashNum] = n->next;
            n->freeNode(globalData());
        }
    }
    this->numNodes = 0;
}

bool hashBv::OrWithChange(hashBv* other)
{
    return MultiTraverse<OrAction>(other);
}

bool hashBv::XorWithChange(hashBv* other)
{
    return MultiTraverse<XorAction>(other);
}
void hashBv::OrWith(hashBv* other)
{
    this->OrWithChange(other);
}

void hashBv::AndWith(hashBv* other)
{
    this->AndWithChange(other);
}

bool hashBv::CompareWith(hashBv* other)
{
    return MultiTraverse<CompareAction>(other);
}

void hashBv::copyFrom(hashBv* other, Compiler* comp)
{
    assert(this != other);

    hashBvNode* freeList = nullptr;

    this->ZeroAll();

    if (this->log2_hashSize != other->log2_hashSize)
    {
        this->nodeArr       = this->getNewVector(other->hashtable_size());
        this->log2_hashSize = other->log2_hashSize;
        assert(this->hashtable_size() == other->hashtable_size());
    }

    int hts = this->hashtable_size();
    // printf("in copyfrom\n");
    for (int h = 0; h < hts; h++)
    {
        // put the current list on the free list
        freeList         = this->nodeArr[h];
        this->nodeArr[h] = nullptr;

        hashBvNode** splicePoint = &(this->nodeArr[h]);
        hashBvNode*  otherNode   = other->nodeArr[h];
        hashBvNode*  newNode     = nullptr;

        while (otherNode)
        {
            // printf("otherNode is True...\n");
            this->numNodes++;

            if (freeList)
            {
                newNode  = freeList;
                freeList = freeList->next;
                newNode->Reconstruct(otherNode->baseIndex);
            }
            else
            {
                newNode = hashBvNode::Create(otherNode->baseIndex, this->compiler);
            }
            newNode->copyFrom(otherNode);

            newNode->next = *splicePoint;
            *splicePoint  = newNode;
            splicePoint   = &(newNode->next);

            otherNode = otherNode->next;
        }
    }
    while (freeList)
    {
        hashBvNode* next = freeList->next;
        freeList->freeNode(globalData());
        freeList = next;
    }
#if 0
    for (int h=0; h<hashtable_size(); h++)
    {
        printf("%p %p\n", this->nodeArr[h], other->nodeArr[h]);
    }
#endif
}

int nodeSort(const void* x, const void* y)
{
    hashBvNode* a = (hashBvNode*)x;
    hashBvNode* b = (hashBvNode*)y;
    return (int)(b->baseIndex - a->baseIndex);
}

void hashBv::InorderTraverse(nodeAction n)
{
    int hts = hashtable_size();

    hashBvNode** x = new (compiler, CMK_hashBv) hashBvNode*[hts];

    {
        // keep an array of the current pointers
        // into each of the bitvector lists
        // in the hashtable
        for (int i = 0; i < hts; i++)
        {
            x[i] = nodeArr[i];
        }

        while (1)
        {
            // pick the lowest node in the hashtable

            indexType lowest       = INT_MAX;
            int       lowest_index = -1;
            for (int i = 0; i < hts; i++)
            {
                if (x[i] && x[i]->baseIndex < lowest)
                {
                    lowest       = x[i]->baseIndex;
                    lowest_index = i;
                }
            }
            // if there was anything left, use it and update
            // the list pointers otherwise we are done
            if (lowest_index != -1)
            {
                n(x[lowest_index]);
                x[lowest_index] = x[lowest_index]->next;
            }
            else
            {
                break;
            }
        }
    }

    delete[] x;
}

void hashBv::InorderTraverseTwo(hashBv* other, dualNodeAction a)
{
    int          sizeThis, sizeOther;
    hashBvNode **nodesThis, **nodesOther;

    sizeThis  = this->hashtable_size();
    sizeOther = other->hashtable_size();

    nodesThis  = new (compiler, CMK_hashBv) hashBvNode*[sizeThis];
    nodesOther = new (compiler, CMK_hashBv) hashBvNode*[sizeOther];

    // populate the arrays
    for (int i = 0; i < sizeThis; i++)
    {
        nodesThis[i] = this->nodeArr[i];
    }

    for (int i = 0; i < sizeOther; i++)
    {
        nodesOther[i] = other->nodeArr[i];
    }

    while (1)
    {
        indexType lowestThis           = INT_MAX;
        indexType lowestOther          = INT_MAX;
        int       lowestHashIndexThis  = -1;
        int       lowestHashIndexOther = -1;

        // find the lowest remaining node in each BV
        for (int i = 0; i < sizeThis; i++)
        {
            if (nodesThis[i] && nodesThis[i]->baseIndex < lowestThis)
            {
                lowestHashIndexThis = i;
                lowestThis          = nodesThis[i]->baseIndex;
            }
        }
        for (int i = 0; i < sizeOther; i++)
        {
            if (nodesOther[i] && nodesOther[i]->baseIndex < lowestOther)
            {
                lowestHashIndexOther = i;
                lowestOther          = nodesOther[i]->baseIndex;
            }
        }
        hashBvNode *nodeThis, *nodeOther;
        nodeThis  = lowestHashIndexThis == -1 ? nullptr : nodesThis[lowestHashIndexThis];
        nodeOther = lowestHashIndexOther == -1 ? nullptr : nodesOther[lowestHashIndexOther];
        // no nodes left in either, so return
        if ((!nodeThis) && (!nodeOther))
        {
            break;

            // there are only nodes left in one bitvector
        }
        else if ((!nodeThis) || (!nodeOther))
        {
            a(this, other, nodeThis, nodeOther);
            if (nodeThis)
            {
                nodesThis[lowestHashIndexThis] = nodesThis[lowestHashIndexThis]->next;
            }
            if (nodeOther)
            {
                nodesOther[lowestHashIndexOther] = nodesOther[lowestHashIndexOther]->next;
            }
        }
        // nodes are left in both so determine if the lowest ones
        // match.  if so process them in a pair.  if not then
        // process the lower of the two alone
        else
        {
            if (nodeThis->baseIndex == nodeOther->baseIndex)
            {
                a(this, other, nodeThis, nodeOther);
                nodesThis[lowestHashIndexThis]   = nodesThis[lowestHashIndexThis]->next;
                nodesOther[lowestHashIndexOther] = nodesOther[lowestHashIndexOther]->next;
            }
            else if (nodeThis->baseIndex < nodeOther->baseIndex)
            {
                a(this, other, nodeThis, nullptr);
                nodesThis[lowestHashIndexThis] = nodesThis[lowestHashIndexThis]->next;
            }
            else if (nodeOther->baseIndex < nodeThis->baseIndex)
            {
                a(this, other, nullptr, nodeOther);
                nodesOther[lowestHashIndexOther] = nodesOther[lowestHashIndexOther]->next;
            }
        }
    }
    delete[] nodesThis;
    delete[] nodesOther;
}

// --------------------------------------------------------------------
// --------------------------------------------------------------------

#ifdef DEBUG
void SimpleDumpNode(hashBvNode* n)
{
    printf("base: %d\n", n->baseIndex);
}

void DumpNode(hashBvNode* n)
{
    n->dump();
}

void SimpleDumpDualNode(hashBv* a, hashBv* b, hashBvNode* n, hashBvNode* m)
{
    printf("nodes: ");
    if (n)
    {
        printf("%d,", n->baseIndex);
    }
    else
    {
        printf("----,");
    }
    if (m)
    {
        printf("%d\n", m->baseIndex);
    }
    else
    {
        printf("----\n");
    }
}
#endif // DEBUG

hashBvIterator::hashBvIterator()
{
    this->bv = nullptr;
}

hashBvIterator::hashBvIterator(hashBv* bv)
{
    this->bv              = bv;
    this->hashtable_index = 0;
    this->current_element = 0;
    this->current_base    = 0;
    this->current_data    = 0;

    if (bv)
    {
        this->hashtable_size = bv->hashtable_size();
        this->currNode       = bv->nodeArr[0];

        if (!this->currNode)
        {
            this->nextNode();
        }
    }
}

void hashBvIterator::initFrom(hashBv* bv)
{
    this->bv              = bv;
    this->hashtable_size  = bv->hashtable_size();
    this->hashtable_index = 0;
    this->currNode        = bv->nodeArr[0];
    this->current_element = 0;
    this->current_base    = 0;
    this->current_data    = 0;

    if (!this->currNode)
    {
        this->nextNode();
    }
    if (this->currNode)
    {
        this->current_data = this->currNode->elements[0];
    }
}

void hashBvIterator::nextNode()
{
    // if we have a valid node then just get the next one in the chain
    if (this->currNode)
    {
        this->currNode = this->currNode->next;
    }

    // else step to the next one in the hash table
    while (!this->currNode)
    {
        hashtable_index++;
        // no more
        if (hashtable_index >= hashtable_size)
        {
            // printf("nextnode bailed\n");
            return;
        }

        this->currNode = bv->nodeArr[hashtable_index];
    }
    // first element in the new node
    this->current_element = 0;
    this->current_base    = this->currNode->baseIndex;
    this->current_data    = this->currNode->elements[0];
    // printf("nextnode returned base %d\n", this->current_base);
    // printf("hti = %d ", hashtable_index);
}

indexType hashBvIterator::nextBit()
{

    // printf("in nextbit for bv:\n");
    // this->bv->dump();

    if (!this->currNode)
    {
        this->nextNode();
    }

top:

    if (!this->currNode)
    {
        return NOMOREBITS;
    }

more_data:
    if (!this->current_data)
    {
        current_element++;
        // printf("current element is %d\n", current_element);
        // reached the end of this node
        if (current_element == (indexType) this->currNode->numElements())
        {
            // printf("going to next node\n");
            this->nextNode();
            goto top;
        }
        else
        {
            assert(current_element < (indexType) this->currNode->numElements());
            // printf("getting more data\n");
            current_data = this->currNode->elements[current_element];
            current_base = this->currNode->baseIndex + current_element * BITS_PER_ELEMENT;
            goto more_data;
        }
    }
    else
    {
        while (current_data)
        {
            if (current_data & 1)
            {
                current_data >>= 1;
                current_base++;

                return current_base - 1;
            }
            else
            {
                current_data >>= 1;
                current_base++;
            }
        }
        goto more_data;
    }
}
