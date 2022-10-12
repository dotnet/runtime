// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ASMTEMPLATES_H
#define ASMTEMPLATES_H

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22008) // "Suppress PREfast warnings about integer overflow"
#endif

inline ULONG GrowBuffer(ULONG startingSize)
{
    int toAdd = startingSize >> 1;
    if (toAdd < 8)
        toAdd = 8;
    if (toAdd > 2048)
        toAdd = 2048;
    return startingSize + toAdd;
}

/*****************************************************************************/
/* LIFO (stack) and FIFO (queue) templates (must precede #include "method.h")*/
template <class T>
class LIST_EL
{
public:
    T*  m_Ptr;
    LIST_EL <T> *m_Next;
    LIST_EL(T *item) {m_Next = NULL; m_Ptr = item; };
};

template <class T>
class LIFO
{
public:
    inline LIFO() { m_pHead = NULL; };
    inline ~LIFO() {T *val; while((val = POP()) != NULL) delete val; };
    void PUSH(T *item)
    {
        m_pTemp = new LIST_EL <T>(item);
        m_pTemp->m_Next = m_pHead;
        m_pHead = m_pTemp;
    };
    T* POP()
    {
        T* ret = NULL;
        if((m_pTemp = m_pHead) != NULL)
        {
            m_pHead = m_pHead->m_Next;
            ret = m_pTemp->m_Ptr;
            delete m_pTemp;
        }
        return ret;
    };
private:
    LIST_EL <T> *m_pHead;
    LIST_EL <T> *m_pTemp;
};


template <class T>
class FIFO
{
public:
    FIFO() { m_Arr = NULL; m_ulArrLen = 0; m_ulCount = 0; m_ulOffset = 0; };
    ~FIFO() {
        if(m_Arr) {
            for(ULONG i=0; i < m_ulCount; i++) {
                if(m_Arr[i+m_ulOffset]) delete m_Arr[i+m_ulOffset];
            }
            delete [] m_Arr;
        }
    };
    void RESET(bool DeleteElements = true) {
        if(m_Arr) {
            for(ULONG i=0; i < m_ulCount; i++) {
                if(DeleteElements) delete m_Arr[i+m_ulOffset];
                m_Arr[i+m_ulOffset] = NULL;
            }
            m_ulCount = 0;
            m_ulOffset= 0;
        }
    };
    void PUSH(T *item)
    {
		if(item)
		{
			if(m_ulCount+m_ulOffset >= m_ulArrLen)
			{
				if(m_ulOffset)
				{
					memcpy(m_Arr,&m_Arr[m_ulOffset],m_ulCount*sizeof(T*));
					m_ulOffset = 0;
				}
				else
				{
                    m_ulArrLen = GrowBuffer(m_ulArrLen);
					T** tmp = new T*[m_ulArrLen];
					if(tmp)
					{
						if(m_Arr)
						{
							memcpy(tmp,m_Arr,m_ulCount*sizeof(T*));
							delete [] m_Arr;
						}
						m_Arr = tmp;
					}
					else fprintf(stderr,"\nOut of memory!\n");
				}
			}
			m_Arr[m_ulOffset+m_ulCount] = item;
			m_ulCount++;
		}
    };
    ULONG COUNT() { return m_ulCount; };
    T* POP()
    {
        T* ret = NULL;
        if(m_ulCount)
        {
            ret = m_Arr[m_ulOffset++];
            m_ulCount--;
        }
        return ret;
    };
    T* PEEK(ULONG idx) { return (idx < m_ulCount) ? m_Arr[m_ulOffset+idx] : NULL; };
private:
    T** m_Arr;
    ULONG       m_ulCount;
    ULONG       m_ulOffset;
    ULONG       m_ulArrLen;
};


// Indx256 implements a trie (or prefix tree) on null-terminated sequences of BYTEs.
//
// It is very memory intensive because it allocates dense arrays for every node
// once the node has any child nodes, though it does allocate tables separately for
// BYTEs in [1,127] and [128,255].  The functions are implemented by choosing the
// table, adjusting the index (making the [128,255] indices zero-based for the
// "high" table) and calling a helper that operates on one table.  The Indx256 type
// serves as the overall trie as well as the nodes.  I.e., the root node is the
// overall trie.
//
// Much more space could be saved here.  Perhap even a hashtable or search tree
// would be sufficient; it's unclear if there was a historical performance-critical
// aspect to this choice.  Note also that since this is used for labels and a
// stress test of this is the output of ildasm since it labels every instruction,
// the pattern "IL_<hex>" could be special-cased.
//
// Example (with the input strings reduced to 0..3 rather than 0..255
//          and only one table per node):
//
// Contents: { 1 -> "A", 11 -> "B", 22 -> "C" }
//
// Trie:
//
//   NULL, [ NULL, ptr_1, ptr_2, NULL ]   // no empty key, keys start with 1 and 2
//                   |      |
//                   |      \--> NULL, [ NULL, NULL, ptr_2, NULL ]     // no 2 key, keys start with 22
//                   |                                 |
//                   |                                 \--> "C", NULL  // 22 is "C", no keys extend 22
//                   |
//                   \--> "A", [ NULL, ptr_1, NULL, NULL ]             // 1 is "A", keys start with 11
//                                       |
//                                       \--> "B", NULL                // 11 is "B", no keys extend 11

// There are two tables per node, so the value here is 256 / 2.
#define INDX256_TABLE_SIZE 128

template <class T> struct Indx256
{
    T* item; // The value corresponding to the sequence ending at this node
    Indx256* tableLow;  // Child nodes: either NULL or points to 128 elements for [1,127].
                        // Element 0 is not used because 0 is the terminator.  This makes indexing simpler.
    Indx256* tableHigh; // Child nodes: either NULL or points to 128 elements for [128,255].
    Indx256() { item=nullptr; tableLow=nullptr; tableHigh=nullptr; };
    ~Indx256()
    {
        ClearAll(true);
    };

    T** IndexString(BYTE* psz, T* pObj)
    {
        if(*psz == 0)
        {
            // Found NULL terminator.  Install value and return pointer to it.
            item = pObj;
            return &item;
        }

        if(*psz >= INDX256_TABLE_SIZE)
        {
            return IndexStringOneTable(tableHigh, *psz - INDX256_TABLE_SIZE, psz + 1, pObj);
        }
        else
        {
            return IndexStringOneTable(tableLow, *psz, psz + 1, pObj);
        }
    };

    T* FindString(BYTE* psz)
    {
        if(*psz == 0)
        {
            // Found NULL terminator.  Return value.
            return item;
        }

        if(*psz >= INDX256_TABLE_SIZE)
        {
            return FindStringOneTable(tableHigh, *psz - INDX256_TABLE_SIZE, psz + 1);
        }
        else
        {
            return FindStringOneTable(tableLow, *psz, psz + 1);
        }
    };

    void ClearAll(bool DeleteObj)
    {
        if(DeleteObj) delete item;
        item = NULL;
        ClearOneTable(tableLow, DeleteObj);
        ClearOneTable(tableHigh, DeleteObj);
    };

private:
    T** IndexStringOneTable(Indx256*& table, BYTE value, BYTE* next, T* pObj)
    {
        // Ensure that child table exists.
        if(table == NULL)
        {
            table = new Indx256[INDX256_TABLE_SIZE] {};
            if(table == NULL)
            {
                _ASSERTE(!"Out of memory in Indx256::IndexString!");
                fprintf(stderr,"\nOut of memory in Indx256::IndexString!\n");
                return NULL;
            }
        }

        // Find the child node for the current BYTE at continue at the next BYTE.
        return table[value].IndexString(next,pObj);
    }

    T* FindStringOneTable(Indx256* table, BYTE value, BYTE* next)
    {
        if(table == NULL)
        {
            // If there are no child nodes, then there is nowhere to
            // look for this key.
            return NULL;
        }

        return table[value].FindString(next);
    }

    void ClearOneTable(Indx256*& table, bool DeleteObj)
    {
        if(table)
        {
            for(unsigned i = 0; i < INDX256_TABLE_SIZE; i++)
            {
                table[i].ClearAll(DeleteObj);
            }
            delete[] table;
            table = NULL;
        }
    }
};

//
// Template intended for named objects, that expose function char* NameOf()
//
template <class T>
class FIFO_INDEXED
{
public:
    FIFO_INDEXED() { m_Arr = NULL; m_ulArrLen = 0; m_ulCount = 0; m_ulOffset = 0; };
    ~FIFO_INDEXED() {
        if(m_Arr)
        {
            RESET(true);
            delete [] m_Arr;
        }
    };
    void RESET(bool DeleteElements = true) {
        if(m_Arr) {
            unsigned i;
            if(DeleteElements)
            {
                for(i=m_ulOffset; i < m_ulOffset+m_ulCount; i++)
                {
                    T** ppT = m_Arr[i];
                    delete *ppT;
                }
            }
            for(i=m_ulOffset; i < m_ulOffset+m_ulCount; i++)
            {
                *m_Arr[i] = NULL;
            }
            memset(&m_Arr[m_ulOffset],0,m_ulCount*sizeof(void*));
            m_ulCount = 0;
            m_ulOffset= 0;
        }
    };
    void PUSH(T *item)
    {
		if(item)
		{
            T** itemaddr = m_Index.IndexString((BYTE*)(item->NameOf()),item);
            if(m_ulCount+m_ulOffset >= m_ulArrLen)
			{
				if(m_ulOffset)
				{
					memcpy(m_Arr,&m_Arr[m_ulOffset],m_ulCount*sizeof(T*));
					m_ulOffset = 0;
				}
				else
				{
					m_ulArrLen = GrowBuffer(m_ulArrLen);
					T*** tmp = new T**[m_ulArrLen];
					if(tmp)
					{
						if(m_Arr)
						{
							memcpy(tmp,m_Arr,m_ulCount*sizeof(T**));
							delete [] m_Arr;
						}
						m_Arr = tmp;
					}
					else fprintf(stderr,"\nOut of memory!\n");
				}
			}
			m_Arr[m_ulOffset+m_ulCount] = itemaddr;
			m_ulCount++;
		}
    };
    ULONG COUNT() { return m_ulCount; };
    T* POP()
    {
        T* ret = NULL;
        if(m_ulCount)
        {
            ret = *(m_Arr[m_ulOffset]);
            *m_Arr[m_ulOffset] = NULL;
            m_ulOffset++;
            m_ulCount--;
        }
        return ret;
    };
    T* PEEK(ULONG idx) { return (idx < m_ulCount) ? *(m_Arr[m_ulOffset+idx]) : NULL; };
    T* FIND(T* item) { return m_Index.FindString((BYTE*)(item->NameOf())); };
private:
    T*** m_Arr;
    ULONG       m_ulCount;
    ULONG       m_ulOffset;
    ULONG       m_ulArrLen;
    Indx256<T>  m_Index;
};

template <class T>
class SORTEDARRAY
{
public:
    SORTEDARRAY() { m_Arr = NULL; m_ulArrLen = 0; m_ulCount = 0; m_ulOffset = 0; };
    ~SORTEDARRAY() {
        if(m_Arr) {
            for(ULONG i=0; i < m_ulCount; i++) {
                if(m_Arr[i+m_ulOffset]) delete m_Arr[i+m_ulOffset];
            }
            delete [] m_Arr;
        }
    };
    void RESET(bool DeleteElements = true) {
        if(m_Arr) {
            if(DeleteElements)
            {
                for(ULONG i=0; i < m_ulCount; i++) {
                    delete m_Arr[i+m_ulOffset];
                }
            }
            memset(m_Arr,0,m_ulArrLen*sizeof(T*));
            m_ulCount = 0;
            m_ulOffset= 0;
        }
    };
    void PUSH(T *item)
    {
		if(item)
		{
			if(m_ulCount+m_ulOffset >= m_ulArrLen)
			{
				if(m_ulOffset)
				{
					memcpy(m_Arr,&m_Arr[m_ulOffset],m_ulCount*sizeof(T*));
					m_ulOffset = 0;
				}
				else
				{
					m_ulArrLen = GrowBuffer(m_ulArrLen);
					T** tmp = new T*[m_ulArrLen];
					if(tmp)
					{
						if(m_Arr)
						{
							memcpy(tmp,m_Arr,m_ulCount*sizeof(T*));
							delete [] m_Arr;
						}
						m_Arr = tmp;
					}
					else fprintf(stderr,"\nOut of memory!\n");
				}
			}
            if(m_ulCount)
            {
                // find  1st arr.element > item
                T** low = &m_Arr[m_ulOffset];
                T** high = &m_Arr[m_ulOffset+m_ulCount-1];
                T** mid;

                if(item->ComparedTo(*high) > 0) mid = high+1;
                else if(item->ComparedTo(*low) < 0) mid = low;
                else for(;;)
                {
                    mid = &low[(high - low) >> 1];

                    int cmp = item->ComparedTo(*mid);

                    if (mid == low)
                    {
                        if(cmp > 0) mid++;
                        break;
                    }

                    if (cmp > 0) low = mid;
                    else        high = mid;
                }

                /////////////////////////////////////////////
                 memmove(mid+1,mid,(BYTE*)&m_Arr[m_ulOffset+m_ulCount]-(BYTE*)mid);
                *mid = item;
            }
			else m_Arr[m_ulOffset+m_ulCount] = item;
			m_ulCount++;
		}
    };
    ULONG COUNT() { return m_ulCount; };
    T* POP()
    {
        T* ret = NULL;
        if(m_ulCount)
        {
            ret = m_Arr[m_ulOffset++];
            m_ulCount--;
        }
        return ret;
    };
    T* PEEK(ULONG idx) { return (idx < m_ulCount) ? m_Arr[m_ulOffset+idx] : NULL; };
    T* FIND(T* item)
    {
        if(m_ulCount)
        {
            T** low = &m_Arr[m_ulOffset];
            T** high = &m_Arr[m_ulOffset+m_ulCount-1];
            T** mid;
            if(item->ComparedTo(*high) == 0) return(*high);
            for(;;)
            {
                mid = &low[(high - low) >> 1];
                int cmp = item->ComparedTo(*mid);
                if (cmp == 0) return(*mid);
                if (mid == low)  break;
                if (cmp > 0) low = mid;
                else        high = mid;
            }
        }
        return NULL;
    };
    /*
    T* FIND(U item)
    {
        if(m_ulCount)
        {
            T** low = &m_Arr[m_ulOffset];
            T** high = &m_Arr[m_ulOffset+m_ulCount-1];
            T** mid;
            if((*high)->Compare(item) == 0) return(*high);
            for(;;)
            {
                mid = &low[(high - low) >> 1];
                int cmp = (*mid)->Compare(item);
                if (cmp == 0) return(*mid);
                if (mid == low)  break;
                if (cmp > 0) low = mid;
                else        high = mid;
            }
        }
        return NULL;
    };
    */
    BOOL DEL(T* item)
    {
        if(m_ulCount)
        {
            T** low = &m_Arr[m_ulOffset];
            T** high = &m_Arr[m_ulOffset+m_ulCount-1];
            T** mid;
            if(item->ComparedTo(*high) == 0)
            {
                delete (*high);
                m_ulCount--;
                return TRUE;
            }
            for(;;)
            {
                mid = &low[(high - low) >> 1];
                int cmp = item->ComparedTo(*mid);
                if (cmp == 0)
                {
                    delete (*mid);
                    memmove(mid,mid+1,(BYTE*)&m_Arr[m_ulOffset+m_ulCount]-(BYTE*)mid-1);
                    m_ulCount--;
                    return TRUE;
                }
                if (mid == low)  break;
                if (cmp > 0) low = mid;
                else        high = mid;
            }
        }
        return FALSE;
    };
private:
    T** m_Arr;
    ULONG       m_ulCount;
    ULONG       m_ulOffset;
    ULONG       m_ulArrLen;
};

template <class T> struct RBNODE
{
private:
    DWORD       dwRed;
public:
    DWORD       dwInUse;
    T*          tVal;
    RBNODE<T>*  pLeft;
    RBNODE<T>*  pRight;
    RBNODE<T>*  pParent;
    RBNODE()
    {
        pLeft = pRight = pParent = NULL;
        tVal = NULL;
        dwRed = dwInUse = 0;
    };
    RBNODE(T* pVal, DWORD dwColor)
    {
        pLeft = pRight = pParent = NULL;
        tVal = pVal;
        dwRed = dwColor;
        dwInUse = 0;
    };
    bool IsRed() { return (dwRed != 0); };
    void SetRed() { dwRed = 1; };
    void SetBlack() { dwRed = 0; };
};

#define BUCKETCOUNT 512

template <class T> class RBNODEBUCKET
{
private:
    RBNODEBUCKET<T>* pNext;
    RBNODE<T> bucket[BUCKETCOUNT];
    unsigned  alloc_count;

public:
    RBNODEBUCKET()
    {
        alloc_count = 0;
        pNext = NULL;
    };

    ~RBNODEBUCKET() { if(pNext) delete pNext; };

    bool CanAlloc() { return (alloc_count < BUCKETCOUNT); };

    RBNODE<T>* AllocNode()
    {
        RBNODE<T>* pRet;
        for(unsigned i = 0; i < BUCKETCOUNT; i++)
        {
            if(bucket[i].dwInUse == 0)
            {
                alloc_count++;
                pRet = &bucket[i];
                *pRet = {};
                pRet->dwInUse = 1;
                return pRet;
            }
        }
        _ASSERTE(!"AllocNode returns NULL");
        return NULL;
    };

    bool FreeNode(RBNODE<T>* ptr)
    {
        size_t idx = ((size_t)ptr - (size_t)bucket)/sizeof(RBNODE<T>);
        if(idx < BUCKETCOUNT)
        {
            bucket[idx].dwInUse = 0;
            alloc_count--;
            return true;
        }
        return false;
    };

    RBNODEBUCKET<T>* Next() { return pNext; };

    void Append(RBNODEBUCKET<T>* ptr) { pNext = ptr; };
};

template <class T> class RBNODEPOOL
{
private:
    RBNODEBUCKET<T> base;

public:
    RBNODEPOOL() = default;

    RBNODE<T>* AllocNode()
    {
        RBNODEBUCKET<T>* pBucket = &base;
        RBNODEBUCKET<T>* pLastBucket = &base;
        do
        {
            if(pBucket->CanAlloc())
            {
                return pBucket->AllocNode();
            }
            pLastBucket = pBucket;
            pBucket = pBucket->Next();
        }
        while (pBucket != NULL);
        pLastBucket->Append(new RBNODEBUCKET<T>);
        return pLastBucket->Next()->AllocNode();
    };

    void FreeNode(RBNODE<T>* ptr)
    {
        RBNODEBUCKET<T>* pBucket = &base;
        do
        {
            if(pBucket->FreeNode(ptr))
                break;
            pBucket = pBucket->Next();
        }
        while (pBucket != NULL);
    };
};

template <class T> class RBTREE
{
private:
    RBNODE<T>* pRoot;
    RBNODE<T>* pNil;
    RBNODEPOOL<T> NodePool;
    void RotateLeft(RBNODE<T>* pX)
    {
        RBNODE<T>* pY;

        pY = pX->pRight;
        pX->pRight = pY->pLeft;

        if(pY->pLeft != pNil)
            pY->pLeft->pParent = pX;

        pY->pParent = pX->pParent;

        if(pX == pX->pParent->pLeft)
            pX->pParent->pLeft = pY;
        else
            pX->pParent->pRight = pY;

        pY->pLeft = pX;
        pX->pParent = pY;
    };

    void RotateRight(RBNODE<T>* pX)
    {
        RBNODE<T>* pY;

        pY = pX->pLeft;
        pX->pLeft = pY->pRight;

        if(pY->pRight != pNil)
            pY->pRight->pParent = pX;

        pY->pParent = pX->pParent;

        if(pX == pX->pParent->pLeft)
            pX->pParent->pLeft = pY;
        else
            pX->pParent->pRight = pY;

        pY->pRight = pX;
        pX->pParent = pY;

    };

    void InsertNode(RBNODE<T>* pZ)
    {
        RBNODE<T>* pX;
        RBNODE<T>* pY;

        pZ->pLeft = pZ->pRight = pNil;
        pY = pRoot;
        pX = pRoot->pLeft;

        if(pX != pY)
        {
            while(pX != pNil)
            {
                pY = pX;
                if(pX->tVal->ComparedTo(pZ->tVal) > 0)
                    pX = pX->pLeft;
                else
                    pX = pX->pRight;
            }
        }
        pZ->pParent = pY;
        if((pY == pRoot) || (pY->tVal->ComparedTo(pZ->tVal) > 0))
            pY->pLeft = pZ;
        else
            pY->pRight = pZ;
    };

    void InitSpecNode(RBNODE<T>* pNode)
    {
        pNode->pLeft = pNode->pRight = pNode->pParent = pNode;
    };

    void DeleteNode(RBNODE<T>* pNode, bool DeletePayload = true)
    {
        if((pNode != pNil)&&(pNode != pRoot))
        {
            DeleteNode(pNode->pLeft, DeletePayload);
            DeleteNode(pNode->pRight, DeletePayload);
            if(DeletePayload)
                delete pNode->tVal;
            NodePool.FreeNode(pNode);
        }
    };

public:
    RBTREE() : NodePool{}
    {
        pRoot = NodePool.AllocNode();
        InitSpecNode(pRoot);

        pNil = NodePool.AllocNode();
        InitSpecNode(pNil);
    };

    ~RBTREE()
    {
        //RESET(false);
        //NodePool.FreeNode(pRoot);
        //NodePool.FreeNode(pNil);
    };

    void RESET(bool DeletePayload = true)
    {
        DeleteNode(pRoot->pLeft, DeletePayload);
        InitSpecNode(pRoot);
        InitSpecNode(pNil);
    };

    void PUSH(T* pT)
    {
        RBNODE<T>* pX;
        RBNODE<T>* pY;
        RBNODE<T>* pNewNode = NodePool.AllocNode();

        pNewNode->tVal = pT;
        pNewNode->SetRed();

        InsertNode(pNewNode);

        for(pX = pNewNode; pX->pParent->IsRed();)
        {
            if(pX->pParent == pX->pParent->pLeft)
            {
                pY = pX->pParent->pRight;
                if(pY->IsRed())
                {
                    pX->pParent->SetBlack();
                    pY->SetBlack();
                    pX->pParent->pParent->SetRed();
                    pX = pX->pParent->pParent;
                }
                else
                {
                    if(pX == pX->pParent->pRight)
                    {
                        pX = pX->pParent;
                        RotateLeft(pX);
                    }
                    pX->pParent->SetBlack();
                    pX->pParent->pParent->SetRed();
                    RotateRight(pX->pParent->pParent);
                }
            }
            else // if(pX->pParent == pX->pParent->pRight)
            {
                pY = pX->pParent->pParent->pLeft;
                if(pY->IsRed())
                {
                    pX->pParent->SetBlack();
                    pY->SetBlack();
                    pX->pParent->pParent->SetRed();
                    pX = pX->pParent->pParent;
                }
                else
                {
                    if(pX == pX->pParent->pLeft)
                    {
                        pX = pX->pParent;
                        RotateRight(pX);
                    }
                    pX->pParent->SetBlack();
                    pX->pParent->pParent->SetRed();
                    RotateLeft(pX->pParent->pParent);
                }
            }// end if(pX->pParent == pX->pParent->pLeft) -- else
        } // end for(pX = pNewNode; pX->pParent->IsRed();)
        pRoot->pLeft->SetBlack();
    };

    T* FIND(T* pT)
    {
        RBNODE<T>* pX = pRoot->pLeft;
        if((pX != pNil) && (pX != pRoot))
        {
            int cmp = pX->tVal->ComparedTo(pT);
            while(cmp != 0)
            {
                if(cmp > 0)
                    pX = pX->pLeft;
                else
                    pX = pX->pRight;
                if(pX == pNil)
                    return NULL;
                cmp = pX->tVal->ComparedTo(pT);
            }
            return pX->tVal;
        }
        return NULL;
    };
};

#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif //ASMTEMPLATES_H

