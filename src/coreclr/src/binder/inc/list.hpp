// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// List.hpp
//


//
// Defines the List class
//
// ============================================================

#ifndef __BINDER__LIST_HPP__
#define __BINDER__LIST_HPP__

#include "bindertypes.hpp"
#include "ex.h"

namespace BINDER_SPACE
{
    //
    // ListNode
    //

    typedef void *LISTNODE;

    template <class Type> class ListNode
    {
    public:
        ListNode(Type item);
        virtual ~ListNode();

        void SetNext(ListNode *pNode);
        void SetPrev(ListNode *pNode);
        Type GetItem();
        ListNode *GetNext();
        ListNode *GetPrev();

    private:
        Type       _type;
        ListNode  *_pNext;
        ListNode  *_pPrev;
    };

    //
    // List
    //

    template <class Type> class List
    {
    public:
        List();
        ~List();

        LISTNODE AddHead(const Type &item);
        LISTNODE AddTail(const Type &item);

        LISTNODE GetHeadPosition();
        LISTNODE GetTailPosition();
        void RemoveAt(LISTNODE pNode);
        void RemoveAll();
        LISTNODE Find(const Type &item);
        int GetCount();
        Type GetNext(LISTNODE &pNode);
        Type GetAt(LISTNODE pNode);
        LISTNODE AddSorted(const Type &item, LPVOID pfn);

    public:
        DWORD _dwSig;

    private:
        ListNode<Type> *_pHead;
        ListNode<Type> *_pTail;
        int             _iCount;
    };

    //
    // ListNode Implementation
    //

    template <class Type> ListNode<Type>::ListNode(Type item)
        : _type(item)
        , _pNext(NULL)
        , _pPrev(NULL)
    {
    }

    template <class Type> ListNode<Type>::~ListNode()
    {
    }

    template <class Type> void ListNode<Type>::SetNext(ListNode *pNode)
    {
        _pNext = pNode;
    }

    template <class Type> void ListNode<Type>::SetPrev(ListNode *pNode)
    {
        _pPrev = pNode;
    }

    template <class Type> Type ListNode<Type>::GetItem()
    {
        return _type;
    }

    template <class Type> ListNode<Type> *ListNode<Type>::GetNext()
    {
        return _pNext;
    }

    template <class Type> ListNode<Type> *ListNode<Type>::GetPrev()
    {
        return _pPrev;
    }


    //
    // List Implementation
    //

    template <class Type> List<Type>::List()
        : _pHead(NULL)
        , _pTail(NULL)
        , _iCount(0)
    {
        _dwSig = 0x5453494c; /* 'TSIL' */
    }

    template <class Type> List<Type>::~List()
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            RemoveAll();
        }
        EX_CATCH_HRESULT(hr);
    }

    template <class Type> LISTNODE List<Type>::AddHead(const Type &item)
    {
        ListNode<Type>                   *pNode = NULL;

        NEW_CONSTR(pNode, ListNode<Type>(item));
        if (pNode) {
            _iCount++;
            pNode->SetNext(_pHead);
            pNode->SetPrev(NULL);
            if (_pHead == NULL) {
                _pTail = pNode;
            }
            else {
                _pHead->SetPrev(pNode);
            }
            _pHead = pNode;
        }
        
        return (LISTNODE)pNode;
    }

    template <class Type> LISTNODE List<Type>::AddSorted(const Type &item, 
                                                         LPVOID pfn)
    {
        ListNode<Type>           *pNode = NULL;
        LISTNODE           pCurrNode = NULL;
        LISTNODE           pPrevNode = NULL;
        int                      i;
        Type                     curItem;
        
        LONG (*pFN) (const Type item1, const Type item2);
        
        pFN = (LONG (*) (const Type item1, const Type item2))pfn;
        
        if(_pHead == NULL) {
            return AddHead(item);
        }
        else {
            pCurrNode = GetHeadPosition();
            curItem = ((ListNode<Type> *) pCurrNode)->GetItem();
            for (i = 0; i < _iCount; i++) {
                if (pFN(item, curItem) < 1) {
                    NEW_CONSTR(pNode, ListNode<Type>(item));
                    if (pNode) {
                        pNode->SetPrev((ListNode<Type> *)pPrevNode);
                        pNode->SetNext((ListNode<Type> *)pCurrNode);
                        // update pPrevNode
                        if(pPrevNode) {
                            ((ListNode<Type> *)pPrevNode)->SetNext(pNode);
                        }
                        else {
                            _pHead = pNode;
                        }
                        // update pCurrNode
                        ((ListNode<Type> *)pCurrNode)->SetPrev(pNode);
                        _iCount++;
                    }
                    break;
                }
                pPrevNode = pCurrNode;
                GetNext(pCurrNode);
                if(i+1 == _iCount) {
                    return AddTail(item);
                }
                else {
                    _ASSERTE(pCurrNode);
                    curItem = GetAt(pCurrNode);
                }
            }
        }
        
        return (LISTNODE)pNode;
    }

    template <class Type> LISTNODE List<Type>::AddTail(const Type &item)
    {
        ListNode<Type>                   *pNode = NULL;
    
        NEW_CONSTR(pNode, ListNode<Type>(item));
        if (pNode) {
            _iCount++;
            if (_pTail) {
                pNode->SetPrev(_pTail);
                _pTail->SetNext(pNode);
                _pTail = pNode;
            }
            else {
                _pHead = _pTail = pNode;
            }
        }

        return (LISTNODE)pNode;
    }

    template <class Type> int List<Type>::GetCount()
    {
        return _iCount;
    }

    template <class Type> LISTNODE List<Type>::GetHeadPosition()
    {
        return (LISTNODE)_pHead;
    }

    template <class Type> LISTNODE List<Type>::GetTailPosition()
    {
        return (LISTNODE)_pTail;
    }

    template <class Type> Type List<Type>::GetNext(LISTNODE &pNode)
    {
        ListNode<Type> *pListNode = (ListNode<Type> *)pNode;

        // Faults if you pass NULL
        _ASSERTE(pNode);

        Type item = pListNode->GetItem();
        pNode = (LISTNODE)(pListNode->GetNext());

        return item;
    }

    template <class Type> void List<Type>::RemoveAll()
    {
        int                        i;
        LISTNODE                   listNode = NULL;
        ListNode<Type>            *pDelNode = NULL;

        listNode = GetHeadPosition();

        for (i = 0; i < _iCount; i++) {
            pDelNode = (ListNode<Type> *)listNode;
            GetNext(listNode);
            SAFE_DELETE(pDelNode);
        }
    
        _iCount = 0;
        _pHead = NULL;
        _pTail = NULL;
    }

    template <class Type> void List<Type>::RemoveAt(LISTNODE pNode)
    {
        ListNode<Type>           *pListNode = (ListNode<Type> *)pNode;
        ListNode<Type>           *pPrevNode = NULL;
        ListNode<Type>           *pNextNode = NULL;

        if (pNode) {
            pPrevNode = pListNode->GetPrev();
            pNextNode = pListNode->GetNext();

            if (pPrevNode) {
                pPrevNode->SetNext(pNextNode);
                if (pNextNode) {
                    pNextNode->SetPrev(pPrevNode);
                }
                else {
                    // We're removing the last node, so we have a new tail
                    _pTail = pPrevNode;
                }
                SAFE_DELETE(pListNode);
            }
            else {
                // No previous, so we are the head of the list
                _pHead = pNextNode;
                if (pNextNode) {
                    pNextNode->SetPrev(NULL);
                }
                else {
                    // No previous, or next. There was only one node.
                    _pHead = NULL;
                    _pTail = NULL;
                }
                SAFE_DELETE(pListNode);
            }

            _iCount--;
        }
    }
        

    template <class Type> LISTNODE List<Type>::Find(const Type &item)
    {
        int                      i;
        Type                     curItem;
        LISTNODE                 pNode = NULL;
        LISTNODE                 pMatchNode = NULL;
        ListNode<Type> *         pListNode = NULL;

        pNode = GetHeadPosition();
        for (i = 0; i < _iCount; i++) {
            pListNode = (ListNode<Type> *)pNode;
            curItem = GetNext(pNode);
            if (curItem == item) {
                pMatchNode = (LISTNODE)pListNode;
                break;
            }
        }

        return pMatchNode;
    }

    template <class Type> Type List<Type>::GetAt(LISTNODE pNode)
    {
        ListNode<Type>                *pListNode = (ListNode<Type> *)pNode;

        // Faults if you pass NULL
        _ASSERTE(pNode);

        return pListNode->GetItem();
    }
};

#endif
