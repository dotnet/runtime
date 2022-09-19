// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GENERIC_TREE_H__
#define __GENERIC_TREE_H__

#include <windows.h>

// Partially balanced binary tree
// it does individual rotations on insertion, but does nto allow deletion.
// thus the worst case depth is not (n), but (n/2)
// Generic parameter is the element type
// Find and Add require a method that compares 2 elements
template <typename _E>
struct tree
{
    _E name;
    tree<_E> *lChild;
    tree<_E> *rChild;
    size_t lDepth;
    size_t rDepth;

    tree(_E e)
    {
        name = e;
        lChild = rChild = NULL;
        lDepth = rDepth = 0;
    }
    ~tree()
    {
        Cleanup();
    }

    bool InOrderWalk( bool (WalkFunc)(_E))
    {
        if (lChild != NULL && !lChild->InOrderWalk(WalkFunc))
            return false;
        if (!WalkFunc(name))
            return false;
        if (rChild != NULL)
            return rChild->InOrderWalk(WalkFunc);
        return true;
    }

    /*
     * return the depths of the tree from here down (minimum of 1)
     */
    size_t MaxDepth()
    {
        return lDepth > rDepth ? lDepth + 1 : rDepth + 1;
    }

    /*
     * Search the binary tree for the given string
     * return a pointer to it was added or NULL if it
     * doesn't exist
     */
    _E * Find(_E SearchVal, int (__cdecl CompFunc)(_E, _E))
    {
        int cmp = CompFunc(name, SearchVal);
        if (cmp < 0)
        {
            if (lChild == NULL)
                return NULL;
            else
                return lChild->Find(SearchVal, CompFunc);
        }
        else if (cmp > 0)
        {
            if (rChild == NULL)
                return NULL;
            else
                return rChild->Find(SearchVal, CompFunc);
        }
        else
            return &name;
    }

    /*
     * Search the binary tree and add the given string
     * return S_OK if it was added or S_FALSE if it already
     * exists (or E_OUTOFMEMORY)
     */
    HRESULT Add(_E add
                    , int (__cdecl CompFunc)(_E, _E))
    {
        int cmp = CompFunc(name, add
                              );
REDO:
        if (cmp == 0)
            return S_FALSE;

        if (cmp < 0)
        {
            if (lChild == NULL)
            {
                lDepth = 1;
                lChild = new tree<_E>(add
                                     );
                if (lChild == NULL)
                    return E_OUTOFMEMORY;
                return S_OK;
            }
            else if (rDepth < lDepth)
            {
                tree<_E> *temp = new tree<_E>(name);
                if (temp == NULL)
                    return E_OUTOFMEMORY;
                temp->rChild = rChild;
                temp->rDepth = rDepth;
                if (lChild != NULL &&
                    (cmp = CompFunc(lChild->name, add
                                       )) > 0)
                    {
                        // push right
                        temp->lChild = NULL;
                        temp->lDepth = 0;
                        name = add
                                   ;
                        rChild = temp;
                        rDepth++;
                        return S_OK;
                    }
                else if (cmp == 0)
                {
                    temp->rChild = NULL;
                    delete temp;
                    return S_FALSE;
                }
                else
                {
                    // Rotate right
                    temp->lChild = lChild->rChild;
                    temp->lDepth = lChild->rDepth;
                    name = lChild->name;
                    lDepth = lChild->lDepth;
                    rDepth = temp->MaxDepth();
                    rChild = temp;
                    temp = lChild->lChild;
                    lChild->lChild = lChild->rChild = NULL;
                    delete lChild;
                    lChild = temp;
                    goto REDO;
                }
            }
            else
            {
                HRESULT hr = lChild->Add(add
                                         , CompFunc);
                lDepth = lChild->MaxDepth();
                return hr;
            }
        }
        else
        {
            if (rChild == NULL)
            {
                rDepth = 1;
                rChild = new tree<_E>(add
                                     );
                if (rChild == NULL)
                    return E_OUTOFMEMORY;
                return S_OK;
            }
            else if (lDepth < rDepth)
            {
                tree<_E> *temp = new tree<_E>(name);
                if (temp == NULL)
                    return E_OUTOFMEMORY;
                temp->lChild = lChild;
                temp->lDepth = lDepth;
                if (rChild != NULL &&
                    (cmp = CompFunc(rChild->name, add
                                       )) < 0)
                    {
                        // push left
                        temp->rChild = NULL;
                        temp->rDepth = 0;
                        name = add
                                   ;
                        lChild = temp;
                        lDepth++;
                        return S_OK;
                    }
                else if (cmp == 0)
                {
                    temp->lChild = NULL;
                    delete temp;
                    return S_FALSE;
                }
                else
                {
                    // Rotate left
                    temp->rChild = rChild->lChild;
                    temp->rDepth = rChild->lDepth;
                    name = rChild->name;
                    rDepth = rChild->rDepth;
                    lDepth = temp->MaxDepth();
                    lChild = temp;
                    temp = rChild->rChild;
                    rChild->rChild = rChild->lChild = NULL;
                    delete rChild;
                    rChild = temp;
                    goto REDO;
                }
            }
            else
            {
                HRESULT hr = rChild->Add(add
                                         , CompFunc);
                rDepth = rChild->MaxDepth();
                return hr;
            }
        }
    }

    /*
     * Free the memory allocated by the tree (recursive)
     */
    void Cleanup()
    {
        if (lChild != NULL)
        {
            lChild->Cleanup();
            delete lChild;
            lChild = NULL;
        }
        if (rChild != NULL)
        {
            rChild->Cleanup();
            delete rChild;
            rChild = NULL;

        }
    }

};

#endif // __GENERIC_TREE_H__
